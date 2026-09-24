import { DetailRow } from '../shared/DetailRow'
import { RunningTimeResult } from '../operations/RunningTimeResult'
import type { RunningTime } from '../operations/api'
import type { NetworkDiagnostic, RouteResponse } from './api'

const formatDistance = (meters: number) =>
  meters >= 1000
    ? `${(meters / 1000).toLocaleString('es-AR', { maximumFractionDigits: 1 })} km`
    : `${meters.toLocaleString('es-AR', { maximumFractionDigits: 0 })} m`

export function NetworkRouteDetail({
  response,
  number,
  onRemove,
  onRunningTime,
  runningTime,
  onRemoveRunningTime,
}: {
  response: Extract<RouteResponse, { status: 'found' }>
  number: number
  onRemove: () => void
  onRunningTime: () => void
  runningTime: RunningTime | null
  onRemoveRunningTime: () => void
}) {
  const route = response.route
  const lines = [
    ...new Set(
      route.segments.map((segment) => segment.name ?? segment.lineReference).filter(Boolean),
    ),
  ]
  const count = route.reversals.length
  return (
    <aside className="detail-panel" aria-label="Detalle de la ruta">
      <p className="detail-identifier">R-{String(number).padStart(2, '0')}</p>
      <p className="label">Ruta por la red existente</p>
      <h2>Ruta por la red existente</h2>
      <p className="detail-provenance">
        Trocha {route.gauge.widthMillimetres.toLocaleString('es-AR')} mm ·{' '}
        {response.includeDisused ? 'vías activas y en desuso' : 'solo vías activas'}
      </p>
      <dl className="detail-list">
        <DetailRow label="Longitud" value={formatDistance(route.lengthMeters)} />
        <DetailRow
          label="Distancia directa"
          value={formatDistance(route.straightLineDistanceMeters)}
        />
        {count ? (
          <div className="network-warning-metric">
            <dt>Inversiones</dt>
            <dd>▲ {count}</dd>
          </div>
        ) : (
          <DetailRow label="Inversiones" value="0" />
        )}
        <DetailRow label="Vías activas" value={formatDistance(route.distanceByStatus.active)} />
        {response.includeDisused && (
          <DetailRow label="En desuso" value={formatDistance(route.distanceByStatus.disused)} />
        )}
        <DetailRow
          label="Trocha deducida"
          value={formatDistance(route.inferredGaugeDistanceMeters)}
        />
        <DetailRow
          label="Líneas recorridas"
          value={lines.length ? lines.join(' · ') : 'Sin nombre en OSM'}
        />
      </dl>
      {count > 0 && (
        <div className="alert alert-warning" role="note">
          <p className="alert-title">
            <span className="glyph glyph-warning" aria-hidden="true" />
            La ruta exige invertir la marcha {count} {count === 1 ? 'vez' : 'veces'}
          </p>
          <p>
            Por qué: en estos puntos las vías forman un ángulo que el tren no puede tomar de
            corrido.
          </p>
          <p className="data">
            {route.reversals
              .map(
                (reversal) =>
                  `${reversal.location.latitude.toFixed(4)}, ${reversal.location.longitude.toFixed(4)}`,
              )
              .join(' · ')}
          </p>
          <p>
            Consecuencia: el tren debe pasar el desvío, detenerse y retroceder. La longitud no
            incluye esa maniobra.
          </p>
        </div>
      )}
      <p className="detail-hint">
        Camino sobre la geometría de OpenStreetMap. La velocidad de la vía se elige al calcular el
        tiempo de recorrido.
      </p>
      {runningTime && <RunningTimeResult value={runningTime} onRemove={onRemoveRunningTime} />}
      <button type="button" className="button-secondary" onClick={onRunningTime}>
        Tiempo de recorrido
      </button>
      <button type="button" className="button-secondary detail-close" onClick={onRemove}>
        Quitar ruta
      </button>
    </aside>
  )
}

const names: Record<NetworkDiagnostic['kind'], string> = {
  possible_data_gap: 'Posible corte de datos',
  sharp_joint: 'Unión en ángulo cerrado',
  station_without_track: 'Estación sin vía',
  end_of_track: 'Fin de vía',
  region_boundary: 'Borde del área de datos',
  track_without_gauge: 'Vía sin trocha',
}

export function NetworkDiagnosticDetail({
  item,
  onClear,
}: {
  item: NetworkDiagnostic
  onClear: () => void
}) {
  const gauge = item.gaugeMillimetres
    ? `${item.gaugeMillimetres.toLocaleString('es-AR')} mm`
    : 'Sin dato'
  const distance = item.distanceMeters === null ? 'Sin dato' : formatDistance(item.distanceMeters)
  const explanation: Record<NetworkDiagnostic['kind'], string> = {
    possible_data_gap: `La vía termina a ${distance} de otra parte de la red de trocha ${gauge}, sin unirse. Puede ser un corte real o un error de mapeo en OSM.`,
    end_of_track: 'La vía termina acá. No hay otra parte de la red a menos de 500 m.',
    region_boundary:
      'La vía sigue fuera del área de datos de Córdoba: el dataset se recorta en ese límite.',
    sharp_joint: `Dos tramos se unen con una desviación de ${item.deflectionDegrees?.toLocaleString('es-AR')}°, mayor que 45°. Un tren no puede pasar de uno al otro de corrido y no hay desvío para invertir la marcha.`,
    station_without_track: `No hay vías a menos de 150 m. La más cercana está a ${distance}.`,
    track_without_gauge:
      'OSM no indica la trocha y la vía no está unida a ninguna red, así que no se puede deducir.',
  }
  return (
    <aside className="detail-panel" aria-label="Diagnóstico de red">
      <p className="label">Diagnóstico de red</p>
      <h2>{names[item.kind]}</h2>
      <dl className="detail-list">
        <DetailRow label="Trocha" value={gauge} />
        <DetailRow
          label="Latitud"
          value={item.location.latitude.toLocaleString('es-AR', { maximumFractionDigits: 5 })}
        />
        <DetailRow
          label="Longitud"
          value={item.location.longitude.toLocaleString('es-AR', { maximumFractionDigits: 5 })}
        />
      </dl>
      <p>{explanation[item.kind]}</p>
      <button type="button" className="button-secondary detail-close" onClick={onClear}>
        Cerrar detalle
      </button>
    </aside>
  )
}
