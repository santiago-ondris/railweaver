import { useMemo } from 'react'
import type { ElevationProfile } from './api'
import { profileMetrics } from './profileMetrics'
import './ProfileDock.css'

const width = 720
const height = 130
const padding = 22

export function ProfileDock({
  profile,
  error,
}: {
  profile: ElevationProfile | null
  error: string | null
}) {
  const metrics = useMemo(() => profileMetrics(profile), [profile])
  if (!profile || !metrics)
    return (
      <section className="analysis-dock">
        <p>{error ?? 'El perfil no contiene cotas disponibles.'}</p>
      </section>
    )
  const points = profile.samples.map((sample) =>
    sample.elevationMeters === null
      ? null
      : {
          x:
            padding + (sample.distanceMeters / profile.totalDistanceMeters) * (width - padding * 2),
          y:
            height -
            padding -
            ((sample.elevationMeters - metrics.min) / Math.max(1, metrics.max - metrics.min)) *
              (height - padding * 2),
        },
  )
  // Samples without elevation split the line into separate paths.
  const paths: string[] = []
  let current = ''
  for (const point of points) {
    if (!point) {
      if (current) paths.push(current)
      current = ''
      continue
    }
    current += `${current ? ' L' : 'M'} ${point.x} ${point.y}`
  }
  if (current) paths.push(current)
  return (
    <section className="analysis-dock" aria-label="Perfil longitudinal">
      <div>
        <p className="label">Perfil longitudinal</p>
        <div className="profile-metrics">
          <span>
            Distancia <b>{formatDistance(profile.totalDistanceMeters)}</b>
          </span>
          <span>
            Cota mín. <b>{metrics.min.toLocaleString('es-AR')} m</b>
          </span>
          <span>
            Cota máx. <b>{metrics.max.toLocaleString('es-AR')} m</b>
          </span>
          <span>
            Subida <b>{metrics.up.toLocaleString('es-AR')} m</b>
          </span>
          <span>
            Bajada <b>{metrics.down.toLocaleString('es-AR')} m</b>
          </span>
          <span>
            Pendiente máx. <b>{metrics.gradient.toLocaleString('es-AR')} ‰</b>
          </span>
        </div>
      </div>
      <svg
        className="profile-chart"
        viewBox={`0 0 ${width} ${height}`}
        role="img"
        aria-label="Gráfico del perfil de elevación"
      >
        <line x1={padding} y1={height - padding} x2={width - padding} y2={height - padding} />
        {paths.map((path, index) => (
          <path key={index} d={path} />
        ))}
        {points.some((point) => point === null) && (
          <text x={width / 2} y={height / 2}>
            Tramo sin dato
          </text>
        )}
      </svg>
    </section>
  )
}

function formatDistance(meters: number) {
  return meters >= 1000
    ? `${(meters / 1000).toLocaleString('es-AR', { maximumFractionDigits: 1 })} km`
    : `${Math.round(meters)} m`
}
