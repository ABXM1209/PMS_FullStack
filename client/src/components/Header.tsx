import { useAtom } from 'jotai'
import { sensorStatusAtom, lastUpdateAtom } from '../stores/moisture'

export function Header() {
  const [status] = useAtom(sensorStatusAtom)
  const [lastUpdate] = useAtom(lastUpdateAtom)

  return (
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
  )
}