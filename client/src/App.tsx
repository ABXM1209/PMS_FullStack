    
import { Provider } from 'jotai'
import { Header } from './components/Header'
import { Controls } from './components/Controls'
import { ErrorAlert } from './components/ErrorAlert'
import { LatestReadingCard } from './components/LatestReadingCard'
import { LiveUpdatesCard } from './components/LiveUpdatesCard'
import { ReadingsTable } from './components/ReadingsTable'
import { useDataPolling } from './hooks/useDataPolling'
import './App.css'

function AppContent() {
  useDataPolling()

  return (
    <div className="app-shell">
      <Header />
      <Controls />
      <ErrorAlert />

      <section className="cards-grid">
        <LatestReadingCard />
        <LiveUpdatesCard />
      </section>

      <ReadingsTable />
    </div>
  )
}

function App() {
  return (
    <Provider>
      <AppContent />
    </Provider>
  )
}

export default App
