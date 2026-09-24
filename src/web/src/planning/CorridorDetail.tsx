import { DetailRow } from '../shared/DetailRow'
import { RunningTimeResult } from '../operations/RunningTimeResult'
import type { RunningTime } from '../operations/api'
import type { CandidateCorridor, CorridorResponse } from './api'
import { formatDistance, formatMeters, formatNumber } from './format'

export function CorridorDetail({
  corridor,
  search,
  id,
  onRemove,
  onRunningTime,
  runningTime,
  onRemoveRunningTime,
}: {
  corridor: CandidateCorridor
  search: CorridorResponse['search']
  id: number
  onRemove: () => void
  onRunningTime: () => void
  runningTime: RunningTime | null
  onRemoveRunningTime: () => void
}) {
  const metric = corridor.metrics
  return (
    <aside className="detail-panel" aria-label="Detalle del corredor">
      <p className="detail-identifier">C-{String(id).padStart(2, '0')}</p>
      <p className="label">Corredor candidato</p>
      <h2>Corredor candidato</h2>
      <p className="detail-provenance">
        Límite {formatNumber(metric.maxGradientLimitPermille)} ‰ · trocha{' '}
        {metric.gaugeMillimetres.toLocaleString('es-AR')} mm · {Math.floor(metric.designSpeedKmh)}{' '}
        km/h · malla {search.gridStepMeters} m
      </p>
      <dl className="detail-list">
        <DetailRow label="Longitud" value={formatDistance(metric.lengthMeters)} />
        <DetailRow
          label="Distancia directa"
          value={formatDistance(metric.straightLineDistanceMeters)}
        />
        <DetailRow label="Sinuosidad" value={formatNumber(metric.sinuosity, 3)} />
        <DetailRow
          label="Velocidad de diseño"
          value={`${Math.floor(metric.designSpeedKmh)} km/h`}
        />
        <DetailRow label="Radio de diseño" value={`${Math.round(metric.designRadiusMeters)} m`} />
        <DetailRow label="Curvas" value={String(metric.curveCount)} />
        <DetailRow
          label="Radio mínimo"
          value={
            metric.minimumRadiusMeters === null
              ? '—'
              : `${Math.round(metric.minimumRadiusMeters)} m`
          }
        />
        <div className={metric.reducedSpeedCurveCount ? 'corridor-warning-metric' : ''}>
          <dt>Curvas con velocidad reducida</dt>
          <dd>
            {metric.reducedSpeedCurveCount ? '△ ' : ''}
            {metric.reducedSpeedCurveCount}
          </dd>
        </div>
        <div className={metric.reducedSpeedCurveCount ? 'corridor-warning-metric' : ''}>
          <dt>Velocidad mínima</dt>
          <dd>
            {metric.reducedSpeedCurveCount ? '△ ' : ''}
            {Math.floor(metric.minimumSpeedKmh)} km/h
          </dd>
        </div>
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
        <DetailRow
          label="Estados explorados"
          value={search.exploredStates.toLocaleString('es-AR')}
        />
      </dl>
      {metric.reducedSpeedCurveCount > 0 && (
        <p className="detail-hint">
          Los tramos lentos siguen resaltados al alejar el mapa. Acercate para leer sus radios y
          velocidades.
        </p>
      )}
      <p className="detail-hint">
        Curvas circulares sin transiciones; peralte supuesto, no dibujado. Corte y terraplén sin
        calcular la obra. Sin túneles, puentes ni costos de suelo. Valores de referencia, a validar.
      </p>
      {runningTime && <RunningTimeResult value={runningTime} onRemove={onRemoveRunningTime} />}
      <button className="button-secondary" type="button" onClick={onRunningTime}>
        Tiempo de recorrido
      </button>
      <button className="button-secondary detail-close" type="button" onClick={onRemove}>
        Quitar corredor
      </button>
    </aside>
  )
}
