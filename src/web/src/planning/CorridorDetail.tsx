import { DetailRow } from '../shared/DetailRow'
import type { CandidateCorridor, CorridorResponse } from './api'
import { formatDistance, formatMeters, formatNumber } from './format'

export function CorridorDetail({
  corridor,
  search,
  id,
  onRemove,
}: {
  corridor: CandidateCorridor
  search: CorridorResponse['search']
  id: number
  onRemove: () => void
}) {
  const metric = corridor.metrics
  return (
    <aside className="detail-panel" aria-label="Detalle del corredor">
      <p className="detail-identifier">C-{String(id).padStart(2, '0')}</p>
      <p className="label">Corredor candidato</p>
      <h2>Corredor candidato</h2>
      <p className="detail-provenance">
        Límite {formatNumber(metric.maxGradientLimitPermille)} ‰ · malla {search.gridStepMeters} m
      </p>
      <dl className="detail-list">
        <DetailRow label="Longitud" value={formatDistance(metric.lengthMeters)} />
        <DetailRow
          label="Distancia directa"
          value={formatDistance(metric.straightLineDistanceMeters)}
        />
        <DetailRow label="Sinuosidad" value={formatNumber(metric.sinuosity, 3)} />
        <DetailRow
          label="Pendiente máxima"
          value={`${formatNumber(metric.maxGradientPermille)} ‰ ≤ ${formatNumber(metric.maxGradientLimitPermille)} ‰`}
        />
        <DetailRow label="Subida" value={formatMeters(metric.ascentMeters)} />
        <DetailRow label="Bajada" value={formatMeters(metric.descentMeters)} />
        <DetailRow label="Cota mínima" value={formatMeters(metric.minElevationMeters)} />
        <DetailRow label="Cota máxima" value={formatMeters(metric.maxElevationMeters)} />
        <DetailRow label="Corte implícito máx." value={formatMeters(metric.maxCutMeters)} />
        <DetailRow label="Relleno implícito máx." value={formatMeters(metric.maxFillMeters)} />
        {metric.distanceByGradientBand.map((band) => (
          <DetailRow
            key={band.fromPercentOfLimit}
            label={`Pendiente ${band.fromPercentOfLimit}–${band.toPercentOfLimit} %`}
            value={formatDistance(band.meters)}
          />
        ))}
        <DetailRow label="Nodos explorados" value={search.exploredNodes.toLocaleString('es-AR')} />
      </dl>
      <p className="detail-hint">
        Supuestos: sin túneles, puentes, curvas ni costos de suelo. Límite de referencia, a validar.
      </p>
      <button className="button-secondary detail-close" type="button" onClick={onRemove}>
        Quitar corredor
      </button>
    </aside>
  )
}
