import {
  ArcType,
  Cartesian3,
  Cartographic,
  Credit,
  CustomHeightmapTerrainProvider,
  EllipsoidTerrainProvider,
  GeographicTilingScheme,
  ImageryLayer,
  Math as CesiumMath,
  HeadingPitchRange,
  Matrix4,
  OpenStreetMapImageryProvider,
  ScreenSpaceEventHandler,
  ScreenSpaceEventType,
  type Cartesian2,
  type Entity,
  type Viewer as CesiumViewer,
  Viewer,
} from 'cesium'
import { useEffect, useRef, useState } from 'react'
import { fetchElevation, fetchElevationProfile, type ElevationProfile } from '../elevation/api'
import { ProfileDock } from '../elevation/ProfileDock'
import { fetchCorridor, type CorridorResponse } from '../planning/api'
import { CandidateCorridorLayer } from '../planning/CandidateCorridorLayer'
import { CorridorDetail } from '../planning/CorridorDetail'
import { CorridorProfile } from '../planning/CorridorProfile'
import { CorridorTool } from '../planning/CorridorTool'
import type { Railway } from '../railways/api'
import { ExistingRailwayLayer, type RailwaySelection } from '../railways/ExistingRailwayLayer'
import { RailwayDetailPanel } from '../railways/RailwayDetailPanel'
import type { Region } from '../regions/api'
import type { Coordinate } from '../shared/geo'
import { tokenColor } from '../styles/tokens'
import { LayerToggle } from './LayerToggle'
import './GeographicViewer.css'

type GeographicViewerProps = {
  region: Region
  railway: Railway
  onTerrainStatus: (status: 'loading' | 'available' | 'missing') => void
}
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
  const corridorActiveRef = useRef(false)
  const corridorPointsRef = useRef<{ origin: Coordinate | null; destination: Coordinate | null }>({
    origin: null,
    destination: null,
  })
  const corridorRequestRef = useRef<AbortController | null>(null)
  const corridorEntitiesRef = useRef<Entity[]>([])
  const corridorLayerRef = useRef<CandidateCorridorLayer | null>(null)
  const corridorSerialRef = useRef(0)
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
  const [corridorActive, setCorridorActive] = useState(false)
  const [corridorOrigin, setCorridorOrigin] = useState<Coordinate | null>(null)
  const [corridorDestination, setCorridorDestination] = useState<Coordinate | null>(null)
  const [corridorGradient, setCorridorGradient] = useState('15')
  const [corridorBusy, setCorridorBusy] = useState(false)
  const [corridorResult, setCorridorResult] = useState<CorridorResponse | null>(null)
  const [corridorSearchResult, setCorridorSearchResult] = useState<CorridorResponse | null>(null)
  const [corridorShown, setCorridorShown] = useState(false)
  const [corridorNumber, setCorridorNumber] = useState(0)
  const [corridorError, setCorridorError] = useState<string | null>(null)

  useEffect(() => {
    profileActiveRef.current = profileActive
  }, [profileActive])

  useEffect(() => {
    const container = containerRef.current
    if (!container) return
    const imageryLayer = new ImageryLayer(
      new OpenStreetMapImageryProvider({
        url: osmTileUrl,
        credit: new Credit(
          '<a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener noreferrer">© OpenStreetMap contributors</a>',
          true,
        ),
      }),
      neutralBasemap,
    )
    const terrainProvider = new CustomHeightmapTerrainProvider({
      width: 65,
      height: 65,
      tilingScheme: new GeographicTilingScheme(),
      credit: new Credit(terrainAttribution, true),
      callback: async (x, y, level) => {
        const response = await fetch(
          `/api/regions/${encodeURIComponent(region.id)}/terrain/${level}/${x}/${y}`,
        )
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
      const { west, south, east, north } = region.boundingBox
      outlineEntityRef.current = viewer.entities.add({
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
      viewer.camera.lookAt(
        Cartesian3.fromDegrees(region.initialCamera.longitude, region.initialCamera.latitude),
        new HeadingPitchRange(
          CesiumMath.toRadians(315),
          CesiumMath.toRadians(-55),
          region.initialCamera.heightMeters,
        ),
      )
      viewer.camera.lookAtTransform(Matrix4.IDENTITY)
      viewerRef.current = viewer
      imageryLayerRef.current = imageryLayer
      railwayLayerRef.current = new ExistingRailwayLayer(viewer, railway, setSelection)
      interaction = new ScreenSpaceEventHandler(scene.canvas)
      const pickCoordinate = (position: Cartesian2): Coordinate | null => {
        const ray = viewer!.camera.getPickRay(position)
        const point = ray ? scene.globe.pick(ray, scene) : undefined
        if (!point) return null
        const cartographic = Cartographic.fromCartesian(point)
        return {
          latitude: CesiumMath.toDegrees(cartographic.latitude),
          longitude: CesiumMath.toDegrees(cartographic.longitude),
        }
      }
      const redrawProfile = () => {
        if (profileEntityRef.current) viewer!.entities.remove(profileEntityRef.current)
        if (profileVerticesRef.current.length > 0) {
          profileEntityRef.current = viewer!.entities.add({
            polyline: {
              positions: Cartesian3.fromDegreesArray(
                profileVerticesRef.current.flatMap((item) => [item.longitude, item.latitude]),
              ),
              clampToGround: true,
              material: tokenColor('primary'),
              width: 3,
            },
          })
        }
      }
      const finishProfile = async () => {
        if (profileVerticesRef.current.length < 2) return
        setProfileActive(false)
        setProfileError(null)
        try {
          setProfile(await fetchElevationProfile(region.id, profileVerticesRef.current))
        } catch {
          setProfileError(
            'No se pudo calcular el perfil. Verificá que el dataset de elevación esté disponible.',
          )
        }
      }
      interaction.setInputAction((event: ScreenSpaceEventHandler.PositionedEvent) => {
        const coordinate = pickCoordinate(event.position)
        if (!coordinate) return
        if (profileActiveRef.current) {
          const last = profileVerticesRef.current.at(-1)
          if (
            !last ||
            Math.abs(last.latitude - coordinate.latitude) > 1e-8 ||
            Math.abs(last.longitude - coordinate.longitude) > 1e-8
          ) {
            profileVerticesRef.current.push(coordinate)
            redrawProfile()
          }
          return
        }
        if (corridorActiveRef.current) {
          if (corridorRequestRef.current) return
          const next =
            corridorPointsRef.current.destination || !corridorPointsRef.current.origin
              ? { origin: coordinate, destination: null }
              : { ...corridorPointsRef.current, destination: coordinate }
          corridorPointsRef.current = next
          setCorridorOrigin(next.origin)
          setCorridorDestination(next.destination)
          setCorridorError(null)
          setCorridorSearchResult(null)
          return
        }
        const picked = scene.pick(event.position) as { id?: unknown } | undefined
        if (
          corridorLayerRef.current?.isPicked(picked?.id) ||
          corridorEntitiesRef.current.includes(picked?.id as Entity)
        ) {
          setCorridorShown(true)
          return
        }
        setCorridorShown(false)
        setClickedCoordinate(coordinate)
        setElevation(undefined)
        void fetchElevation(region.id, coordinate)
          .then((result) => setElevation(result.elevation))
          .catch(() => setElevation(null))
      }, ScreenSpaceEventType.LEFT_CLICK)
      interaction.setInputAction(() => {
        void finishProfile()
      }, ScreenSpaceEventType.LEFT_DOUBLE_CLICK)
      const keyboard = (event: KeyboardEvent) => {
        if (event.key === 'Escape' && corridorActiveRef.current) {
          corridorActiveRef.current = false
          corridorRequestRef.current?.abort()
          corridorPointsRef.current = { origin: null, destination: null }
          setCorridorOrigin(null)
          setCorridorDestination(null)
          setCorridorActive(false)
          setCorridorBusy(false)
          setCorridorError(null)
          setCorridorSearchResult(null)
          return
        }
        if (!profileActiveRef.current) return
        if (event.key === 'Enter') void finishProfile()
        if (event.key === 'Escape') {
          profileVerticesRef.current = []
          redrawProfile()
          setProfileActive(false)
          setProfile(null)
        }
      }
      window.addEventListener('keydown', keyboard)
      return () => {
        window.removeEventListener('keydown', keyboard)
        corridorRequestRef.current?.abort()
        interaction?.destroy()
        railwayLayerRef.current?.destroy()
        railwayLayerRef.current = null
        corridorLayerRef.current?.destroy()
        corridorLayerRef.current = null
        viewerRef.current = null
        if (viewer && !viewer.isDestroyed()) viewer.destroy()
      }
    } catch {
      onTerrainStatus('missing')
      if (viewer && !viewer.isDestroyed()) viewer.destroy()
      else if (!imageryLayer.isDestroyed()) imageryLayer.destroy()
    }
  }, [region, railway, onTerrainStatus])

  useEffect(() => {
    if (imageryLayerRef.current) imageryLayerRef.current.show = imageryVisible
    viewerRef.current?.scene.requestRender()
  }, [imageryVisible])
  useEffect(() => {
    if (outlineEntityRef.current) outlineEntityRef.current.show = outlineVisible
    viewerRef.current?.scene.requestRender()
  }, [outlineVisible])
  useEffect(() => {
    if (railwayLayerRef.current) railwayLayerRef.current.show = railwayVisible
  }, [railwayVisible])
  useEffect(() => {
    const viewer = viewerRef.current
    if (!viewer) return
    viewer.terrainProvider =
      terrainVisible && terrainProviderRef.current && !terrainUnavailable
        ? terrainProviderRef.current
        : new EllipsoidTerrainProvider()
    viewer.scene.requestRender()
  }, [terrainVisible, terrainUnavailable])

  useEffect(() => {
    const viewer = viewerRef.current
    if (!viewer) return
    for (const entity of corridorEntitiesRef.current) viewer.entities.remove(entity)
    corridorEntitiesRef.current = []
    for (const [name, point] of [
      ['Origen', corridorOrigin],
      ['Destino', corridorDestination],
    ] as const) {
      if (!point) continue
      corridorEntitiesRef.current.push(
        viewer.entities.add({
          name,
          position: Cartesian3.fromDegrees(point.longitude, point.latitude),
          point: {
            pixelSize: 10,
            color: tokenColor('primary'),
            outlineColor: tokenColor('map-ground'),
            outlineWidth: 2,
          },
          label: {
            text: name,
            font: `${getComputedStyle(document.documentElement).getPropertyValue('--rw-text-body-size')} ${getComputedStyle(document.documentElement).getPropertyValue('--rw-font-ui')}`,
            fillColor: tokenColor('primary'),
            showBackground: true,
            backgroundColor: tokenColor('map-ground'),
          },
        }),
      )
    }
    viewer.scene.requestRender()
  }, [corridorOrigin, corridorDestination])

  useEffect(() => {
    const viewer = viewerRef.current
    if (!viewer) return
    corridorLayerRef.current?.destroy()
    corridorLayerRef.current = null
    const alignment = corridorResult?.corridor?.alignment
    if (alignment) corridorLayerRef.current = new CandidateCorridorLayer(viewer, alignment)
    viewer.scene.requestRender()
  }, [corridorResult])

  const beginProfile = () => {
    corridorActiveRef.current = false
    corridorRequestRef.current?.abort()
    setCorridorActive(false)
    setCorridorBusy(false)
    setCorridorShown(false)
    setCorridorOrigin(null)
    setCorridorDestination(null)
    corridorPointsRef.current = { origin: null, destination: null }
    profileVerticesRef.current = []
    if (profileEntityRef.current) viewerRef.current?.entities.remove(profileEntityRef.current)
    profileEntityRef.current = null
    setProfile(null)
    setProfileError(null)
    setProfileActive(true)
  }

  const beginCorridor = () => {
    profileActiveRef.current = false
    setProfileActive(false)
    setProfile(null)
    setProfileError(null)
    setCorridorShown(false)
    profileVerticesRef.current = []
    if (profileEntityRef.current) viewerRef.current?.entities.remove(profileEntityRef.current)
    profileEntityRef.current = null
    corridorActiveRef.current = true
    setCorridorActive(true)
    corridorPointsRef.current = { origin: null, destination: null }
    setCorridorOrigin(null)
    setCorridorDestination(null)
    setCorridorError(null)
    setCorridorSearchResult(null)
  }
  const cancelCorridor = () => {
    corridorRequestRef.current?.abort()
    corridorRequestRef.current = null
    corridorActiveRef.current = false
    corridorPointsRef.current = { origin: null, destination: null }
    setCorridorActive(false)
    setCorridorOrigin(null)
    setCorridorDestination(null)
    setCorridorError(null)
    setCorridorSearchResult(null)
    setCorridorBusy(false)
  }
  const generateCorridor = async () => {
    if (!corridorOrigin || !corridorDestination) return
    const controller = new AbortController()
    corridorRequestRef.current = controller
    setCorridorBusy(true)
    setCorridorError(null)
    setCorridorSearchResult(null)
    try {
      const result = await fetchCorridor(
        region.id,
        corridorOrigin,
        corridorDestination,
        Number(corridorGradient),
        controller.signal,
      )
      if (controller.signal.aborted) return
      if (result.status === 'found') {
        setCorridorResult(result)
        corridorSerialRef.current++
        setCorridorNumber(corridorSerialRef.current)
        setCorridorShown(true)
        setCorridorActive(false)
        corridorActiveRef.current = false
      } else {
        setCorridorSearchResult(result)
      }
    } catch (error) {
      if (!controller.signal.aborted)
        setCorridorError(error instanceof Error ? error.message : 'Error de red.')
    } finally {
      if (corridorRequestRef.current === controller) {
        corridorRequestRef.current = null
        setCorridorBusy(false)
      }
    }
  }

  return (
    <>
      <aside className="side-rail" aria-label="Control de capas">
        <h2 className="label">Capas</h2>
        <ul className="layer-list">
          <LayerToggle
            name="Mapa base"
            detail="OpenStreetMap, neutralizado"
            pressed={imageryVisible}
            toggle={() => setImageryVisible(!imageryVisible)}
          />
          <LayerToggle
            name="Relieve"
            detail={terrainUnavailable ? 'Dataset no disponible' : 'Copernicus DEM GLO-30'}
            pressed={terrainVisible}
            toggle={() => {
              setTerrainUnavailable(false)
              setTerrainVisible(!terrainVisible)
            }}
          />
          <LayerToggle
            name="Vías existentes"
            detail={`${railway.tracks.length.toLocaleString('es-AR')} tramos · ${railway.stations.length.toLocaleString('es-AR')} estaciones`}
            pressed={railwayVisible}
            toggle={() => setRailwayVisible(!railwayVisible)}
          />
          <LayerToggle
            name="Región"
            detail={`Área de referencia de ${region.name}`}
            pressed={outlineVisible}
            toggle={() => setOutlineVisible(!outlineVisible)}
          />
        </ul>
        <h2 className="label tool-heading">Herramientas</h2>
        <button
          type="button"
          className="button-secondary"
          aria-pressed={profileActive}
          onClick={beginProfile}
        >
          Perfil
        </button>
        <button
          type="button"
          className="button-secondary"
          aria-pressed={corridorActive}
          disabled={terrainUnavailable}
          title={terrainUnavailable ? 'Requiere el dataset de relieve' : undefined}
          onClick={beginCorridor}
        >
          Corredor
        </button>
        {profileActive && (
          <p className="tool-hint">
            Marcá vértices. Doble clic o Enter para terminar; Esc para cancelar.
          </p>
        )}
      </aside>
      <section className="map-stage" aria-label={`Visor geográfico de ${region.name}`}>
        <div ref={containerRef} className="map-canvas" />
        {corridorActive && (
          <CorridorTool
            origin={corridorOrigin}
            destination={corridorDestination}
            gradient={corridorGradient}
            setGradient={setCorridorGradient}
            bounds={region.boundingBox}
            busy={corridorBusy}
            result={corridorSearchResult}
            error={corridorError}
            onGenerate={() => {
              void generateCorridor()
            }}
            onCancel={cancelCorridor}
          />
        )}
      </section>
      {corridorShown && corridorResult?.corridor ? (
        <CorridorDetail
          corridor={corridorResult.corridor}
          search={corridorResult.search}
          id={corridorNumber}
          onRemove={() => {
            setCorridorResult(null)
            setCorridorShown(false)
            setCorridorOrigin(null)
            setCorridorDestination(null)
          }}
        />
      ) : (
        <RailwayDetailPanel
          railway={railway}
          selection={selection}
          coordinate={clickedCoordinate}
          elevation={elevation}
          onClear={() => setSelection(null)}
        />
      )}
      {corridorShown && corridorResult?.corridor ? (
        <CorridorProfile corridor={corridorResult.corridor} />
      ) : (
        (profile || profileError) && <ProfileDock profile={profile} error={profileError} />
      )}
    </>
  )
}
