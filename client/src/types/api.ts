export type MoistureReading = {
  timestamp: string
  percent?: number
  raw?: string
  values?: Record<string, string | number | boolean | null>
}

export type SensorStatus = {
  running: boolean
  lastCommand: string
  lastCommandAtUtc: string
}

export type SensorCommandResult = {
  success: boolean
  message: string
  running: boolean
  lastCommand: string
  lastCommandAtUtc: string
}