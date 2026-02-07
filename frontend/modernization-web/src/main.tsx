import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import * as Sentry from '@sentry/react'
import type { ErrorEvent } from '@sentry/react'
import '@fontsource/inter/400.css'
import '@fontsource/inter/600.css'
import '@fontsource/inter/700.css'
import '@fontsource/jetbrains-mono/400.css'
import '@fontsource/jetbrains-mono/600.css'
import './index.css'
import App from './App.tsx'
import { AuthProvider } from './hooks/AuthProvider'

const sentryDsn = import.meta.env.VITE_SENTRY_DSN
const sentryTracesSampleRate = Number(import.meta.env.VITE_SENTRY_TRACES_SAMPLE_RATE ?? '1.0')
const sentryEnabled = Boolean(sentryDsn) && import.meta.env.PROD

const scrubAccessToken = (event: ErrorEvent) => {
  if (event.request?.headers) {
    if ('Authorization' in event.request.headers) {
      event.request.headers.Authorization = '[redacted]'
    }
    if ('accessToken' in event.request.headers) {
      event.request.headers.accessToken = '[redacted]'
    }
  }

  if (event.extra && typeof event.extra === 'object' && 'accessToken' in event.extra) {
    event.extra.accessToken = '[redacted]'
  }

  if (event.tags && 'accessToken' in event.tags) {
    event.tags.accessToken = '[redacted]'
  }

  return event
}

if (sentryEnabled) {
  Sentry.init({
    dsn: sentryDsn,
    environment: import.meta.env.VITE_ENV ?? import.meta.env.MODE,
    integrations: [Sentry.browserTracingIntegration()],
    tracesSampleRate: Number.isNaN(sentryTracesSampleRate) ? 1.0 : sentryTracesSampleRate,
    beforeSend: scrubAccessToken,
  })

  Sentry.setTag('service.name', 'frontend')
}

const appContent = sentryEnabled ? (
  <Sentry.ErrorBoundary fallback={<div>Algo deu errado.</div>}>
    <App />
  </Sentry.ErrorBoundary>
) : (
  <App />
)

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AuthProvider>{appContent}</AuthProvider>
  </StrictMode>,
)
