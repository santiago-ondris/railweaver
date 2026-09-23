import { greatCircleDistance, type Coordinate } from '../shared/geo'
import { formatNumber, formatPoint } from './format'
import './CorridorTool.css'

const presets = [
  { label: 'Carga', value: 10 },
  { label: 'Mixta', value: 15 },
  { label: 'Pasajeros', value: 25 },
  { label: 'Montaña', value: 35 },
]

export function CorridorTool({
  origin,
  destination,
  gradient,
  setGradient,
  gauge,
  setGauge,
  designSpeed,
  setDesignSpeed,
  bounds,
  busy,
  onGenerate,
  onCancel,
}: {
  origin: Coordinate | null
  destination: Coordinate | null
  gradient: string
  setGradient: (value: string) => void
  gauge: 1000 | 1676
  setGauge: (value: 1000 | 1676) => void
  designSpeed: string
  setDesignSpeed: (value: string) => void
  bounds: { west: number; south: number; east: number; north: number }
  busy: boolean
  onGenerate: () => void
  onCancel: () => void
}) {
  const limit = Number(gradient)
  const gradientError =
    !gradient || !Number.isFinite(limit) || limit < 1 || limit > 40
      ? 'Ingresá una pendiente entre 1 y 40 ‰.'
      : null
  const speed = Number(designSpeed)
  const speedError =
    !designSpeed || !Number.isFinite(speed) || speed < 20 || speed > 120
      ? 'Ingresá una velocidad entre 20 y 120 km/h.'
      : null
  const width = gauge === 1676 ? 1740 : 1060
  const cant = gauge === 1676 ? 240 : 160
  const absolute = gauge === 1676 ? 300 : 100
  const designRadius = Math.max(absolute, (width * speed * speed) / (3.6 ** 2 * 9.80665 * cant))
  const outside = [origin, destination].some(
    (point) =>
      point &&
      (point.latitude < bounds.south ||
        point.latitude > bounds.north ||
        point.longitude < bounds.west ||
        point.longitude > bounds.east),
  )
  const pointError = outside
    ? 'Los puntos deben estar dentro de la región.'
    : origin && destination && greatCircleDistance(origin, destination) < 1000
      ? 'Los puntos deben estar separados por al menos 1 km.'
      : null
  return (
    <aside className="corridor-tool" aria-label="Corredor candidato">
      <p className="label">Planificación</p>
      <h2>Corredor candidato</h2>
      <label className="corridor-label" htmlFor="corridor-gradient">
        Pendiente máxima
      </label>
      <div className="corridor-presets">
        {presets.map((preset) => (
          <button
            key={preset.value}
            type="button"
            className="button-secondary"
            aria-pressed={limit === preset.value}
            disabled={busy}
            onClick={() => setGradient(String(preset.value))}
          >
            {preset.label} {preset.value} ‰
          </button>
        ))}
      </div>
      <input
        id="corridor-gradient"
        className="corridor-input data"
        type="number"
        min="1"
        max="40"
        step="0.1"
        disabled={busy}
        value={gradient}
        onChange={(event) => setGradient(event.target.value)}
        aria-describedby="corridor-gradient-note"
      />
      <p id="corridor-gradient-note" className="detail-hint">
        Valores de referencia, a validar.
      </p>
      {gradientError && (
        <p className="corridor-validation" role="alert">
          {gradientError}
        </p>
      )}
      <p className="corridor-label">Trocha</p>
      <div className="corridor-presets corridor-gauge">
        <button
          type="button"
          className="button-secondary"
          aria-pressed={gauge === 1676}
          disabled={busy}
          onClick={() => setGauge(1676)}
        >
          <span className="corridor-gauge-check" aria-hidden="true">
            ✓
          </span>{' '}
          Ancha 1.676 mm
        </button>
        <button
          type="button"
          className="button-secondary"
          aria-pressed={gauge === 1000}
          disabled={busy}
          onClick={() => setGauge(1000)}
        >
          <span className="corridor-gauge-check" aria-hidden="true">
            ✓
          </span>{' '}
          Métrica 1.000 mm
        </button>
      </div>
      <label className="corridor-label" htmlFor="corridor-speed">
        Velocidad de diseño
      </label>
      <div className="corridor-presets">
        {(
          [
            { label: 'Montaña', value: 40 },
            { label: 'Regional', value: 80 },
            { label: 'Troncal', value: 120 },
          ] as const
        ).map((preset) => (
          <button
            key={preset.value}
            type="button"
            className="button-secondary"
            aria-pressed={speed === preset.value}
            disabled={busy}
            onClick={() => setDesignSpeed(String(preset.value))}
          >
            {preset.label} {preset.value} km/h
          </button>
        ))}
      </div>
      <input
        id="corridor-speed"
        className="corridor-input data"
        type="number"
        min="20"
        max="120"
        step="1"
        disabled={busy}
        value={designSpeed}
        onChange={(event) => setDesignSpeed(event.target.value)}
      />
      <p className="detail-hint data">
        Radio de diseño {Number.isFinite(designRadius) ? formatNumber(designRadius, 0) : '—'} m ·
        mínimo absoluto {absolute} m
      </p>
      <p className="detail-hint">Valores de referencia, a validar.</p>
      {speedError && (
        <p className="corridor-validation" role="alert">
          {speedError}
        </p>
      )}
      <dl className="detail-list">
        <div>
          <dt>Origen</dt>
          <dd>{origin ? formatPoint(origin) : 'Marcá un punto en el mapa'}</dd>
        </div>
        <div>
          <dt>Destino</dt>
          <dd>{destination ? formatPoint(destination) : 'Marcá un punto en el mapa'}</dd>
        </div>
      </dl>
      {pointError && (
        <p className="corridor-validation" role="alert">
          {pointError}
        </p>
      )}
      <div className="corridor-actions">
        <button
          className="button-primary"
          type="button"
          onClick={onGenerate}
          disabled={
            !origin || !destination || !!gradientError || !!speedError || !!pointError || busy
          }
        >
          Generar corredor
        </button>
        <button className="button-secondary" type="button" onClick={onCancel}>
          Cancelar
        </button>
      </div>
    </aside>
  )
}
