import { Header } from './Header'
import { Footer } from './Footer'
import { Outlet } from 'react-router-dom'

export function Layout() {
  return (
    <div className="flex min-h-full flex-col  bg-surface text-foreground">
      <Header />
      <main className="mx-auto w-full max-w-7xl flex-1 px-4 py-8">
        <Outlet />
      </main>
      <Footer />
    </div>
  )
}
