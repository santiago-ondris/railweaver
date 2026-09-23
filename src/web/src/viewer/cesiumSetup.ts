import {
  ArcType,
  Cartesian3,
  Cartographic,
  Credit,
  CustomHeightmapTerrainProvider,
  GeographicTilingScheme,
  HeadingPitchRange,
  ImageryLayer,
  Math as CesiumMath,
  Matrix4,
  OpenStreetMapImageryProvider,
  Viewer,
  type Cartesian2,
  type Entity,
} from 'cesium'
import { fetchTerrainTile, terrainTileSize } from '../elevation/api'
import type { Region } from '../regions/api'
import type { Coordinate } from '../shared/geo'
import { tokenColor } from '../styles/tokens'

const osmTileUrl = import.meta.env.VITE_OSM_TILE_URL ?? 'https://tile.openstreetmap.org/'
const terrainAttribution = 'Copernicus WorldDEM-30 · DLR/Airbus · Unión Europea/ESA'
const neutralBasemap = { saturation: 0.18, brightness: 1.06, contrast: 0.82, gamma: 1.12 } as const

/** OpenStreetMap basemap, desaturated so railway data reads on top (DESIGN.md › Map). */
export function createBasemapLayer() {
  return new ImageryLayer(
    new OpenStreetMapImageryProvider({
      url: osmTileUrl,
      credit: new Credit(
        '<a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener noreferrer">© OpenStreetMap contributors</a>',
        true,
      ),
    }),
    neutralBasemap,
  )
}

/** Region terrain served by the API; reports per tile whether terrain data exists. */
export function createTerrainProvider(
  regionId: string,
  onAvailability: (available: boolean) => void,
) {
  return new CustomHeightmapTerrainProvider({
    width: terrainTileSize,
    height: terrainTileSize,
    tilingScheme: new GeographicTilingScheme(),
    credit: new Credit(terrainAttribution, true),
    callback: async (x, y, level) => {
      const tile = await fetchTerrainTile(regionId, level, x, y)
      onAvailability(tile !== null)
      return tile ?? new Float32Array(terrainTileSize * terrainTileSize)
    },
  })
}

/** A Cesium viewer without its stock widgets, styled with the design tokens. */
export function createViewer(
  container: HTMLElement,
  baseLayer: ImageryLayer,
  terrainProvider: CustomHeightmapTerrainProvider,
) {
  const viewer = new Viewer(container, {
    animation: false,
    baseLayer,
    baseLayerPicker: false,
    fullscreenButton: false,
    geocoder: false,
    homeButton: false,
    infoBox: false,
    navigationHelpButton: false,
    projectionPicker: false,
    requestRenderMode: true,
    scene3DOnly: true,
    sceneModePicker: false,
    selectionIndicator: false,
    terrainProvider,
    timeline: false,
  })
  const { scene } = viewer
  scene.backgroundColor = tokenColor('ground')
  scene.globe.baseColor = tokenColor('map-ground')
  scene.globe.enableLighting = true
  scene.globe.showGroundAtmosphere = false
  if (scene.skyBox) scene.skyBox.show = false
  if (scene.skyAtmosphere) scene.skyAtmosphere.show = false
  if (scene.sun) scene.sun.show = false
  if (scene.moon) scene.moon.show = false
  return viewer
}

export function addRegionOutline(viewer: Viewer, region: Region): Entity {
  const { west, south, east, north } = region.boundingBox
  return viewer.entities.add({
    name: `Área de referencia: ${region.name}`,
    polyline: {
      positions: Cartesian3.fromDegreesArray([
        west,
        south,
        east,
        south,
        east,
        north,
        west,
        north,
        west,
        south,
      ]),
      arcType: ArcType.RHUMB,
      clampToGround: true,
      material: tokenColor('primary'),
      width: 1.5,
    },
  })
}

export function frameRegion(viewer: Viewer, region: Region) {
  viewer.camera.lookAt(
    Cartesian3.fromDegrees(region.initialCamera.longitude, region.initialCamera.latitude),
    new HeadingPitchRange(
      CesiumMath.toRadians(315),
      CesiumMath.toRadians(-55),
      region.initialCamera.heightMeters,
    ),
  )
  viewer.camera.lookAtTransform(Matrix4.IDENTITY)
}

/** The terrain coordinate under a screen position, or null when it misses the globe. */
export function pickCoordinate(viewer: Viewer, position: Cartesian2): Coordinate | null {
  const { scene } = viewer
  const ray = viewer.camera.getPickRay(position)
  const point = ray ? scene.globe.pick(ray, scene) : undefined
  if (!point) return null
  const cartographic = Cartographic.fromCartesian(point)
  return {
    latitude: CesiumMath.toDegrees(cartographic.latitude),
    longitude: CesiumMath.toDegrees(cartographic.longitude),
  }
}
