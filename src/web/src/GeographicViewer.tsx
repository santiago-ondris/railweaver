import {
  Cartesian3,
  Color,
  Credit,
  EllipsoidTerrainProvider,
  ImageryLayer,
  Math as CesiumMath,
  OpenStreetMapImageryProvider,
  Rectangle,
  type Entity,
  Viewer,
} from 'cesium'
import { useEffect, useRef, useState } from 'react'
import type { Region } from './regions'

type GeographicViewerProps = {
  region: Region
}

const osmTileUrl = import.meta.env.VITE_OSM_TILE_URL ?? 'https://tile.openstreetmap.org/'

export function GeographicViewer({ region }: GeographicViewerProps) {
  const containerRef = useRef<HTMLDivElement>(null)
  const viewerRef = useRef<Viewer | null>(null)
  const imageryLayerRef = useRef<ImageryLayer | null>(null)
  const outlineEntityRef = useRef<Entity | null>(null)
  const viewerErrorRef = useRef<HTMLDivElement>(null)
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
        ),
      }),
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

      const outlineEntity = viewer.entities.add({
        name: `Área de referencia: ${region.name}`,
        rectangle: {
          coordinates: Rectangle.fromDegrees(
            region.boundingBox.west,
            region.boundingBox.south,
            region.boundingBox.east,
            region.boundingBox.north,
          ),
          fill: true,
          height: 0,
          material: Color.fromCssColorString('#f0a23a').withAlpha(0.08),
          outline: true,
          outlineColor: Color.fromCssColorString('#f0a23a'),
          outlineWidth: 3,
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

      if (viewerErrorRef.current) {
        viewerErrorRef.current.textContent =
          error instanceof Error ? error.message : 'No se pudo iniciar Cesium.'
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
    <section className="map-stage" aria-label={`Visor geográfico de ${region.name}`}>
      <div ref={containerRef} className="map-canvas" />

      <aside className="layer-panel" aria-label="Control de capas">
        <div className="panel-heading">
          <span className="eyebrow">Capas</span>
          <strong>Vista geográfica</strong>
        </div>

        <button
          type="button"
          className="layer-toggle"
          aria-pressed={imageryVisible}
          onClick={() => setImageryVisible((visible) => !visible)}
        >
          <span>
            <span className="layer-name">Mapa base</span>
            <span className="layer-detail">OpenStreetMap</span>
          </span>
          <span className="toggle-state">{imageryVisible ? 'Visible' : 'Oculto'}</span>
        </button>

        <button
          type="button"
          className="layer-toggle"
          aria-pressed={outlineVisible}
          onClick={() => setOutlineVisible((visible) => !visible)}
        >
          <span>
            <span className="layer-name">Región</span>
            <span className="layer-detail">Bounding box de {region.name}</span>
          </span>
          <span className="toggle-state">{outlineVisible ? 'Visible' : 'Oculto'}</span>
        </button>
      </aside>

      <div className="region-label">
        <span className="region-marker" aria-hidden="true" />
        <span>
          <span className="eyebrow">Región activa</span>
          <strong>{region.name}, Argentina</strong>
        </span>
      </div>

      <div ref={viewerErrorRef} className="viewer-error" role="alert" hidden />
    </section>
  )
}
