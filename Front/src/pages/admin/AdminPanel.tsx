import { Link, Outlet, useLocation } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useAuthStore } from '../../store/authStore'

export default function AdminPanel() {
  const { t } = useTranslation()
  const { user } = useAuthStore()
  const location = useLocation()

  const isRootAdmin = location.pathname === '/admin'

  const navItems = [
    { path: '/admin', label: t('admin.nav.dashboard'), exact: true },
    { path: '/admin/categories', label: t('admin.nav.categories') },
    { path: '/admin/tags', label: t('admin.nav.tags') },
    { path: '/admin/stores', label: t('admin.nav.stores') },
    { path: '/admin/users', label: t('admin.nav.users') },
    { path: '/admin/roles', label: t('admin.nav.roles') },
  ]

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-text">{t('admin.title')}</h1>
        <p className="text-text-muted mt-2">{t('admin.welcome', { name: user?.firstName || user?.name })}</p>
      </div>

      {/* Navigation Tabs */}
      <nav className="border-b border-border">
        <div className="flex gap-4">
          {navItems.map(item => {
            const isActive = item.exact 
              ? location.pathname === item.path
              : location.pathname.startsWith(item.path) && item.path !== '/admin'
            return (
              <Link
                key={item.path}
                to={item.path}
                className={`px-4 py-2 -mb-px text-sm font-medium border-b-2 transition-colors ${
                  isActive
                    ? 'border-brand text-brand'
                    : 'border-transparent text-text-muted hover:text-text hover:border-border'
                }`}
              >
                {item.label}
              </Link>
            )
          })}
        </div>
      </nav>

      {/* Content Area */}
      {isRootAdmin ? (
        <>
          {/* Dashboard Cards */}
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            <Link to="/admin/categories" className="card p-6 hover:shadow-md transition-shadow">
              <h3 className="text-lg font-semibold text-text mb-2">{t('admin.cards.categories')}</h3>
              <p className="text-text-muted text-sm">{t('admin.cards.categories_desc')}</p>
            </Link>
            <Link to="/admin/tags" className="card p-6 hover:shadow-md transition-shadow">
              <h3 className="text-lg font-semibold text-text mb-2">{t('admin.cards.tags')}</h3>
              <p className="text-text-muted text-sm">{t('admin.cards.tags_desc')}</p>
            </Link>
            <Link to="/admin/stores" className="card p-6 hover:shadow-md transition-shadow">
              <h3 className="text-lg font-semibold text-text mb-2">{t('admin.cards.stores')}</h3>
              <p className="text-text-muted text-sm">{t('admin.cards.stores_desc')}</p>
            </Link>
            <div className="card p-6">
              <h3 className="text-lg font-semibold text-text mb-2">{t('admin.cards.orders')}</h3>
              <p className="text-text-muted text-sm">{t('admin.cards.orders_desc')}</p>
            </div>
            <Link to="/admin/users" className="card p-6 hover:shadow-md transition-shadow">
              <h3 className="text-lg font-semibold text-text mb-2">{t('admin.cards.users')}</h3>
              <p className="text-text-muted text-sm">{t('admin.cards.users_desc')}</p>
            </Link>
            <Link to="/admin/roles" className="card p-6 hover:shadow-md transition-shadow">
              <h3 className="text-lg font-semibold text-text mb-2">{t('admin.cards.roles')}</h3>
              <p className="text-text-muted text-sm">{t('admin.cards.roles_desc')}</p>
            </Link>
          </div>

          {/* Roles & Permissions */}
          <div className="card p-6">
            <h3 className="text-lg font-semibold text-text mb-4">{t('admin.your_roles')}</h3>
            <div className="flex flex-wrap gap-2">
              {user?.roles && user.roles.length > 0 ? (
                user.roles.map((role, idx) => (
                  <span
                    key={idx}
                    className="px-3 py-1 rounded-full bg-brand/10 text-brand text-sm font-medium"
                  >
                    {role}
                  </span>
                ))
              ) : (
                <span className="text-text-muted text-sm">{t('admin.no_roles')}</span>
              )}
            </div>
          </div>

          <div className="card p-6">
            <h3 className="text-lg font-semibold text-text mb-4">{t('admin.your_permissions')}</h3>
            <div className="flex flex-wrap gap-2">
              {user?.permissions && user.permissions.length > 0 ? (
                user.permissions.map((permission, idx) => (
                  <span
                    key={idx}
                    className="px-3 py-1 rounded-full bg-violet-100 dark:bg-violet-900/30 text-violet-700 dark:text-violet-300 text-sm font-medium"
                  >
                    {permission}
                  </span>
                ))
              ) : (
                <span className="text-text-muted text-sm">{t('admin.no_permissions')}</span>
              )}
            </div>
          </div>
        </>
      ) : (
        <Outlet />
      )}
    </div>
  )
}
