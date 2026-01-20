import { useTranslation } from 'react-i18next'
import LanguageSelector from '../ui/LanguageSelector'

export function Footer() {
  const { t } = useTranslation()

  return (
    <footer className="mt-16 border-t border-foreground/10 bg-surface/95">
      <div className="mx-auto w-full max-w-7xl px-4 py-8 text-sm text-foreground-muted">
        <div className="flex flex-col items-center justify-between gap-4 md:flex-row">
          <p>{t('footer.copy', { year: new Date().getFullYear() })}</p>

          <div className="flex items-center gap-4">
            <a href="#" className="hover:text-foreground">{t('footer.terms')}</a>
            <a href="#" className="hover:text-foreground">{t('footer.privacy')}</a>
            <a href="#" className="hover:text-foreground">{t('footer.support')}</a>
            <div className="ml-2">
              <LanguageSelector dropUp />
            </div>
          </div>
        </div>
      </div>
    </footer>
  )
}
