import { Color } from 'cesium'

/**
 * Reads a DESIGN.md color token (a `--rw-color-*` CSS custom property) as a Cesium Color.
 * Cesium draws on a WebGL canvas and cannot use CSS variables directly, so this is the
 * single place where the renderer takes colors from the design system.
 */
export function tokenColor(token: string): Color {
  const value = getComputedStyle(document.documentElement)
    .getPropertyValue(`--rw-color-${token}`)
    .trim()

  if (!value) {
    throw new Error(`Design token --rw-color-${token} is not defined.`)
  }

  return Color.fromCssColorString(value)
}
