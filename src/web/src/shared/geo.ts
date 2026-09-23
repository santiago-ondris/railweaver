export type Coordinate = { latitude: number; longitude: number }

/** Haversine distance in metres on the mean Earth sphere (IUGG radius). */
export function greatCircleDistance(a: Coordinate, b: Coordinate) {
  const rad = Math.PI / 180
  const lat = (b.latitude - a.latitude) * rad
  const lon = (b.longitude - a.longitude) * rad
  const value =
    Math.sin(lat / 2) ** 2 +
    Math.cos(a.latitude * rad) * Math.cos(b.latitude * rad) * Math.sin(lon / 2) ** 2
  return 2 * 6371008.8 * Math.asin(Math.min(1, Math.sqrt(value)))
}
