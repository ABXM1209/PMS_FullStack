    
import { useEffect, useState } from 'react'
import './App.css'

type MoistureReading = {
  timestamp: string
  percent?: number
  raw?: string
  values?: Record<string, string | number | boolean | null>
}

type SensorStatus = {
  running: boolean
  lastCommand: string
  lastCommandAtUtc: string
}

type SensorCommandResult = {
  success: boolean
  message: string
  running: boolean
  lastCommand: string
  lastCommandAtUtc: string
}

const apiBase = import.meta.env.VITE_API_BASE_URL ?? ''
const api = (path: string) => `${apiBase}${path}`

function App() {
  const [readings, setReadings] = useState<MoistureReading[]>([])
  const [status, setStatus] = useState<SensorStatus | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [commandPending, setCommandPending] = useState(false)

  const latestReading = readings[0]
  const lastUpdate = latestReading ? new Date(latestReading.timestamp).toLocaleString() : ''

  const fetchLatest = async () => {
    try {
      setError(null)
      setLoading(true)
      const response = await fetch(api('/api/moisture/latest'))
      if (!response.ok) throw new Error(await response.text())
      const payload = (await response.json()) as MoistureReading[]
      setReadings(payload)
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err))
    } finally {
      setLoading(false)
    }
  }

  const fetchStatus = async () => {
    try {
      const response = await fetch(api('/api/moisture/status'))
      if (!response.ok) throw new Error(await response.text())
      setStatus((await response.json()) as SensorStatus)
    } catch (err) {
      console.error(err)
    }
  }

  const sendCommand = async (action: 'start' | 'stop') => {
    try {
      setCommandPending(true)
      const response = await fetch(api(`/api/moisture/${action}`), {
        method: 'POST',
      })
      if (!response.ok) throw new Error(await response.text())
      const result = (await response.json()) as SensorCommandResult
      setStatus({
        running: result.running,
        lastCommand: result.lastCommand,
        lastCommandAtUtc: result.lastCommandAtUtc,
      })
      await fetchLatest()
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err))
    } finally {
      setCommandPending(false)
    }
  }

  useEffect(() => {
    fetchLatest()
    fetchStatus()
    const interval = window.setInterval(() => {
      fetchLatest()
      fetchStatus()
    }, 8000)
    return () => window.clearInterval(interval)
  }, [])

  return (
    <div className="app-shell">
      <header className="hero-card">
        <div>
          <p className="eyebrow">Moisture Dashboard</p>
          <h1>Live soil sensor data</h1>
          <p>Current readings are loaded from the Thinger.io bucket and refreshed automatically.
            Future MQTT / sensor control flows can be wired into the same API layer.</p>
        </div>
        <div className="status-panel">
          <div className="status-pill">
            <span>Sensor</span>
            <strong>{status?.running ? 'Running' : 'Stopped'}</strong>
          </div>
          <div className="status-pill secondary">
            <span>Last command</span>
            <strong>{status?.lastCommand ?? 'unknown'}</strong>
          </div>
          <div className="status-pill secondary">
            <span>Last update</span>
            <strong>{lastUpdate || 'waiting...'}</strong>
          </div>
        </div>
      </header>

      <section className="controls-row">
        <button
          className="button primary"
          type="button"
          onClick={() => sendCommand('start')}
          disabled={commandPending || status?.running === true}
        >
          Start sensor
        </button>
        <button
          className="button danger"
          type="button"
          onClick={() => sendCommand('stop')}
          disabled={commandPending || status?.running === false}
        >
          Stop sensor
        </button>
        <button
          className="button outline"
          type="button"
          onClick={() => { fetchLatest(); fetchStatus() }}
          disabled={loading}
        >
          Refresh now
        </button>
      </section>

      {error ? <div className="alert">{error}</div> : null}

      <section className="cards-grid">
        <article className="card large-card">
          <h2>Latest reading</h2>
          {latestReading ? (
            <div className="reading-grid">
              <div>
                <span>Percent</span>
                <strong>{latestReading.percent?.toFixed(1) ?? '—'} %</strong>
              </div>
              <div>
                <span>Raw value</span>
                <strong>{latestReading.raw ?? '—'}</strong>
              </div>
              <div>
                <span>Timestamp</span>
                <strong>{lastUpdate}</strong>
              </div>
            </div>
          ) : (
            <p>{loading ? 'Loading latest moisture reading…' : 'No recent reading available.'}</p>
          )}
        </article>

        <article className="card">
          <h2>Live updates</h2>
          <p>Data is refreshed every 8 seconds from the Thinger bucket. In the future this can be replaced with MQTT or SSE event streaming.</p>
        </article>
      </section>

      <section className="table-card card">
        <div className="table-header">
          <h2>Recent bucket history</h2>
          <span>{readings.length} entries</span>
        </div>
        <div className="table-scroll">
          <table>
            <thead>
              <tr>
                <th>Date</th>
                <th>Percent</th>
                <th>Raw</th>
              </tr>
            </thead>
            <tbody>
              {readings.map((reading) => (
                <tr key={reading.timestamp + reading.raw}>
                  <td>{new Date(reading.timestamp).toLocaleString()}</td>
                  <td>{reading.percent?.toFixed(1) ?? '—'}</td>
                  <td>{reading.raw ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  )
}

export default App
