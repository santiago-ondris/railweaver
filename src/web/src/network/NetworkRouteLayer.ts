import {
  BillboardCollection,
  Cartesian2,
  Cartesian3,
  GeometryInstance,
  GroundPolylineGeometry,
  GroundPolylinePrimitive,
  HeightReference,
  Material,
  PolylineMaterialAppearance,
  type Viewer,
  type Entity,
} from 'cesium'
import { tokenColor } from '../styles/tokens'
import type { NetworkRoute } from './api'

export class NetworkRouteLayer {
  private readonly viewer: Viewer
  private readonly casing: GroundPolylinePrimitive
  private readonly line: GroundPolylinePrimitive
  private readonly markers: BillboardCollection
  private readonly labels: Entity[] = []
  private readonly pickId = { kind: 'network-route' }

  constructor(viewer: Viewer, route: NetworkRoute) {
    this.viewer = viewer
    const positions = Cartesian3.fromDegreesArray(
      route.geometry.flatMap((point) => [point.longitude, point.latitude]),
    )
    const addLine = (width: number, color: ReturnType<typeof tokenColor>) =>
      viewer.scene.groundPrimitives.add(
        new GroundPolylinePrimitive({
          geometryInstances: new GeometryInstance({
            id: this.pickId,
            geometry: new GroundPolylineGeometry({ positions, width }),
          }),
          appearance: new PolylineMaterialAppearance({
            material: Material.fromType('Color', { color }),
          }),
        }),
      )
    this.casing = addLine(9, tokenColor('map-ground'))
    this.line = addLine(5, tokenColor('ink'))
    this.markers = viewer.scene.primitives.add(new BillboardCollection({ scene: viewer.scene }))
    const square = markerImage('square')
    const triangle = markerImage('triangle')
    const style = getComputedStyle(document.documentElement)
    const font = `${style.getPropertyValue('--rw-text-body-size')} ${style.getPropertyValue('--rw-font-ui')}`
    const addLabel = (text: string, longitude: number, latitude: number, warning: boolean) => {
      this.labels.push(
        viewer.entities.add({
          position: Cartesian3.fromDegrees(longitude, latitude),
          label: {
            text,
            font,
            heightReference: HeightReference.CLAMP_TO_GROUND,
            fillColor: tokenColor(warning ? 'warning-text' : 'ink'),
            showBackground: true,
            backgroundColor: tokenColor('map-ground'),
            pixelOffset: new Cartesian2(0, -18),
          },
        }),
      )
    }
    for (const [text, point] of [
      ['Origen', route.originStop.location],
      ['Destino', route.destinationStop.location],
    ] as const) {
      this.markers.add({
        id: this.pickId,
        image: square,
        position: Cartesian3.fromDegrees(point.longitude, point.latitude),
        heightReference: HeightReference.CLAMP_TO_GROUND,
        width: 12,
        height: 12,
        disableDepthTestDistance: Number.POSITIVE_INFINITY,
      })
      addLabel(text, point.longitude, point.latitude, false)
    }
    for (const reversal of route.reversals) {
      this.markers.add({
        id: this.pickId,
        image: triangle,
        position: Cartesian3.fromDegrees(reversal.location.longitude, reversal.location.latitude),
        heightReference: HeightReference.CLAMP_TO_GROUND,
        width: 15,
        height: 15,
        disableDepthTestDistance: Number.POSITIVE_INFINITY,
      })
      addLabel('Invierte la marcha', reversal.location.longitude, reversal.location.latitude, true)
    }
    viewer.scene.requestRender()
  }

  isPicked(id: unknown) {
    return id === this.pickId || this.labels.includes(id as Entity)
  }

  destroy() {
    if (this.viewer.isDestroyed()) return
    this.viewer.scene.groundPrimitives.remove(this.line)
    this.viewer.scene.groundPrimitives.remove(this.casing)
    this.viewer.scene.primitives.remove(this.markers)
    for (const label of this.labels) this.viewer.entities.remove(label)
    this.viewer.scene.requestRender()
  }
}

export function markerImage(shape: 'triangle' | 'circle' | 'square'): HTMLCanvasElement {
  const canvas = document.createElement('canvas')
  canvas.width = 18
  canvas.height = 18
  const context = canvas.getContext('2d')
  if (!context) throw new Error('Could not create a network marker.')
  context.lineWidth = 2
  context.strokeStyle = tokenColor(
    shape === 'triangle' ? 'warning' : 'ink-subtle',
  ).toCssColorString()
  context.fillStyle = tokenColor('map-ground').toCssColorString()
  context.beginPath()
  if (shape === 'triangle') {
    context.moveTo(9, 2)
    context.lineTo(16, 15)
    context.lineTo(2, 15)
    context.closePath()
  } else if (shape === 'square') context.rect(3, 3, 12, 12)
  else context.arc(9, 9, 6, 0, Math.PI * 2)
  context.fill()
  context.stroke()
  return canvas
}
