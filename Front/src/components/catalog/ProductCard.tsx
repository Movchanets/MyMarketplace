import { useTranslation } from 'react-i18next'
import type { ProductSummaryDto } from '../../api/catalogApi'

interface ProductCardProps {
  product: ProductSummaryDto
  onAddToCart?: (productId: string) => void
  onAddToWishlist?: (productId: string) => void
  onClick?: (productSlug: string) => void
}

export default function ProductCard({ product, onAddToCart, onAddToWishlist, onClick }: ProductCardProps) {
  const { t } = useTranslation()

  const handleCardClick = () => {
    onClick?.(product.slug)
  }

  const handleAddToCart = (e: React.MouseEvent) => {
    e.stopPropagation()
    onAddToCart?.(product.id)
  }

  const handleAddToWishlist = (e: React.MouseEvent) => {
    e.stopPropagation()
    onAddToWishlist?.(product.id)
  }

  return (
    <div
      className="card overflow-hidden flex flex-col hover:shadow-lg transition-all duration-200 cursor-pointer group"
      onClick={handleCardClick}
    >
      {/* Product Image */}
      <div className="relative aspect-[4/3] bg-background-secondary overflow-hidden">
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
              {t('productCard.inStock')}
            </span>
          ) : (
            <span className="px-2.5 py-1 text-xs font-semibold rounded-full bg-red-500 text-white shadow-sm">
              {t('productCard.outOfStock')}
            </span>
          )}
        </div>

        {/* Wishlist Button */}
        <button
          onClick={handleAddToWishlist}
          className="absolute top-3 right-3 p-2 rounded-full bg-white/80 dark:bg-gray-900/80 hover:bg-white dark:hover:bg-gray-900 shadow-sm backdrop-blur-sm transition-all opacity-0 group-hover:opacity-100"
          title={t('productCard.addToWishlist')}
        >
          <svg className="w-5 h-5 text-gray-600 dark:text-gray-300 hover:text-red-500 dark:hover:text-red-400 transition-colors" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
          </svg>
        </button>

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
          <h3 className="font-semibold text-text line-clamp-2 leading-tight group-hover:text-brand transition-colors" title={product.name}>
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

        {/* Add to Cart Button */}
        <div className="mt-auto pt-3">
          <button
            onClick={handleAddToCart}
            disabled={!product.inStock}
            className={`w-full flex items-center justify-center gap-2 px-4 py-2.5 rounded-lg font-medium transition-all ${
              product.inStock
                ? 'bg-brand hover:bg-brand-dark text-white'
                : 'bg-gray-200 dark:bg-gray-700 text-gray-500 dark:text-gray-400 cursor-not-allowed'
            }`}
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2.293 2.293c-.63.63-.184 1.707.707 1.707H17m0 0a2 2 0 100 4 2 2 0 000-4zm-8 2a2 2 0 11-4 0 2 2 0 014 0z" />
            </svg>
            {product.inStock ? t('productCard.addToCart') : t('productCard.outOfStock')}
          </button>
        </div>
      </div>
    </div>
  )
}
