import {
  BillboardCollection,
  Cartesian3,
  Color,
  Material,
  PolylineCollection,
  ScreenSpaceEventHandler,
  ScreenSpaceEventType,
  type Viewer,
} from 'cesium'
import type { Railway, RailwayStation, TrackSegment } from './railways'
import { tokenColor } from './styles/tokens'

export type RailwaySelection =
  | { kind: 'track'; item: TrackSegment }
  | { kind: 'station'; item: RailwayStation }

type PickResult = {
  id?: unknown
}

export class ExistingRailwayLayer {
  private readonly viewer: Viewer
  private readonly polylines: PolylineCollection
  private readonly stations: BillboardCollection
  private readonly handler: ScreenSpaceEventHandler

  constructor(
    viewer: Viewer,
    railway: Railway,
    onSelect: (selection: RailwaySelection | null) => void,
  ) {
    this.viewer = viewer
    this.polylines = viewer.scene.primitives.add(new PolylineCollection())
    this.stations = viewer.scene.primitives.add(new BillboardCollection({ scene: viewer.scene }))

    for (const track of railway.tracks) {
      this.polylines.add({
        id: { kind: 'track', item: track } satisfies RailwaySelection,
        material: trackMaterial(track),
        positions: Cartesian3.fromDegreesArrayHeights(
          track.geometry.flatMap(({ longitude, latitude }) => [longitude, latitude, 12]),
        ),
        width: track.status === 'Active' ? 2 : 1.5,
      })
    }

    const stationImage = createStationImage()
    for (const station of railway.stations) {
      this.stations.add({
        id: { kind: 'station', item: station } satisfies RailwaySelection,
        image: stationImage,
        position: Cartesian3.fromDegrees(station.location.longitude, station.location.latitude, 20),
        width: 10,
        height: 10,
        disableDepthTestDistance: Number.POSITIVE_INFINITY,
      })
    }

    this.handler = new ScreenSpaceEventHandler(viewer.scene.canvas)
    this.handler.setInputAction((event: ScreenSpaceEventHandler.PositionedEvent) => {
      const picked = viewer.scene.pick(event.position) as PickResult | undefined
      onSelect(isRailwaySelection(picked?.id) ? picked.id : null)
    }, ScreenSpaceEventType.LEFT_CLICK)

    viewer.scene.requestRender()
  }

  set show(value: boolean) {
    this.polylines.show = value
    this.stations.show = value
    this.viewer.scene.requestRender()
  }

  destroy() {
    if (!this.handler.isDestroyed()) {
      this.handler.destroy()
    }
    if (!this.viewer.isDestroyed()) {
      this.viewer.scene.primitives.remove(this.polylines)
      this.viewer.scene.primitives.remove(this.stations)
    }
  }
}

function trackMaterial(track: TrackSegment): Material {
  if (track.status === 'Active') {
    return Material.fromType('Color', { color: tokenColor('map-rail-existing') })
  }

  const abandoned = track.status === 'Abandoned'
  return Material.fromType('PolylineDash', {
    color: tokenColor(abandoned ? 'ink-subtle' : 'ink-muted'),
    gapColor: Color.TRANSPARENT,
    dashLength: abandoned ? 6 : 12,
    dashPattern: abandoned ? 0b0001000100010001 : 0b1111000011110000,
  })
}

function createStationImage(): HTMLCanvasElement {
  const canvas = document.createElement('canvas')
  canvas.width = 12
  canvas.height = 12
  const context = canvas.getContext('2d')
  if (!context) {
    throw new Error('Could not create the station marker canvas.')
  }

  context.fillStyle = tokenColor('raised').toCssColorString()
  context.strokeStyle = tokenColor('ink').toCssColorString()
  context.lineWidth = 2
  context.fillRect(1, 1, 10, 10)
  context.strokeRect(1, 1, 10, 10)
  return canvas
}

function isRailwaySelection(value: unknown): value is RailwaySelection {
  if (!value || typeof value !== 'object' || !('kind' in value)) {
    return false
  }
  return value.kind === 'track' || value.kind === 'station'
}
