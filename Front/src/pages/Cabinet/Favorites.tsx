import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useFavoritesStore } from '../../store/favoritesStore'

export default function Favorites() {
  const { t } = useTranslation()
  const { favorites, isLoading, loadFavorites } = useFavoritesStore()

  useEffect(() => {
    loadFavorites()
  }, [loadFavorites])

  if (isLoading) {
    return (
      <div className="flex justify-center items-center min-h-[400px]">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-brand"></div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-3xl font-bold text-text">{t('menu.favorites')}</h1>
        <span className="text-sm text-text-muted">
          {favorites.size} {t('favorites.items', { count: favorites.size })}
        </span>
      </div>

      {favorites.size === 0 ? (
        <div className="text-center py-16">
          <div className="max-w-md mx-auto">
            <svg className="w-24 h-24 text-text-muted mx-auto mb-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
            </svg>
            <h2 className="text-2xl font-semibold text-text mb-2">{t('favorites.empty.title')}</h2>
            <p className="text-text-muted mb-6">{t('favorites.empty.description')}</p>
            <a
              href="/"
              className="inline-flex items-center px-6 py-3 bg-brand hover:bg-brand-dark text-white font-medium rounded-lg transition-colors"
            >
              {t('favorites.empty.browse')}
            </a>
          </div>
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
          {/* For now, we'll show placeholder cards since we don't have the full product data */}
          {/* In a real implementation, you'd fetch the product details or use the data from the store */}
          {Array.from(favorites).map((productId: string) => (
            <div key={productId} className="card p-4">
              <div className="text-center text-text-muted">
                <p>{t('favorites.productId')}: {productId}</p>
                <p className="text-sm mt-2">{t('favorites.implementationNote')}</p>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}