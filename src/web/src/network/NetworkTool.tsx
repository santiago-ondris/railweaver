import { useState } from 'react'
import type { RailwayStation } from '../railways/api'
import { stationTypeLabel } from '../railways/labels'
import type { RouteResponse } from './api'
import './NetworkTool.css'

function normalize(value: string) {
  return value
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLocaleLowerCase('es-AR')
}

function StationField({
  label,
  stations,
  selected,
  onSelect,
}: {
  label: string
  stations: RailwayStation[]
  selected: RailwayStation | null
  onSelect: (station: RailwayStation | null) => void
}) {
  const [query, setQuery] = useState('')
  const [focused, setFocused] = useState(false)
  const text = selected?.name ?? query
  const suggestions = text
    ? stations
        .filter((station) => normalize(station.name).includes(normalize(text)))
        .sort((a, b) => a.name.localeCompare(b.name, 'es-AR'))
        .slice(0, 8)
    : []
  return (
    <div className="network-field">
      <label htmlFor={`network-${label}`}>{label}</label>
      <input
        id={`network-${label}`}
        value={text}
        autoComplete="off"
        onFocus={() => setFocused(true)}
        onBlur={() => window.setTimeout(() => setFocused(false), 100)}
        onChange={(event) => {
          setQuery(event.target.value)
          onSelect(null)
        }}
        aria-expanded={focused && suggestions.length > 0}
        aria-controls={`network-${label}-suggestions`}
      />
      {focused && suggestions.length > 0 && (
        <ul id={`network-${label}-suggestions`} className="network-suggestions" role="listbox">
          {suggestions.map((station) => (
            <li key={station.id} role="option" aria-selected={selected?.id === station.id}>
              <button
                type="button"
                onMouseDown={(event) => event.preventDefault()}
                onClick={() => {
                  onSelect(station)
                  setFocused(false)
                }}
              >
                <span>{station.name}</span>
                <small>{stationTypeLabel(station.type)}</small>
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}

export function NetworkTool({
  stations,
  origin,
  destination,
  onOrigin,
  onDestination,
  includeDisused,
  onIncludeDisused,
  busy,
  unreachable,
  error,
  onSearch,
  onCancel,
}: {
  stations: RailwayStation[]
  origin: RailwayStation | null
  destination: RailwayStation | null
  onOrigin: (station: RailwayStation | null) => void
  onDestination: (station: RailwayStation | null) => void
  includeDisused: boolean
  onIncludeDisused: (include: boolean) => void
  busy: boolean
  unreachable: RouteResponse | null
  error: string | null
  onSearch: (includeDisused?: boolean) => void
  onCancel: () => void
}) {
  const same = origin && destination && origin.id === destination.id
  const alert =
    unreachable?.status === 'unreachable'
      ? explain(
          unreachable,
          origin?.name ?? 'Origen',
          destination?.name ?? 'Destino',
          includeDisused,
        )
      : null
  return (
    <aside className="network-tool" aria-label="Ruta por la red">
      <p className="label">Planificación</p>
      <h2>Ruta por la red</h2>
      <p className="detail-hint">Elegí una estación en el mapa o escribí su nombre.</p>
      <StationField label="Origen" stations={stations} selected={origin} onSelect={onOrigin} />
      <StationField
        label="Destino"
        stations={stations}
        selected={destination}
        onSelect={onDestination}
      />
      {same && (
        <p className="network-error" role="alert">
          Origen y destino deben ser estaciones distintas.
        </p>
      )}
      <label className="network-checkbox">
        <input
          type="checkbox"
          checked={includeDisused}
          onChange={(event) => onIncludeDisused(event.target.checked)}
        />
        Incluir vías en desuso
      </label>
      <p className="detail-hint">Las vías abandonadas nunca se usan: ya no tienen rieles.</p>
      <div className="network-actions">
        <button
          type="button"
          className="button-primary"
          disabled={!origin || !destination || !!same || busy}
          onClick={() => onSearch()}
        >
          Buscar ruta
        </button>
        <button type="button" className="button-secondary" onClick={onCancel}>
          Cancelar
        </button>
      </div>
      {busy && <p role="status">Buscando ruta…</p>}
      {alert && (
        <div className="alert alert-alarm" role="alert">
          <p className="alert-title">
            <span className="glyph glyph-alarm" aria-hidden="true" />
            {alert.title}
          </p>
          <p>Por qué: {alert.why}</p>
          <p>Qué probar: {alert.next}</p>
          {unreachable?.status === 'unreachable' && unreachable.reachableWithDisused && (
            <button
              type="button"
              className="network-action-chip"
              onClick={() => {
                onIncludeDisused(true)
                onSearch(true)
              }}
            >
              ◆ Hay ruta si se incluyen vías en desuso
            </button>
          )}
        </div>
      )}
      {error && (
        <div className="alert alert-alarm" role="alert">
          <p className="alert-title">
            <span className="glyph glyph-alarm" aria-hidden="true" />
            No se pudo buscar la ruta
          </p>
          <p>{error}</p>
        </div>
      )}
    </aside>
  )
}

function explain(
  result: Extract<RouteResponse, { status: 'unreachable' }>,
  origin: string,
  destination: string,
  includeDisused: boolean,
) {
  switch (result.reason) {
    case 'origin_without_track':
    case 'destination_without_track':
      return {
        title: `${result.reason === 'origin_without_track' ? origin : destination} no tiene vías utilizables a menos de 150 m`,
        why: includeDisused
          ? 'No hay vías cerca en los datos.'
          : 'Las vías cercanas pueden estar en desuso o abandonadas.',
        next: 'Probá con otra estación.',
      }
    case 'no_common_gauge':
      return {
        title: `${origin} y ${destination} están en redes de trocha distinta`,
        why: `${origin}: trocha ${result.originGauges.join(' / ')} mm. ${destination}: trocha ${result.destinationGauges.join(' / ')} mm. Un tren no puede pasar de una trocha a otra.`,
        next: 'Probá con estaciones de la misma red.',
      }
    case 'disconnected':
      return {
        title: `No hay continuidad de vía entre ${origin} y ${destination}`,
        why: 'La red está cortada entre ambas. Puede ser un corte real o un hueco de los datos de OSM.',
        next: 'Activá la capa Diagnóstico de red.',
      }
    case 'no_feasible_movement':
      return {
        title: 'Las vías se conectan, pero un tren no puede recorrerlas',
        why: 'En el camino hay uniones con ángulos mayores a 45° sin desvío para invertir la marcha.',
        next: 'Verificá Unión en ángulo cerrado en Diagnóstico de red.',
      }
  }
}
