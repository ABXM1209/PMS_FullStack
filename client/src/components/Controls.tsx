import { useAtom } from 'jotai'
import { sensorStatusAtom, moistureLoadingAtom, commandPendingAtom } from '../stores/moisture'
import { sendSensorCommandAtom, fetchLatestReadingsAtom, fetchSensorStatusAtom } from '../stores/actions'

export function Controls() {
  const [status] = useAtom(sensorStatusAtom)
  const [loading] = useAtom(moistureLoadingAtom)
  const [commandPending] = useAtom(commandPendingAtom)
  const [, sendCommand] = useAtom(sendSensorCommandAtom)
  const [, fetchLatest] = useAtom(fetchLatestReadingsAtom)
  const [, fetchStatus] = useAtom(fetchSensorStatusAtom)

  const handleRefresh = () => {
    fetchLatest()
    fetchStatus()
  }

  return (
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
        onClick={handleRefresh}
        disabled={loading}
      >
        Refresh now
      </button>
    </section>
  )
}