import { useEffect } from 'react'
import { useAtom } from 'jotai'
import { fetchLatestReadingsAtom, fetchSensorStatusAtom } from '../stores/actions'

export function useDataPolling(intervalMs: number = 8000) {
  const [, fetchLatest] = useAtom(fetchLatestReadingsAtom)
  const [, fetchStatus] = useAtom(fetchSensorStatusAtom)

  useEffect(() => {
    // Initial fetch
    fetchLatest()
    fetchStatus()

    // Set up polling
    const interval = window.setInterval(() => {
      fetchLatest()
      fetchStatus()
    }, intervalMs)

    return () => window.clearInterval(interval)
  }, [fetchLatest, fetchStatus, intervalMs])
}