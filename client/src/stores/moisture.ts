import { atom } from 'jotai'
import type { MoistureReading, SensorStatus } from '../types/api'

// Moisture readings state
export const moistureReadingsAtom = atom<MoistureReading[]>([])
export const moistureLoadingAtom = atom<boolean>(false)
export const moistureErrorAtom = atom<string | null>(null)

// Sensor status state
export const sensorStatusAtom = atom<SensorStatus | null>(null)

// Command pending state
export const commandPendingAtom = atom<boolean>(false)

// Derived atoms
export const latestReadingAtom = atom((get) => {
  const readings = get(moistureReadingsAtom)
  return readings[0] || null
})

export const lastUpdateAtom = atom((get) => {
  const latest = get(latestReadingAtom)
  return latest ? new Date(latest.timestamp).toLocaleString() : ''
})