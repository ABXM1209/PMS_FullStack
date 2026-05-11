import { useAtom } from 'jotai'
import { latestReadingAtom, moistureLoadingAtom, lastUpdateAtom } from '../stores/moisture'

export function LatestReadingCard() {
  const [latestReading] = useAtom(latestReadingAtom)
  const [loading] = useAtom(moistureLoadingAtom)
  const [lastUpdate] = useAtom(lastUpdateAtom)

  return (
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
  )
}