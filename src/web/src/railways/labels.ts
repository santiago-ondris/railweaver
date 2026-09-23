import type { RailwayStation, TrackGauge, TrackSegment } from './api'

export function gaugeLabel(gauge: TrackGauge) {
  return gauge.widthMillimetres === 0
    ? 'Sin dato'
    : `${gauge.widthMillimetres.toLocaleString('es-AR')} mm`
}

export function trackStatusLabel(status: TrackSegment['status']) {
  return { Active: 'Activa', Disused: 'En desuso', Abandoned: 'Abandonada' }[status]
}

export function trackUsageLabel(usage: TrackSegment['usage']) {
  return {
    Unknown: 'Sin clasificar',
    MainLine: 'Línea principal',
    BranchLine: 'Ramal',
    Siding: 'Apartadero',
    Yard: 'Playa de maniobras',
    IndustrialSpur: 'Desvío industrial',
  }[usage]
}

export function stationTypeLabel(type: RailwayStation['type']) {
  return { Station: 'Estación', Halt: 'Apeadero', Junction: 'Empalme' }[type]
}
