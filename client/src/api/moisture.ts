import type { MoistureReading, SensorStatus, SensorCommandResult } from '../types/api'
import { api } from './config'

export const moistureApi = {
  async getLatest(): Promise<MoistureReading[]> {
    const response = await fetch(api('/api/moisture/latest'))
    if (!response.ok) throw new Error(await response.text())
    return response.json()
  },

  async getStatus(): Promise<SensorStatus> {
    const response = await fetch(api('/api/moisture/status'))
    if (!response.ok) throw new Error(await response.text())
    return response.json()
  },

  async startSensor(): Promise<SensorCommandResult> {
    const response = await fetch(api('/api/moisture/start'), {
      method: 'POST',
    })
    if (!response.ok) throw new Error(await response.text())
    return response.json()
  },

  async stopSensor(): Promise<SensorCommandResult> {
    const response = await fetch(api('/api/moisture/stop'), {
      method: 'POST',
    })
    if (!response.ok) throw new Error(await response.text())
    return response.json()
  },
}