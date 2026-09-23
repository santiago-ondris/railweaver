import { useCallback, useEffect, useState } from 'react'
import { GeographicViewer } from '../viewer/GeographicViewer'
import { fetchRailway } from '../railways/api'
import type { Railway } from '../railways/api'
import { fetchRegion } from '../regions/api'
import type { Region } from '../regions/api'
import './App.css'

type AppState =
  { kind: 'loading' } | { kind: 'ready'; region: Region; railway: Railway } | { kind: 'error' }

function App() {
  const [appState, setAppState] = useState<AppState>({ kind: 'loading' })
  const [terrainStatus, setTerrainStatus] = useState<'loading' | 'available' | 'missing'>('loading')
  const handleTerrainStatus = useCallback(
    (status: 'loading' | 'available' | 'missing') => setTerrainStatus(status),
    [],
  )

  useEffect(() => {
    const controller = new AbortController()

    Promise.all([
      fetchRegion('cordoba', controller.signal),
      fetchRailway('cordoba', controller.signal),
    ])
      .then(([region, railway]) => setAppState({ kind: 'ready', region, railway }))
      .catch((error: unknown) => {
        if (!(error instanceof DOMException && error.name === 'AbortError')) {
          setAppState({ kind: 'error' })
        }
      })

    return () => controller.abort()
  }, [])

  const region = appState.kind === 'ready' ? appState.region : null

  return (
    <div className="app-shell">
      <header className="top-bar">
        <div className="brand-lockup">
          <img
            className="brand-mark"
            src="/brand/railweaver-mark.svg"
            alt=""
            width="22"
            height="22"
          />
          <span className="wordmark">RailWeaver</span>
        </div>
        <div className="top-bar-context">
          <span className="label">Región</span>
          <span>{region ? `${region.name}, Argentina` : '—'}</span>
        </div>
      </header>

      <main className="workspace">
        {appState.kind === 'loading' && (
          <div className="workspace-state" role="status">
            <span className="label">Región</span>
            <span>Cargando la definición geográfica…</span>
          </div>
        )}

        {appState.kind === 'error' && (
          <div className="workspace-state">
            <div className="alert alert-alarm" role="alert">
              <p className="alert-title">
                <span className="glyph glyph-alarm" aria-hidden="true" />
                No se pudieron cargar los datos geográficos
              </p>
              <p>
                <b>Qué pasó:</b> la API no devolvió la región o su infraestructura ferroviaria.
              </p>
              <p>
                <b>Cómo resolverlo:</b> iniciá <code className="data">RailWeaver.Api</code> con{' '}
                <code className="data">dotnet run --project src/RailWeaver.Api</code> y recargá la
                página.
              </p>
            </div>
          </div>
        )}

        {appState.kind === 'ready' && (
          <GeographicViewer
            region={appState.region}
            railway={appState.railway}
            onTerrainStatus={handleTerrainStatus}
          />
        )}
      </main>

      <footer className="status-bar">
        <span>WGS 84</span>
        {region && (
          <span>
            Región: {region.source.name} · consultado {region.source.accessedOn}
          </span>
        )}
        {appState.kind === 'ready' && (
          <span>
            Vías: {appState.railway.source.attribution} · {appState.railway.source.license}
          </span>
        )}
        {appState.kind === 'ready' && terrainStatus === 'available' && (
          <span>Relieve: Copernicus DEM GLO-30 · DLR/Airbus · UE/ESA</span>
        )}
        {appState.kind === 'ready' && terrainStatus === 'missing' && (
          <span>○ Relieve no disponible · visor plano</span>
        )}
      </footer>
    </div>
  )
}

export default App
