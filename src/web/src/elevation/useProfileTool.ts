import { Cartesian3, type Entity, type Viewer } from 'cesium'
import { useEffect, useRef, useState, type RefObject } from 'react'
import type { Coordinate } from '../shared/geo'
import { tokenColor } from '../styles/tokens'
import { fetchElevationProfile, type ElevationProfile } from './api'

/**
 * Longitudinal profile tool: the user marks vertices on the map and the API samples the
 * terrain along them. Map clicks and keys reach it through the viewer, which reads the
 * mutable refs so Cesium handlers always see the current tool state.
 */
export function useProfileTool(viewerRef: RefObject<Viewer | null>, regionId: string) {
  const activeRef = useRef(false)
  const verticesRef = useRef<Coordinate[]>([])
  const entityRef = useRef<Entity | null>(null)
  const [active, setActive] = useState(false)
  const [profile, setProfile] = useState<ElevationProfile | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    activeRef.current = active
  }, [active])

  const removeLine = () => {
    if (entityRef.current) viewerRef.current?.entities.remove(entityRef.current)
    entityRef.current = null
  }

  const redraw = () => {
    const viewer = viewerRef.current
    if (!viewer) return
    removeLine()
    if (verticesRef.current.length === 0) return
    entityRef.current = viewer.entities.add({
      polyline: {
        positions: Cartesian3.fromDegreesArray(
          verticesRef.current.flatMap((item) => [item.longitude, item.latitude]),
        ),
        clampToGround: true,
        material: tokenColor('primary'),
        width: 3,
      },
    })
  }

  /** Starts a new profile, discarding the previous one. */
  const begin = () => {
    verticesRef.current = []
    removeLine()
    setProfile(null)
    setError(null)
    setActive(true)
  }

  /** Leaves the tool and clears its line and result (another tool takes over). */
  const reset = () => {
    activeRef.current = false
    setActive(false)
    setProfile(null)
    setError(null)
    verticesRef.current = []
    removeLine()
  }

  /** Esc while drawing: drops the vertices and the result. */
  const cancel = () => {
    verticesRef.current = []
    redraw()
    setActive(false)
    setProfile(null)
  }

  const addVertex = (coordinate: Coordinate) => {
    const last = verticesRef.current.at(-1)
    // A double click also fires two single clicks on the same spot; keep one vertex.
    if (
      last &&
      Math.abs(last.latitude - coordinate.latitude) <= 1e-8 &&
      Math.abs(last.longitude - coordinate.longitude) <= 1e-8
    )
      return
    verticesRef.current.push(coordinate)
    redraw()
  }

  const finish = async () => {
    if (verticesRef.current.length < 2) return
    setActive(false)
    setError(null)
    try {
      setProfile(await fetchElevationProfile(regionId, verticesRef.current))
    } catch {
      setError(
        'No se pudo calcular el perfil. Verificá que el dataset de elevación esté disponible.',
      )
    }
  }

  return {
    active,
    profile,
    error,
    isActive: () => activeRef.current,
    begin,
    reset,
    cancel,
    addVertex,
    finish,
  }
}
