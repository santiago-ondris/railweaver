import type { Coordinate } from './elevation'
import type { CandidateCorridor, CorridorResponse } from './corridors'

const presets = [
  { label: 'Carga', value: 10 }, { label: 'Mixta', value: 15 },
  { label: 'Pasajeros', value: 25 }, { label: 'Montaña', value: 35 },
]
const number = (value: number, digits = 1) => value.toLocaleString('es-AR', { minimumFractionDigits: digits, maximumFractionDigits: digits })
const meters = (value: number) => `${number(value, 2)} m`
const distance = (value: number) => value >= 1000 ? `${number(value / 1000, 2)} km` : meters(value)

export function CorridorTool({ origin, destination, gradient, setGradient, bounds, busy, result, error, onGenerate, onCancel }: {
  origin: Coordinate | null; destination: Coordinate | null; gradient: string; setGradient: (value: string) => void
  bounds: { west: number; south: number; east: number; north: number }; busy: boolean
  result: CorridorResponse | null; error: string | null; onGenerate: () => void; onCancel: () => void
}) {
  const limit = Number(gradient)
  const gradientError = !gradient || !Number.isFinite(limit) || limit < 1 || limit > 40 ? 'Ingresá una pendiente entre 1 y 40 ‰.' : null
  const outside = [origin, destination].some((point) => point &&
    (point.latitude < bounds.south || point.latitude > bounds.north || point.longitude < bounds.west || point.longitude > bounds.east))
  const pointError = outside ? 'Los puntos deben estar dentro de la región.' :
    origin && destination && greatCircleDistance(origin, destination) < 1000 ? 'Los puntos deben estar separados por al menos 1 km.' : null
  return <aside className="corridor-tool" aria-label="Corredor candidato">
    <p className="label">Planificación</p><h2>Corredor candidato</h2>
    <label className="corridor-label" htmlFor="corridor-gradient">Pendiente máxima</label>
    <div className="corridor-presets">{presets.map((preset) => <button key={preset.value} type="button"
      className="button-secondary" aria-pressed={limit === preset.value} onClick={() => setGradient(String(preset.value))}>
      {preset.label} {preset.value} ‰</button>)}</div>
    <input id="corridor-gradient" className="corridor-input data" type="number" min="1" max="40" step="0.1"
      value={gradient} onChange={(event) => setGradient(event.target.value)} aria-describedby="corridor-gradient-note" />
    <p id="corridor-gradient-note" className="detail-hint">Valores de referencia, a validar.</p>
    {gradientError && <p className="corridor-validation" role="alert">{gradientError}</p>}
    <dl className="detail-list">
      <div><dt>Origen</dt><dd>{origin ? pointLabel(origin) : 'Marcá un punto en el mapa'}</dd></div>
      <div><dt>Destino</dt><dd>{destination ? pointLabel(destination) : 'Marcá un punto en el mapa'}</dd></div>
    </dl>
    {pointError && <p className="corridor-validation" role="alert">{pointError}</p>}
    <div className="corridor-actions">
      <button className="button-primary" type="button" onClick={onGenerate}
        disabled={!origin || !destination || !!gradientError || !!pointError || busy}>Generar corredor</button>
      <button className="button-secondary" type="button" onClick={onCancel}>Cancelar</button>
    </div>
    {busy && <p role="status">Calculando corredor…</p>}
    {result?.status === 'no_feasible_path' && <CorridorAlert title={`No existe un corredor con pendiente ≤ ${number(limit)} ‰ entre estos puntos`}
      why="El relieve exige pendientes mayores en toda el área de búsqueda. Esta versión no usa túneles ni puentes."
      next="Subí el límite o mové los puntos." search={result.search} />}
    {result?.status === 'endpoint_without_elevation' && <CorridorAlert title="Un punto no tiene dato de relieve"
      why="No se puede evaluar la cota de uno de los extremos." next="Mové el punto a una zona con relieve disponible." />}
    {error && <CorridorAlert title="No se pudo calcular el corredor" why={error} next="Revisá el relieve y volvé a intentar." />}
  </aside>
}

export function CorridorDetail({ corridor, search, id, onRemove }: {
  corridor: CandidateCorridor; search: CorridorResponse['search']; id: number; onRemove: () => void
}) {
  const metric = corridor.metrics
  return <aside className="detail-panel" aria-label="Detalle del corredor">
    <p className="detail-identifier">C-{String(id).padStart(2, '0')}</p>
    <p className="label">Corredor candidato</p><h2>Corredor candidato</h2>
    <p className="detail-provenance">Límite {number(metric.maxGradientLimitPermille)} ‰ · malla {search.gridStepMeters} m</p>
    <dl className="detail-list">
      <Metric label="Longitud" value={distance(metric.lengthMeters)} />
      <Metric label="Distancia directa" value={distance(metric.straightLineDistanceMeters)} />
      <Metric label="Sinuosidad" value={number(metric.sinuosity, 3)} />
      <Metric label="Pendiente máxima" value={`${number(metric.maxGradientPermille)} ‰ ≤ ${number(metric.maxGradientLimitPermille)} ‰`} />
      <Metric label="Subida" value={meters(metric.ascentMeters)} /><Metric label="Bajada" value={meters(metric.descentMeters)} />
      <Metric label="Cota mínima" value={meters(metric.minElevationMeters)} /><Metric label="Cota máxima" value={meters(metric.maxElevationMeters)} />
      <Metric label="Corte implícito máx." value={meters(metric.maxCutMeters)} /><Metric label="Relleno implícito máx." value={meters(metric.maxFillMeters)} />
      {metric.distanceByGradientBand.map((band) => <Metric key={band.fromPercentOfLimit}
        label={`Pendiente ${band.fromPercentOfLimit}–${band.toPercentOfLimit} %`} value={distance(band.meters)} />)}
      <Metric label="Nodos explorados" value={search.exploredNodes.toLocaleString('es-AR')} />
    </dl>
    <p className="detail-hint">Supuestos: sin túneles, puentes, curvas ni costos de suelo. Límite de referencia, a validar.</p>
    <button className="button-secondary detail-close" type="button" onClick={onRemove}>Quitar corredor</button>
  </aside>
}

export function CorridorProfile({ corridor }: { corridor: CandidateCorridor }) {
  const samples = corridor.terrainProfile.samples
  const track = corridor.trackProfile
  const values = [...samples.flatMap((sample) => sample.elevationMeters === null ? [] : [sample.elevationMeters]),
    ...track.map((point) => point.elevationMeters)]
  const min = Math.min(...values), max = Math.max(...values)
  const width = 720, height = 130, pad = 22
  const x = (distanceMeters: number) => pad + distanceMeters / corridor.terrainProfile.totalDistanceMeters * (width - pad * 2)
  const y = (elevation: number) => height - pad - (elevation - min) / Math.max(1, max - min) * (height - pad * 2)
  const terrainPaths: string[] = []
  const terrainAreas: string[] = []
  let path = '', firstX = 0, lastX = 0
  for (const sample of samples) {
    if (sample.elevationMeters === null) {
      if (path) { terrainPaths.push(path); terrainAreas.push(`${path} L ${lastX} ${height - pad} L ${firstX} ${height - pad} Z`) }
      path = ''; continue
    }
    if (!path) firstX = x(sample.distanceMeters)
    lastX = x(sample.distanceMeters)
    path += `${path ? ' L' : 'M'} ${x(sample.distanceMeters)} ${y(sample.elevationMeters)}`
  }
  if (path) { terrainPaths.push(path); terrainAreas.push(`${path} L ${lastX} ${height - pad} L ${firstX} ${height - pad} Z`) }
  const trackPath = track.map((point, index) => `${index ? 'L' : 'M'} ${x(point.distanceMeters)} ${y(point.elevationMeters)}`).join(' ')
  return <section className="analysis-dock" aria-label="Perfil longitudinal del corredor">
    <div><p className="label">Perfil longitudinal</p><p className="data">Máx. {number(corridor.metrics.maxGradientPermille)} ‰ ≤ límite {number(corridor.metrics.maxGradientLimitPermille)} ‰</p>
      {samples.some((sample) => sample.elevationMeters === null) && <p className="detail-hint">Hay tramos de terreno sin dato.</p>}</div>
    <svg className="profile-chart corridor-chart" viewBox={`0 0 ${width} ${height}`} role="img" aria-label="Perfil del terreno y de la línea de vía">
      <line x1={pad} y1={height - pad} x2={width - pad} y2={height - pad} />
      {terrainAreas.map((part, index) => <path key={index} className="corridor-terrain-area" d={part} />)}
      {terrainPaths.map((part, index) => <path key={index} className="corridor-terrain" d={part} />)}
      <path className="corridor-track" d={trackPath} />
      {samples.some((sample) => sample.elevationMeters === null) && <text x={width / 2} y={height / 2}>Tramo sin dato</text>}
    </svg>
  </section>
}

function Metric({ label, value }: { label: string; value: string }) { return <div><dt>{label}</dt><dd>{value}</dd></div> }
function CorridorAlert({ title, why, next, search }: { title: string; why: string; next: string; search?: CorridorResponse['search'] }) {
  return <div className="alert alert-alarm" role="alert"><p className="alert-title"><span className="glyph glyph-alarm" />{title}</p>
    <p>Por qué: {why}</p><p>Qué probar: {next}</p>
    {search && <p className="data">Malla {search.gridStepMeters} m · {search.exploredNodes.toLocaleString('es-AR')} nodos explorados</p>}</div>
}
function pointLabel(point: Coordinate) { return `${number(point.latitude, 5)}, ${number(point.longitude, 5)}` }
function greatCircleDistance(a: Coordinate, b: Coordinate) {
  const rad = Math.PI / 180
  const lat = (b.latitude - a.latitude) * rad, lon = (b.longitude - a.longitude) * rad
  const value = Math.sin(lat / 2) ** 2 + Math.cos(a.latitude * rad) * Math.cos(b.latitude * rad) * Math.sin(lon / 2) ** 2
  return 2 * 6371008.8 * Math.asin(Math.min(1, Math.sqrt(value)))
}
