import {
  BillboardCollection,
  Cartesian3,
  GeometryInstance,
  GroundPolylineGeometry,
  GroundPolylinePrimitive,
  HeightReference,
  Material,
  PolylineMaterialAppearance,
  type Viewer,
} from 'cesium'
import type { Railway } from '../railways/api'
import { tokenColor } from '../styles/tokens'
import type { NetworkDiagnostic } from './api'
import { markerImage } from './NetworkRouteLayer'

export type DiagnosticPick = { kind: 'network-diagnostic'; item: NetworkDiagnostic }

export class NetworkDiagnosticLayer {
  private readonly viewer: Viewer
  private readonly markers: BillboardCollection
  private readonly unknownTracks: GroundPolylinePrimitive | null

  constructor(viewer: Viewer, diagnostics: NetworkDiagnostic[], railway: Railway) {
    this.viewer = viewer
    this.markers = viewer.scene.primitives.add(new BillboardCollection({ scene: viewer.scene }))
    const triangle = markerImage('triangle')
    const circle = markerImage('circle')
    for (const item of diagnostics) {
      const warning =
        item.kind === 'possible_data_gap' ||
        item.kind === 'sharp_joint' ||
        item.kind === 'station_without_track' ||
        item.kind === 'track_without_gauge'
      this.markers.add({
        id: { kind: 'network-diagnostic', item } satisfies DiagnosticPick,
        image: warning ? triangle : circle,
        position: Cartesian3.fromDegrees(item.location.longitude, item.location.latitude),
        heightReference: HeightReference.CLAMP_TO_GROUND,
        width: 16,
        height: 16,
        disableDepthTestDistance: Number.POSITIVE_INFINITY,
      })
    }
    const ids = new Set(
      diagnostics.filter((item) => item.kind === 'track_without_gauge').map((item) => item.trackId),
    )
    const instances = railway.tracks
      .filter((track) => ids.has(track.id))
      .map(
        (track) =>
          new GeometryInstance({
            geometry: new GroundPolylineGeometry({
              positions: Cartesian3.fromDegreesArray(
                track.geometry.flatMap((point) => [point.longitude, point.latitude]),
              ),
              width: 3,
            }),
          }),
      )
    this.unknownTracks = instances.length
      ? viewer.scene.groundPrimitives.add(
          new GroundPolylinePrimitive({
            geometryInstances: instances,
            appearance: new PolylineMaterialAppearance({
              material: Material.fromType('Color', { color: tokenColor('warning') }),
            }),
          }),
        )
      : null
    viewer.scene.requestRender()
  }

  static picked(id: unknown): NetworkDiagnostic | null {
    if (
      typeof id === 'object' &&
      id !== null &&
      'kind' in id &&
      id.kind === 'network-diagnostic' &&
      'item' in id
    )
      return id.item as NetworkDiagnostic
    return null
  }

  destroy() {
    if (this.viewer.isDestroyed()) return
    this.viewer.scene.primitives.remove(this.markers)
    if (this.unknownTracks) this.viewer.scene.groundPrimitives.remove(this.unknownTracks)
    this.viewer.scene.requestRender()
  }
}
