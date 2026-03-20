import { useState, useEffect } from 'react'
import aspireLogo from '/Aspire.png'
import './App.css'

interface WeatherForecast {
  date: string
  temperatureC: number
  temperatureF: number
  summary: string
}

function App() {
  
  const [weatherData, setWeatherData] = useState<WeatherForecast[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [useCelsius, setUseCelsius] = useState(false)
  const [userAuthenticated, setUserAuthenticated] = useState<boolean | null>(null)
  const [userEmail, setUserEmail] = useState<string | null>(null)

  const fetchWeatherForecast = async (force: boolean = false) => {
    // Only fetch when user is confirmed authenticated (unless forced)
    if (!force && userAuthenticated !== true) {
      setError('User not authenticated')
      return
    }

    setLoading(true)
    setError(null)

    try {
      const response = await fetch('/api/weatherforecast', { credentials: 'include' })

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`)
      }

      const data: WeatherForecast[] = await response.json()
      setWeatherData(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch weather data')
      console.error('Error fetching weather forecast:', err)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    const checkUser = async () => {
      try {
        const resp = await fetch('/bff/user', { credentials: 'include' })

        if (resp.status === 200) {
          // Ensure the response is JSON and contains user info. Static HTML (dev server fallback)
          const contentType = resp.headers.get('content-type') || ''
          if (!contentType.includes('application/json')) {
            // not JSON -> likely dev server served index.html; treat as not authenticated
            setUserAuthenticated(false)
            return
          }

          try {
            const userJson = await resp.json()
            const username = (userJson && (userJson.username ?? userJson.Username)) || ''
            const email = (userJson && (userJson.email ?? userJson.Email)) || ''
            if (username && username.length > 0) {
              setUserEmail(email || null)
              setUserAuthenticated(true)
              // user is authenticated, load data (force fetch since state update is async)
              fetchWeatherForecast(true)
              return
            }

            // JSON present but no username -> treat as not authenticated
            setUserAuthenticated(false)
            return
          } catch (e) {
            console.error('Failed to parse /bff/user JSON', e)
            setUserAuthenticated(false)
            return
          }
        }

        if (resp.status === 401) {
          setUserAuthenticated(false)
          const currentUrl = window.location.pathname + window.location.search + window.location.hash
          const returnUrl = encodeURIComponent(currentUrl)
          window.location.href = `/bff/login?returnUrl=${returnUrl}`
          return
        }

        // other statuses: treat as not authenticated and redirect to login
        setUserAuthenticated(false)
        const currentUrl = window.location.pathname + window.location.search + window.location.hash
        const returnUrl = encodeURIComponent(currentUrl)
        window.location.href = `/bff/login?returnUrl=${returnUrl}`
      } catch (err) {
        console.error('Error checking user authentication:', err)
        // network error: mark as not authenticated and do not load data
        setUserAuthenticated(false)
        const currentUrl = window.location.pathname + window.location.search + window.location.hash
        const returnUrl = encodeURIComponent(currentUrl)
        window.location.href = `/bff/login?returnUrl=${returnUrl}`
      }
    }

    checkUser()
  }, [])

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString(undefined, { 
      weekday: 'short', 
      month: 'short', 
      day: 'numeric' 
    })
  }

  // Avoid rendering any application chrome or content until authentication is resolved
  if (userAuthenticated === null) {
    return (
      <div className="app-container" style={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
        <div className="card" aria-live="polite">
          <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center' }}>
            <svg width="20" height="20" viewBox="0 0 24 24" className={`refresh-icon spinning`} aria-hidden="true">
              <path d="M21.5 2v6h-6M2.5 22v-6h6M2 11.5a10 10 0 0 1 18.8-4.3M22 12.5a10 10 0 0 1-18.8 4.2" stroke="currentColor" strokeWidth="2" fill="none"/>
            </svg>
            <span>Checking authentication...</span>
          </div>
        </div>
      </div>
    )
  }

  if (userAuthenticated !== true) {
    // User is not authenticated; checkUser will redirect to /bff/login. Render blank to avoid content flash.
    return <div className="app-container" />
  }

  return (
    <div className="app-container">
      <header className="app-header" style={{ position: 'relative' }}>
        <a 
          href="https://aspire.dev" 
          target="_blank" 
          rel="noopener noreferrer"
          aria-label="Visit Aspire website (opens in new tab)"
          className="logo-link"
        >
          <img src={aspireLogo} className="logo" alt="Aspire logo" />
        </a>
        <div style={{ position: 'absolute', right: '1.5rem', top: '1.5rem', display: 'flex', alignItems: 'center', gap: '0.75rem' }} className="auth-bar">
          {userEmail && <div className="user-email" style={{ color: 'var(--text-tertiary)', fontSize: '0.95rem' }}>{userEmail}</div>}
          <button
            className="refresh-button"
            onClick={async () => {
              try {
                await fetch('/bff/logout', { method: 'POST', credentials: 'include' })
              } catch (e) {
                console.error('Logout failed', e)
              }
              // After logout, reload so auth flow redirects
              window.location.href = '/'
            }}
            aria-label="Logout"
            type="button"
          >
            Logout
          </button>
        </div>
        <h1 className="app-title">Aspire Starter with Auth</h1>
        <p className="app-subtitle">Modern distributed application development</p>
      </header>

      <main className="main-content">
        <section className="weather-section" aria-labelledby="weather-heading">
          <div className="card">
            <div className="section-header">
              <h2 id="weather-heading" className="section-title">Weather Forecast</h2>
              <div className="header-actions">
                <fieldset className="toggle-switch" aria-label="Temperature unit selection">
                  <legend className="visually-hidden">Temperature unit</legend>
                  <button 
                    className={`toggle-option ${!useCelsius ? 'active' : ''}`}
                    onClick={() => setUseCelsius(false)}
                    aria-pressed={!useCelsius}
                    type="button"
                  >
                    <span aria-hidden="true">°F</span>
                    <span className="visually-hidden">Fahrenheit</span>
                  </button>
                  <button 
                    className={`toggle-option ${useCelsius ? 'active' : ''}`}
                    onClick={() => setUseCelsius(true)}
                    aria-pressed={useCelsius}
                    type="button"
                  >
                    <span aria-hidden="true">°C</span>
                    <span className="visually-hidden">Celsius</span>
                  </button>
                </fieldset>
                <button 
                  className="refresh-button"
                  onClick={fetchWeatherForecast} 
                  disabled={loading || userAuthenticated !== true}
                  aria-label={loading ? 'Loading weather forecast' : 'Refresh weather forecast'}
                  type="button"
                >
                  <svg 
                    className={`refresh-icon ${loading ? 'spinning' : ''}`}
                    width="20" 
                    height="20" 
                    viewBox="0 0 24 24" 
                    fill="none" 
                    stroke="currentColor" 
                    strokeWidth="2"
                    aria-hidden="true"
                    focusable="false"
                  >
                    <path d="M21.5 2v6h-6M2.5 22v-6h6M2 11.5a10 10 0 0 1 18.8-4.3M22 12.5a10 10 0 0 1-18.8 4.2"/>
                  </svg>
                  <span>{loading ? 'Loading...' : 'Refresh'}</span>
                </button>
              </div>
            </div>

            {error && (
              <div className="error-message" role="alert" aria-live="polite">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
                  <circle cx="12" cy="12" r="10"/>
                  <line x1="12" y1="8" x2="12" y2="12"/>
                  <line x1="12" y1="16" x2="12.01" y2="16"/>
                </svg>
                <span>{error}</span>
              </div>
            )}

            {loading && weatherData.length === 0 && userAuthenticated === true && (
              <div className="loading-skeleton" role="status" aria-live="polite" aria-label="Loading weather data">
                {[...Array(5)].map((_, i) => (
                  <div key={i} className="skeleton-row" aria-hidden="true" />
                ))}
                <span className="visually-hidden">Loading weather forecast data...</span>
              </div>
            )}

            {userAuthenticated === true && weatherData.length > 0 && (
              <div className="weather-grid">
                {weatherData.map((forecast, index) => (
                  <article key={index} className="weather-card" aria-label={`Weather for ${formatDate(forecast.date)}`}>
                    <h3 className="weather-date">
                      <time dateTime={forecast.date}>{formatDate(forecast.date)}</time>
                    </h3>
                    <p className="weather-summary">{forecast.summary}</p>
                    <div className="weather-temps" aria-label={`Temperature: ${useCelsius ? forecast.temperatureC : forecast.temperatureF} degrees ${useCelsius ? 'Celsius' : 'Fahrenheit'}`}>
                      <div className="temp-group">
                        <span className="temp-value" aria-hidden="true">
                          {useCelsius ? forecast.temperatureC : forecast.temperatureF}°
                        </span>
                        <span className="temp-unit" aria-hidden="true">{useCelsius ? 'Celsius' : 'Fahrenheit'}</span>
                      </div>
                    </div>
                  </article>
                ))}
              </div>
            )}
          </div>
        </section>
      </main>

      <footer className="app-footer">
        <nav aria-label="Footer navigation">
          <a href="https://aspire.dev" target="_blank" rel="noopener noreferrer">
            Learn more about Aspire<span className="visually-hidden"> (opens in new tab)</span>
          </a>
          <a 
            href="https://github.com/dotnet/aspire" 
            target="_blank" 
            rel="noopener noreferrer"
            className="github-link"
            aria-label="View Aspire on GitHub (opens in new tab)"
          >
            <img src="/github.svg" alt="" width="24" height="24" aria-hidden="true" />
            <span className="visually-hidden">GitHub</span>
          </a>
        </nav>
      </footer>
    </div>
  )
}

export default App
