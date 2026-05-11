import { useAtom } from 'jotai'
import { moistureErrorAtom } from '../stores/moisture'

export function ErrorAlert() {
  const [error] = useAtom(moistureErrorAtom)

  if (!error) return null

  return <div className="alert">{error}</div>
}