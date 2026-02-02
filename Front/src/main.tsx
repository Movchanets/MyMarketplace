import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import './index.css'
import './i18n'
import { AppWithProviders } from './App.tsx'

// Global promise rejection logging (helps capture Promise.then rejections)
window.addEventListener('unhandledrejection', (event) => {
  console.error('Unhandled promise rejection:', event.reason)
})
window.addEventListener('rejectionhandled', (event) => {
  console.warn('Promise rejection handled later:', event.reason)
})

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <AppWithProviders />
    </BrowserRouter>
  </StrictMode>,
)
