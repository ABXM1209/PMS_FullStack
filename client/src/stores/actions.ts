import { atom } from 'jotai'
import { moistureReadingsAtom, moistureLoadingAtom, moistureErrorAtom, sensorStatusAtom, commandPendingAtom } from './moisture'
import { moistureApi } from '../api/moisture'

// Async actions for fetching data
export const fetchLatestReadingsAtom = atom(null, async (_get, set) => {
  try {
    set(moistureErrorAtom, null)
    set(moistureLoadingAtom, true)
    const readings = await moistureApi.getLatest()
    set(moistureReadingsAtom, readings)
  } catch (err) {
    set(moistureErrorAtom, err instanceof Error ? err.message : String(err))
  } finally {
    set(moistureLoadingAtom, false)
  }
})

export const fetchSensorStatusAtom = atom(null, async (_get, set) => {
  try {
    const status = await moistureApi.getStatus()
    set(sensorStatusAtom, status)
  } catch (err) {
    console.error('Failed to fetch sensor status:', err)
  }
})

export const sendSensorCommandAtom = atom(null, async (_get, set, action: 'start' | 'stop') => {
  try {
    set(commandPendingAtom, true)
    const result = action === 'start'
      ? await moistureApi.startSensor()
      : await moistureApi.stopSensor()

    set(sensorStatusAtom, {
      running: result.running,
      lastCommand: result.lastCommand,
      lastCommandAtUtc: result.lastCommandAtUtc,
    })

    // Refresh readings after command
    await set(fetchLatestReadingsAtom)
  } catch (err) {
    set(moistureErrorAtom, err instanceof Error ? err.message : String(err))
  } finally {
    set(commandPendingAtom, false)
  }
})