import { useTranslation } from 'react-i18next'
import { CatalogSection } from '../../components/catalog/CatalogSection'

export default function Home() {
  const { t } = useTranslation()

  return (
    <div className="space-y-10">
      {/* Hero section */}
      <section className="grid gap-6 md:grid-cols-2 md:items-center">
        <div className="space-y-4">
          <h1 className="text-3xl font-bold tracking-tight text-text md:text-5xl">{t('site.name')}</h1>
          <p className="max-w-prose text-text-muted">{t('site.home.tagline')}</p>
          <div className="flex gap-3">
            <button className="btn-primary">{t('home.cta.catalog')}</button>
            <button className="rounded-md border border-text/20 px-4 py-2 text-text hover:bg-text/5">
              {t('home.cta.learn_more')}
            </button>
          </div>
        </div>
        <div className="card h-56 md:h-72" />
      </section>

      {/* Catalog Section - Categories grid */}
      <CatalogSection />

      {/* Recommendations */}
      <section>
        <h2 className="mb-4 text-xl font-semibold text-text">{t('home.recommendations')}</h2>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {[1, 2, 3, 4, 5, 6, 7, 8].map((i) => (
            <div key={i} className="card h-56" />
          ))}
        </div>
      </section>
    </div>
  )
}
