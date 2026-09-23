import type { RouteResponse } from './api'

const width = 720
const height = 130
const padding = 24

export function NetworkProfile({
  response,
}: {
  response: Extract<RouteResponse, { status: 'found' }>
}) {
  const profile = response.profile
  if (!profile || !profile.samples.some((sample) => sample.elevationMeters !== null))
    return (
      <section className="analysis-dock" aria-label="Terreno bajo la ruta">
        <div>
          <p className="label">Terreno bajo la ruta</p>
          <p>
            {response.profileUnavailableReason === 'too_many_samples'
              ? 'Perfil no disponible: la ruta es demasiado larga.'
              : 'Perfil no disponible: falta el dataset de relieve.'}
          </p>
        </div>
      </section>
    )
  const elevations = profile.samples.flatMap((sample) =>
    sample.elevationMeters === null ? [] : [sample.elevationMeters],
  )
  const min = Math.min(...elevations)
  const max = Math.max(...elevations)
  const x = (distance: number) =>
    padding + (distance / profile.totalDistanceMeters) * (width - padding * 2)
  const y = (elevation: number) =>
    height - padding - ((elevation - min) / Math.max(1, max - min)) * (height - padding * 2)
  const segments: { x: number; y: number }[][] = []
  let segment: { x: number; y: number }[] = []
  for (const sample of profile.samples) {
    if (sample.elevationMeters === null) {
      if (segment.length) segments.push(segment)
      segment = []
    } else segment.push({ x: x(sample.distanceMeters), y: y(sample.elevationMeters) })
  }
  if (segment.length) segments.push(segment)
  const line = (points: { x: number; y: number }[]) =>
    points.map((point, index) => `${index ? 'L' : 'M'} ${point.x} ${point.y}`).join(' ')
  return (
    <section className="analysis-dock" aria-label="Terreno bajo la ruta">
      <div>
        <p className="label">Terreno bajo la ruta</p>
        <p className="detail-hint">
          Cota del terreno, no de la vía: puentes y cortes aparecen como saltos.
        </p>
      </div>
      <svg
        className="profile-chart network-profile-chart"
        viewBox={`0 0 ${width} ${height}`}
        role="img"
        aria-label="Perfil del terreno con inversiones de marcha"
      >
        <line x1={padding} y1={height - padding} x2={width - padding} y2={height - padding} />
        {segments.map((points, index) => (
          <path
            key={`area-${index}`}
            className="network-terrain-area"
            d={`${line(points)} L ${points[points.length - 1].x} ${height - padding} L ${points[0].x} ${height - padding} Z`}
          />
        ))}
        {segments.map((points, index) => (
          <path key={index} d={line(points)} />
        ))}
        {response.route.reversals.map((reversal, index) => (
          <g key={index}>
            <line
              className="network-reversal-line"
              x1={x(reversal.distanceAlongMeters)}
              y1={padding}
              x2={x(reversal.distanceAlongMeters)}
              y2={height - padding}
            />
            <text x={x(reversal.distanceAlongMeters)} y={padding - 5}>
              Invierte la marcha
            </text>
          </g>
        ))}
      </svg>
    </section>
  )
}
