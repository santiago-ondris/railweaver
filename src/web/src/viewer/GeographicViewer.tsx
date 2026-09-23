import {
  EllipsoidTerrainProvider,
  ScreenSpaceEventHandler,
  ScreenSpaceEventType,
  type CustomHeightmapTerrainProvider,
  type Entity,
  type ImageryLayer,
  type Viewer,
} from 'cesium'
import { useEffect, useRef, useState } from 'react'
import { fetchElevation } from '../elevation/api'
import { ProfileDock } from '../elevation/ProfileDock'
import { useProfileTool } from '../elevation/useProfileTool'
import { NetworkDiagnosticLayer } from '../network/NetworkDiagnosticLayer'
import { NetworkDiagnosticDetail, NetworkRouteDetail } from '../network/NetworkDetail'
import { NetworkProfile } from '../network/NetworkProfile'
import { NetworkRouteLayer } from '../network/NetworkRouteLayer'
import { NetworkTool } from '../network/NetworkTool'
import { useNetworkTool } from '../network/useNetworkTool'
import type { NetworkDiagnostic } from '../network/api'
import { CorridorDetail } from '../planning/CorridorDetail'
import { CorridorProfile } from '../planning/CorridorProfile'
import { CorridorTool } from '../planning/CorridorTool'
import { useCorridorTool } from '../planning/useCorridorTool'
import type { Railway } from '../railways/api'
import { ExistingRailwayLayer, type RailwaySelection } from '../railways/ExistingRailwayLayer'
import { RailwayDetailPanel } from '../railways/RailwayDetailPanel'
import type { Region } from '../regions/api'
import type { Coordinate } from '../shared/geo'
import {
  addRegionOutline,
  createBasemapLayer,
  createTerrainProvider,
  createViewer,
  frameRegion,
  pickCoordinate,
} from './cesiumSetup'
import { LayerToggle } from './LayerToggle'
import './GeographicViewer.css'

type GeographicViewerProps = {
  region: Region
  railway: Railway
  onTerrainStatus: (status: 'loading' | 'available' | 'missing') => void
}

/**
 * The map workspace: owns the Cesium viewer and its layers, routes map clicks and keys to
 * the active tool (profile, corridor) or to terrain inspection, and lays out the side rail,
 * detail panel and analysis dock.
 */
export function GeographicViewer({ region, railway, onTerrainStatus }: GeographicViewerProps) {
  const containerRef = useRef<HTMLDivElement>(null)
  const viewerRef = useRef<Viewer | null>(null)
  const imageryLayerRef = useRef<ImageryLayer | null>(null)
  const outlineEntityRef = useRef<Entity | null>(null)
  const railwayLayerRef = useRef<ExistingRailwayLayer | null>(null)
  const terrainProviderRef = useRef<CustomHeightmapTerrainProvider | null>(null)
  const networkLayerRef = useRef<NetworkRouteLayer | null>(null)
  const diagnosticLayerRef = useRef<NetworkDiagnosticLayer | null>(null)
  const [imageryVisible, setImageryVisible] = useState(true)
  const [terrainVisible, setTerrainVisible] = useState(true)
  const [terrainUnavailable, setTerrainUnavailable] = useState(false)
  const [outlineVisible, setOutlineVisible] = useState(true)
  const [railwayVisible, setRailwayVisible] = useState(true)
  const [diagnosticsVisible, setDiagnosticsVisible] = useState(false)
  const [diagnostic, setDiagnostic] = useState<NetworkDiagnostic | null>(null)
  const [selection, setSelection] = useState<RailwaySelection | null>(null)
  const [clickedCoordinate, setClickedCoordinate] = useState<Coordinate | null>(null)
  const [elevation, setElevation] = useState<number | null | undefined>(undefined)
  const profileTool = useProfileTool(viewerRef, region.id)
  const corridorTool = useCorridorTool(viewerRef, region.id)
  const networkTool = useNetworkTool(region.id)

  // Cesium handlers are registered once per viewer; they read the tools through this ref.
  const toolsRef = useRef({ profileTool, corridorTool, networkTool })
  useEffect(() => {
    toolsRef.current = { profileTool, corridorTool, networkTool }
  })
  const { detach: detachCorridor } = corridorTool
  const { detach: detachNetwork } = networkTool

  useEffect(() => {
    const container = containerRef.current
    if (!container) return
    const imageryLayer = createBasemapLayer()
    const terrainProvider = createTerrainProvider(region.id, (available) => {
      setTerrainUnavailable(!available)
      onTerrainStatus(available ? 'available' : 'missing')
      if (!available) setTerrainVisible(false)
    })
    terrainProviderRef.current = terrainProvider
    let viewer: Viewer | null = null
    try {
      viewer = createViewer(container, imageryLayer, terrainProvider)
      const activeViewer = viewer
      outlineEntityRef.current = addRegionOutline(viewer, region)
      frameRegion(viewer, region)
      viewerRef.current = viewer
      imageryLayerRef.current = imageryLayer
      railwayLayerRef.current = new ExistingRailwayLayer(viewer, railway, setSelection)

      const inspect = (coordinate: Coordinate) => {
        setClickedCoordinate(coordinate)
        setElevation(undefined)
        void fetchElevation(region.id, coordinate)
          .then((result) => setElevation(result.elevation))
          .catch(() => setElevation(null))
      }
      const interaction = new ScreenSpaceEventHandler(viewer.scene.canvas)
      interaction.setInputAction((event: ScreenSpaceEventHandler.PositionedEvent) => {
        const { profileTool, corridorTool, networkTool } = toolsRef.current
        const picked = activeViewer.scene.pick(event.position) as { id?: unknown } | undefined
        if (networkTool.isActive()) {
          const id = picked?.id as { kind?: string; item?: unknown } | undefined
          if (id?.kind === 'station')
            networkTool.placeStation(id.item as Railway['stations'][number])
          return
        }
        const networkDiagnostic = NetworkDiagnosticLayer.picked(picked?.id)
        if (networkDiagnostic) {
          setDiagnostic(networkDiagnostic)
          networkTool.hide()
          corridorTool.hide()
          return
        }
        if (networkLayerRef.current?.isPicked(picked?.id)) {
          networkTool.show()
          setDiagnostic(null)
          corridorTool.hide()
          return
        }
        const coordinate = pickCoordinate(activeViewer, event.position)
        if (!coordinate) return
        if (profileTool.isActive()) return profileTool.addVertex(coordinate)
        if (corridorTool.isActive()) return corridorTool.placeEndpoint(coordinate)
        if (corridorTool.isPicked(picked?.id)) return corridorTool.show()
        corridorTool.hide()
        networkTool.hide()
        setDiagnostic(null)
        inspect(coordinate)
      }, ScreenSpaceEventType.LEFT_CLICK)
      interaction.setInputAction(() => {
        void toolsRef.current.profileTool.finish()
      }, ScreenSpaceEventType.LEFT_DOUBLE_CLICK)

      const keyboard = (event: KeyboardEvent) => {
        const { profileTool, corridorTool, networkTool } = toolsRef.current
        if (event.key === 'Escape' && corridorTool.isActive()) return corridorTool.cancel()
        if (event.key === 'Escape' && networkTool.isActive()) return networkTool.cancel()
        if (!profileTool.isActive()) return
        if (event.key === 'Enter') void profileTool.finish()
        if (event.key === 'Escape') profileTool.cancel()
      }
      window.addEventListener('keydown', keyboard)

      return () => {
        window.removeEventListener('keydown', keyboard)
        detachCorridor()
        detachNetwork()
        interaction.destroy()
        networkLayerRef.current?.destroy()
        networkLayerRef.current = null
        diagnosticLayerRef.current?.destroy()
        diagnosticLayerRef.current = null
        railwayLayerRef.current?.destroy()
        railwayLayerRef.current = null
        viewerRef.current = null
        if (!activeViewer.isDestroyed()) activeViewer.destroy()
      }
    } catch {
      onTerrainStatus('missing')
      if (viewer && !viewer.isDestroyed()) viewer.destroy()
      else if (!imageryLayer.isDestroyed()) imageryLayer.destroy()
    }
  }, [region, railway, onTerrainStatus, detachCorridor, detachNetwork])

  useEffect(() => {
    const viewer = viewerRef.current
    if (!viewer) return
    networkLayerRef.current?.destroy()
    networkLayerRef.current = null
    if (networkTool.result?.status === 'found')
      networkLayerRef.current = new NetworkRouteLayer(viewer, networkTool.result.route)
  }, [networkTool.result])

  useEffect(() => {
    const viewer = viewerRef.current
    if (!viewer) return
    diagnosticLayerRef.current?.destroy()
    diagnosticLayerRef.current = null
    if (diagnosticsVisible && networkTool.network)
      diagnosticLayerRef.current = new NetworkDiagnosticLayer(
        viewer,
        networkTool.network.diagnostics,
        railway,
      )
  }, [diagnosticsVisible, networkTool.network, railway])

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

  // The profile and corridor tools are mutually exclusive.
  const beginProfile = () => {
    corridorTool.cancel()
    corridorTool.hide()
    networkTool.cancel()
    networkTool.hide()
    profileTool.begin()
  }
  const beginCorridor = () => {
    profileTool.reset()
    networkTool.cancel()
    networkTool.hide()
    corridorTool.begin()
  }
  const beginNetwork = () => {
    profileTool.reset()
    corridorTool.cancel()
    corridorTool.hide()
    setDiagnostic(null)
    networkTool.begin()
  }

  const shownCorridor = corridorTool.shown ? corridorTool.result : null

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
            name="Diagnóstico de red"
            detail={`${networkTool.network?.diagnostics.filter((item) => item.kind === 'possible_data_gap').length ?? 0} posibles cortes`}
            pressed={diagnosticsVisible}
            toggle={() => setDiagnosticsVisible(!diagnosticsVisible)}
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
          aria-pressed={profileTool.active}
          onClick={beginProfile}
        >
          Perfil
        </button>
        <button
          type="button"
          className="button-secondary"
          aria-pressed={corridorTool.active}
          disabled={terrainUnavailable}
          title={terrainUnavailable ? 'Requiere el dataset de relieve' : undefined}
          onClick={beginCorridor}
        >
          Corredor
        </button>
        <button
          type="button"
          className="button-secondary"
          aria-pressed={networkTool.active}
          disabled={!networkTool.network}
          title={networkTool.networkError ? 'Requiere la red ferroviaria' : undefined}
          onClick={beginNetwork}
        >
          Ruta
        </button>
        {profileTool.active && (
          <p className="tool-hint">
            Marcá vértices. Doble clic o Enter para terminar; Esc para cancelar.
          </p>
        )}
      </aside>
      <section className="map-stage" aria-label={`Visor geográfico de ${region.name}`}>
        <div ref={containerRef} className="map-canvas" />
        {corridorTool.active && (
          <CorridorTool
            origin={corridorTool.origin}
            destination={corridorTool.destination}
            gradient={corridorTool.gradient}
            setGradient={corridorTool.setGradient}
            bounds={region.boundingBox}
            busy={corridorTool.busy}
            result={corridorTool.searchResult}
            error={corridorTool.error}
            onGenerate={() => {
              void corridorTool.generate()
            }}
            onCancel={corridorTool.cancel}
          />
        )}
        {networkTool.active && (
          <NetworkTool
            stations={railway.stations}
            origin={networkTool.origin}
            destination={networkTool.destination}
            onOrigin={networkTool.selectOrigin}
            onDestination={networkTool.selectDestination}
            includeDisused={networkTool.includeDisused}
            onIncludeDisused={networkTool.setIncludeDisused}
            busy={networkTool.busy}
            unreachable={networkTool.unreachable}
            error={networkTool.error}
            onSearch={(disused) => {
              void networkTool.search(disused)
            }}
            onCancel={networkTool.cancel}
          />
        )}
      </section>
      {shownCorridor?.corridor ? (
        <CorridorDetail
          corridor={shownCorridor.corridor}
          search={shownCorridor.search}
          id={corridorTool.number}
          onRemove={corridorTool.remove}
        />
      ) : networkTool.shown && networkTool.result?.status === 'found' ? (
        <NetworkRouteDetail
          response={networkTool.result}
          number={networkTool.number}
          onRemove={networkTool.remove}
        />
      ) : diagnostic ? (
        <NetworkDiagnosticDetail item={diagnostic} onClear={() => setDiagnostic(null)} />
      ) : (
        <RailwayDetailPanel
          railway={railway}
          inferredGauges={networkTool.network?.inferredGauges ?? []}
          selection={selection}
          coordinate={clickedCoordinate}
          elevation={elevation}
          onClear={() => setSelection(null)}
        />
      )}
      {shownCorridor?.corridor ? (
        <CorridorProfile corridor={shownCorridor.corridor} />
      ) : networkTool.shown && networkTool.result?.status === 'found' ? (
        <NetworkProfile response={networkTool.result} />
      ) : (
        (profileTool.profile || profileTool.error) && (
          <ProfileDock profile={profileTool.profile} error={profileTool.error} />
        )
      )}
    </>
  )
}
