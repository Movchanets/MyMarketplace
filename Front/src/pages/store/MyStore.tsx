import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { myStoreApi, type MyStoreDto } from '../../api/storeApi'

export default function MyStore() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [store, setStore] = useState<MyStoreDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const fetchStore = async () => {
      setLoading(true)
      try {
        const response = await myStoreApi.get()
        if (response.isSuccess) {
          if (response.payload) {
            setStore(response.payload)
          } else {
            // No store - redirect to create
            navigate('/cabinet/create-store')
          }
        } else {
          setError(response.message || t('errors.fetch_failed'))
        }
      } catch {
        setError(t('errors.fetch_failed'))
      } finally {
        setLoading(false)
      }
    }

    fetchStore()
  }, [navigate, t])

  if (loading) {
    return (
      <div className="flex justify-center items-center min-h-[300px]">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-brand"></div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="p-6">
        <div className="bg-red-100 dark:bg-red-900/30 border border-red-400 dark:border-red-700 text-red-700 dark:text-red-300 px-4 py-3 rounded">
          {error}
        </div>
      </div>
    )
  }

  if (!store) {
    return null
  }

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-text">{store.name}</h1>
          <p className="text-text-muted">{store.slug}</p>
        </div>
        <div className="flex gap-2">
          {store.isVerified ? (
            <span className="px-3 py-1 rounded-full bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-300 text-sm font-medium">
              {t('store.status.verified')}
            </span>
          ) : (
            <span className="px-3 py-1 rounded-full bg-yellow-100 dark:bg-yellow-900/30 text-yellow-700 dark:text-yellow-300 text-sm font-medium">
              {t('store.status.unverified')}
            </span>
          )}
          {store.isSuspended && (
            <span className="px-3 py-1 rounded-full bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-300 text-sm font-medium">
              {t('store.status.suspended')}
            </span>
          )}
        </div>
      </div>

      {/* Store Stats */}
      <div className="grid gap-4 md:grid-cols-3">
        <div className="card p-6">
          <div className="text-3xl font-bold text-brand">{store.productCount}</div>
          <div className="text-text-muted text-sm">{t('store.stats.products')}</div>
        </div>
        <div className="card p-6">
          <div className="text-3xl font-bold text-text">0</div>
          <div className="text-text-muted text-sm">{t('store.stats.orders')}</div>
        </div>
        <div className="card p-6">
          <div className="text-text-muted text-sm">{t('store.stats.created')}</div>
          <div className="text-text font-medium">{new Date(store.createdAt).toLocaleDateString()}</div>
        </div>
      </div>

      {/* Store Description */}
      {store.description && (
        <div className="card p-6">
          <h2 className="text-lg font-semibold text-text mb-2">{t('store.description')}</h2>
          <p className="text-text-muted">{store.description}</p>
        </div>
      )}

      {/* Verification Notice */}
      {!store.isVerified && (
        <div className="bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg p-4">
          <div className="flex items-start">
            <svg className="w-5 h-5 text-yellow-500 mt-0.5 mr-3" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
            </svg>
            <div>
              <h3 className="text-yellow-800 dark:text-yellow-200 font-medium">{t('store.verification_pending_title')}</h3>
              <p className="text-yellow-700 dark:text-yellow-300 text-sm mt-1">{t('store.verification_pending_message')}</p>
            </div>
          </div>
        </div>
      )}

      {/* Suspended Notice */}
      {store.isSuspended && (
        <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-4">
          <div className="flex items-start">
            <svg className="w-5 h-5 text-red-500 mt-0.5 mr-3" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
            </svg>
            <div>
              <h3 className="text-red-800 dark:text-red-200 font-medium">{t('store.suspended_title')}</h3>
              <p className="text-red-700 dark:text-red-300 text-sm mt-1">{t('store.suspended_message')}</p>
            </div>
          </div>
        </div>
      )}

      {/* Quick Actions */}
      <div className="card p-6">
        <h2 className="text-lg font-semibold text-text mb-4">{t('store.quick_actions')}</h2>
        <div className="flex flex-wrap gap-3">
          <button
            className="btn btn-brand"
            disabled={store.isSuspended}
            onClick={() => navigate('/cabinet/products/create')}
          >
            {t('store.add_product')}
          </button>
          <button className="btn btn-secondary">
            {t('store.edit_store')}
          </button>
        </div>
      </div>
    </div>
  )
}
