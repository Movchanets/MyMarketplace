import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { myStoreApi, type MyStoreDto } from '../../api/storeApi'
import { ErrorAlert } from '../../components/ui/ErrorAlert'
import { WarningAlert } from '../../components/ui/WarningAlert'

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
         <ErrorAlert>{error}</ErrorAlert>
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
          <h1 className="text-2xl font-bold text-foreground">{store.name}</h1>
          <p className="text-foreground-muted">{store.slug}</p>
        </div>
         <div className="flex gap-2">
           {store.isVerified ? (
             <span className="px-3 py-1 rounded-full bg-success-light dark:bg-success-dark/20 text-success dark:text-success text-sm font-medium">
               {t('store.status.verified')}
             </span>
           ) : (
             <span className="px-3 py-1 rounded-full bg-warning-light dark:bg-warning-dark/20 text-warning dark:text-warning text-sm font-medium">
               {t('store.status.unverified')}
             </span>
           )}
           {store.isSuspended && (
             <span className="px-3 py-1 rounded-full bg-error-light dark:bg-error-dark/20 text-error dark:text-error text-sm font-medium">
               {t('store.status.suspended')}
             </span>
           )}
        </div>
      </div>

      {/* Store Stats */}
      <div className="grid gap-4 md:grid-cols-3">
        <div className="card p-6">
          <div className="text-3xl font-bold text-brand">{store.productCount}</div>
          <div className="text-foreground-muted text-sm">{t('store.stats.products')}</div>
        </div>
        <div className="card p-6">
          <div className="text-3xl font-bold text-foreground">0</div>
          <div className="text-foreground-muted text-sm">{t('store.stats.orders')}</div>
        </div>
        <div className="card p-6">
          <div className="text-foreground-muted text-sm">{t('store.stats.created')}</div>
          <div className="text-foreground font-medium">{new Date(store.createdAt).toLocaleDateString()}</div>
        </div>
      </div>

      {/* Store Description */}
      {store.description && (
        <div className="card p-6">
          <h2 className="text-lg font-semibold text-foreground mb-2">{t('store.description')}</h2>
          <p className="text-foreground-muted">{store.description}</p>
        </div>
      )}

       {/* Verification Notice */}
       {!store.isVerified && (
         <WarningAlert>
           <div className="flex items-start">
             <svg className="w-5 h-5 text-warning mt-0.5 mr-3" fill="currentColor" viewBox="0 0 20 20">
               <path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
             </svg>
             <div>
               <h3 className="text-warning-dark dark:text-warning font-medium">{t('store.verification_pending_title')}</h3>
               <p className="text-warning-text dark:text-warning text-sm mt-1">{t('store.verification_pending_message')}</p>
             </div>
           </div>
         </WarningAlert>
       )}

       {/* Suspended Notice */}
       {store.isSuspended && (
         <ErrorAlert>
           <div className="flex items-start">
             <svg className="w-5 h-5 text-error mt-0.5 mr-3" fill="currentColor" viewBox="0 0 20 20">
               <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
             </svg>
             <div>
               <h3 className="text-error-dark dark:text-error font-medium">{t('store.suspended_title')}</h3>
               <p className="text-error-text dark:text-error text-sm mt-1">{t('store.suspended_message')}</p>
             </div>
           </div>
         </ErrorAlert>
       )}

      {/* Quick Actions */}
      <div className="card p-6">
        <h2 className="text-lg font-semibold text-foreground mb-4">{t('store.quick_actions')}</h2>
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
