import {
  ArcType,
  Cartesian3,
  Credit,
  EllipsoidTerrainProvider,
  ImageryLayer,
  Math as CesiumMath,
  OpenStreetMapImageryProvider,
  type Entity,
  Viewer,
} from 'cesium'
import { useEffect, useRef, useState } from 'react'
import type { Region } from './regions'
import { tokenColor } from './styles/tokens'

type GeographicViewerProps = {
  region: Region
}

const osmTileUrl = import.meta.env.VITE_OSM_TILE_URL ?? 'https://tile.openstreetmap.org/'

// Neutralizes the colorful OSM raster toward DESIGN.md `map-ground` (RW-002, decision 3).
// Tuned by eye; revisit when the basemap is replaced.
const neutralBasemap = {
  saturation: 0.18,
  brightness: 1.06,
  contrast: 0.82,
  gamma: 1.12,
} as const

export function GeographicViewer({ region }: GeographicViewerProps) {
  const containerRef = useRef<HTMLDivElement>(null)
  const viewerRef = useRef<Viewer | null>(null)
  const imageryLayerRef = useRef<ImageryLayer | null>(null)
  const outlineEntityRef = useRef<Entity | null>(null)
  const viewerErrorRef = useRef<HTMLDivElement>(null)
  const viewerErrorMessageRef = useRef<HTMLSpanElement>(null)
  const [imageryVisible, setImageryVisible] = useState(true)
  const [outlineVisible, setOutlineVisible] = useState(true)

  useEffect(() => {
    const container = containerRef.current
    if (!container) {
      return
    }

    const imageryLayer = new ImageryLayer(
      new OpenStreetMapImageryProvider({
        url: osmTileUrl,
        credit: new Credit(
          '<a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener noreferrer">© OpenStreetMap contributors</a>',
          // Always on screen, not only behind "Data attribution" (OSM attribution guidelines, RW-001).
          true,
        ),
      }),
      neutralBasemap,
    )

    let viewer: Viewer | null = null

    try {
      viewer = new Viewer(container, {
        animation: false,
        baseLayer: imageryLayer,
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
        terrainProvider: new EllipsoidTerrainProvider(),
        timeline: false,
      })

      const { scene } = viewer
      scene.backgroundColor = tokenColor('ground')
      scene.globe.baseColor = tokenColor('map-ground')
      scene.globe.showGroundAtmosphere = false
      if (scene.skyBox) scene.skyBox.show = false
      if (scene.skyAtmosphere) scene.skyAtmosphere.show = false
      if (scene.sun) scene.sun.show = false
      if (scene.moon) scene.moon.show = false

      const { west, south, east, north } = region.boundingBox
      const outlineEntity = viewer.entities.add({
        name: `Área de referencia: ${region.name}`,
        polyline: {
          // Ground-clamped rhumb lines follow parallels and meridians without z-fighting the globe.
          positions: Cartesian3.fromDegreesArray([
            west, south, east, south, east, north, west, north, west, south,
          ]),
          arcType: ArcType.RHUMB,
          clampToGround: true,
          material: tokenColor('primary'),
          width: 1.5,
        },
      })

      viewer.camera.setView({
        destination: Cartesian3.fromDegrees(
          region.initialCamera.longitude,
          region.initialCamera.latitude,
          region.initialCamera.heightMeters,
        ),
        orientation: {
          heading: 0,
          pitch: CesiumMath.toRadians(-90),
          roll: 0,
        },
      })

      viewerRef.current = viewer
      imageryLayerRef.current = imageryLayer
      outlineEntityRef.current = outlineEntity
    } catch (error) {
      if (viewer && !viewer.isDestroyed()) {
        viewer.destroy()
      } else if (!imageryLayer.isDestroyed()) {
        imageryLayer.destroy()
      }

      if (viewerErrorRef.current && viewerErrorMessageRef.current) {
        viewerErrorMessageRef.current.textContent =
          error instanceof Error ? error.message : 'Cesium no pudo crear el visor.'
        viewerErrorRef.current.hidden = false
      }
    }

    return () => {
      imageryLayerRef.current = null
      outlineEntityRef.current = null
      viewerRef.current = null

      if (viewer && !viewer.isDestroyed()) {
        viewer.destroy()
      }
    }
  }, [region])

  useEffect(() => {
    if (imageryLayerRef.current) {
      imageryLayerRef.current.show = imageryVisible
      viewerRef.current?.scene.requestRender()
    }
  }, [imageryVisible])

  useEffect(() => {
    if (outlineEntityRef.current) {
      outlineEntityRef.current.show = outlineVisible
      viewerRef.current?.scene.requestRender()
    }
  }, [outlineVisible])

  return (
    <>
      <aside className="side-rail" aria-label="Control de capas">
        <h2 className="label">Capas</h2>
        <ul className="layer-list">
          <li>
            <button
              type="button"
              className="layer-toggle"
              aria-pressed={imageryVisible}
              onClick={() => setImageryVisible((visible) => !visible)}
            >
              <span className="layer-box" aria-hidden="true" />
              <span className="layer-text">
                <span className="layer-name">Mapa base</span>
                <span className="layer-detail">OpenStreetMap, neutralizado</span>
              </span>
            </button>
          </li>
          <li>
            <button
              type="button"
              className="layer-toggle"
              aria-pressed={outlineVisible}
              onClick={() => setOutlineVisible((visible) => !visible)}
            >
              <span className="layer-box" aria-hidden="true" />
              <span className="layer-text">
                <span className="layer-name">Región</span>
                <span className="layer-detail">Área de referencia de {region.name}</span>
              </span>
            </button>
          </li>
        </ul>
      </aside>

      <section className="map-stage" aria-label={`Visor geográfico de ${region.name}`}>
        <div ref={containerRef} className="map-canvas" />

        <div ref={viewerErrorRef} className="alert alert-alarm viewer-error" role="alert" hidden>
          <p className="alert-title">
            <span className="glyph glyph-alarm" aria-hidden="true" />
            No se pudo iniciar el visor
          </p>
          <p>
            <b>Qué pasó:</b> <span ref={viewerErrorMessageRef} />
          </p>
          <p>
            <b>Cómo resolverlo:</b> verificá que el navegador tenga WebGL habilitado y recargá la página.
          </p>
        </div>
      </section>
    </>
  )
}
