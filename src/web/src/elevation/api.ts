import type { Coordinate } from '../shared/geo'

export type ElevationResult = { elevation: number | null; reason: string | null }
export type ProfileSample = Coordinate & {
  distanceMeters: number
  elevationMeters: number | null
  gradientPermille: number | null
}
export type ElevationProfile = { totalDistanceMeters: number; samples: ProfileSample[] }

export async function fetchElevation(
  regionId: string,
  coordinate: Coordinate,
): Promise<ElevationResult> {
  const query = new URLSearchParams({
    lat: String(coordinate.latitude),
    lon: String(coordinate.longitude),
  })
  const response = await fetch(`/api/regions/${encodeURIComponent(regionId)}/elevation?${query}`)
  if (!response.ok) throw new Error(`Elevation request failed with status ${response.status}.`)
  return response.json() as Promise<ElevationResult>
}

export async function fetchElevationProfile(
  regionId: string,
  coordinates: Coordinate[],
): Promise<ElevationProfile> {
  const response = await fetch(`/api/regions/${encodeURIComponent(regionId)}/elevation/profile`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ coordinates }),
  })
  if (!response.ok)
    throw new Error(`Elevation profile request failed with status ${response.status}.`)
  const result = (await response.json()) as {
    totalDistanceMeters: number
    samples: Array<{
      distanceMeters: number
      coordinate: Coordinate
      elevationMeters: number | null
      gradientPermille: number | null
    }>
  }
  return {
    totalDistanceMeters: result.totalDistanceMeters,
    samples: result.samples.map((sample) => ({ ...sample.coordinate, ...sample })),
  }
}

export const terrainTileSize = 65

/** One heightmap tile (65×65 float32 heights), or null when the region has no terrain for it. */
export async function fetchTerrainTile(
  regionId: string,
  level: number,
  x: number,
  y: number,
): Promise<Float32Array | null> {
  const response = await fetch(
    `/api/regions/${encodeURIComponent(regionId)}/terrain/${level}/${x}/${y}`,
  )
  if (!response.ok) return null
  return new Float32Array(await response.arrayBuffer())
}
