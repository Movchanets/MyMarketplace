import { useState, useEffect, useCallback, useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { productsApi, type ProductSummaryDto } from '../../api/catalogApi'

const ITEMS_PER_PAGE = 8

export default function MyProducts() {
  const { t } = useTranslation()
  const navigate = useNavigate()

  const [products, setProducts] = useState<ProductSummaryDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [deleteId, setDeleteId] = useState<string | null>(null)
  const [deleting, setDeleting] = useState(false)
  const [currentPage, setCurrentPage] = useState(1)

  const fetchProducts = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const response = await productsApi.getMy()
      if (response.isSuccess) {
        setProducts(response.payload || [])
      } else {
        setError(response.message || t('errors.fetch_failed'))
      }
    } catch {
      setError(t('errors.fetch_failed'))
    } finally {
      setLoading(false)
    }
  }, [t])

  useEffect(() => {
    fetchProducts()
  }, [fetchProducts])

  // Pagination calculations
  const totalPages = Math.ceil(products.length / ITEMS_PER_PAGE)
  const paginatedProducts = useMemo(() => {
    const start = (currentPage - 1) * ITEMS_PER_PAGE
    return products.slice(start, start + ITEMS_PER_PAGE)
  }, [products, currentPage])

  // Reset to page 1 if current page becomes invalid
  useEffect(() => {
    if (currentPage > totalPages && totalPages > 0) {
      setCurrentPage(totalPages)
    }
  }, [currentPage, totalPages])

  const handleDelete = async (productId: string) => {
    setDeleting(true)
    try {
      const response = await productsApi.delete(productId)
      if (response.isSuccess) {
        setProducts(products.filter(p => p.id !== productId))
        setDeleteId(null)
      } else {
        setError(response.message || t('errors.delete_failed'))
      }
    } catch {
      setError(t('errors.delete_failed'))
    } finally {
      setDeleting(false)
    }
  }

  const handleToggleActive = async (productId: string, currentActive: boolean) => {
    try {
      const response = await productsApi.toggleActive(productId, !currentActive)
      if (response.isSuccess) {
        setProducts(products.map(p => 
          p.id === productId ? { ...p, isActive: !currentActive } : p
        ))
      } else {
        setError(response.message || t('errors.update_failed'))
      }
    } catch {
      setError(t('errors.update_failed'))
    }
  }

  const goToPage = (page: number) => {
    setCurrentPage(Math.max(1, Math.min(page, totalPages)))
  }

  const handleViewProduct = (productSlug: string) => {
    navigate(`/product/${productSlug}`)
  }

  if (loading) {
    return (
      <div className="flex justify-center items-center min-h-[300px]">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-brand"></div>
      </div>
    )
  }

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-text">{t('myProducts.title')}</h1>
          {products.length > 0 && (
            <p className="text-sm text-text-muted mt-1">
              {t('myProducts.totalCount', { count: products.length })}
            </p>
          )}
        </div>
        <button
          onClick={() => navigate('/cabinet/products/create')}
          className="btn btn-brand flex items-center gap-2"
        >
          <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
          </svg>
          {t('myProducts.addProduct')}
        </button>
      </div>

      {/* Error */}
      {error && (
        <div className="bg-red-100 dark:bg-red-900/30 border border-red-400 dark:border-red-700 text-red-700 dark:text-red-300 px-4 py-3 rounded">
          {error}
          <button onClick={() => setError(null)} className="float-right font-bold">&times;</button>
        </div>
      )}

      {/* Products Grid */}
      {products.length === 0 ? (
        <div className="card p-12 text-center">
          <svg className="w-16 h-16 mx-auto text-text-muted mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
          </svg>
          <h2 className="text-xl font-semibold text-text mb-2">{t('myProducts.empty')}</h2>
          <p className="text-text-muted mb-6">{t('myProducts.emptyHint')}</p>
          <button
            onClick={() => navigate('/cabinet/products/create')}
            className="btn btn-brand"
          >
            {t('myProducts.createFirst')}
          </button>
        </div>
      ) : (
        <>
          <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
            {paginatedProducts.map((product) => (
              <div 
                key={product.id} 
                className="card overflow-hidden flex flex-col hover:shadow-lg transition-shadow duration-200"
              >
                {/* Product Image */}
                <div 
                  className="relative aspect-[4/3] bg-background-secondary overflow-hidden cursor-pointer group"
                  onClick={() => handleViewProduct(product.slug)}
                >
                  {product.baseImageUrl ? (
                    <img
                      src={product.baseImageUrl}
                      alt={product.name}
                      className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
                    />
                  ) : (
                    <div className="w-full h-full flex items-center justify-center bg-gradient-to-br from-gray-100 to-gray-200 dark:from-gray-800 dark:to-gray-900">
                      <svg className="w-16 h-16 text-text-muted opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
                      </svg>
                    </div>
                  )}

                  {/* Stock Status Badge */}
                  <div className="absolute top-3 left-3">
                    {product.inStock ? (
                      <span className="px-2.5 py-1 text-xs font-semibold rounded-full bg-green-500 text-white shadow-sm">
                        {t('myProducts.inStock')}
                      </span>
                    ) : (
                      <span className="px-2.5 py-1 text-xs font-semibold rounded-full bg-red-500 text-white shadow-sm">
                        {t('myProducts.outOfStock')}
                      </span>
                    )}
                  </div>

                  {/* Price Badge */}
                  <div className="absolute bottom-3 right-3">
                    <span className="px-3 py-1.5 text-sm font-bold rounded-lg bg-white/90 dark:bg-gray-900/90 text-brand shadow-sm backdrop-blur-sm">
                      {product.minPrice != null ? `${product.minPrice.toFixed(2)} ₴` : '—'}
                    </span>
                  </div>
                </div>

                {/* Product Info */}
                <div className="p-4 flex-1 flex flex-col gap-3">
                  <div>
                    <h3 className="font-semibold text-text line-clamp-2 leading-tight" title={product.name}>
                      {product.name}
                    </h3>
                    {product.categories.length > 0 && (
                      <p className="text-sm text-text-muted mt-1">
                        {product.categories[0].name}
                      </p>
                    )}
                  </div>

                  {/* Tags */}
                  {product.tags.length > 0 && (
                    <div className="flex flex-wrap gap-1.5">
                      {product.tags.slice(0, 3).map(tag => (
                        <span
                          key={tag.id}
                          className="px-2 py-0.5 text-xs rounded-md bg-brand/10 text-brand font-medium"
                        >
                          {tag.name}
                        </span>
                      ))}
                      {product.tags.length > 3 && (
                        <span className="px-2 py-0.5 text-xs text-text-muted">
                          +{product.tags.length - 3}
                        </span>
                      )}
                    </div>
                  )}

                  {/* Action Buttons */}
                  <div className="mt-auto pt-3 flex flex-col gap-2 border-t border-border">
                    {/* View Product Button */}
                    <button
                      onClick={() => handleViewProduct(product.slug)}
                      className="w-full flex items-center justify-center gap-2 px-3 py-2.5 text-sm font-medium rounded-lg bg-blue-500/10 hover:bg-blue-500/20 transition-colors text-blue-600 dark:text-blue-400"
                    >
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                      </svg>
                      {t('common.view')}
                    </button>

                    {/* Active Toggle */}
                    <button
                      onClick={() => handleToggleActive(product.id, product.isActive)}
                      className={`w-full flex items-center justify-center gap-2 px-3 py-2.5 text-sm font-medium rounded-lg transition-colors ${
                        product.isActive 
                          ? 'bg-green-500/10 hover:bg-green-500/20 text-green-600 dark:text-green-400'
                          : 'bg-gray-500/10 hover:bg-gray-500/20 text-gray-600 dark:text-gray-400'
                      }`}
                    >
                      {product.isActive ? (
                        <>
                          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                          </svg>
                          {t('myProducts.active')}
                        </>
                      ) : (
                        <>
                          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21" />
                          </svg>
                          {t('myProducts.inactive')}
                        </>
                      )}
                    </button>

                    {/* Edit Button */}
                    <button
                      onClick={() => navigate(`/cabinet/products/${product.id}/edit`)}
                      className="w-full flex items-center justify-center gap-2 px-3 py-2.5 text-sm font-medium rounded-lg bg-brand/10 hover:bg-brand/20 transition-colors text-brand"
                    >
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                      </svg>
                      {t('common.edit')}
                    </button>

                    {/* SKU Management Button */}
                    <button
                      onClick={() => navigate(`/cabinet/products/${product.id}/skus`)}
                      className="w-full flex items-center justify-center gap-2 px-3 py-2.5 text-sm font-medium rounded-lg bg-purple-500/10 hover:bg-purple-500/20 transition-colors text-purple-600 dark:text-purple-400"
                    >
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 7h.01M7 3h5c.512 0 1.024.195 1.414.586l7 7a2 2 0 010 2.828l-7 7a2 2 0 01-2.828 0l-7-7A1.994 1.994 0 013 12V7a4 4 0 014-4z" />
                      </svg>
                      {t('myProducts.manageVariants')}
                    </button>

                    {/* Delete Button */}
                    <button
                      onClick={() => setDeleteId(product.id)}
                      className="w-full flex items-center justify-center gap-2 px-3 py-2.5 text-sm font-medium rounded-lg bg-red-500/10 hover:bg-red-500/20 transition-colors text-red-600 dark:text-red-400"
                    >
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                      </svg>
                      {t('common.delete')}
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-center gap-2 pt-6">
              {/* Previous Button */}
              <button
                onClick={() => goToPage(currentPage - 1)}
                disabled={currentPage === 1}
                className="p-2 rounded-lg border border-border hover:bg-background-secondary disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                aria-label={t('pagination.previous')}
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
                </svg>
              </button>

              {/* Page Numbers */}
              <div className="flex items-center gap-1">
                {Array.from({ length: totalPages }, (_, i) => i + 1).map((page) => {
                  // Show first, last, current, and adjacent pages
                  const showPage = page === 1 || 
                    page === totalPages || 
                    Math.abs(page - currentPage) <= 1

                  // Show ellipsis
                  const showEllipsisBefore = page === currentPage - 2 && currentPage > 3
                  const showEllipsisAfter = page === currentPage + 2 && currentPage < totalPages - 2

                  if (showEllipsisBefore || showEllipsisAfter) {
                    return (
                      <span key={page} className="px-2 text-text-muted">
                        …
                      </span>
                    )
                  }

                  if (!showPage) return null

                  return (
                    <button
                      key={page}
                      onClick={() => goToPage(page)}
                      className={`min-w-[40px] h-10 rounded-lg font-medium transition-colors ${
                        currentPage === page
                          ? 'bg-brand text-white'
                          : 'hover:bg-background-secondary text-text'
                      }`}
                    >
                      {page}
                    </button>
                  )
                })}
              </div>

              {/* Next Button */}
              <button
                onClick={() => goToPage(currentPage + 1)}
                disabled={currentPage === totalPages}
                className="p-2 rounded-lg border border-border hover:bg-background-secondary disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                aria-label={t('pagination.next')}
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                </svg>
              </button>
            </div>
          )}

          {/* Page Info */}
          {totalPages > 1 && (
            <p className="text-center text-sm text-text-muted">
              {t('pagination.pageInfo', { current: currentPage, total: totalPages })}
            </p>
          )}
        </>
      )}

      {/* Delete Confirmation Modal */}
      {deleteId && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm">
          <div className="card p-6 max-w-md mx-4 shadow-2xl">
            <div className="flex items-center gap-4 mb-4">
              <div className="p-3 rounded-full bg-red-100 dark:bg-red-900/30">
                <svg className="w-6 h-6 text-red-600 dark:text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                </svg>
              </div>
              <div>
                <h2 className="text-xl font-bold text-text">{t('myProducts.deleteConfirm.title')}</h2>
                <p className="text-text-muted text-sm">{t('myProducts.deleteConfirm.message')}</p>
              </div>
            </div>
            <div className="flex gap-3 justify-end mt-6">
              <button
                onClick={() => setDeleteId(null)}
                className="btn btn-secondary"
                disabled={deleting}
              >
                {t('common.cancel')}
              </button>
              <button
                onClick={() => handleDelete(deleteId)}
                className="px-4 py-2.5 rounded-lg bg-red-600 hover:bg-red-700 text-white font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                disabled={deleting}
              >
                {deleting ? (
                  <span className="flex items-center gap-2">
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                    {t('common.deleting')}
                  </span>
                ) : (
                  t('common.delete')
                )}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
