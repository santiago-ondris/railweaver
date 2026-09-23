import type { Coordinate } from '../shared/geo'

export type CorridorSearch = {
  gridStepMeters: number
  gridColumns: number
  gridRows: number
  exploredNodes: number
  bounds: { west: number; south: number; east: number; north: number }
}
export type TrackProfilePoint = Coordinate & {
  distanceMeters: number
  elevationMeters: number
  gradientPermille: number | null
}
export type CorridorMetrics = {
  lengthMeters: number
  straightLineDistanceMeters: number
  sinuosity: number
  maxGradientPermille: number
  maxGradientLimitPermille: number
  ascentMeters: number
  descentMeters: number
  minElevationMeters: number
  maxElevationMeters: number
  distanceByGradientBand: Array<{
    fromPercentOfLimit: number
    toPercentOfLimit: number
    meters: number
  }>
  maxCutMeters: number
  maxFillMeters: number
}
export type CandidateCorridor = {
  alignment: Coordinate[]
  trackProfile: TrackProfilePoint[]
  terrainProfile: {
    totalDistanceMeters: number
    samples: Array<{
      coordinate: Coordinate
      distanceMeters: number
      elevationMeters: number | null
      gradientPermille: number | null
    }>
  }
  metrics: CorridorMetrics
}
export type CorridorResponse = {
  status: 'found' | 'no_feasible_path' | 'endpoint_without_elevation'
  search: CorridorSearch
  corridor: CandidateCorridor | null
}

export async function fetchCorridor(
  regionId: string,
  origin: Coordinate,
  destination: Coordinate,
  maxGradientPermille: number,
  signal: AbortSignal,
): Promise<CorridorResponse> {
  const response = await fetch(`/api/regions/${encodeURIComponent(regionId)}/corridors`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    signal,
    body: JSON.stringify({ origin, destination, maxGradientPermille }),
  })
  if (!response.ok) {
    const body = (await response.json().catch(() => null)) as { message?: string } | null
    throw new Error(
      body?.message ??
        `No se pudo calcular el corredor (${response.status}). Ejecutá python3 tools/fetch-elevation.py si falta el relieve.`,
    )
  }
  return response.json() as Promise<CorridorResponse>
}
