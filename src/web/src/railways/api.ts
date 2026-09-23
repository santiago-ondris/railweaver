export type TrackGauge = {
  widthMillimetres: number
  kind: 'Unknown' | 'Metre' | 'Standard' | 'Broad'
}

export type RailwayCoordinate = {
  latitude: number
  longitude: number
}

export type TrackSegment = {
  id: string
  geometry: RailwayCoordinate[]
  gauge: TrackGauge
  gaugeInferred: boolean
  status: 'Active' | 'Disused' | 'Abandoned'
  usage: 'Unknown' | 'MainLine' | 'BranchLine' | 'Siding' | 'Yard' | 'IndustrialSpur'
  name: string | null
  lineReference: string | null
}

export type RailwayStation = {
  id: string
  name: string
  location: RailwayCoordinate
  type: 'Station' | 'Halt' | 'Junction'
  gauge: TrackGauge
  gaugeInferred: boolean
}

export type Railway = {
  source: {
    extractedOn: string
    osmDataTimestamp: string
    attribution: string
    license: string
    licenseUrl: string
  }
  tracks: TrackSegment[]
  stations: RailwayStation[]
}

export async function fetchRailway(id: string, signal: AbortSignal): Promise<Railway> {
  const response = await fetch(`/api/regions/${encodeURIComponent(id)}/railway`, { signal })

  if (!response.ok) {
    throw new Error(`Railway request failed with status ${response.status}.`)
  }

  return response.json() as Promise<Railway>
}
