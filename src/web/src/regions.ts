export type Region = {
  id: string
  name: string
  boundingBox: {
    west: number
    south: number
    east: number
    north: number
  }
  initialCamera: {
    latitude: number
    longitude: number
    heightMeters: number
  }
  source: {
    name: string
    url: string
    accessedOn: string
    license: string
  }
}

export async function fetchRegion(id: string, signal: AbortSignal): Promise<Region> {
  const response = await fetch(`/api/regions/${encodeURIComponent(id)}`, { signal })

  if (!response.ok) {
    throw new Error(`Region request failed with status ${response.status}.`)
  }

  return response.json() as Promise<Region>
}
