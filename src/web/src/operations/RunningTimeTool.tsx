import { useState } from 'react'
import type { Train, TrainPreset } from './api'
import './Operations.css'

type Target = { kind: 'corridor' } | { kind: 'route'; reversalCount: number }
type Field =
  | 'lengthMeters'
  | 'maxSpeedKmh'
  | 'accelerationMetersPerSecondSquared'
  | 'brakingMetersPerSecondSquared'
const fields: {
  key: Field
  label: string
  min: number
  max: number
  step: string
  unit: string
}[] = [
  { key: 'lengthMeters', label: 'Largo', min: 10, max: 1500, step: '1', unit: 'm' },
  { key: 'maxSpeedKmh', label: 'Velocidad máxima', min: 10, max: 160, step: '0.1', unit: 'km/h' },
  {
    key: 'accelerationMetersPerSecondSquared',
    label: 'Aceleración',
    min: 0.01,
    max: 1.5,
    step: '0.01',
    unit: 'm/s²',
  },
  {
    key: 'brakingMetersPerSecondSquared',
    label: 'Frenado de servicio',
    min: 0.05,
    max: 1.5,
    step: '0.01',
    unit: 'm/s²',
  },
]

export function RunningTimeTool({
  target,
  presets,
  busy,
  error,
  onCalculate,
  onCancel,
}: {
  target: Target
  presets: TrainPreset[]
  busy: boolean
  error: string | null
  onCalculate: (train: Train, lineSpeed: number, dwellMinutes: number) => void
  onCancel: () => void
}) {
  const [selected, setSelected] = useState<string | null>(presets[0].id)
  const [values, setValues] = useState<Record<Field, string>>(toFields(presets[0]))
  const [lineSpeed, setLineSpeed] = useState('')
  const [dwell, setDwell] = useState('0')
  const trainErrors = fields.map((field) => {
    const value = values?.[field.key] ?? ''
    const number = Number(value)
    return !value || !Number.isFinite(number) || number < field.min || number > field.max
      ? `${field.label}: ingresá entre ${field.min} y ${field.max} ${field.unit}.`
      : null
  })
  const speedValue = Number(lineSpeed)
  const speedError =
    target.kind === 'route' &&
    (!lineSpeed || !Number.isFinite(speedValue) || speedValue < 5 || speedValue > 160)
  const dwellValue = Number(dwell)
  const dwellError =
    target.kind === 'route' &&
    target.reversalCount > 0 &&
    (!dwell || !Number.isFinite(dwellValue) || dwellValue < 0 || dwellValue > 120)
  const valid = values && trainErrors.every((item) => !item) && !speedError && !dwellError

  return (
    <aside className="running-time-tool" aria-label="Tiempo de recorrido">
      <p className="label">Operaciones</p>
      <h2>Tiempo de recorrido</h2>
      <p className="operations-field-label">Tren</p>
      <div className="operations-presets">
        {presets.map((preset) => (
          <button
            key={preset.id}
            type="button"
            className="button-secondary"
            aria-pressed={selected === preset.id}
            disabled={busy}
            onClick={() => {
              setSelected(preset.id)
              setValues(toFields(preset))
            }}
          >
            {preset.name}
          </button>
        ))}
      </div>
      <p className="detail-hint">Valores de referencia, a validar.</p>
      {fields.map((field, index) => (
        <div className="operations-field" key={field.key}>
          <label htmlFor={`running-${field.key}`}>
            {field.label} ({field.unit})
          </label>
          <input
            id={`running-${field.key}`}
            className="data"
            type="number"
            min={field.min}
            max={field.max}
            step={field.step}
            disabled={busy}
            value={values?.[field.key] ?? ''}
            onChange={(event) => {
              setSelected(null)
              setValues({ ...values!, [field.key]: event.target.value })
            }}
          />
          {trainErrors[index] && <p className="operations-error">{trainErrors[index]}</p>}
        </div>
      ))}
      {target.kind === 'route' && (
        <>
          <div className="operations-field">
            <label htmlFor="running-line-speed">Velocidad de la vía (km/h)</label>
            <input
              id="running-line-speed"
              className="data"
              type="number"
              min="5"
              max="160"
              value={lineSpeed}
              disabled={busy}
              onChange={(event) => setLineSpeed(event.target.value)}
            />
            {speedError && <p className="operations-error">Ingresá entre 5 y 160 km/h.</p>}
            <p className="detail-hint">
              OSM no informa la velocidad de estas vías: se usa este valor en toda la ruta.
            </p>
          </div>
          {target.reversalCount > 0 && (
            <div className="operations-field">
              <label htmlFor="running-dwell">Minutos detenido en cada inversión</label>
              <input
                id="running-dwell"
                className="data"
                type="number"
                min="0"
                max="120"
                value={dwell}
                disabled={busy}
                onChange={(event) => setDwell(event.target.value)}
              />
              {dwellError && <p className="operations-error">Ingresá entre 0 y 120 minutos.</p>}
              <p className="detail-hint">
                Cambio de cabina. Sin dato de referencia: por defecto no se cuenta.
              </p>
            </div>
          )}
        </>
      )}
      <div className="operations-actions">
        <button
          type="button"
          className="button-primary"
          disabled={!valid || busy}
          onClick={() =>
            onCalculate(
              {
                name: selected
                  ? (presets.find((preset) => preset.id === selected)?.name ?? 'Tren personalizado')
                  : 'Tren personalizado',
                lengthMeters: Number(values!.lengthMeters),
                maxSpeedKmh: Number(values!.maxSpeedKmh),
                accelerationMetersPerSecondSquared: Number(
                  values!.accelerationMetersPerSecondSquared,
                ),
                brakingMetersPerSecondSquared: Number(values!.brakingMetersPerSecondSquared),
              },
              speedValue,
              dwellValue,
            )
          }
        >
          Calcular
        </button>
        <button type="button" className="button-secondary" onClick={onCancel}>
          Cancelar
        </button>
      </div>
      {busy && <p role="status">Calculando tiempo de recorrido…</p>}
      {error && (
        <p className="operations-error" role="alert">
          {error}
        </p>
      )}
    </aside>
  )
}

function toFields(train: Train): Record<Field, string> {
  return {
    lengthMeters: String(train.lengthMeters),
    maxSpeedKmh: String(train.maxSpeedKmh),
    accelerationMetersPerSecondSquared: String(train.accelerationMetersPerSecondSquared),
    brakingMetersPerSecondSquared: String(train.brakingMetersPerSecondSquared),
  }
}
