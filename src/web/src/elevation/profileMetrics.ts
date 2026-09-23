import type { ElevationProfile } from './api'

export type ProfileMetrics = {
  min: number
  max: number
  up: number
  down: number
  gradient: number
}

/** Summary of a sampled profile; null when no sample has an elevation. */
export function profileMetrics(profile: ElevationProfile | null): ProfileMetrics | null {
  if (!profile) return null
  const elevations = profile.samples.flatMap((sample) =>
    sample.elevationMeters === null ? [] : [sample.elevationMeters],
  )
  if (elevations.length === 0) return null
  let up = 0
  let down = 0
  for (let index = 1; index < profile.samples.length; index++) {
    const a = profile.samples[index - 1].elevationMeters
    const b = profile.samples[index].elevationMeters
    if (a === null || b === null) continue
    if (b > a) up += b - a
    else down += a - b
  }
  return {
    min: Math.min(...elevations),
    max: Math.max(...elevations),
    up: Math.round(up * 10) / 10,
    down: Math.round(down * 10) / 10,
    gradient: Math.max(...profile.samples.map((sample) => Math.abs(sample.gradientPermille ?? 0))),
  }
}
