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

  return (
    <div className="app-shell">
      <header className="app-header">
        <div className="brand-lockup">
          <span className="brand-mark" aria-hidden="true">RW</span>
          <span>
            <strong>RailWeaver</strong>
            <span>Planificación ferroviaria</span>
          </span>
        </div>
        <div className="milestone-badge">M0 · Geographic viewer</div>
      </header>

      <main className="app-main">
        {regionState.kind === 'loading' && (
          <div className="status-card" role="status">
            <span className="status-indicator" aria-hidden="true" />
            <span>
              <strong>Preparando el territorio</strong>
              <span>Cargando la definición geográfica…</span>
            </span>
          </div>
        )}

        {regionState.kind === 'error' && (
          <div className="status-card status-card-error" role="alert">
            <span>
              <strong>No pudimos cargar la región</strong>
              <span>Verificá que RailWeaver.Api esté ejecutándose.</span>
            </span>
          </div>
        )}

        {regionState.kind === 'ready' && <GeographicViewer region={regionState.region} />}
      </main>
    </div>
  )
}

export default App
