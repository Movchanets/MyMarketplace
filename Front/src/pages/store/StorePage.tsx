import { useState, useEffect, useMemo } from 'react'
import { useParams, Link, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { storesApi, type PublicStoreDto } from '../../api/storesApi'
import ProductCard from '../../components/catalog/ProductCard'

const ITEMS_PER_PAGE = 12

export default function StorePage() {
  const { t } = useTranslation()
  const { slug } = useParams<{ slug: string }>()
  const navigate = useNavigate()

  const [store, setStore] = useState<PublicStoreDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [currentPage, setCurrentPage] = useState(1)

  useEffect(() => {
    const fetchStore = async () => {
      if (!slug) return

      setLoading(true)
      setError(null)
      try {
        const response = await storesApi.getBySlug(slug)
        if (response.isSuccess && response.payload) {
          setStore(response.payload)
        } else {
          setError(response.message || t('storePage.notFound'))
        }
      } catch {
        setError(t('errors.fetch_failed'))
      } finally {
        setLoading(false)
      }
    }

    fetchStore()
  }, [slug, t])

  // Pagination
  const totalPages = store ? Math.ceil(store.products.length / ITEMS_PER_PAGE) : 0
  const paginatedProducts = useMemo(() => {
    if (!store) return []
    const start = (currentPage - 1) * ITEMS_PER_PAGE
    return store.products.slice(start, start + ITEMS_PER_PAGE)
  }, [store, currentPage])

  useEffect(() => {
    if (currentPage > totalPages && totalPages > 0) {
      setCurrentPage(totalPages)
    }
  }, [currentPage, totalPages])

  const goToPage = (page: number) => {
    setCurrentPage(Math.max(1, Math.min(page, totalPages)))
  }

  const handleProductClick = (productSlug: string) => {
    navigate(`/product/${productSlug}`)
  }

  const handleAddToCart = async (productId: string) => {
    // Fetch product details to get the default SKU
    try {
      const { productsApi } = await import('../../api/catalogApi')
      const { useCartStore } = await import('../../store/cartStore')
      
      const result = await productsApi.getById(productId)
      if (result.isSuccess && result.payload) {
        const product = result.payload
        // Use the first SKU as default
        const defaultSku = product.skus[0]
        if (defaultSku) {
          const { addToCart } = useCartStore.getState()
          const added = await addToCart(productId, defaultSku.id, 1)
          if (added) {
            console.log('Added to cart successfully')
          } else {
            const { lastError } = useCartStore.getState()
            console.error('Failed to add to cart:', lastError || 'Unknown error')
          }
        }
      }
    } catch (error) {
      console.error('Failed to add to cart:', error)
    }
  }



  // Loading state
  if (loading) {
    return (
      <div className="flex justify-center items-center min-h-[400px]">
        <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-brand"></div>
      </div>
    )
  }

  // Error state
  if (error || !store) {
    return (
      <div className="max-w-2xl mx-auto p-6 text-center">
        <div className="card p-12">
          <svg className="w-20 h-20 mx-auto text-foreground-muted mb-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9.172 16.172a4 4 0 015.656 0M9 10h.01M15 10h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <h1 className="text-2xl font-bold text-foreground mb-3">{t('storePage.notFound')}</h1>
          <p className="text-foreground-muted mb-6">{error || t('storePage.notFoundHint')}</p>
          <Link to="/" className="btn btn-brand">
            {t('common.backToHome')}
          </Link>
        </div>
      </div>
    )
  }

  return (
    <div className="p-6 space-y-8">
      {/* Store Header */}
      <div className="card p-6 md:p-8">
        <div className="flex flex-col md:flex-row md:items-center gap-4">
          {/* Store Icon */}
          <div className="w-20 h-20 rounded-2xl bg-gradient-to-br from-brand/20 to-brand/5 flex items-center justify-center flex-shrink-0">
            <svg className="w-10 h-10 text-brand" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2.293 2.293c-.63.63-.184 1.707.707 1.707H17m0 0a2 2 0 100 4 2 2 0 000-4zm-8 2a2 2 0 11-4 0 2 2 0 014 0z" />
            </svg>
          </div>

          {/* Store Info */}
          <div className="flex-1">
            <div className="flex items-center gap-3 flex-wrap">
              <h1 className="text-2xl md:text-3xl font-bold text-foreground">{store.name}</h1>
               {store.isVerified && (
                 <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-success/10 text-success text-sm font-medium">
                   <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M6.267 3.455a3.066 3.066 0 001.745-.723 3.066 3.066 0 013.976 0 3.066 3.066 0 001.745.723 3.066 3.066 0 012.812 2.812c.051.643.304 1.254.723 1.745a3.066 3.066 0 010 3.976 3.066 3.066 0 00-.723 1.745 3.066 3.066 0 01-2.812 2.812 3.066 3.066 0 00-1.745.723 3.066 3.066 0 01-3.976 0 3.066 3.066 0 00-1.745-.723 3.066 3.066 0 01-2.812-2.812 3.066 3.066 0 00-.723-1.745 3.066 3.066 0 010-3.976 3.066 3.066 0 00.723-1.745 3.066 3.066 0 012.812-2.812zm7.44 5.252a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                  </svg>
                  {t('storePage.verified')}
                </span>
              )}
            </div>
            {store.description && (
              <p className="text-foreground-muted mt-2 max-w-2xl">{store.description}</p>
            )}
            <div className="flex items-center gap-4 mt-3 text-sm text-foreground-muted">
              <span className="flex items-center gap-1.5">
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
                </svg>
                {t('storePage.productCount', { count: store.products.length })}
              </span>
              <span className="flex items-center gap-1.5">
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                </svg>
                {t('storePage.memberSince', { date: new Date(store.createdAt).toLocaleDateString() })}
              </span>
            </div>
          </div>
        </div>
      </div>

      {/* Products Section */}
      <div>
        <h2 className="text-xl font-semibold text-foreground mb-4">{t('storePage.products')}</h2>

        {store.products.length === 0 ? (
          <div className="card p-12 text-center">
            <svg className="w-16 h-16 mx-auto text-foreground-muted mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
            </svg>
            <h3 className="text-lg font-semibold text-foreground mb-2">{t('storePage.noProducts')}</h3>
            <p className="text-foreground-muted">{t('storePage.noProductsHint')}</p>
          </div>
        ) : (
          <>
            {/* Products Grid */}
            <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
              {paginatedProducts.map((product) => (
                <ProductCard
                  key={product.id}
                  product={product}
                  onClick={handleProductClick}
                  onAddToCart={handleAddToCart}
                />
              ))}
            </div>

            {/* Pagination */}
            {totalPages > 1 && (
              <div className="flex items-center justify-center gap-2 mt-8">
                <button
                  onClick={() => goToPage(currentPage - 1)}
                  disabled={currentPage === 1}
                  className="p-2 rounded-lg hover:bg-surface disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                >
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
                  </svg>
                </button>

                {Array.from({ length: totalPages }, (_, i) => i + 1).map((page) => {
                  if (
                    page === 1 ||
                    page === totalPages ||
                    (page >= currentPage - 1 && page <= currentPage + 1)
                  ) {
                    return (
                      <button
                        key={page}
                        onClick={() => goToPage(page)}
                        className={`min-w-[40px] h-10 rounded-lg font-medium transition-colors ${
                          page === currentPage
                            ? 'bg-brand text-white'
                            : 'hover:bg-surface text-foreground'
                        }`}
                      >
                        {page}
                      </button>
                    )
                  } else if (
                    page === currentPage - 2 ||
                    page === currentPage + 2
                  ) {
                    return (
                      <span key={page} className="text-foreground-muted">
                        ...
                      </span>
                    )
                  }
                  return null
                })}

                <button
                  onClick={() => goToPage(currentPage + 1)}
                  disabled={currentPage === totalPages}
                  className="p-2 rounded-lg hover:bg-surface disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                >
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                  </svg>
                </button>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  )
}
