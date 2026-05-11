export function LiveUpdatesCard() {
  return (
    <article className="card">
      <h2>Live updates</h2>
      <p>Data is refreshed every 8 seconds from the Thinger bucket. In the future this can be replaced with MQTT or SSE event streaming.</p>
    </article>
  )
}