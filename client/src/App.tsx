import './App.css'
import { useEffect, useMemo, useState } from 'react'

type TelemetryReading = {
  deviceId: string
  timestamp: string
  percent: number | null
  raw: number | null
  data: number | null
  pumpOn: boolean | null
  autoMode: boolean | null
  online: boolean | null
  source: string
}

type DeviceSummary = {
  id: string
  name: string
  bucketId: string
  telemetryTopic: string
  commandTopic: string
  latest: TelemetryReading | null
}

type ApiHealth = {
  thinger: {
    mqttConnected: boolean
    devices: DeviceSummary[]
  }
}

function App() {
  const [devices, setDevices] = useState<DeviceSummary[]>([])
  const [selectedDeviceId, setSelectedDeviceId] = useState<string>('')
  const [filter, setFilter] = useState('')
  const [latest, setLatest] = useState<TelemetryReading | null>(null)
  const [history, setHistory] = useState<TelemetryReading[]>([])
  const [health, setHealth] = useState<ApiHealth | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [commandPending, setCommandPending] = useState(false)

  async function loadDashboard() {
    try {
      setError(null)
      const devicesUrl = filter.trim()
        ? `/api/devices?search=${encodeURIComponent(filter.trim())}`
        : '/api/devices'

      const [healthResponse, devicesResponse] = await Promise.all([
        fetch('/api/health'),
        fetch(devicesUrl),
      ])

      if (!healthResponse.ok || !devicesResponse.ok) {
        throw new Error('Backend could not load Thinger.io telemetry.')
      }

      const nextHealth = await healthResponse.json() as ApiHealth
      const nextDevices = await devicesResponse.json() as DeviceSummary[]
      const nextSelectedId = selectedDeviceId || nextDevices[0]?.id || ''

      setHealth(nextHealth)
      setDevices(nextDevices)
      setSelectedDeviceId(nextSelectedId)

      if (nextSelectedId) {
        await loadDevice(nextSelectedId)
      }
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Unexpected dashboard error.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void loadDashboard()
    const timer = window.setInterval(loadDashboard, 15000)
    return () => window.clearInterval(timer)
  }, [filter, selectedDeviceId])

  async function loadDevice(deviceId: string) {
    const [latestResponse, historyResponse] = await Promise.all([
      fetch(`/api/devices/${encodeURIComponent(deviceId)}/telemetry/latest`),
      fetch(`/api/devices/${encodeURIComponent(deviceId)}/telemetry/history?items=12`),
    ])

    if (!latestResponse.ok || !historyResponse.ok) {
      throw new Error(`Backend could not load telemetry for ${deviceId}.`)
    }

    setLatest(await latestResponse.json())
    setHistory(await historyResponse.json())
  }

  async function selectDevice(deviceId: string) {
    setSelectedDeviceId(deviceId)
    setLoading(true)
    try {
      setError(null)
      await loadDevice(deviceId)
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Could not load selected device.')
    } finally {
      setLoading(false)
    }
  }

  const moisture = latest?.percent ?? null
  const pumpOn = latest?.pumpOn ?? false
  const autoMode = latest?.autoMode ?? false
  const online = latest?.online ?? false
  const selectedDevice = devices.find((device) => device.id === selectedDeviceId) ?? devices[0] ?? null

  const moistureStatus = useMemo(() => {
    if (moisture === null) return 'Unknown'
    if (moisture < 25) return 'Dry'
    if (moisture < 60) return 'Balanced'
    return 'Wet'
  }, [moisture])

  async function sendPumpCommand(command: { enabled?: boolean, auto?: boolean }) {
    if (!selectedDevice) return

    setCommandPending(true)
    try {
      const response = await fetch(`/api/devices/${encodeURIComponent(selectedDevice.id)}/pump`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(command),
      })

      if (!response.ok) {
        throw new Error('Pump command failed.')
      }

      setLatest((current) => current ? {
        ...current,
        pumpOn: command.enabled ?? current.pumpOn,
        autoMode: command.auto ?? current.autoMode,
      } : current)
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Could not send pump command.')
    } finally {
      setCommandPending(false)
    }
  }

  return (
    <main className="dashboard">
      <section className="toolbar">
        <div>
          <p className="eyebrow">Plant Monitoring System</p>
          <h1>Watering Devices</h1>
        </div>
        <button className="refresh" onClick={loadDashboard} disabled={loading}>
          Refresh
        </button>
      </section>

      {error && <div className="alert">{error}</div>}

      <section className="workspace">
        <aside className="devicePanel">
          <div className="panelHeader">
            <h2>Devices</h2>
            <span>{devices.length}</span>
          </div>
          <input
            value={filter}
            onChange={(event) => setFilter(event.target.value)}
            placeholder="Filter devices"
          />
          <div className="deviceList">
            {devices.map((device) => (
              <button
                className={device.id === selectedDeviceId ? 'device active' : 'device'}
                key={device.id}
                onClick={() => selectDevice(device.id)}
              >
                <span>
                  {device.name}
                  <b>{device.latest?.online ? 'Online' : 'Offline'}</b>
                </span>
                <small>
                  {device.latest?.percent === null || device.latest?.percent === undefined ? '--' : `${Math.round(device.latest.percent)}%`} moisture
                  {' / '}
                  pump {device.latest?.pumpOn ? 'on' : 'off'}
                </small>
              </button>
            ))}
          </div>
        </aside>

        <section className="deviceDetail">
          <div className="detailHeader">
            <div>
              <p className="eyebrow">{selectedDevice?.id ?? 'No device selected'}</p>
              <h2>{selectedDevice?.name ?? 'Select a device'}</h2>
            </div>
            <span className={online ? 'status online' : 'status'}>
              {online ? 'Device online' : 'Device offline'}
            </span>
          </div>

          <section className="metrics">
            <article className="metric">
              <span>Moisture</span>
              <strong>{moisture === null ? '--' : `${Math.round(moisture)}%`}</strong>
              <small>{moistureStatus}</small>
            </article>
            <article className="metric">
              <span>Raw Sensor</span>
              <strong>{latest?.raw ?? latest?.data ?? '--'}</strong>
              <small>{latest?.source ?? 'No source'}</small>
            </article>
            <article className="metric">
              <span>Pump</span>
              <strong>{pumpOn ? 'On' : 'Off'}</strong>
              <small>{selectedDevice?.commandTopic ?? 'No topic'}</small>
            </article>
            <article className="metric">
              <span>Mode</span>
              <strong>{autoMode ? 'Auto' : 'Manual'}</strong>
              <small>{health?.thinger.mqttConnected ? 'MQTT connected' : 'MQTT waiting'}</small>
            </article>
          </section>

          <section className="controls">
            <div>
              <h2>Manual Pump Control</h2>
              <p>{selectedDevice?.commandTopic ?? 'Select a device before sending commands.'}</p>
            </div>
            <div className="buttonGroup">
              <button onClick={() => sendPumpCommand({ enabled: true })} disabled={commandPending || !selectedDevice}>Turn On</button>
              <button onClick={() => sendPumpCommand({ enabled: false })} disabled={commandPending || !selectedDevice}>Turn Off</button>
              <button onClick={() => sendPumpCommand({ auto: !autoMode })} disabled={commandPending || !selectedDevice}>{autoMode ? 'Switch to Manual' : 'Switch to Auto'}</button>
            </div>
          </section>

          <section className="history">
            <div className="sectionTitle">
              <h2>Recent Thinger.io Data</h2>
              <span>{selectedDevice?.bucketId ?? 'No bucket'}</span>
            </div>
            <div className="tableWrap">
              <table>
                <thead>
                  <tr>
                    <th>Time</th>
                    <th>Moisture</th>
                    <th>Raw</th>
                    <th>Pump</th>
                    <th>Mode</th>
                    <th>Device</th>
                    <th>Source</th>
                  </tr>
                </thead>
                <tbody>
                  {history.map((reading) => (
                    <tr key={`${reading.deviceId}-${reading.timestamp}-${reading.raw ?? reading.percent}`}>
                      <td>{new Date(reading.timestamp).toLocaleString()}</td>
                      <td>{reading.percent === null ? '--' : `${Math.round(reading.percent)}%`}</td>
                      <td>{reading.raw ?? reading.data ?? '--'}</td>
                      <td>{reading.pumpOn ? 'On' : 'Off'}</td>
                      <td>{reading.autoMode ? 'Auto' : 'Manual'}</td>
                      <td>{reading.online ? 'Online' : 'Offline'}</td>
                      <td>{reading.source}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        </section>
      </section>
    </main>
  )
}

export default App
