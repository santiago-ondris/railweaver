import { useEffect, useState } from 'react'

type Health = { status: string; name: string; version: string }

type ApiState =
  | { kind: 'loading' }
  | { kind: 'ok'; health: Health }
  | { kind: 'unreachable' }

function App() {
  const [api, setApi] = useState<ApiState>({ kind: 'loading' })

  useEffect(() => {
    fetch('/api/health')
      .then((res) => (res.ok ? (res.json() as Promise<Health>) : Promise.reject(res.status)))
      .then((health) => setApi({ kind: 'ok', health }))
      .catch(() => setApi({ kind: 'unreachable' }))
  }, [])

  return (
    <main>
      <h1>RailWeaver</h1>
      <p>Plan. Build. Simulate. Understand.</p>
      <p>
        API:{' '}
        {api.kind === 'loading' && 'checking…'}
        {api.kind === 'ok' && (
          <code>
            {api.health.status} · v{api.health.version}
          </code>
        )}
        {api.kind === 'unreachable' && 'unreachable (is RailWeaver.Api running?)'}
      </p>
    </main>
  )
}

export default App
