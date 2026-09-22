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
import { ExistingRailwayLayer } from './ExistingRailwayLayer'
import type { RailwaySelection } from './ExistingRailwayLayer'
import type { Railway, TrackGauge } from './railways'
import type { Region } from './regions'
import { tokenColor } from './styles/tokens'

type GeographicViewerProps = {
  region: Region
  railway: Railway
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

export function GeographicViewer({ region, railway }: GeographicViewerProps) {
  const containerRef = useRef<HTMLDivElement>(null)
  const viewerRef = useRef<Viewer | null>(null)
  const imageryLayerRef = useRef<ImageryLayer | null>(null)
  const outlineEntityRef = useRef<Entity | null>(null)
  const railwayLayerRef = useRef<ExistingRailwayLayer | null>(null)
  const viewerErrorRef = useRef<HTMLDivElement>(null)
  const viewerErrorMessageRef = useRef<HTMLSpanElement>(null)
  const [imageryVisible, setImageryVisible] = useState(true)
  const [outlineVisible, setOutlineVisible] = useState(true)
  const [railwayVisible, setRailwayVisible] = useState(true)
  const [selection, setSelection] = useState<RailwaySelection | null>(null)

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
      railwayLayerRef.current = new ExistingRailwayLayer(viewer, railway, setSelection)
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
      railwayLayerRef.current?.destroy()
      railwayLayerRef.current = null
      viewerRef.current = null

      if (viewer && !viewer.isDestroyed()) {
        viewer.destroy()
      }
    }
  }, [region, railway])

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

  useEffect(() => {
    if (railwayLayerRef.current) {
      railwayLayerRef.current.show = railwayVisible
    }
  }, [railwayVisible])

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
              aria-pressed={railwayVisible}
              onClick={() => {
                setRailwayVisible((visible) => !visible)
                if (railwayVisible) setSelection(null)
              }}
            >
              <span className="layer-box" aria-hidden="true" />
              <span className="layer-text">
                <span className="layer-name">Vías existentes</span>
                <span className="layer-detail">
                  {railway.tracks.length.toLocaleString('es-AR')} tramos ·{' '}
                  {railway.stations.length.toLocaleString('es-AR')} estaciones
                </span>
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

      <RailwayDetailPanel railway={railway} selection={selection} onClear={() => setSelection(null)} />
    </>
  )
}

type RailwayDetailPanelProps = {
  railway: Railway
  selection: RailwaySelection | null
  onClear: () => void
}

function RailwayDetailPanel({ railway, selection, onClear }: RailwayDetailPanelProps) {
  if (!selection) {
    return (
      <aside className="detail-panel" aria-label="Detalle ferroviario">
        <p className="label">Infraestructura existente</p>
        <h2>Red ferroviaria de Córdoba</h2>
        <p className="detail-provenance">
          Datos extraídos el <span className="data">{railway.source.extractedOn}</span>
        </p>
        <dl className="detail-list">
          <DetailRow label="Tramos" value={railway.tracks.length.toLocaleString('es-AR')} />
          <DetailRow label="Estaciones" value={railway.stations.length.toLocaleString('es-AR')} />
          <DetailRow label="Licencia" value={railway.source.license} />
        </dl>
        <p className="detail-hint">Seleccioná una vía o una estación en el mapa para inspeccionarla.</p>
      </aside>
    )
  }

  const isTrack = selection.kind === 'track'
  const title = isTrack
    ? selection.item.name ?? selection.item.lineReference ?? 'Tramo sin nombre'
    : selection.item.name

  return (
    <aside className="detail-panel" aria-label="Detalle ferroviario">
      <p className="detail-identifier">{selection.item.id}</p>
      <p className="label">{isTrack ? 'Tramo de vía' : stationTypeLabel(selection.item.type)}</p>
      <h2>{title}</h2>
      <p className="detail-provenance">{railway.source.attribution}</p>

      <dl className="detail-list">
        {isTrack ? (
          <>
            <DetailRow label="Estado" value={trackStatusLabel(selection.item.status)} />
            <DetailRow label="Uso" value={trackUsageLabel(selection.item.usage)} />
            {selection.item.lineReference && (
              <DetailRow label="Ramal" value={selection.item.lineReference} />
            )}
          </>
        ) : (
          <DetailRow label="Tipo" value={stationTypeLabel(selection.item.type)} />
        )}
        <DetailRow label="Trocha" value={gaugeLabel(selection.item.gauge)} />
        <DetailRow
          label="Confianza"
          value={gaugeConfidenceLabel(selection.item.gauge, selection.item.gaugeInferred)}
        />
      </dl>

      <button type="button" className="button-secondary detail-close" onClick={onClear}>
        Cerrar detalle
      </button>
    </aside>
  )
}

function DetailRow({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt>{label}</dt>
      <dd>{value}</dd>
    </div>
  )
}

function gaugeLabel(gauge: TrackGauge): string {
  return gauge.widthMillimetres === 0
    ? 'Sin dato'
    : `${gauge.widthMillimetres.toLocaleString('es-AR')} mm`
}

function gaugeConfidenceLabel(gauge: TrackGauge, inferred: boolean): string {
  if (gauge.widthMillimetres === 0) return 'No disponible'
  return inferred ? 'Inferida por ramal u operador' : 'Leída de OpenStreetMap'
}

function trackStatusLabel(status: Railway['tracks'][number]['status']): string {
  return { Active: 'Activa', Disused: 'En desuso', Abandoned: 'Abandonada' }[status]
}

function trackUsageLabel(usage: Railway['tracks'][number]['usage']): string {
  return {
    Unknown: 'Sin clasificar',
    MainLine: 'Línea principal',
    BranchLine: 'Ramal',
    Siding: 'Apartadero',
    Yard: 'Playa de maniobras',
    IndustrialSpur: 'Desvío industrial',
  }[usage]
}

function stationTypeLabel(type: Railway['stations'][number]['type']): string {
  return { Station: 'Estación', Halt: 'Apeadero', Junction: 'Empalme' }[type]
}
