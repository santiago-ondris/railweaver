import type { Coordinate } from '../shared/geo'

export const formatNumber = (value: number, digits = 1) =>
  value.toLocaleString('es-AR', { minimumFractionDigits: digits, maximumFractionDigits: digits })

export const formatMeters = (value: number) => `${formatNumber(value, 2)} m`

export const formatDistance = (value: number) =>
  value >= 1000 ? `${formatNumber(value / 1000, 2)} km` : formatMeters(value)

export const formatPoint = (point: Coordinate) =>
  `${formatNumber(point.latitude, 5)}, ${formatNumber(point.longitude, 5)}`
