import { Link, NavLink, useLocation } from 'react-router-dom'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { AuthModal } from '../auth/AuthModal'
import type { User } from '../../store/authStore'
import { useAuthStore } from '../../store/authStore'
import SidenavMenu from '../navigation/SidenavMenu'
import { CatalogButton } from '../catalog/CatalogButton'
import { SearchBar } from '../search/SearchBar'
import { ThemeToggle } from '../ui/ThemeToggle'

const navLinkClass = ({ isActive }: { isActive: boolean }) =>
  `px-3 py-2 rounded-md text-sm font-medium transition-colors ${
    isActive ? 'bg-brand text-white' : 'text-foreground-muted hover:text-foreground hover:bg-surface'
  }`

export function Header() {
  const [isAuthModalOpen, setIsAuthModalOpen] = useState(false)
  const [isSidenavOpen, setIsSidenavOpen] = useState(false)
  const { isAuthenticated, user } = useAuthStore()
  const location = useLocation()
  const inCabinet = location.pathname.startsWith('/cabinet')
  const { t } = useTranslation()

  return (
    <>
      <header className="sticky top-0 z-50 w-full border-b border-foreground/10 bg-surface/95 shadow">
        <div className="mx-auto flex h-20 w-full max-w-7xl items-center gap-4 px-4">
          {/* Logo */}
          <Link to="/" className="flex shrink-0 items-center gap-2">
            <div className="h-8 w-8 rounded bg-brand" />
            <span className="hidden text-lg font-semibold text-foreground sm:block">{t('site.name')}</span>
          </Link>

          {/* Catalog Button */}
          <CatalogButton />

          {/* Search Bar */}
          <SearchBar />

          {/* Navigation */}
          <nav className="hidden shrink-0 gap-1 lg:flex">
            <NavLink to="/" className={navLinkClass} end>
              {t('nav.home')}
            </NavLink>
            <NavLink to="/about" className={navLinkClass}>
              {t('nav.about')}
            </NavLink>
            <NavLink to="/contacts" className={navLinkClass}>
              {t('nav.contacts')}
            </NavLink>
          </nav>

          {/* User section */}
          <div className="flex shrink-0 items-center gap-2">
            {/* Theme Toggle */}
            <ThemeToggle />

            {isAuthenticated ? (
              <div className="flex items-center gap-3">
                <span className="hidden text-sm text-foreground xl:block">{t('greeting', { name: user?.firstName || '' })}</span>
                {!inCabinet && (
                  <>
                    <button
                      type="button"
                      onClick={() => setIsSidenavOpen(true)}
                      aria-expanded={isSidenavOpen}
                      title={t('header.my_profile')}
                      className="relative group inline-flex flex-col items-center no-underline px-1 py-0.5 rounded-md hover:bg-surface"
                    >
                      <span className="h-9 w-9 inline-flex items-center justify-center rounded-full bg-surface text-sm font-medium ring-1 ring-white/10">
                        {(() => {
                          const img = user?.avatarUrl
                          return img ? (
                            <img src={img} alt={user?.name ?? 'avatar'} className="h-9 w-9 rounded-full object-cover" />
                          ) : (
                            <span className="text-sm text-foreground">
                              {user?.name
                                ? user.name
                                    .split(' ')
                                    .map((s) => s[0])
                                    .slice(0, 2)
                                    .join('')
                                    .toUpperCase()
                                : 'U'}
                            </span>
                          )
                        })()}
                      </span>

                      <span className="mt-1 text-xs text-foreground-muted hidden sm:block transition-colors duration-150 group-hover:text-foreground group-hover:font-medium">
                        {t('header.cabinet')}
                      </span>
                    </button>

                    <Link
                      to="/cabinet/favorites"
                      className="relative group inline-flex flex-col items-center no-underline px-1 py-0.5 rounded-md hover:bg-surface"
                      aria-label={t('menu.favorites')}
                    >
                      <span className="h-9 w-9 inline-flex items-center justify-center text-lg text-foreground-muted group-hover:text-foreground">&#128420;</span>
                      <span className="mt-1 text-xs text-foreground-muted hidden sm:block transition-colors duration-150 group-hover:text-foreground group-hover:font-medium">
                        {t('menu.favorites')}
                      </span>
                    </Link>
                  </>
                )}

                <Link
                  to="/cart"
                  className="relative group inline-flex flex-col items-center no-underline px-1 py-0.5 rounded-md hover:bg-surface"
                  aria-label={t('header.cart')}
                >
                  <span className="h-9 w-9 inline-flex items-center justify-center text-lg text-foreground-muted group-hover:text-foreground">&#128722;</span>
                  <span className="mt-1 text-xs text-foreground-muted hidden sm:block transition-colors duration-150 group-hover:text-foreground group-hover:font-medium">
                    {t('header.cart')}
                  </span>
                </Link>

                <SidenavMenu isOpen={isSidenavOpen} onClose={() => setIsSidenavOpen(false)} user={user as User | null} />
              </div>
            ) : (
              <button onClick={() => setIsAuthModalOpen(true)} className="btn-primary">
                {t('auth.login')}
              </button>
            )}
          </div>
        </div>
      </header>

      <AuthModal isOpen={isAuthModalOpen} onClose={() => setIsAuthModalOpen(false)} />
    </>
  )
}
