import type { AlignmentSection } from '../planning/api'

export type Train = {
  name: string
  lengthMeters: number
  maxSpeedKmh: number
  accelerationMetersPerSecondSquared: number
  brakingMetersPerSecondSquared: number
}
export type TrainPreset = Train & { id: string }
export type RunningTime = {
  train: Train
  metrics: {
    totalSeconds: number
    runningSeconds: number
    dwellSeconds: number
    routeLengthMeters: number
    travelledMeters: number
    commercialSpeedKmh: number
    maxReachedSpeedKmh: number
    regimes: { regime: string; seconds: number; meters: number }[]
    costliestCurve: {
      fromMeters: number
      toMeters: number
      radiusMeters: number | null
      speedLimitKmh: number
      timeLostSeconds: number
    } | null
  }
  speedProfile: { distanceMeters: number; speedKmh: number; trackLimitKmh: number }[]
  reversals: {
    distanceMeters: number
    maneuverMeters: number
    maneuverTrackMeters: number | null
    maneuverFits: boolean | null
    dwellSeconds: number
  }[]
}

export async function fetchPresets(signal: AbortSignal): Promise<TrainPreset[]> {
  const response = await fetch('/api/rolling-stock/presets', { signal })
  if (!response.ok) throw new Error('No se pudieron cargar los tipos de tren.')
  return response.json() as Promise<TrainPreset[]>
}

export async function calculateCorridor(
  regionId: string,
  sections: AlignmentSection[],
  train: Train,
  signal: AbortSignal,
): Promise<RunningTime> {
  return post(
    `/api/regions/${encodeURIComponent(regionId)}/corridors/running-time`,
    {
      sections,
      train,
    },
    signal,
  )
}

export async function calculateRoute(
  regionId: string,
  originStationId: string,
  destinationStationId: string,
  includeDisused: boolean,
  lineSpeedKmh: number,
  reversalDwellMinutes: number,
  train: Train,
  signal: AbortSignal,
): Promise<RunningTime> {
  return post(
    `/api/regions/${encodeURIComponent(regionId)}/network/routes/running-time`,
    {
      originStationId,
      destinationStationId,
      includeDisused,
      lineSpeedKmh,
      reversalDwellMinutes,
      train,
    },
    signal,
  )
}

async function post(url: string, body: object, signal: AbortSignal): Promise<RunningTime> {
  const response = await fetch(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
    signal,
  })
  if (!response.ok) {
    const error = (await response.json().catch(() => null)) as { message?: string } | null
    throw new Error(error?.message ?? `No se pudo calcular el tiempo (${response.status}).`)
  }
  return response.json() as Promise<RunningTime>
}
