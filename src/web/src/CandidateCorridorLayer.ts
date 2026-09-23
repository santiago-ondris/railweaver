import {
  Cartesian3, GeometryInstance, GroundPolylineGeometry, GroundPolylinePrimitive,
  Material, PolylineMaterialAppearance, type Viewer,
} from 'cesium'
import type { Coordinate } from './elevation'
import { tokenColor } from './styles/tokens'

export class CandidateCorridorLayer {
  private readonly viewer: Viewer
  private readonly casing: GroundPolylinePrimitive
  private readonly line: GroundPolylinePrimitive
  private readonly pickId = { kind: 'candidate-corridor' }

  constructor(viewer: Viewer, alignment: Coordinate[]) {
    this.viewer = viewer
    const positions = Cartesian3.fromDegreesArray(
      alignment.flatMap(({ longitude, latitude }) => [longitude, latitude]),
    )
    const addLine = (width: number, color: ReturnType<typeof tokenColor>) =>
      viewer.scene.groundPrimitives.add(new GroundPolylinePrimitive({
        geometryInstances: new GeometryInstance({
          id: this.pickId,
          geometry: new GroundPolylineGeometry({ positions, width }),
        }),
        appearance: new PolylineMaterialAppearance({
          material: Material.fromType('Color', { color }),
        }),
      }))
    this.casing = addLine(7, tokenColor('map-ground'))
    this.line = addLine(3, tokenColor('primary'))
    viewer.scene.requestRender()
  }

  isPicked(id: unknown) { return id === this.pickId }

  destroy() {
    if (this.viewer.isDestroyed()) return
    this.viewer.scene.groundPrimitives.remove(this.line)
    this.viewer.scene.groundPrimitives.remove(this.casing)
    this.viewer.scene.requestRender()
  }
}
