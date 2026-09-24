import type { RunningTime } from './api'

const width = 720
const height = 130
export function SpeedChart({
  value,
  padding = 24,
  axisLength,
}: {
  value: RunningTime
  padding?: number
  axisLength?: number
}) {
  const pad = padding
  const length = axisLength ?? value.metrics.routeLengthMeters
  const maximum = Math.max(
    value.train.maxSpeedKmh,
    ...value.speedProfile.map((point) => point.trackLimitKmh),
  )
  const x = (distance: number) => pad + (distance / length) * (width - 2 * pad)
  const y = (speed: number) => height - pad - (speed / Math.max(maximum, 1)) * (height - 2 * pad)
  const trainPath = value.speedProfile
    .map((point, index) => `${index ? 'L' : 'M'} ${x(point.distanceMeters)} ${y(point.speedKmh)}`)
    .join(' ')
  const limits = value.speedProfile
  const limitPath = limits
    .map((point, index) =>
      index
        ? `L ${x(point.distanceMeters)} ${y(limits[index - 1].trackLimitKmh)} L ${x(point.distanceMeters)} ${y(point.trackLimitKmh)}`
        : `M ${x(point.distanceMeters)} ${y(point.trackLimitKmh)}`,
    )
    .join(' ')
  return (
    <section className="speed-chart" aria-label="Velocidad del tren">
      <p className="label">Velocidad del tren</p>
      <p className="detail-hint data">
        Máxima del tren: {value.train.maxSpeedKmh.toLocaleString('es-AR')} km/h · Vía: línea
        escalonada
      </p>
      <svg
        viewBox={`0 0 ${width} ${height}`}
        role="img"
        aria-label="Perfil de velocidad del tren y límite de la vía en km/h"
      >
        <line
          className="speed-axis"
          x1={pad}
          y1={height - pad}
          x2={width - pad}
          y2={height - pad}
        />
        <line
          className="speed-maximum"
          x1={pad}
          y1={y(value.train.maxSpeedKmh)}
          x2={width - pad}
          y2={y(value.train.maxSpeedKmh)}
        />
        <path className="speed-limit" d={limitPath} />
        <path className="speed-train" d={trainPath} />
        {value.reversals.map((reversal, index) => (
          <g key={index}>
            <line
              className="speed-reversal"
              x1={x(reversal.distanceMeters)}
              y1={pad}
              x2={x(reversal.distanceMeters)}
              y2={height - pad}
            />
            <title>
              Invierte la marcha en km{' '}
              {(reversal.distanceMeters / 1000).toLocaleString('es-AR', {
                maximumFractionDigits: 2,
              })}
            </title>
            <text x={x(reversal.distanceMeters) + 3} y={pad - 4}>
              Invierte la marcha
            </text>
          </g>
        ))}
        <text x={pad} y={height - 4}>
          0 km/h
        </text>
        <text x={width - pad} y={height - 4} textAnchor="end">
          {(length / 1000).toLocaleString('es-AR', { maximumFractionDigits: 1 })} km
        </text>
      </svg>
    </section>
  )
}
