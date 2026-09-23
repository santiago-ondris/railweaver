import type { RailwayCoordinate, TrackGauge } from '../railways/api'

export type NetworkProfile = {
  totalDistanceMeters: number
  samples: {
    distanceMeters: number
    coordinate: RailwayCoordinate
    elevationMeters: number | null
    gradientPermille: number | null
  }[]
}

export type NetworkDiagnostic = {
  kind:
    | 'region_boundary'
    | 'possible_data_gap'
    | 'end_of_track'
    | 'sharp_joint'
    | 'station_without_track'
    | 'track_without_gauge'
  gaugeMillimetres: number | null
  location: RailwayCoordinate
  trackId: string | null
  stationId: string | null
  distanceMeters: number | null
  deflectionDegrees: number | null
}

export type NetworkData = {
  networks: {
    gauge: TrackGauge
    nodes: number
    edges: number
    lengthMeters: number
    components: number
    largestComponentLengthMeters: number
  }[]
  inferredGauges: { trackId: string; widthMillimetres: number }[]
  diagnostics: NetworkDiagnostic[]
}

export type NetworkRoute = {
  gauge: TrackGauge
  geometry: RailwayCoordinate[]
  lengthMeters: number
  straightLineDistanceMeters: number
  reversals: { location: RailwayCoordinate; distanceAlongMeters: number }[]
  distanceByStatus: { active: number; disused: number }
  inferredGaugeDistanceMeters: number
  segments: {
    trackId: string
    name: string | null
    lineReference: string | null
    status: 'Active' | 'Disused'
    lengthMeters: number
  }[]
  originStop: { trackId: string; location: RailwayCoordinate }
  destinationStop: { trackId: string; location: RailwayCoordinate }
}

export type RouteResponse =
  | {
      status: 'found'
      includeDisused: boolean
      route: NetworkRoute
      profile: NetworkProfile | null
      profileUnavailableReason: 'elevation_unavailable' | 'too_many_samples' | null
    }
  | {
      status: 'unreachable'
      includeDisused: boolean
      reason:
        | 'origin_without_track'
        | 'destination_without_track'
        | 'no_common_gauge'
        | 'disconnected'
        | 'no_feasible_movement'
      originGauges: number[]
      destinationGauges: number[]
      reachableWithDisused: boolean | null
    }

export async function fetchNetwork(regionId: string, signal: AbortSignal): Promise<NetworkData> {
  const response = await fetch(`/api/regions/${encodeURIComponent(regionId)}/network`, { signal })
  if (!response.ok) throw new Error(`No se pudo cargar la red (${response.status}).`)
  return response.json() as Promise<NetworkData>
}

export async function fetchRoute(
  regionId: string,
  originStationId: string,
  destinationStationId: string,
  includeDisused: boolean,
  signal: AbortSignal,
): Promise<RouteResponse> {
  const response = await fetch(`/api/regions/${encodeURIComponent(regionId)}/network/routes`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ originStationId, destinationStationId, includeDisused }),
    signal,
  })
  if (!response.ok) {
    const body = (await response.json().catch(() => null)) as { message?: string } | null
    throw new Error(body?.message ?? `No se pudo buscar la ruta (${response.status}).`)
  }
  return response.json() as Promise<RouteResponse>
}
