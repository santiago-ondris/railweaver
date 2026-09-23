import { useCallback, useEffect, useRef, useState } from 'react'
import type { RailwayStation } from '../railways/api'
import { fetchNetwork, fetchRoute, type NetworkData, type RouteResponse } from './api'

export function useNetworkTool(regionId: string) {
  const [network, setNetwork] = useState<NetworkData | null>(null)
  const [networkError, setNetworkError] = useState(false)
  const [active, setActive] = useState(false)
  const activeRef = useRef(false)
  const [origin, setOrigin] = useState<RailwayStation | null>(null)
  const [destination, setDestination] = useState<RailwayStation | null>(null)
  const endpointsRef = useRef<{
    origin: RailwayStation | null
    destination: RailwayStation | null
  }>({
    origin: null,
    destination: null,
  })
  const [includeDisused, setIncludeDisused] = useState(false)
  const [busy, setBusy] = useState(false)
  const [result, setResult] = useState<RouteResponse | null>(null)
  const [unreachable, setUnreachable] = useState<RouteResponse | null>(null)
  const [shown, setShown] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [number, setNumber] = useState(0)
  const requestRef = useRef<AbortController | null>(null)
  const serialRef = useRef(0)

  useEffect(() => {
    const controller = new AbortController()
    void fetchNetwork(regionId, controller.signal)
      .then(setNetwork)
      .catch((error: unknown) => {
        if (!(error instanceof DOMException && error.name === 'AbortError')) setNetworkError(true)
      })
    return () => controller.abort()
  }, [regionId])

  const selectOrigin = (station: RailwayStation | null) => {
    endpointsRef.current.origin = station
    setOrigin(station)
    setUnreachable(null)
    setError(null)
  }
  const selectDestination = (station: RailwayStation | null) => {
    endpointsRef.current.destination = station
    setDestination(station)
    setUnreachable(null)
    setError(null)
  }
  const begin = () => {
    activeRef.current = true
    setActive(true)
    setShown(false)
    setIncludeDisused(false)
    selectOrigin(null)
    selectDestination(null)
  }
  const cancel = () => {
    requestRef.current?.abort()
    requestRef.current = null
    activeRef.current = false
    setActive(false)
    setBusy(false)
    selectOrigin(null)
    selectDestination(null)
  }
  const placeStation = (station: RailwayStation) => {
    if (requestRef.current) return
    const endpoints = endpointsRef.current
    if (!endpoints.origin || endpoints.destination) {
      selectOrigin(station)
      selectDestination(null)
    } else selectDestination(station)
  }
  const search = async (disused = includeDisused) => {
    if (!origin || !destination || origin.id === destination.id) return
    const controller = new AbortController()
    requestRef.current = controller
    setBusy(true)
    setError(null)
    setUnreachable(null)
    try {
      const response = await fetchRoute(
        regionId,
        origin.id,
        destination.id,
        disused,
        controller.signal,
      )
      if (controller.signal.aborted) return
      if (response.status === 'found') {
        setResult(response)
        serialRef.current++
        setNumber(serialRef.current)
        setShown(true)
        activeRef.current = false
        setActive(false)
      } else setUnreachable(response)
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
  const remove = () => {
    setResult(null)
    setShown(false)
  }
  const detach = useCallback(() => requestRef.current?.abort(), [])
  return {
    network,
    networkError,
    active,
    origin,
    destination,
    includeDisused,
    setIncludeDisused,
    busy,
    result,
    unreachable,
    shown,
    error,
    number,
    selectOrigin,
    selectDestination,
    placeStation,
    begin,
    cancel,
    search,
    remove,
    show: () => setShown(true),
    hide: () => setShown(false),
    isActive: () => activeRef.current,
    detach,
  }
}
