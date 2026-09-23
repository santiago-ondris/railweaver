import { DetailRow } from '../shared/DetailRow'
import type { Coordinate } from '../shared/geo'
import type { Railway } from './api'
import type { RailwaySelection } from './ExistingRailwayLayer'
import { gaugeLabel, stationTypeLabel, trackStatusLabel, trackUsageLabel } from './labels'

export function RailwayDetailPanel({
  railway,
  selection,
  coordinate,
  elevation,
  onClear,
}: {
  railway: Railway
  selection: RailwaySelection | null
  coordinate: Coordinate | null
  elevation: number | null | undefined
  onClear: () => void
}) {
  const title = selection
    ? selection.kind === 'track'
      ? (selection.item.name ?? selection.item.lineReference ?? 'Tramo sin nombre')
      : selection.item.name
    : 'Punto del terreno'
  return (
    <aside className="detail-panel" aria-label="Detalle ferroviario">
      <p className="label">
        {selection
          ? selection.kind === 'track'
            ? 'Tramo de vía'
            : stationTypeLabel(selection.item.type)
          : 'Inspección'}
      </p>
      <h2>{coordinate ? title : 'Red ferroviaria de Córdoba'}</h2>
      <p className="detail-provenance">{railway.source.attribution}</p>
      <dl className="detail-list">
        {coordinate && (
          <>
            <DetailRow
              label="Latitud"
              value={coordinate.latitude.toLocaleString('es-AR', { maximumFractionDigits: 5 })}
            />
            <DetailRow
              label="Longitud"
              value={coordinate.longitude.toLocaleString('es-AR', { maximumFractionDigits: 5 })}
            />
            <DetailRow
              label="Cota"
              value={
                elevation === undefined
                  ? 'Consultando…'
                  : elevation === null
                    ? 'Sin dato'
                    : `${elevation.toLocaleString('es-AR', { minimumFractionDigits: 2 })} m`
              }
            />
          </>
        )}
        {selection?.kind === 'track' && (
          <>
            <DetailRow label="Estado" value={trackStatusLabel(selection.item.status)} />
            <DetailRow label="Uso" value={trackUsageLabel(selection.item.usage)} />
            <DetailRow label="Trocha" value={gaugeLabel(selection.item.gauge)} />
          </>
        )}
        {selection?.kind === 'station' && (
          <DetailRow label="Trocha" value={gaugeLabel(selection.item.gauge)} />
        )}
        {!coordinate && (
          <>
            <DetailRow label="Tramos" value={railway.tracks.length.toLocaleString('es-AR')} />
            <DetailRow label="Estaciones" value={railway.stations.length.toLocaleString('es-AR')} />
          </>
        )}
      </dl>
      {selection && (
        <button type="button" className="button-secondary detail-close" onClick={onClear}>
          Cerrar detalle
        </button>
      )}
    </aside>
  )
}
