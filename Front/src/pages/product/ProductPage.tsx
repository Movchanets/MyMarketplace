import { useState, useEffect, useCallback, useMemo } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import {
  productsApi,
  type ProductDetailsDto,
  type SkuDto,
  type MediaImageDto
} from '../../api/catalogApi'
import { useFavoritesStore, useIsFavorited } from '../../store/favoritesStore'

export default function ProductPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { productSlug, skuCode } = useParams<{ productSlug: string; skuCode?: string }>()

  const [product, setProduct] = useState<ProductDetailsDto | null>(null)
  const [selectedSku, setSelectedSku] = useState<SkuDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [selectedImage, setSelectedImage] = useState<string | null>(null)
  const [quantity, setQuantity] = useState(1)

  // Favorites functionality
  const { toggleFavorite, isToggling } = useFavoritesStore()
  const isFavorited = useIsFavorited(product?.id || '')

  // Fetch product data by slug
  const fetchProduct = useCallback(async () => {
    if (!productSlug) return

    setLoading(true)
    setError(null)
    try {
      const result = await productsApi.getBySlug(productSlug, skuCode)
      if (result.isSuccess && result.payload) {
        setProduct(result.payload)
        
        // Select SKU based on URL or default to first
        const skus = result.payload.skus
        if (skus.length > 0) {
          const targetSku = skuCode 
            ? skus.find(s => s.skuCode === skuCode) || skus[0]
            : skus[0]
          setSelectedSku(targetSku)
        }
      } else {
        setError(result.message || t('product.notFound'))
      }
    } catch {
      setError(t('common.error'))
    } finally {
      setLoading(false)
    }
  }, [productSlug, skuCode, t])

  useEffect(() => {
    fetchProduct()
  }, [fetchProduct])

  // Update URL when SKU changes
  useEffect(() => {
    if (selectedSku && product && selectedSku.skuCode !== skuCode) {
      navigate(`/product/${product.slug}/${selectedSku.skuCode}`, { replace: true })
    }
  }, [selectedSku, product, skuCode, navigate])

  // Set initial image when SKU changes
  useEffect(() => {
    if (selectedSku) {
      // Prefer SKU gallery, fallback to product gallery, then base image
      const skuImages = selectedSku.gallery || []
      const productImages = product?.gallery || []
      
      if (skuImages.length > 0) {
        setSelectedImage(skuImages[0].url)
      } else if (productImages.length > 0) {
        setSelectedImage(productImages[0].url)
      } else if (product?.baseImageUrl) {
        setSelectedImage(product.baseImageUrl)
      } else {
        setSelectedImage(null)
      }
    }
  }, [selectedSku, product])

  // Get all images for gallery (SKU images first, then product images)
  const allImages = useMemo(() => {
    const images: MediaImageDto[] = []
    
    // Add SKU-specific images first
    if (selectedSku?.gallery) {
      images.push(...selectedSku.gallery)
    }
    
    // Add product gallery images
    if (product?.gallery) {
      images.push(...product.gallery)
    }
    
    return images
  }, [selectedSku, product])

  // Get unique attribute keys across all SKUs for variant selection
  const variantAttributes = useMemo(() => {
    if (!product?.skus) return {}
    
    const attrs: Record<string, Set<string>> = {}
    
    for (const sku of product.skus) {
      if (sku.attributes) {
        for (const [key, value] of Object.entries(sku.attributes)) {
          if (!attrs[key]) {
            attrs[key] = new Set()
          }
          attrs[key].add(String(value))
        }
      }
    }
    
    // Convert Sets to arrays
    const result: Record<string, string[]> = {}
    for (const [key, values] of Object.entries(attrs)) {
      result[key] = Array.from(values)
    }
    
    return result
  }, [product])

  // Get selected attribute values from current SKU
  const selectedAttributes = useMemo(() => {
    if (!selectedSku?.attributes) return {}
    
    const result: Record<string, string> = {}
    for (const [key, value] of Object.entries(selectedSku.attributes)) {
      result[key] = String(value)
    }
    return result
  }, [selectedSku])

  // Handle attribute selection - find matching SKU
  const handleAttributeChange = (attributeKey: string, value: string) => {
    if (!product?.skus) return
    
    // Build target attributes
    const targetAttrs = { ...selectedAttributes, [attributeKey]: value }
    
    // Find SKU that matches all selected attributes
    const matchingSku = product.skus.find(sku => {
      if (!sku.attributes) return false
      
      for (const [key, val] of Object.entries(targetAttrs)) {
        if (String(sku.attributes[key]) !== val) {
          return false
        }
      }
      return true
    })
    
    if (matchingSku) {
      setSelectedSku(matchingSku)
    }
  }

  // Check if a specific attribute value is available (has matching SKU)
  const isAttributeValueAvailable = (attributeKey: string, value: string): boolean => {
    if (!product?.skus) return false
    
    // Build target attributes with this value
    const targetAttrs = { ...selectedAttributes, [attributeKey]: value }
    
    // Check if any SKU matches
    return product.skus.some(sku => {
      if (!sku.attributes) return false
      
      for (const [key, val] of Object.entries(targetAttrs)) {
        if (String(sku.attributes[key]) !== val) {
          return false
        }
      }
      return true
    })
  }

  const handleAddToCart = async () => {
    if (!product || !selectedSku) return
    
    const { useCartStore } = await import('../../store/cartStore')
    const { addToCart } = useCartStore.getState()
    
    try {
      const added = await addToCart(product.id, selectedSku.id, quantity)
      if (added) {
        // Show success feedback (could be a toast notification)
        console.log('Added to cart successfully')
      } else {
        const { lastError } = useCartStore.getState()
        console.error('Failed to add to cart:', lastError || 'Unknown error')
      }
    } catch (error) {
      console.error('Failed to add to cart (unexpected):', error)
    }
  }

  const handleBuyNow = () => {
    if (!selectedSku) return
    // TODO: Implement buy now functionality
    console.log('Buy now:', selectedSku.id, 'quantity:', quantity)
  }

  const handleToggleFavorite = () => {
    if (!product?.id) return
    toggleFavorite(product.id)
  }

   // Loading state
   if (loading) {
     return (
       <div className="max-w-6xl mx-auto p-6">
         <div className="animate-pulse">
           <div className="grid md:grid-cols-2 gap-8">
             <div className="aspect-square bg-surface-hover dark:bg-background rounded-xl"></div>
             <div className="space-y-4">
               <div className="h-8 bg-surface-hover dark:bg-background rounded w-3/4"></div>
               <div className="h-6 bg-surface-hover dark:bg-background rounded w-1/4"></div>
               <div className="h-24 bg-surface-hover dark:bg-background rounded"></div>
               <div className="h-12 bg-surface-hover dark:bg-background rounded w-1/3"></div>
             </div>
           </div>
         </div>
       </div>
     )
   }

  // Error state
  if (error || !product) {
    return (
      <div className="max-w-2xl mx-auto p-6 text-center">
        <div className="card p-12">
          <svg className="w-20 h-20 mx-auto text-foreground-muted mb-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9.172 16.172a4 4 0 015.656 0M9 10h.01M15 10h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <h1 className="text-2xl font-bold text-foreground mb-3">{t('productPage.notFound')}</h1>
          <p className="text-foreground-muted mb-6">{error || t('productPage.notFoundHint')}</p>
          <Link to="/" className="btn btn-brand">
            {t('common.backToHome')}
          </Link>
        </div>
      </div>
    )
  }

  return (
    <div className="max-w-6xl mx-auto p-6">
      {/* Breadcrumb using primary category */}
      <nav className="flex items-center gap-2 text-sm text-foreground-muted mb-6">
        <Link to="/" className="hover:text-brand transition-colors">{t('common.home')}</Link>
        <span>/</span>
        {product.primaryCategory ? (
          <>
            <Link 
              to={`/category/${product.primaryCategory.slug}`} 
              className="hover:text-brand transition-colors"
            >
              {product.primaryCategory.name}
            </Link>
            <span>/</span>
          </>
        ) : product.categories[0] && (
          <>
            <Link 
              to={`/category/${product.categories[0].slug}`} 
              className="hover:text-brand transition-colors"
            >
              {product.categories[0].name}
            </Link>
            <span>/</span>
          </>
        )}
        <span className="text-foreground">{product.name}</span>
      </nav>

      <div className="grid md:grid-cols-2 gap-8">
        {/* Image Gallery */}
        <div className="space-y-4">
          {/* Main Image */}
          <div className="aspect-square bg-background-secondary rounded-xl overflow-hidden">
            {selectedImage ? (
              <img
                src={selectedImage}
                alt={product.name}
                className="w-full h-full object-contain"
              />
            ) : (
              <div className="w-full h-full flex items-center justify-center">
                <svg className="w-24 h-24 text-foreground-muted opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
                </svg>
              </div>
            )}
          </div>

          {/* Thumbnail Gallery */}
          {allImages.length > 0 && (
            <div className="flex gap-2 overflow-x-auto pb-2">
              {/* Base image thumbnail if exists and different from gallery */}
              {product.baseImageUrl && !allImages.some(img => img.url === product.baseImageUrl) && (
                <button
                  onClick={() => setSelectedImage(product.baseImageUrl)}
                  className={`flex-shrink-0 w-20 h-20 rounded-lg overflow-hidden border-2 transition-all ${
                    selectedImage === product.baseImageUrl
                      ? 'border-brand ring-2 ring-brand/30'
                      : 'border-border hover:border-brand/50'
                  }`}
                >
                  <img
                    src={product.baseImageUrl}
                    alt={product.name}
                    className="w-full h-full object-cover"
                  />
                </button>
              )}
              
              {allImages.map((img) => (
                <button
                  key={img.id}
                  onClick={() => setSelectedImage(img.url)}
                  className={`flex-shrink-0 w-20 h-20 rounded-lg overflow-hidden border-2 transition-all ${
                    selectedImage === img.url
                      ? 'border-brand ring-2 ring-brand/30'
                      : 'border-border hover:border-brand/50'
                  }`}
                >
                  <img
                    src={img.url}
                    alt={img.altText || product.name}
                    className="w-full h-full object-cover"
                  />
                </button>
              ))}
            </div>
          )}
        </div>

        {/* Product Info */}
        <div className="space-y-6">
           {/* Title & Price */}
           <div>
             <div className="flex items-start justify-between gap-4">
               <h1 className="text-2xl md:text-3xl font-bold text-foreground flex-1">{product.name}</h1>
               <button
                  onClick={handleToggleFavorite}
                  disabled={isToggling.has(product.id)}
                  className={`p-2 rounded-full transition-all duration-200 ${
                    isFavorited
                      ? 'bg-error hover:bg-error-dark text-white'
                      : 'bg-surface-hover dark:bg-background hover:bg-surface-hover dark:hover:bg-background text-foreground-muted dark:text-foreground-muted'
                  }`}
                 title={isFavorited ? t('productPage.removeFromFavorites') : t('productPage.addToFavorites')}
               >
                 <svg
                    className={`w-6 h-6 transition-colors ${
                      isFavorited ? 'text-white' : 'text-foreground-muted dark:text-foreground-muted'
                    }`}
                   fill={isFavorited ? 'currentColor' : 'none'}
                   stroke="currentColor"
                   viewBox="0 0 24 24"
                 >
                   <path
                     strokeLinecap="round"
                     strokeLinejoin="round"
                     strokeWidth={2}
                     d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z"
                   />
                 </svg>
               </button>
             </div>
            
            {selectedSku && (
              <div className="mt-3 flex items-baseline gap-3">
                <span className="text-3xl font-bold text-brand">
                  {selectedSku.price.toFixed(2)} ₴
                </span>
                {selectedSku.skuCode && (
                  <span className="text-sm text-foreground-muted font-mono">
                    {selectedSku.skuCode}
                  </span>
                )}
              </div>
            )}
          </div>

           {/* Stock Status */}
           {selectedSku && (
             <div className="flex items-center gap-2">
               {selectedSku.stockQuantity > 0 ? (
                 <>
                   <span className="w-3 h-3 rounded-full bg-success"></span>
                   <span className="text-success font-medium">
                     {t('productPage.inStock')} ({selectedSku.stockQuantity})
                   </span>
                 </>
               ) : (
                 <>
                   <span className="w-3 h-3 rounded-full bg-error"></span>
                   <span className="text-error font-medium">
                     {t('productPage.outOfStock')}
                   </span>
                 </>
               )}
             </div>
           )}

          {/* Variant Selection */}
          {Object.keys(variantAttributes).length > 0 && (
            <div className="space-y-4">
              {Object.entries(variantAttributes).map(([attrKey, values]) => (
                <div key={attrKey}>
                  <label className="block text-sm font-medium text-foreground mb-2">
                    {attrKey}: <span className="text-brand">{selectedAttributes[attrKey] || '—'}</span>
                  </label>
                  <div className="flex flex-wrap gap-2">
                    {values.map((value) => {
                      const isSelected = selectedAttributes[attrKey] === value
                      const isAvailable = isAttributeValueAvailable(attrKey, value)
                      
                      return (
                         <button
                           key={value}
                           onClick={() => handleAttributeChange(attrKey, value)}
                           disabled={!isAvailable}
                           className={`px-4 py-2 rounded-lg border-2 text-sm font-medium transition-all ${
                             isSelected
                               ? 'border-brand bg-brand text-white'
                               : isAvailable
                                 ? 'border-border hover:border-brand text-foreground'
                                 : 'border-border bg-surface-hover dark:bg-background text-foreground-muted opacity-50 cursor-not-allowed line-through'
                           }`}
                         >
                          {value}
                        </button>
                      )
                    })}
                  </div>
                </div>
              ))}
            </div>
          )}

          {/* Quantity & Actions */}
          <div className="space-y-4 pt-4 border-t border-border">
            {/* Quantity Selector */}
            <div className="flex items-center gap-4">
              <label className="text-sm font-medium text-foreground">{t('productPage.quantity')}:</label>
              <div className="flex items-center border border-border rounded-lg">
                <button
                  onClick={() => setQuantity(Math.max(1, quantity - 1))}
                  className="px-3 py-2 hover:bg-background-secondary transition-colors"
                >
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M20 12H4" />
                  </svg>
                </button>
                <input
                  type="number"
                  min="1"
                  max={selectedSku?.stockQuantity || 99}
                  value={quantity}
                  onChange={(e) => setQuantity(Math.max(1, parseInt(e.target.value) || 1))}
                  className="w-16 text-center py-2 border-x border-border bg-transparent text-foreground focus:outline-none"
                />
                <button
                  onClick={() => setQuantity(Math.min(selectedSku?.stockQuantity || 99, quantity + 1))}
                  className="px-3 py-2 hover:bg-background-secondary transition-colors"
                >
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                  </svg>
                </button>
              </div>
            </div>

            {/* Action Buttons */}
            <div className="flex gap-3">
              <button
                onClick={handleAddToCart}
                disabled={!selectedSku || selectedSku.stockQuantity === 0}
                className="flex-1 btn btn-secondary py-3 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <svg className="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2.293 2.293c-.63.63-.184 1.707.707 1.707H17m0 0a2 2 0 100 4 2 2 0 000-4zm-8 2a2 2 0 11-4 0 2 2 0 014 0z" />
                </svg>
                {t('productPage.addToCart')}
              </button>
              <button
                onClick={handleBuyNow}
                disabled={!selectedSku || selectedSku.stockQuantity === 0}
                className="flex-1 btn btn-brand py-3 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {t('productPage.buyNow')}
              </button>
            </div>
          </div>

          {/* Description */}
          {product.description && (
            <div className="pt-4 border-t border-border">
              <h3 className="text-lg font-semibold text-foreground mb-2">{t('productPage.description')}</h3>
              <p className="text-foreground-muted whitespace-pre-wrap">{product.description}</p>
            </div>
          )}

          {/* Attributes */}
          {selectedSku?.mergedAttributes && Object.keys(selectedSku.mergedAttributes).length > 0 && (
            <div className="pt-4 border-t border-border">
              <h3 className="text-lg font-semibold text-foreground mb-3">{t('productPage.specifications')}</h3>
              <dl className="grid grid-cols-2 gap-2">
                {Object.entries(selectedSku.mergedAttributes).map(([key, value]) => (
                  <div key={key} className="flex justify-between py-2 border-b border-border/50">
                    <dt className="text-foreground-muted">{key}</dt>
                    <dd className="text-foreground font-medium">{String(value)}</dd>
                  </div>
                ))}
              </dl>
            </div>
          )}

          {/* Categories (excluding primary) as clickable tags */}
          {product.categories.filter(c => !c.isPrimary).length > 0 && (
            <div className="pt-4 border-t border-border">
              <h3 className="text-sm font-medium text-foreground-muted mb-2">{t('productPage.categories')}</h3>
              <div className="flex flex-wrap gap-2">
                {product.categories.filter(c => !c.isPrimary).map(category => (
                  <Link
                    key={category.id}
                    to={`/category/${category.slug}`}
                    className="px-3 py-1.5 text-sm rounded-lg bg-background-secondary text-foreground hover:bg-brand/10 hover:text-brand transition-colors"
                  >
                    {category.name}
                  </Link>
                ))}
              </div>
            </div>
          )}

          {/* Tags */}
          {product.tags.length > 0 && (
            <div className="flex flex-wrap gap-2">
              {product.tags.map(tag => (
                <span
                  key={tag.id}
                  className="px-3 py-1 text-sm rounded-full bg-brand/10 text-brand"
                >
                  #{tag.name}
                </span>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
