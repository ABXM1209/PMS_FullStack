import { useAtom } from 'jotai'
import { moistureReadingsAtom } from '../stores/moisture'

export function ReadingsTable() {
  const [readings] = useAtom(moistureReadingsAtom)

  return (
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
  )
}