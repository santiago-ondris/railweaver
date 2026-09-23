import {
  Cartesian3,
  GeometryInstance,
  GroundPolylineGeometry,
  GroundPolylinePrimitive,
  HeightReference,
  Material,
  PolylineMaterialAppearance,
  SceneTransforms,
  type Entity,
  type Viewer,
} from 'cesium'
import { greatCircleDistance, type Coordinate } from '../shared/geo'
import { tokenColor } from '../styles/tokens'
import type { CandidateCorridor } from './api'

export class CandidateCorridorLayer {
  private readonly viewer: Viewer
  private readonly casing: GroundPolylinePrimitive
  private readonly line: GroundPolylinePrimitive
  private readonly warnings: GroundPolylinePrimitive | null
  private readonly labels: Array<{ entity: Entity; position: Cartesian3; speed: number }> = []
  private readonly removeCameraListener: () => void
  private readonly pickId = { kind: 'candidate-corridor' }

  constructor(viewer: Viewer, corridor: CandidateCorridor) {
    this.viewer = viewer
    const alignment = corridor.alignment
    const positions = Cartesian3.fromDegreesArray(
      alignment.flatMap(({ longitude, latitude }) => [longitude, latitude]),
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
    this.casing = addLine(7, tokenColor('map-ground'))
    this.line = addLine(3, tokenColor('primary'))
    const distances = [0]
    for (let i = 1; i < alignment.length; i++)
      distances.push(distances[i - 1] + greatCircleDistance(alignment[i - 1], alignment[i]))
    const slow = corridor.sections.filter(
      (section) =>
        section.kind === 'curve' && section.speedLimitKmh < corridor.metrics.designSpeedKmh,
    )
    const instances = slow
      .map((section) => {
        const vertices: Coordinate[] = []
        for (let i = 0; i < distances.length; i++)
          if (distances[i] >= section.fromMeters - 0.02 && distances[i] <= section.toMeters + 0.02)
            vertices.push(alignment[i])
        if (section.curveMidpoint) {
          const rootStyle = getComputedStyle(document.documentElement)
          const position = Cartesian3.fromDegrees(
            section.curveMidpoint.longitude,
            section.curveMidpoint.latitude,
          )
          this.labels.push({
            position,
            speed: section.speedLimitKmh,
            entity: viewer.entities.add({
              position,
              label: {
                text: `△ R ${Math.round(section.radiusMeters ?? 0)} m · ${Math.floor(section.speedLimitKmh)} km/h`,
                font: `${rootStyle.getPropertyValue('--rw-text-data-size')} ${rootStyle.getPropertyValue('--rw-font-data')}`,
                fillColor: tokenColor('warning-text'),
                heightReference: HeightReference.CLAMP_TO_GROUND,
                showBackground: true,
                backgroundColor: tokenColor('map-ground'),
              },
              properties: { corridorPick: this.pickId },
            }),
          })
        }
        return vertices.length >= 2
          ? new GeometryInstance({
              id: this.pickId,
              geometry: new GroundPolylineGeometry({
                positions: Cartesian3.fromDegreesArray(
                  vertices.flatMap(({ longitude, latitude }) => [longitude, latitude]),
                ),
                width: 4,
              }),
            })
          : null
      })
      .filter((instance): instance is GeometryInstance => instance !== null)
    this.warnings = instances.length
      ? viewer.scene.groundPrimitives.add(
          new GroundPolylinePrimitive({
            geometryInstances: instances,
            appearance: new PolylineMaterialAppearance({
              material: Material.fromType('Color', { color: tokenColor('warning') }),
            }),
          }),
        )
      : null
    this.removeCameraListener = viewer.camera.changed.addEventListener(() => this.updateLabels())
    this.updateLabels()
    viewer.scene.requestRender()
  }

  private updateLabels() {
    const { camera, scene } = this.viewer
    const occupied: Array<{ x: number; y: number }> = []
    const showDetails = camera.positionCartographic.height < 25_000
    for (const label of [...this.labels].sort((a, b) => a.speed - b.speed)) {
      const screen = showDetails
        ? SceneTransforms.worldToWindowCoordinates(scene, label.position)
        : undefined
      const visible =
        !!screen &&
        screen.x >= 0 &&
        screen.y >= 0 &&
        screen.x <= scene.canvas.clientWidth &&
        screen.y <= scene.canvas.clientHeight &&
        !occupied.some(
          (point) => Math.abs(point.x - screen.x) < 200 && Math.abs(point.y - screen.y) < 28,
        )
      label.entity.show = visible
      if (visible && screen) occupied.push({ x: screen.x, y: screen.y })
    }
    scene.requestRender()
  }

  isPicked(id: unknown) {
    return id === this.pickId || this.labels.some((label) => label.entity === id)
  }

  destroy() {
    if (this.viewer.isDestroyed()) return
    this.removeCameraListener()
    if (this.warnings) this.viewer.scene.groundPrimitives.remove(this.warnings)
    this.viewer.scene.groundPrimitives.remove(this.line)
    this.viewer.scene.groundPrimitives.remove(this.casing)
    for (const label of this.labels) this.viewer.entities.remove(label.entity)
    this.viewer.scene.requestRender()
  }
}
