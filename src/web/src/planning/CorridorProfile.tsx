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
          {formatNumber(corridor.metrics.maxGradientLimitPermille)} ‰ · reducido en curvas
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
      <svg
        className="corridor-curve-band"
        viewBox={`0 0 ${width} 44`}
        role="img"
        aria-label="Curvas a la derecha sobre la línea; curvas a la izquierda debajo"
      >
        <line className="corridor-curve-baseline" x1={pad} y1={20} x2={width - pad} y2={20} />
        {corridor.sections
          .filter((section) => section.kind === 'curve')
          .map((section, index) => {
            const from = x(section.fromMeters)
            const to = x(section.toMeters)
            const right = section.direction === 'right'
            const slow = section.speedLimitKmh < corridor.metrics.designSpeedKmh
            return (
              <g key={index} className={slow ? 'corridor-curve-slow' : 'corridor-curve-normal'}>
                <rect x={from} y={right ? 9 : 20} width={Math.max(0, to - from)} height={11} />
                {to - from >= 40 && (
                  <text className="data" x={(from + to) / 2} y={right ? 7 : 42} textAnchor="middle">
                    R {Math.round(section.radiusMeters ?? 0)}
                  </text>
                )}
              </g>
            )
          })}
      </svg>
      <p className="detail-hint">Arriba: curva a la derecha · Abajo: a la izquierda</p>
    </section>
  )
}
