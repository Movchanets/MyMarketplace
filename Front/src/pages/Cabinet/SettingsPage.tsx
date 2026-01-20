import { Link, useSearchParams, useNavigate } from 'react-router-dom'
import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'

import ProfileForm from '../../components/Cabinet/ProfileForm'
import ChangePasswordForm from '../../components/Cabinet/ChangePasswordForm'

export default function SettingsPage() {
  const { t } = useTranslation()
  const tabs: { id: string; label: string }[] = [
    { id: 'profile', label: t('settings.tabs.profile') },
    { id: 'security', label: t('settings.tabs.security') },
    { id: 'notifications', label: t('settings.tabs.notifications') },
    { id: 'payments', label: t('settings.tabs.payments') },
  ]

  const [searchParams, setSearchParams] = useSearchParams()
  const navigate = useNavigate()

  // Choose active tab from ?tab=... default to profile
  const active = searchParams.get('tab') || 'profile'

  useEffect(() => {
    // Ensure a tab is present in URL; if not, push default
    if (!searchParams.get('tab')) {
      setSearchParams({ tab: 'profile' }, { replace: true })
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const onSelect = (id: string) => {
    setSearchParams({ tab: id })
    // keep user in the cabinet settings route
    navigate({ pathname: '/cabinet/user/settings', search: `?tab=${id}` })
  }

  return (
    <div className="p-6">
      <h1 className="text-2xl font-semibold mb-4">{t('menu.settings')}</h1>

      <div className="mb-6 border-b border-surface/60 dark:border-surface-card/30">
        <nav className="flex gap-2 -mb-px">
          {tabs.map((t) => (
            <button
              key={t.id}
              onClick={() => onSelect(t.id)}
              className={`px-4 py-2 text-sm rounded-t-md border-b-2 transition-colors ${
                active === t.id
                  ? 'border-brand text-brand bg-surface dark:bg-surface-card/5'
                  : 'border-transparent text-text-muted dark:text-text-muted hover:text-text dark:hover:text-text'
              }`}
              aria-current={active === t.id ? 'page' : undefined}
            >
              {t.label}
            </button>
          ))}
        </nav>
      </div>

      <div className="bg-surface-card p-6 rounded-md shadow-sm">
        {active === 'profile' && (
          <section>
            <h2 className="text-lg font-medium mb-2 text-text dark:text-white">{t('settings.profile.title')}</h2>
            <p className="text-sm text-text-muted dark:text-text-muted/80">{t('settings.profile.description')}</p>
            <div className="mt-4">
              <ProfileForm />
            </div>
          </section>
        )}

        {active === 'security' && (
          <section>
            <h2 className="text-lg font-medium mb-2">{t('settings.security.title')}</h2>
            <p className="text-sm text-text-muted">{t('settings.security.description')}</p>
            <div className="mt-4">
              <ChangePasswordForm />
            </div>
          </section>
        )}

        {active === 'notifications' && (
          <section>
            <h2 className="text-lg font-medium mb-2">{t('settings.notifications.title')}</h2>
            <p className="text-sm text-text-muted">{t('settings.notifications.description')}</p>
          </section>
        )}

        {active === 'payments' && (
          <section>
            <h2 className="text-lg font-medium mb-2">{t('settings.payments.title')}</h2>
            <p className="text-sm text-text-muted">{t('settings.payments.description')}</p>
          </section>
        )}

        <div className="mt-6">
          <Link to="/cabinet" className="text-sm text-brand hover:underline">
            {t('settings.return_to_cabinet')}
          </Link>
        </div>
      </div>
    </div>
  )
}
