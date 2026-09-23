import { Cartesian3, type Entity, type Viewer } from 'cesium'
import { useCallback, useEffect, useRef, useState, type RefObject } from 'react'
import type { Coordinate } from '../shared/geo'
import { tokenColor } from '../styles/tokens'
import { fetchCorridor, type CorridorResponse } from './api'
import { CandidateCorridorLayer } from './CandidateCorridorLayer'

type Endpoints = { origin: Coordinate | null; destination: Coordinate | null }
const noEndpoints: Endpoints = { origin: null, destination: null }

/**
 * Candidate corridor tool: the user marks origin and destination, picks a maximum gradient
 * and the API searches a corridor. Owns the endpoint markers and the corridor layer on the
 * map. Map clicks and keys reach it through the viewer, which reads the mutable refs so
 * Cesium handlers always see the current tool state.
 */
export function useCorridorTool(viewerRef: RefObject<Viewer | null>, regionId: string) {
  const activeRef = useRef(false)
  const endpointsRef = useRef<Endpoints>(noEndpoints)
  const requestRef = useRef<AbortController | null>(null)
  const markersRef = useRef<Entity[]>([])
  const layerRef = useRef<CandidateCorridorLayer | null>(null)
  const serialRef = useRef(0)
  const [active, setActive] = useState(false)
  const [origin, setOrigin] = useState<Coordinate | null>(null)
  const [destination, setDestination] = useState<Coordinate | null>(null)
  const [gradient, setGradient] = useState('15')
  const [gauge, setGauge] = useState<1000 | 1676>(1676)
  const [designSpeed, setDesignSpeed] = useState('80')
  const [busy, setBusy] = useState(false)
  const [searchStartedAt, setSearchStartedAt] = useState<number | null>(null)
  /** Last search that found a corridor: the corridor shown on the map. */
  const [result, setResult] = useState<CorridorResponse | null>(null)
  /** Last search that did not find one, reported inside the tool. */
  const [searchResult, setSearchResult] = useState<CorridorResponse | null>(null)
  /** Whether the detail panel and profile show the corridor instead of the inspection. */
  const [shown, setShown] = useState(false)
  const [number, setNumber] = useState(0)
  const [error, setError] = useState<string | null>(null)

  const updateGradient = (value: string) => {
    setGradient(value)
    setSearchResult(null)
    setError(null)
  }
  const updateGauge = (value: 1000 | 1676) => {
    setGauge(value)
    setSearchResult(null)
    setError(null)
  }
  const updateDesignSpeed = (value: string) => {
    setDesignSpeed(value)
    setSearchResult(null)
    setError(null)
  }

  useEffect(() => {
    const viewer = viewerRef.current
    if (!viewer) return
    for (const entity of markersRef.current) viewer.entities.remove(entity)
    markersRef.current = []
    const rootStyle = getComputedStyle(document.documentElement)
    const font = `${rootStyle.getPropertyValue('--rw-text-body-size')} ${rootStyle.getPropertyValue('--rw-font-ui')}`
    for (const [name, point] of [
      ['Origen', origin],
      ['Destino', destination],
    ] as const) {
      if (!point) continue
      markersRef.current.push(
        viewer.entities.add({
          name,
          position: Cartesian3.fromDegrees(point.longitude, point.latitude),
          point: {
            pixelSize: 10,
            color: tokenColor('primary'),
            outlineColor: tokenColor('map-ground'),
            outlineWidth: 2,
          },
          label: {
            text: name,
            font,
            fillColor: tokenColor('primary'),
            showBackground: true,
            backgroundColor: tokenColor('map-ground'),
          },
        }),
      )
    }
    viewer.scene.requestRender()
  }, [viewerRef, origin, destination])

  useEffect(() => {
    const viewer = viewerRef.current
    if (!viewer) return
    layerRef.current?.destroy()
    layerRef.current = null
    const alignment = result?.corridor?.alignment
    if (alignment && result?.corridor)
      layerRef.current = new CandidateCorridorLayer(viewer, result.corridor)
    viewer.scene.requestRender()
  }, [viewerRef, result])

  const clearEndpoints = () => {
    endpointsRef.current = noEndpoints
    setOrigin(null)
    setDestination(null)
  }

  const begin = () => {
    activeRef.current = true
    setActive(true)
    setShown(false)
    clearEndpoints()
    setError(null)
    setSearchResult(null)
  }

  const abortSearch = () => {
    requestRef.current?.abort()
    requestRef.current = null
    setBusy(false)
  }

  const dismissFeedback = () => {
    setSearchResult(null)
    setError(null)
  }

  /** Leaves the tool, aborting a running search; a corridor already found stays. */
  const cancel = () => {
    abortSearch()
    activeRef.current = false
    setActive(false)
    clearEndpoints()
    setError(null)
    setSearchResult(null)
  }

  /** The first click sets the origin, the second the destination; a third starts over. */
  const placeEndpoint = (coordinate: Coordinate) => {
    if (requestRef.current) return
    const current = endpointsRef.current
    const next =
      current.destination || !current.origin
        ? { origin: coordinate, destination: null }
        : { ...current, destination: coordinate }
    endpointsRef.current = next
    setOrigin(next.origin)
    setDestination(next.destination)
    setError(null)
    setSearchResult(null)
  }

  const generate = async () => {
    if (!origin || !destination || requestRef.current) return
    const controller = new AbortController()
    requestRef.current = controller
    setSearchStartedAt(Date.now())
    setBusy(true)
    setError(null)
    setSearchResult(null)
    try {
      const response = await fetchCorridor(
        regionId,
        origin,
        destination,
        Number(gradient),
        gauge,
        Number(designSpeed),
        controller.signal,
      )
      if (controller.signal.aborted) return
      if (response.status === 'found') {
        setResult(response)
        serialRef.current++
        setNumber(serialRef.current)
        setShown(true)
        setActive(false)
        activeRef.current = false
      } else {
        setSearchResult(response)
      }
    } catch (error) {
      if (!controller.signal.aborted)
        setError(error instanceof Error ? error.message : 'Error de red.')
    } finally {
      if (requestRef.current === controller) {
        requestRef.current = null
        setBusy(false)
      }
    }
  }

  /** Removes the corridor found from the map and the panels. */
  const remove = () => {
    setResult(null)
    setShown(false)
    clearEndpoints()
  }

  const isPicked = (id: unknown) =>
    !!layerRef.current?.isPicked(id) || markersRef.current.includes(id as Entity)

  /** Releases what lives on a viewer that is about to be destroyed. */
  const detach = useCallback(() => {
    requestRef.current?.abort()
    layerRef.current?.destroy()
    layerRef.current = null
  }, [])

  return {
    active,
    origin,
    destination,
    gradient,
    setGradient: updateGradient,
    gauge,
    setGauge: updateGauge,
    designSpeed,
    setDesignSpeed: updateDesignSpeed,
    busy,
    searchStartedAt,
    result,
    searchResult,
    shown,
    number,
    error,
    isActive: () => activeRef.current,
    begin,
    cancel,
    abortSearch,
    dismissFeedback,
    placeEndpoint,
    generate,
    remove,
    isPicked,
    show: () => setShown(true),
    hide: () => setShown(false),
    detach,
  }
}
