import { useState, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import {
  useAdminStores,
  useSuspendStore,
  useUnsuspendStore,
  useVerifyStore,
} from '../../hooks/queries/useAdminManagement'

const ITEMS_PER_PAGE = 10

export default function StoresManagement() {
  const { t } = useTranslation()
  const [error, setError] = useState<string | null>(null)
  const [currentPage, setCurrentPage] = useState(1)
  const [actionLoading, setActionLoading] = useState<string | null>(null)
  const storesQuery = useAdminStores(true)
  const verifyStoreMutation = useVerifyStore()
  const suspendStoreMutation = useSuspendStore()
  const unsuspendStoreMutation = useUnsuspendStore()
  const stores = storesQuery.data ?? []
  const loading = storesQuery.isLoading
  const queryError = storesQuery.error instanceof Error ? storesQuery.error.message : null

  // Pagination
  const totalPages = Math.ceil(stores.length / ITEMS_PER_PAGE)
  const paginatedStores = useMemo(() => {
    const start = (currentPage - 1) * ITEMS_PER_PAGE
    return stores.slice(start, start + ITEMS_PER_PAGE)
  }, [stores, currentPage])

  const handleVerify = async (id: string) => {
    setActionLoading(id)
    try {
      setError(null)
      await verifyStoreMutation.mutateAsync(id)
    } catch (err) {
      setError(err instanceof Error ? err.message : t('errors.save_failed'))
    } finally {
      setActionLoading(null)
    }
  }

  const handleSuspend = async (id: string) => {
    if (!window.confirm(t('admin.stores.confirm_suspend'))) return
    setActionLoading(id)
    try {
      setError(null)
      await suspendStoreMutation.mutateAsync(id)
    } catch (err) {
      setError(err instanceof Error ? err.message : t('errors.save_failed'))
    } finally {
      setActionLoading(null)
    }
  }

  const handleUnsuspend = async (id: string) => {
    setActionLoading(id)
    try {
      setError(null)
      await unsuspendStoreMutation.mutateAsync(id)
    } catch (err) {
      setError(err instanceof Error ? err.message : t('errors.save_failed'))
    } finally {
      setActionLoading(null)
    }
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString()
  }

  if (loading) {
    return (
      <div className="flex justify-center items-center min-h-[200px]">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-brand"></div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-2xl font-bold text-foreground">{t('admin.stores.title')}</h2>
      </div>

      {(error || queryError) && (
        <div className="bg-red-100 dark:bg-red-900/30 border border-red-400 dark:border-red-700 text-red-700 dark:text-red-300 px-4 py-3 rounded">
          {error || queryError}
        </div>
      )}

      {/* Stores Table */}
      <div className="card overflow-hidden">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-border">
            <thead className="bg-surface-secondary">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-foreground-muted uppercase tracking-wider">
                  {t('admin.stores.name')}
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-foreground-muted uppercase tracking-wider">
                  {t('admin.stores.owner')}
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-foreground-muted uppercase tracking-wider">
                  {t('admin.stores.status')}
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-foreground-muted uppercase tracking-wider">
                  {t('admin.stores.products')}
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-foreground-muted uppercase tracking-wider">
                  {t('admin.stores.created')}
                </th>
                <th className="px-6 py-3 text-right text-xs font-medium text-foreground-muted uppercase tracking-wider">
                  {t('admin.catalog.actions')}
                </th>
              </tr>
            </thead>
            <tbody className="bg-surface divide-y divide-border">
              {paginatedStores.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-6 py-4 text-center text-foreground-muted">
                    {t('admin.stores.no_stores')}
                  </td>
                </tr>
              ) : (
                paginatedStores.map(store => (
                  <tr key={store.id} className="hover:bg-surface-secondary/50 transition-colors">
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="text-foreground font-medium">{store.name}</div>
                      <div className="text-foreground-muted text-sm">{store.slug}</div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="text-foreground text-sm">{store.ownerName || '-'}</div>
                      <div className="text-foreground-muted text-xs">{store.ownerEmail || '-'}</div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="flex flex-col gap-1">
                        {store.isVerified ? (
                          <span className="inline-flex px-2 py-1 rounded-full bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-300 text-xs font-medium">
                            {t('admin.stores.verified')}
                          </span>
                        ) : (
                          <span className="inline-flex px-2 py-1 rounded-full bg-yellow-100 dark:bg-yellow-900/30 text-yellow-700 dark:text-yellow-300 text-xs font-medium">
                            {t('admin.stores.unverified')}
                          </span>
                        )}
                        {store.isSuspended && (
                          <span className="inline-flex px-2 py-1 rounded-full bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-300 text-xs font-medium">
                            {t('admin.stores.suspended')}
                          </span>
                        )}
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-foreground-muted text-sm">
                      {store.productCount}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-foreground-muted text-sm">
                      {formatDate(store.createdAt)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm">
                      <div className="flex justify-end gap-2">
                        {!store.isVerified && (
                          <button
                            onClick={() => handleVerify(store.id)}
                            disabled={actionLoading === store.id}
                            className="px-3 py-1 rounded bg-green-600 hover:bg-green-700 text-white text-xs font-medium
                              disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                          >
                            {actionLoading === store.id ? '...' : t('admin.stores.verify')}
                          </button>
                        )}
                        {store.isSuspended ? (
                          <button
                            onClick={() => handleUnsuspend(store.id)}
                            disabled={actionLoading === store.id}
                            className="px-3 py-1 rounded bg-blue-600 hover:bg-blue-700 text-white text-xs font-medium
                              disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                          >
                            {actionLoading === store.id ? '...' : t('admin.stores.unsuspend')}
                          </button>
                        ) : (
                          <button
                            onClick={() => handleSuspend(store.id)}
                            disabled={actionLoading === store.id}
                            className="px-3 py-1 rounded bg-red-600 hover:bg-red-700 text-white text-xs font-medium
                              disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                          >
                            {actionLoading === store.id ? '...' : t('admin.stores.suspend')}
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        {totalPages > 1 && (
          <div className="flex items-center justify-between px-6 py-4 border-t border-border bg-surface-secondary/30">
            <div className="text-sm text-foreground-muted">
              {t('admin.catalog.showing')} {(currentPage - 1) * ITEMS_PER_PAGE + 1}-
              {Math.min(currentPage * ITEMS_PER_PAGE, stores.length)} {t('admin.catalog.of')} {stores.length}
            </div>
            <div className="flex gap-2">
              <button
                onClick={() => setCurrentPage(p => Math.max(1, p - 1))}
                disabled={currentPage === 1}
                className="px-3 py-1 rounded border border-border text-foreground-muted hover:bg-surface-secondary 
                  disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                {t('admin.catalog.prev')}
              </button>
              {Array.from({ length: totalPages }, (_, i) => i + 1).map(page => (
                <button
                  key={page}
                  onClick={() => setCurrentPage(page)}
                  className={`px-3 py-1 rounded border transition-colors ${
                    currentPage === page
                      ? 'bg-brand text-white border-brand'
                      : 'border-border text-foreground-muted hover:bg-surface-secondary'
                  }`}
                >
                  {page}
                </button>
              ))}
              <button
                onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))}
                disabled={currentPage === totalPages}
                className="px-3 py-1 rounded border border-border text-foreground-muted hover:bg-surface-secondary 
                  disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                {t('admin.catalog.next')}
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
