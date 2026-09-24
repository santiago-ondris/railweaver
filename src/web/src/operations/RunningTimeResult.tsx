import { DetailRow } from '../shared/DetailRow'
import type { RunningTime } from './api'

const number = (value: number, digits = 1) =>
  value.toLocaleString('es-AR', { maximumFractionDigits: digits })
const duration = (seconds: number) => {
  const total = Math.round(seconds)
  const hours = Math.floor(total / 3600)
  const minutes = Math.floor((total % 3600) / 60)
  const rest = total % 60
  return `${hours ? `${hours} h ` : ''}${minutes} min ${rest} s`
}
const regimeNames: Record<string, string> = {
  accelerating: 'Acelerando',
  train_maximum: 'A la máxima del tren',
  track_limited: 'Limitado por la vía',
  braking: 'Frenando',
  dwell: 'Detenido en inversiones',
}

export function RunningTimeResult({
  value,
  onRemove,
}: {
  value: RunningTime
  onRemove: () => void
}) {
  const { metrics, reversals, train } = value
  const short = reversals.find((reversal) => reversal.maneuverFits === false)
  const curve = metrics.costliestCurve
  return (
    <section className="operations-result" aria-label="Tiempo de recorrido calculado">
      <h3>Tiempo de recorrido · {train.name}</h3>
      <dl className="detail-list">
        <DetailRow label="Tiempo total" value={duration(metrics.totalSeconds)} />
        <DetailRow
          label="Velocidad comercial"
          value={`${number(metrics.commercialSpeedKmh)} km/h`}
        />
        <DetailRow
          label="Velocidad máxima alcanzada"
          value={`${number(metrics.maxReachedSpeedKmh)} km/h`}
        />
        {reversals.length > 0 && (
          <DetailRow
            label="Distancia recorrida"
            value={`${number(metrics.travelledMeters / 1000, 2)} km`}
          />
        )}
        {metrics.regimes
          .filter((item) => item.seconds > 0)
          .map((item) => (
            <DetailRow
              key={item.regime}
              label={regimeNames[item.regime] ?? item.regime}
              value={`${duration(item.seconds)} · ${number((100 * item.seconds) / metrics.totalSeconds)} %`}
            />
          ))}
      </dl>
      {curve && (
        <p className="detail-hint data">
          Curva más costosa: R {number(curve.radiusMeters ?? 0, 0)} m en km{' '}
          {number(curve.fromMeters / 1000, 2)} · {number(curve.speedLimitKmh)} km/h · +
          {number(curve.timeLostSeconds)} s
        </p>
      )}
      {reversals.length > 0 && (
        <p className="detail-hint data">
          Inversiones: {reversals.length} · maniobra de {number(train.lengthMeters, 0)} m cada una
        </p>
      )}
      {short && (
        <div className="alert alert-warning" role="note">
          <p className="alert-title">
            <span className="glyph glyph-warning" aria-hidden="true" />
            La vía de maniobra es más corta que el tren
          </p>
          <p>
            Por qué: en km {number(short.distanceMeters / 1000, 2)} hay{' '}
            {number(short.maneuverTrackMeters ?? 0, 0)} m de vía para pasar el desvío y el tren mide{' '}
            {number(train.lengthMeters, 0)} m.
          </p>
          <p>
            Consecuencia: el tiempo supone que la maniobra entra. En la realidad habría que partir
            el tren o elegir otro.
          </p>
        </div>
      )}
      <p className="detail-hint">
        Tiempo de marcha puro: aceleración y frenado constantes, sin efecto de la pendiente, sin
        márgenes ni paradas intermedias.
      </p>
      <button type="button" className="button-secondary" onClick={onRemove}>
        Quitar tiempo de recorrido
      </button>
    </section>
  )
}
