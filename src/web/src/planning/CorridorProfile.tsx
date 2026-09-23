import type { CandidateCorridor } from './api'
import { formatNumber } from './format'
import './CorridorProfile.css'

const width = 720
const height = 130
const pad = 22

export function CorridorProfile({ corridor }: { corridor: CandidateCorridor }) {
  const samples = corridor.terrainProfile.samples
  const track = corridor.trackProfile
  const values = [
    ...samples.flatMap((sample) =>
      sample.elevationMeters === null ? [] : [sample.elevationMeters],
    ),
    ...track.map((point) => point.elevationMeters),
  ]
  const min = Math.min(...values)
  const max = Math.max(...values)
  const x = (distanceMeters: number) =>
    pad + (distanceMeters / corridor.terrainProfile.totalDistanceMeters) * (width - pad * 2)
  const y = (elevation: number) =>
    height - pad - ((elevation - min) / Math.max(1, max - min)) * (height - pad * 2)

  // Terrain gaps (no elevation data) split the terrain line and its filled area into parts.
  const terrainPaths: string[] = []
  const terrainAreas: string[] = []
  let path = ''
  let firstX = 0
  let lastX = 0
  const closePart = () => {
    if (!path) return
    terrainPaths.push(path)
    terrainAreas.push(`${path} L ${lastX} ${height - pad} L ${firstX} ${height - pad} Z`)
    path = ''
  }
  for (const sample of samples) {
    if (sample.elevationMeters === null) {
      closePart()
      continue
    }
    if (!path) firstX = x(sample.distanceMeters)
    lastX = x(sample.distanceMeters)
    path += `${path ? ' L' : 'M'} ${x(sample.distanceMeters)} ${y(sample.elevationMeters)}`
  }
  closePart()
  const trackPath = track
    .map(
      (point, index) =>
        `${index ? 'L' : 'M'} ${x(point.distanceMeters)} ${y(point.elevationMeters)}`,
    )
    .join(' ')
  const hasGaps = samples.some((sample) => sample.elevationMeters === null)

  return (
    <section className="analysis-dock" aria-label="Perfil longitudinal del corredor">
      <div>
        <p className="label">Perfil longitudinal</p>
        <p className="data">
          Máx. {formatNumber(corridor.metrics.maxGradientPermille)} ‰ ≤ límite{' '}
          {formatNumber(corridor.metrics.maxGradientLimitPermille)} ‰
        </p>
        {hasGaps && <p className="detail-hint">Hay tramos de terreno sin dato.</p>}
      </div>
      <svg
        className="profile-chart corridor-chart"
        viewBox={`0 0 ${width} ${height}`}
        role="img"
        aria-label="Perfil del terreno y de la línea de vía"
      >
        <line x1={pad} y1={height - pad} x2={width - pad} y2={height - pad} />
        {terrainAreas.map((part, index) => (
          <path key={index} className="corridor-terrain-area" d={part} />
        ))}
        {terrainPaths.map((part, index) => (
          <path key={index} className="corridor-terrain" d={part} />
        ))}
        <path className="corridor-track" d={trackPath} />
        {hasGaps && (
          <text x={width / 2} y={height / 2}>
            Tramo sin dato
          </text>
        )}
      </svg>
    </section>
  )
}
