import { useEffect, useState } from 'react'
import { GeographicViewer } from './GeographicViewer'
import { fetchRegion } from './regions'
import type { Region } from './regions'

type RegionState =
  | { kind: 'loading' }
  | { kind: 'ready'; region: Region }
  | { kind: 'error' }

function App() {
  const [regionState, setRegionState] = useState<RegionState>({ kind: 'loading' })

  useEffect(() => {
    const controller = new AbortController()

    fetchRegion('cordoba', controller.signal)
      .then((region) => setRegionState({ kind: 'ready', region }))
      .catch((error: unknown) => {
        if (!(error instanceof DOMException && error.name === 'AbortError')) {
          setRegionState({ kind: 'error' })
        }
      })

    return () => controller.abort()
  }, [])

  const region = regionState.kind === 'ready' ? regionState.region : null

  return (
    <div className="app-shell">
      <header className="top-bar">
        <div className="brand-lockup">
          <img className="brand-mark" src="/brand/railweaver-mark.svg" alt="" width="22" height="22" />
          <span className="wordmark">RailWeaver</span>
        </div>
        <div className="top-bar-context">
          <span className="label">Región</span>
          <span>{region ? `${region.name}, Argentina` : '—'}</span>
        </div>
      </header>

      <main className="workspace">
        {regionState.kind === 'loading' && (
          <div className="workspace-state" role="status">
            <span className="label">Región</span>
            <span>Cargando la definición geográfica…</span>
          </div>
        )}

        {regionState.kind === 'error' && (
          <div className="workspace-state">
            <div className="alert alert-alarm" role="alert">
              <p className="alert-title">
                <span className="glyph glyph-alarm" aria-hidden="true" />
                No se pudo cargar la región
              </p>
              <p>
                <b>Qué pasó:</b> la API no devolvió la región <code className="data">cordoba</code>.
              </p>
              <p>
                <b>Cómo resolverlo:</b> iniciá <code className="data">RailWeaver.Api</code> con{' '}
                <code className="data">dotnet run --project src/RailWeaver.Api</code> y recargá la página.
              </p>
            </div>
          </div>
        )}

        {region && <GeographicViewer region={region} />}
      </main>

      <footer className="status-bar">
        <span>WGS 84</span>
        {region && (
          <span>
            Región: {region.source.name} · consultado {region.source.accessedOn}
          </span>
        )}
      </footer>
    </div>
  )
}

export default App
