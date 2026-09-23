import { useEffect, useState } from 'react'
import type { CorridorResponse } from './api'
import { formatNumber } from './format'
import './CorridorFeedback.css'

export function CorridorFeedback({
  busy,
  startedAt,
  result,
  error,
  gradient,
  gauge,
  onCancelSearch,
  onDismiss,
}: {
  busy: boolean
  startedAt: number | null
  result: CorridorResponse | null
  error: string | null
  gradient: string
  gauge: 1000 | 1676
  onCancelSearch: () => void
  onDismiss: () => void
}) {
  const [now, setNow] = useState(0)
  useEffect(() => {
    if (!busy) return
    const interval = window.setInterval(() => setNow(Date.now()), 1000)
    return () => window.clearInterval(interval)
  }, [busy])

  if (!busy && !result && !error) return null
  const absolute = gauge === 1676 ? 300 : 100
  const elapsed = Math.max(0, Math.floor((now - (startedAt ?? now)) / 1000))

  return (
    <div className="corridor-feedback-overlay">
      {busy ? (
        <section className="corridor-feedback-card" role="status">
          <p className="label">Corredor candidato</p>
          <h2>Generando corredor…</h2>
          <p>
            Buscando un trazado con pendiente y curvas admisibles. Después se calculan los arcos y
            la rasante.
          </p>
          <p className="data" aria-hidden="true">
            Tiempo transcurrido: {elapsed} s
          </p>
          <button className="button-secondary" type="button" onClick={onCancelSearch}>
            Cancelar búsqueda
          </button>
        </section>
      ) : (
        <section className="corridor-feedback-card alert alert-alarm" role="alert">
          <h2 className="alert-title">
            <span className="glyph glyph-alarm" aria-hidden="true" />
            {result?.status === 'no_feasible_path'
              ? `No existe un corredor con pendiente ≤ ${formatNumber(Number(gradient))} ‰ y curvas de radio ≥ ${absolute} m entre estos puntos`
              : result?.status === 'endpoint_without_elevation'
                ? 'Un punto no tiene dato de relieve'
                : 'No se pudo calcular el corredor'}
          </h2>
          {result?.status === 'no_feasible_path' ? (
            <>
              <p>
                Por qué:{' '}
                {result.feasibleWithoutCurveLimit
                  ? `Con esa pendiente hay camino, pero exige curvas más cerradas que el mínimo absoluto de la trocha ${gauge} mm (${absolute} m).`
                  : 'El relieve exige pendientes mayores en toda el área de búsqueda. Esta versión no usa túneles ni puentes.'}
              </p>
              <p>
                Qué probar:{' '}
                {result.feasibleWithoutCurveLimit
                  ? gauge === 1676
                    ? 'Probá trocha métrica, subí el límite de pendiente o mové los puntos.'
                    : 'Subí el límite de pendiente o mové los puntos.'
                  : 'Subí el límite o mové los puntos.'}
              </p>
              <p className="data">
                Malla {result.search.gridStepMeters} m ·{' '}
                {result.search.exploredStates.toLocaleString('es-AR')} estados explorados
              </p>
            </>
          ) : result?.status === 'endpoint_without_elevation' ? (
            <>
              <p>Por qué: no se puede evaluar la cota de uno de los extremos.</p>
              <p>Qué probar: mové el punto a una zona con relieve disponible.</p>
            </>
          ) : (
            <>
              <p>Por qué: {error}</p>
              <p>Qué probar: revisá el relieve y volvé a intentar.</p>
            </>
          )}
          <button className="button-secondary" type="button" onClick={onDismiss}>
            Volver al formulario
          </button>
        </section>
      )}
    </div>
  )
}
