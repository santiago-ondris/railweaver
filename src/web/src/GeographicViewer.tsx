import {
  ArcType, Cartesian3, Cartographic, Credit, CustomHeightmapTerrainProvider,
  EllipsoidTerrainProvider, GeographicTilingScheme, ImageryLayer, Math as CesiumMath,
  HeadingPitchRange, Matrix4, OpenStreetMapImageryProvider, ScreenSpaceEventHandler, ScreenSpaceEventType,
  type Cartesian2, type Entity, type Viewer as CesiumViewer, Viewer,
} from 'cesium'
import { useEffect, useMemo, useRef, useState } from 'react'
import { ExistingRailwayLayer, type RailwaySelection } from './ExistingRailwayLayer'
import { fetchElevation, fetchElevationProfile, type Coordinate, type ElevationProfile } from './elevation'
import type { Railway, TrackGauge } from './railways'
import type { Region } from './regions'
import { tokenColor } from './styles/tokens'

type GeographicViewerProps = { region: Region; railway: Railway; onTerrainStatus: (status: 'loading' | 'available' | 'missing') => void }
const osmTileUrl = import.meta.env.VITE_OSM_TILE_URL ?? 'https://tile.openstreetmap.org/'
const terrainAttribution = 'Copernicus WorldDEM-30 · DLR/Airbus · Unión Europea/ESA'
const neutralBasemap = { saturation: 0.18, brightness: 1.06, contrast: 0.82, gamma: 1.12 } as const

export function GeographicViewer({ region, railway, onTerrainStatus }: GeographicViewerProps) {
  const containerRef = useRef<HTMLDivElement>(null)
  const viewerRef = useRef<CesiumViewer | null>(null)
  const imageryLayerRef = useRef<ImageryLayer | null>(null)
  const outlineEntityRef = useRef<Entity | null>(null)
  const profileEntityRef = useRef<Entity | null>(null)
  const railwayLayerRef = useRef<ExistingRailwayLayer | null>(null)
  const terrainProviderRef = useRef<CustomHeightmapTerrainProvider | null>(null)
  const profileActiveRef = useRef(false)
  const profileVerticesRef = useRef<Coordinate[]>([])
  const [imageryVisible, setImageryVisible] = useState(true)
  const [terrainVisible, setTerrainVisible] = useState(true)
  const [terrainUnavailable, setTerrainUnavailable] = useState(false)
  const [outlineVisible, setOutlineVisible] = useState(true)
  const [railwayVisible, setRailwayVisible] = useState(true)
  const [selection, setSelection] = useState<RailwaySelection | null>(null)
  const [clickedCoordinate, setClickedCoordinate] = useState<Coordinate | null>(null)
  const [elevation, setElevation] = useState<number | null | undefined>(undefined)
  const [profileActive, setProfileActive] = useState(false)
  const [profile, setProfile] = useState<ElevationProfile | null>(null)
  const [profileError, setProfileError] = useState<string | null>(null)

  useEffect(() => { profileActiveRef.current = profileActive }, [profileActive])

  useEffect(() => {
    const container = containerRef.current
    if (!container) return
    const imageryLayer = new ImageryLayer(new OpenStreetMapImageryProvider({
      url: osmTileUrl,
      credit: new Credit('<a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener noreferrer">© OpenStreetMap contributors</a>', true),
    }), neutralBasemap)
    const terrainProvider = new CustomHeightmapTerrainProvider({
      width: 65, height: 65, tilingScheme: new GeographicTilingScheme(), credit: new Credit(terrainAttribution, true),
      callback: async (x, y, level) => {
        const response = await fetch(`/api/regions/${encodeURIComponent(region.id)}/terrain/${level}/${x}/${y}`)
        if (!response.ok) {
          setTerrainUnavailable(true)
          onTerrainStatus('missing')
          setTerrainVisible(false)
          return new Float32Array(65 * 65)
        }
        setTerrainUnavailable(false)
        onTerrainStatus('available')
        return new Float32Array(await response.arrayBuffer())
      },
    })
    terrainProviderRef.current = terrainProvider
    let viewer: CesiumViewer | null = null
    let interaction: ScreenSpaceEventHandler | null = null
    try {
      viewer = new Viewer(container, {
        animation: false, baseLayer: imageryLayer, baseLayerPicker: false, fullscreenButton: false,
        geocoder: false, homeButton: false, infoBox: false, navigationHelpButton: false,
        projectionPicker: false, requestRenderMode: true, scene3DOnly: true, sceneModePicker: false,
        selectionIndicator: false, terrainProvider, timeline: false,
      })
      const { scene } = viewer
      scene.backgroundColor = tokenColor('ground'); scene.globe.baseColor = tokenColor('map-ground')
      scene.globe.enableLighting = true
      scene.globe.showGroundAtmosphere = false
      if (scene.skyBox) scene.skyBox.show = false
      if (scene.skyAtmosphere) scene.skyAtmosphere.show = false
      if (scene.sun) scene.sun.show = false
      if (scene.moon) scene.moon.show = false
      const { west, south, east, north } = region.boundingBox
      outlineEntityRef.current = viewer.entities.add({
        name: `Área de referencia: ${region.name}`,
        polyline: { positions: Cartesian3.fromDegreesArray([west, south, east, south, east, north, west, north, west, south]),
          arcType: ArcType.RHUMB, clampToGround: true, material: tokenColor('primary'), width: 1.5 },
      })
      viewer.camera.lookAt(
        Cartesian3.fromDegrees(region.initialCamera.longitude, region.initialCamera.latitude),
        new HeadingPitchRange(
          CesiumMath.toRadians(315),
          CesiumMath.toRadians(-55),
          region.initialCamera.heightMeters,
        ),
      )
      viewer.camera.lookAtTransform(Matrix4.IDENTITY)
      viewerRef.current = viewer; imageryLayerRef.current = imageryLayer
      railwayLayerRef.current = new ExistingRailwayLayer(viewer, railway, setSelection)
      interaction = new ScreenSpaceEventHandler(scene.canvas)
      const pickCoordinate = (position: Cartesian2): Coordinate | null => {
        const ray = viewer!.camera.getPickRay(position)
        const point = ray ? scene.globe.pick(ray, scene) : undefined
        if (!point) return null
        const cartographic = Cartographic.fromCartesian(point)
        return { latitude: CesiumMath.toDegrees(cartographic.latitude), longitude: CesiumMath.toDegrees(cartographic.longitude) }
      }
      const redrawProfile = () => {
        if (profileEntityRef.current) viewer!.entities.remove(profileEntityRef.current)
        if (profileVerticesRef.current.length > 0) {
          profileEntityRef.current = viewer!.entities.add({ polyline: {
            positions: Cartesian3.fromDegreesArray(profileVerticesRef.current.flatMap((item) => [item.longitude, item.latitude])),
            clampToGround: true, material: tokenColor('primary'), width: 3,
          } })
        }
      }
      const finishProfile = async () => {
        if (profileVerticesRef.current.length < 2) return
        setProfileActive(false); setProfileError(null)
        try { setProfile(await fetchElevationProfile(region.id, profileVerticesRef.current)) }
        catch { setProfileError('No se pudo calcular el perfil. Verificá que el dataset de elevación esté disponible.') }
      }
      interaction.setInputAction((event: ScreenSpaceEventHandler.PositionedEvent) => {
        const coordinate = pickCoordinate(event.position)
        if (!coordinate) return
        if (profileActiveRef.current) {
          const last = profileVerticesRef.current.at(-1)
          if (!last || Math.abs(last.latitude - coordinate.latitude) > 1e-8
            || Math.abs(last.longitude - coordinate.longitude) > 1e-8) {
            profileVerticesRef.current.push(coordinate); redrawProfile()
          }
          return
        }
        setClickedCoordinate(coordinate); setElevation(undefined)
        void fetchElevation(region.id, coordinate).then((result) => setElevation(result.elevation)).catch(() => setElevation(null))
      }, ScreenSpaceEventType.LEFT_CLICK)
      interaction.setInputAction(() => { void finishProfile() }, ScreenSpaceEventType.LEFT_DOUBLE_CLICK)
      const keyboard = (event: KeyboardEvent) => {
        if (!profileActiveRef.current) return
        if (event.key === 'Enter') void finishProfile()
        if (event.key === 'Escape') {
          profileVerticesRef.current = []; redrawProfile(); setProfileActive(false); setProfile(null)
        }
      }
      window.addEventListener('keydown', keyboard)
      return () => {
        window.removeEventListener('keydown', keyboard)
        interaction?.destroy(); railwayLayerRef.current?.destroy(); railwayLayerRef.current = null
        viewerRef.current = null
        if (viewer && !viewer.isDestroyed()) viewer.destroy()
      }
    } catch {
      onTerrainStatus('missing')
      if (viewer && !viewer.isDestroyed()) viewer.destroy()
      else if (!imageryLayer.isDestroyed()) imageryLayer.destroy()
    }
  }, [region, railway, onTerrainStatus])

  useEffect(() => { if (imageryLayerRef.current) imageryLayerRef.current.show = imageryVisible; viewerRef.current?.scene.requestRender() }, [imageryVisible])
  useEffect(() => { if (outlineEntityRef.current) outlineEntityRef.current.show = outlineVisible; viewerRef.current?.scene.requestRender() }, [outlineVisible])
  useEffect(() => { if (railwayLayerRef.current) railwayLayerRef.current.show = railwayVisible }, [railwayVisible])
  useEffect(() => {
    const viewer = viewerRef.current
    if (!viewer) return
    viewer.terrainProvider = terrainVisible && terrainProviderRef.current && !terrainUnavailable
      ? terrainProviderRef.current
      : new EllipsoidTerrainProvider()
    viewer.scene.requestRender()
  }, [terrainVisible, terrainUnavailable])

  const beginProfile = () => {
    profileVerticesRef.current = []; if (profileEntityRef.current) viewerRef.current?.entities.remove(profileEntityRef.current)
    profileEntityRef.current = null; setProfile(null); setProfileError(null); setProfileActive(true)
  }

  return <>
    <aside className="side-rail" aria-label="Control de capas">
      <h2 className="label">Capas</h2>
      <ul className="layer-list">
        <LayerToggle name="Mapa base" detail="OpenStreetMap, neutralizado" pressed={imageryVisible} toggle={() => setImageryVisible(!imageryVisible)} />
        <LayerToggle name="Relieve" detail={terrainUnavailable ? 'Dataset no disponible' : 'Copernicus DEM GLO-30'} pressed={terrainVisible} toggle={() => {
          setTerrainUnavailable(false); setTerrainVisible(!terrainVisible)
        }} />
        <LayerToggle name="Vías existentes" detail={`${railway.tracks.length.toLocaleString('es-AR')} tramos · ${railway.stations.length.toLocaleString('es-AR')} estaciones`} pressed={railwayVisible} toggle={() => setRailwayVisible(!railwayVisible)} />
        <LayerToggle name="Región" detail={`Área de referencia de ${region.name}`} pressed={outlineVisible} toggle={() => setOutlineVisible(!outlineVisible)} />
      </ul>
      <h2 className="label tool-heading">Herramientas</h2>
      <button type="button" className="button-secondary" aria-pressed={profileActive} onClick={beginProfile}>Perfil</button>
      {profileActive && <p className="tool-hint">Marcá vértices. Doble clic o Enter para terminar; Esc para cancelar.</p>}
    </aside>
    <section className="map-stage" aria-label={`Visor geográfico de ${region.name}`}><div ref={containerRef} className="map-canvas" /></section>
    <RailwayDetailPanel railway={railway} selection={selection} coordinate={clickedCoordinate} elevation={elevation} onClear={() => setSelection(null)} />
    {(profile || profileError) && <AnalysisDock profile={profile} error={profileError} />}
  </>
}

function LayerToggle({ name, detail, pressed, toggle }: { name: string; detail: string; pressed: boolean; toggle: () => void }) {
  return <li><button type="button" className="layer-toggle" aria-pressed={pressed} onClick={toggle}>
    <span className="layer-box" aria-hidden="true" /><span className="layer-text"><span className="layer-name">{name}</span><span className="layer-detail">{detail}</span></span>
  </button></li>
}

function RailwayDetailPanel({ railway, selection, coordinate, elevation, onClear }: {
  railway: Railway; selection: RailwaySelection | null; coordinate: Coordinate | null; elevation: number | null | undefined; onClear: () => void
}) {
  const title = selection ? (selection.kind === 'track' ? selection.item.name ?? selection.item.lineReference ?? 'Tramo sin nombre' : selection.item.name) : 'Punto del terreno'
  return <aside className="detail-panel" aria-label="Detalle ferroviario">
    <p className="label">{selection ? (selection.kind === 'track' ? 'Tramo de vía' : stationTypeLabel(selection.item.type)) : 'Inspección'}</p>
    <h2>{coordinate ? title : 'Red ferroviaria de Córdoba'}</h2>
    <p className="detail-provenance">{railway.source.attribution}</p>
    <dl className="detail-list">
      {coordinate && <><DetailRow label="Latitud" value={coordinate.latitude.toLocaleString('es-AR', { maximumFractionDigits: 5 })} />
        <DetailRow label="Longitud" value={coordinate.longitude.toLocaleString('es-AR', { maximumFractionDigits: 5 })} />
        <DetailRow label="Cota" value={elevation === undefined ? 'Consultando…' : elevation === null ? 'Sin dato' : `${elevation.toLocaleString('es-AR', { minimumFractionDigits: 2 })} m`} /></>}
      {selection?.kind === 'track' && <><DetailRow label="Estado" value={trackStatusLabel(selection.item.status)} /><DetailRow label="Uso" value={trackUsageLabel(selection.item.usage)} /><DetailRow label="Trocha" value={gaugeLabel(selection.item.gauge)} /></>}
      {selection?.kind === 'station' && <DetailRow label="Trocha" value={gaugeLabel(selection.item.gauge)} />}
      {!coordinate && <><DetailRow label="Tramos" value={railway.tracks.length.toLocaleString('es-AR')} /><DetailRow label="Estaciones" value={railway.stations.length.toLocaleString('es-AR')} /></>}
    </dl>
    {selection && <button type="button" className="button-secondary detail-close" onClick={onClear}>Cerrar detalle</button>}
  </aside>
}

function AnalysisDock({ profile, error }: { profile: ElevationProfile | null; error: string | null }) {
  const metrics = useMemo(() => profileMetrics(profile), [profile])
  if (!profile || !metrics) return <section className="analysis-dock"><p>{error ?? 'El perfil no contiene cotas disponibles.'}</p></section>
  const width = 720, height = 130, padding = 22
  const points = profile.samples.map((sample) => sample.elevationMeters === null ? null : {
    x: padding + sample.distanceMeters / profile.totalDistanceMeters * (width - padding * 2),
    y: height - padding - (sample.elevationMeters - metrics.min) / Math.max(1, metrics.max - metrics.min) * (height - padding * 2),
  })
  const paths: string[] = []; let current = ''
  for (const point of points) { if (!point) { if (current) paths.push(current); current = ''; continue }; current += `${current ? ' L' : 'M'} ${point.x} ${point.y}` }
  if (current) paths.push(current)
  return <section className="analysis-dock" aria-label="Perfil longitudinal">
    <div><p className="label">Perfil longitudinal</p><div className="profile-metrics">
      <span>Distancia <b>{formatDistance(profile.totalDistanceMeters)}</b></span><span>Cota mín. <b>{metrics.min.toLocaleString('es-AR')} m</b></span>
      <span>Cota máx. <b>{metrics.max.toLocaleString('es-AR')} m</b></span><span>Subida <b>{metrics.up.toLocaleString('es-AR')} m</b></span>
      <span>Bajada <b>{metrics.down.toLocaleString('es-AR')} m</b></span><span>Pendiente máx. <b>{metrics.gradient.toLocaleString('es-AR')} ‰</b></span>
    </div></div>
    <svg className="profile-chart" viewBox={`0 0 ${width} ${height}`} role="img" aria-label="Gráfico del perfil de elevación">
      <line x1={padding} y1={height - padding} x2={width - padding} y2={height - padding} />
      {paths.map((path, index) => <path key={index} d={path} />)}
      {points.some((point) => point === null) && <text x={width / 2} y={height / 2}>Tramo sin dato</text>}
    </svg>
  </section>
}

function profileMetrics(profile: ElevationProfile | null) {
  if (!profile) return null
  const elevations = profile.samples.flatMap((sample) => sample.elevationMeters === null ? [] : [sample.elevationMeters])
  if (elevations.length === 0) return null
  let up = 0, down = 0
  for (let index = 1; index < profile.samples.length; index++) {
    const a = profile.samples[index - 1].elevationMeters, b = profile.samples[index].elevationMeters
    if (a === null || b === null) continue
    if (b > a) up += b - a; else down += a - b
  }
  return { min: Math.min(...elevations), max: Math.max(...elevations), up: Math.round(up * 10) / 10,
    down: Math.round(down * 10) / 10, gradient: Math.max(...profile.samples.map((sample) => Math.abs(sample.gradientPermille ?? 0))) }
}

function DetailRow({ label, value }: { label: string; value: string }) { return <div><dt>{label}</dt><dd>{value}</dd></div> }
function formatDistance(meters: number) { return meters >= 1000 ? `${(meters / 1000).toLocaleString('es-AR', { maximumFractionDigits: 1 })} km` : `${Math.round(meters)} m` }
function gaugeLabel(gauge: TrackGauge) { return gauge.widthMillimetres === 0 ? 'Sin dato' : `${gauge.widthMillimetres.toLocaleString('es-AR')} mm` }
function trackStatusLabel(status: Railway['tracks'][number]['status']) { return { Active: 'Activa', Disused: 'En desuso', Abandoned: 'Abandonada' }[status] }
function trackUsageLabel(usage: Railway['tracks'][number]['usage']) { return { Unknown: 'Sin clasificar', MainLine: 'Línea principal', BranchLine: 'Ramal', Siding: 'Apartadero', Yard: 'Playa de maniobras', IndustrialSpur: 'Desvío industrial' }[usage] }
function stationTypeLabel(type: Railway['stations'][number]['type']) { return { Station: 'Estación', Halt: 'Apeadero', Junction: 'Empalme' }[type] }
