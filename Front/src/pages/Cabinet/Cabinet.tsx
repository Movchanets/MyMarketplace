import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuthStore } from '../../store/authStore'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import LanguageSelector from '../../components/ui/LanguageSelector'

const linkClass = ({ isActive }: { isActive: boolean }) =>
  `flex items-center gap-3 px-4 py-3 text-sm rounded-md transition-colors ${
    isActive ? 'bg-brand text-white' : 'text-foreground-muted hover:text-foreground hover:bg-surface'
  }`

export default function Cabinet() {
  const { logout, user } = useAuthStore()
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [confirmOpen, setConfirmOpen] = useState(false)
  return (
  <div className="min-h-screen bg-bg dark:bg-[#071428]">
      <div className="max-w-7xl mx-auto p-6">
        <div className="bg-transparent rounded-md overflow-hidden shadow-sm">
          <div className="flex">
            {/* left persistent sidenav */}
            <aside className="w-64 bg-white dark:bg-[#0b1228] dark:border-surface/30 border-r border-surface/40 p-4 text-foreground dark:text-gray-100 flex flex-col">
              <div className="mb-4 flex items-center gap-3">
                <div className="h-10 w-10 rounded-full bg-violet-200 dark:bg-violet-600 flex items-center justify-center text-sm font-semibold text-white">BM</div>
                <div>
                  <div className="text-sm font-medium">{(user?.firstName && user?.lastName) ? (user.firstName + ' ' + user.lastName) : t('common.user')}</div>
                  <div className="text-xs text-foreground-muted">{user?.email || ''}</div>
                </div>
              </div>

              <nav className="flex flex-col gap-1 flex-1 overflow-auto">
                <NavLink to="/cabinet/orders" className={linkClass}>
                  <span>📦</span>
                  {t('menu.orders')}
                </NavLink>
                <NavLink to="/cabinet/tracking" className={linkClass}>
                  <span>🚚</span>
                  {t('menu.tracking')}
                </NavLink>
                <NavLink to="/cabinet/favorites" className={linkClass}>
                  <span>🤍</span>
                  {t('menu.favorites')}
                </NavLink>
                <NavLink to="/cabinet/wallet" className={linkClass}>
                  <span>👛</span>
                  {t('menu.wallet')}
                </NavLink>
                <NavLink to="/cabinet/my-store" className={linkClass}>
                  <span>🏪</span>
                  {t('menu.myStore')}
                </NavLink>
                <NavLink to="/cabinet/products" className={linkClass}>
                  <span>📦</span>
                  {t('myProducts.title')}
                </NavLink>
                <NavLink to="/cabinet/user/settings?tab=profile" className={linkClass}>
                  <span>⚙️</span>
                  {t('menu.settings')}
                </NavLink>
                <NavLink to="/cabinet/support" className={linkClass}>
                  <span>📞</span>
                  {t('menu.support')}
                </NavLink>
                <NavLink to="/cabinet/help" className={linkClass}>
                  <span>❓</span>
                  {t('menu.help')}
                </NavLink>
              </nav>

              <div className="mt-4">
                <LanguageSelector align="left" dropUp />
              </div>

              <div className="mt-4">
                <button
                  onClick={() => setConfirmOpen(true)}
                  className="w-full rounded-md border border-foreground/20 px-4 py-2 text-sm text-foreground hover:bg-foreground/5"
                >
                  {t('logout')}
                </button>
              </div>
            </aside>

            {/* confirm modal for cabinet logout */}
            {confirmOpen && (
              <div className="fixed inset-0 z-60 flex items-center justify-center">
                <div className="absolute inset-0 bg-black/40" onClick={() => setConfirmOpen(false)} />
                <div className="relative z-10 w-full max-w-md rounded-md bg-white dark:bg-[#071428] p-6 shadow-lg">
                  <h3 className="text-lg font-semibold text-foreground dark:text-white">{t('confirm_logout_title')}</h3>
                  <p className="mt-2 text-sm text-foreground-muted dark:text-foreground-muted/80">{t('confirm_logout_text')}</p>
                  <div className="mt-4 flex justify-end gap-2">
                    <button
                      onClick={() => setConfirmOpen(false)}
                      className="rounded-md px-3 py-2 text-sm bg-transparent text-foreground-muted hover:text-foreground"
                    >
                      {t('cancel')}
                    </button>
                    <button
                      onClick={() => {
                        logout()
                        setConfirmOpen(false)
                        navigate('/')
                      }}
                      className="rounded-md bg-brand px-3 py-2 text-sm text-white"
                    >
                      {t('logout')}
                    </button>
                  </div>
                </div>
              </div>
            )}

            {/* main content area with tabs (child routes render here) */}
            <main className="flex-1 p-6 bg-surface">
              <Outlet />
            </main>
          </div>
        </div>
      </div>
    </div>
  )
}
