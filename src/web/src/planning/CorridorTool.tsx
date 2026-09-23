import { greatCircleDistance, type Coordinate } from '../shared/geo'
import type { CorridorResponse } from './api'
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
  bounds,
  busy,
  result,
  error,
  onGenerate,
  onCancel,
}: {
  origin: Coordinate | null
  destination: Coordinate | null
  gradient: string
  setGradient: (value: string) => void
  bounds: { west: number; south: number; east: number; north: number }
  busy: boolean
  result: CorridorResponse | null
  error: string | null
  onGenerate: () => void
  onCancel: () => void
}) {
  const limit = Number(gradient)
  const gradientError =
    !gradient || !Number.isFinite(limit) || limit < 1 || limit > 40
      ? 'Ingresá una pendiente entre 1 y 40 ‰.'
      : null
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
          disabled={!origin || !destination || !!gradientError || !!pointError || busy}
        >
          Generar corredor
        </button>
        <button className="button-secondary" type="button" onClick={onCancel}>
          Cancelar
        </button>
      </div>
      {busy && <p role="status">Calculando corredor…</p>}
      {result?.status === 'no_feasible_path' && (
        <CorridorAlert
          title={`No existe un corredor con pendiente ≤ ${formatNumber(limit)} ‰ entre estos puntos`}
          why="El relieve exige pendientes mayores en toda el área de búsqueda. Esta versión no usa túneles ni puentes."
          next="Subí el límite o mové los puntos."
          search={result.search}
        />
      )}
      {result?.status === 'endpoint_without_elevation' && (
        <CorridorAlert
          title="Un punto no tiene dato de relieve"
          why="No se puede evaluar la cota de uno de los extremos."
          next="Mové el punto a una zona con relieve disponible."
        />
      )}
      {error && (
        <CorridorAlert
          title="No se pudo calcular el corredor"
          why={error}
          next="Revisá el relieve y volvé a intentar."
        />
      )}
    </aside>
  )
}

function CorridorAlert({
  title,
  why,
  next,
  search,
}: {
  title: string
  why: string
  next: string
  search?: CorridorResponse['search']
}) {
  return (
    <div className="alert alert-alarm" role="alert">
      <p className="alert-title">
        <span className="glyph glyph-alarm" />
        {title}
      </p>
      <p>Por qué: {why}</p>
      <p>Qué probar: {next}</p>
      {search && (
        <p className="data">
          Malla {search.gridStepMeters} m · {search.exploredNodes.toLocaleString('es-AR')} nodos
          explorados
        </p>
      )}
    </div>
  )
}
