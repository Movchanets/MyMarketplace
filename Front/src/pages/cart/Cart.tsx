import { useEffect, useState, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router-dom'
import { useCartStore } from '../../store/cartStore'
import { useAuthStore } from '../../store/authStore'
import { productsApi } from '../../api/catalogApi'

// Icons using simple SVG components instead of lucide-react
const Minus = ({ className }: { className?: string }) => <svg className={className} xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M5 12h14"/></svg>
const Plus = ({ className }: { className?: string }) => <svg className={className} xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M5 12h14"/><path d="M12 5v14"/></svg>
const Trash2 = ({ className }: { className?: string }) => <svg className={className} xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M3 6h18"/><path d="M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6"/><path d="M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2"/><line x1="10" x2="10" y1="11" y2="17"/><line x1="14" x2="14" y1="11" y2="17"/></svg>
const ShoppingBag = ({ className }: { className?: string }) => <svg className={className} xmlns="http://www.w3.org/2000/svg" width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"><path d="M6 2 3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4Z"/><path d="M3 6h18"/><path d="M16 10a4 4 0 0 1-8 0"/></svg>
const ArrowRight = ({ className }: { className?: string }) => <svg className={className} xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M5 12h14"/><path d="m12 5 7 7-7 7"/></svg>
const Package = ({ className }: { className?: string }) => <svg className={className} xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"><path d="m7.5 4.27 9 5.15"/><path d="M21 8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16Z"/><path d="m3.3 7 8.7 5 8.7-5"/><path d="M12 22V12"/></svg>
const AlertTriangle = ({ className }: { className?: string }) => <svg className={className} xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3Z"/><line x1="12" x2="12" y1="9" y2="13"/><line x1="12" x2="12.01" y1="17" y2="17"/></svg>

// Extended cart item with stock info
interface CartItemWithStock {
  id?: string
  productId: string
  productSlug: string
  productName: string
  productImageUrl: string | null
  skuId: string
  skuCode: string
  skuAttributes?: string
  quantity: number
  unitPrice: number
  subtotal: number
  stockQuantity: number
  isOutOfStock: boolean
}

export default function Cart() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated)
  const {
    cart,
    guestCart,
    isLoading,
    isUpdating,
    error: cartError,
    getTotalItems,
    loadCart,
    updateQuantityBySku,
    removeFromCartBySku,
    clearCart,
    clearError,
    clearGuestCart,
  } = useCartStore()

  const [promoCode, setPromoCode] = useState('')
  const [savedForLater, setSavedForLater] = useState<string[]>([])
  const [cartItemsWithStock, setCartItemsWithStock] = useState<CartItemWithStock[]>([])
  const [stockErrors, setStockErrors] = useState<Record<string, string>>({})
  const [isCheckingStock, setIsCheckingStock] = useState(false)

  // Load cart and stock info
  useEffect(() => {
    loadCart()
  }, [loadCart, isAuthenticated])

  // Fetch stock information for cart items
  const fetchStockInfo = useCallback(async () => {
    const items = isAuthenticated ? (cart?.items ?? []) : guestCart
    if (items.length === 0) {
      setCartItemsWithStock([])
      return
    }

    setIsCheckingStock(true)
    try {
      const itemsWithStock: CartItemWithStock[] = []

      for (const item of items) {
        try {
          const result = await productsApi.getById(item.productId)
          if (result.isSuccess && result.payload) {
            const product = result.payload
            const sku = product.skus.find(s => s.id === item.skuId)
            const stockQuantity = sku?.stockQuantity ?? 0
            const isOutOfStock = stockQuantity === 0

            // Check if current quantity exceeds stock
            if (item.quantity > stockQuantity && stockQuantity > 0) {
              setStockErrors(prev => ({
                ...prev,
                [item.skuId]: t('cart.stockExceeded', 'Only {{stock}} items available', { stock: stockQuantity })
              }))
            }

            itemsWithStock.push({
              id: (item as { id?: string }).id,
              productId: item.productId,
              productSlug: product.slug,
              productName: (item as { productName?: string }).productName || product.name,
              productImageUrl: (item as { productImageUrl?: string | null }).productImageUrl || product.baseImageUrl,
              skuId: item.skuId,
              skuCode: (item as { skuCode?: string }).skuCode || sku?.skuCode || '',
              skuAttributes: (item as { skuAttributes?: string }).skuAttributes,
              quantity: item.quantity,
              unitPrice: (item as { unitPrice?: number }).unitPrice || sku?.price || 0,
              subtotal: ((item as { unitPrice?: number }).unitPrice || sku?.price || 0) * item.quantity,
              stockQuantity,
              isOutOfStock,
            })
          }
        } catch (error) {
          console.error('Failed to load stock info:', error)
          // Add item without stock info
          itemsWithStock.push({
            id: (item as { id?: string }).id,
            productId: item.productId,
            productSlug: '',
            productName: (item as { productName?: string }).productName || 'Unknown Product',
            productImageUrl: (item as { productImageUrl?: string | null }).productImageUrl || null,
            skuId: item.skuId,
            skuCode: (item as { skuCode?: string }).skuCode || '',
            skuAttributes: (item as { skuAttributes?: string }).skuAttributes,
            quantity: item.quantity,
            unitPrice: (item as { unitPrice?: number }).unitPrice || 0,
            subtotal: ((item as { unitPrice?: number }).unitPrice || 0) * item.quantity,
            stockQuantity: 0,
            isOutOfStock: true,
          })
        }
      }

      setCartItemsWithStock(itemsWithStock)
    } finally {
      setIsCheckingStock(false)
    }
  }, [cart, guestCart, isAuthenticated, t])

  useEffect(() => {
    fetchStockInfo()
  }, [fetchStockInfo])

  const handleQuantityChange = async (item: CartItemWithStock, newQuantity: number) => {
    // Clear previous error for this item
    setStockErrors(prev => {
      const newErrors = { ...prev }
      delete newErrors[item.skuId]
      return newErrors
    })

    // Validate minimum quantity
    if (newQuantity < 1) {
      setStockErrors(prev => ({
        ...prev,
        [item.skuId]: t('cart.minQuantity', 'Minimum quantity is 1')
      }))
      return
    }

    // Validate maximum quantity (hard limit of 99)
    if (newQuantity > 99) {
      setStockErrors(prev => ({
        ...prev,
        [item.skuId]: t('cart.maxQuantity', 'Maximum quantity is 99')
      }))
      return
    }

    // Validate stock availability
    if (newQuantity > item.stockQuantity) {
      setStockErrors(prev => ({
        ...prev,
        [item.skuId]: t('cart.stockExceeded', 'Only {{stock}} items available', { stock: item.stockQuantity })
      }))
      return
    }

    // Update quantity - use SKU-based method which works for both authenticated and guest carts
    const success = await updateQuantityBySku(item.skuId, newQuantity)
    
    if (!success) {
      setStockErrors(prev => ({
        ...prev,
        [item.skuId]: t('cart.updateFailed', 'Failed to update quantity. Please try again.')
      }))
      return
    }

    // Refresh stock info after update
    await fetchStockInfo()
  }

  const handleRemoveItem = async (item: CartItemWithStock) => {
    // Clear error for this item
    setStockErrors(prev => {
      const newErrors = { ...prev }
      delete newErrors[item.skuId]
      return newErrors
    })

    // Use SKU-based method which works for both authenticated and guest carts
    const success = await removeFromCartBySku(item.skuId)
    
    if (!success) {
      setStockErrors(prev => ({
        ...prev,
        [item.skuId]: t('cart.removeFailed', 'Failed to remove item. Please try again.')
      }))
      return
    }

    // Refresh stock info
    await fetchStockInfo()
  }

  const handleClearCart = async () => {
    if (window.confirm(t('cart.clearConfirm', 'Are you sure you want to clear your cart?'))) {
      setStockErrors({})
      if (isAuthenticated) {
        await clearCart()
      } else {
        clearGuestCart()
      }
      setCartItemsWithStock([])
    }
  }

  const handleSaveForLater = (skuId: string) => {
    setSavedForLater((prev) => [...prev, skuId])
  }

  const handleMoveToCart = (skuId: string) => {
    setSavedForLater((prev) => prev.filter((id) => id !== skuId))
  }

  const handleCheckout = () => {
    // Check for stock errors before checkout
    const hasStockErrors = cartItemsWithStock.some(item => 
      item.isOutOfStock || item.quantity > item.stockQuantity
    )

    if (hasStockErrors) {
      alert(t('cart.stockErrorBeforeCheckout', 'Please resolve stock issues before proceeding to checkout'))
      return
    }

    if (!isAuthenticated) {
      navigate('/auth', { state: { from: '/checkout' } })
      return
    }
    navigate('/checkout')
  }

  const totalItems = getTotalItems()
  const totalPrice = cartItemsWithStock
    .filter(item => !savedForLater.includes(item.skuId) && !item.isOutOfStock)
    .reduce((sum, item) => sum + item.subtotal, 0)

  // Filter out saved for later items
  const activeItems = cartItemsWithStock.filter((item) => !savedForLater.includes(item.skuId))
  const savedItems = cartItemsWithStock.filter((item) => savedForLater.includes(item.skuId))

  // Check if any item is out of stock
  const hasOutOfStockItems = activeItems.some(item => item.isOutOfStock)

  if (isLoading || isCheckingStock) {
    return (
      <div className="container mx-auto px-4 py-8">
        <div className="animate-pulse">
          <div className="h-8 bg-surface-hover rounded w-1/4 mb-8"></div>
          <div className="space-y-4">
            {[1, 2, 3].map((i) => (
              <div key={i} className="h-24 bg-surface-hover rounded"></div>
            ))}
          </div>
        </div>
      </div>
    )
  }

  if (totalItems === 0) {
    return (
      <div className="container mx-auto px-4 py-16">
        <div className="text-center">
          <ShoppingBag className="mx-auto h-16 w-16 text-foreground-muted mb-4" />
          <h2 className="text-2xl font-bold text-foreground mb-2">
            {t('cart.empty.title', 'Your cart is empty')}
          </h2>
          <p className="text-foreground-muted mb-6">
            {t('cart.empty.description', 'Looks like you haven\'t added anything to your cart yet.')}
          </p>
          <Link
            to="/"
            className="inline-flex items-center px-6 py-3 border border-transparent text-base font-medium rounded-md text-white bg-brand hover:bg-brand-dark"
          >
            {t('cart.empty.continueShopping', 'Continue Shopping')}
          </Link>
        </div>
      </div>
    )
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-3xl font-bold text-foreground mb-8">
        {t('cart.title', 'Shopping Cart')}
      </h1>

      {(cartError || Object.keys(stockErrors).length > 0) && (
        <div className="mb-6 p-4 bg-error/10 border border-error/20 rounded-md">
          {cartError && <p className="text-error">{cartError}</p>}
          {Object.entries(stockErrors).map(([skuId, errorMsg]) => (
            <p key={skuId} className="text-error text-sm mt-1">{errorMsg}</p>
          ))}
          <button
            onClick={clearError}
            className="text-sm text-error/80 underline mt-2"
          >
            {t('common.dismiss', 'Dismiss')}
          </button>
        </div>
      )}

      {hasOutOfStockItems && (
        <div className="mb-6 p-4 bg-warning/10 border border-warning/20 rounded-md flex items-start gap-3">
          <AlertTriangle className="w-5 h-5 text-warning flex-shrink-0 mt-0.5" />
          <div>
            <p className="text-warning font-medium">
              {t('cart.outOfStockWarning', 'Some items are out of stock')}
            </p>
            <p className="text-warning/80 text-sm mt-1">
              {t('cart.outOfStockDescription', 'Please remove out-of-stock items or adjust quantities to proceed with checkout')}
            </p>
          </div>
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Cart Items */}
        <div className="lg:col-span-2">
          <div className="bg-surface rounded-lg shadow-sm border border-border">
            <div className="p-4 border-b border-border flex justify-between items-center">
              <span className="text-sm text-foreground-muted">
                {t('cart.itemsCount', '{{count}} items', { count: totalItems })}
              </span>
              <button
                onClick={handleClearCart}
                disabled={isUpdating}
                className="text-sm text-error hover:text-error-dark disabled:opacity-50"
              >
                {t('cart.clear', 'Clear Cart')}
              </button>
            </div>

            <div className="divide-y divide-border">
              {activeItems.map((item) => (
                <div 
                  key={item.id || item.skuId} 
                  className={`p-4 flex gap-4 ${item.isOutOfStock ? 'opacity-60 bg-error/5' : ''}`}
                >
                  {/* Product Image */}
                  <div className="w-24 h-24 flex-shrink-0">
                    {item.productImageUrl ? (
                      <img
                        src={item.productImageUrl}
                        alt={item.productName}
                        className="w-full h-full object-cover rounded-md"
                      />
                    ) : (
                      <div className="w-full h-full bg-surface-hover rounded-md flex items-center justify-center">
                        <Package className="w-8 h-8 text-foreground-muted" />
                      </div>
                    )}
                  </div>

                  {/* Product Details */}
                  <div className="flex-1 min-w-0">
                    <div className="flex items-start justify-between">
                      <Link
                        to={`/product/${item.productSlug || item.productId}`}
                        className="text-lg font-medium text-foreground hover:text-brand truncate block"
                      >
                        {item.productName}
                      </Link>
                      {item.isOutOfStock && (
                        <span className="ml-2 px-2 py-0.5 text-xs font-medium bg-error text-white rounded">
                          {t('cart.outOfStock', 'Out of Stock')}
                        </span>
                      )}
                    </div>
                    <p className="text-sm text-foreground-muted mt-1">
                      {item.skuCode}
                      {item.skuAttributes && (
                        <span className="ml-2 text-xs">
                          {item.skuAttributes}
                        </span>
                      )}
                    </p>
                    
                    {/* Stock info */}
                    {!item.isOutOfStock && item.stockQuantity <= 5 && (
                      <p className="text-xs text-warning mt-1">
                        {t('cart.lowStock', 'Only {{stock}} left in stock', { stock: item.stockQuantity })}
                      </p>
                    )}
                    
                    <p className="text-lg font-semibold text-foreground mt-2">
                      ₴{item.subtotal.toFixed(2)}
                    </p>

                    {/* Quantity Controls */}
                    <div className="flex items-center gap-4 mt-3">
                      <div className="flex items-center border border-border rounded-md bg-background">
                        <button
                          onClick={() => handleQuantityChange(item, item.quantity - 1)}
                          disabled={isUpdating || item.quantity <= 1 || item.isOutOfStock}
                          className="p-2 hover:bg-surface-hover disabled:opacity-30 disabled:cursor-not-allowed text-foreground"
                          title={item.quantity <= 1 ? t('cart.minQuantityReached', 'Minimum quantity reached') : undefined}
                        >
                          <Minus className="w-4 h-4" />
                        </button>
                        <span className={`px-4 py-2 min-w-[3rem] text-center ${item.isOutOfStock ? 'text-foreground-muted' : 'text-foreground'}`}>
                          {item.quantity}
                        </span>
                        <button
                          onClick={() => handleQuantityChange(item, item.quantity + 1)}
                          disabled={isUpdating || item.quantity >= item.stockQuantity || item.isOutOfStock}
                          className="p-2 hover:bg-surface-hover disabled:opacity-30 disabled:cursor-not-allowed text-foreground"
                          title={item.quantity >= item.stockQuantity ? t('cart.maxStockReached', 'Maximum stock reached') : undefined}
                        >
                          <Plus className="w-4 h-4" />
                        </button>
                      </div>

                      {!item.isOutOfStock && (
                        <button
                          onClick={() => handleSaveForLater(item.skuId)}
                          className="text-sm text-brand hover:text-brand-dark"
                        >
                          {t('cart.saveForLater', 'Save for later')}
                        </button>
                      )}

                      <button
                        onClick={() => handleRemoveItem(item)}
                        disabled={isUpdating}
                        className="p-2 text-error hover:bg-error/10 rounded-md disabled:opacity-50"
                      >
                        <Trash2 className="w-5 h-5" />
                      </button>
                    </div>

                    {/* Stock error message */}
                    {stockErrors[item.skuId] && (
                      <p className="text-error text-sm mt-2 flex items-center gap-1">
                        <AlertTriangle className="w-4 h-4" />
                        {stockErrors[item.skuId]}
                      </p>
                    )}
                  </div>

                  {/* Subtotal */}
                  <div className="text-right">
                    <p className={`text-lg font-semibold ${item.isOutOfStock ? 'text-foreground-muted line-through' : 'text-foreground'}`}>
                      ₴{item.subtotal.toFixed(2)}
                    </p>
                    {item.isOutOfStock && (
                      <p className="text-error text-sm mt-1">
                        {t('cart.unavailable', 'Unavailable')}
                      </p>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Saved for Later */}
          {savedItems.length > 0 && (
            <div className="mt-8 bg-surface rounded-lg shadow-sm border border-border">
              <div className="p-4 border-b border-border">
                <h3 className="text-lg font-medium text-foreground">
                  {t('cart.savedForLater', 'Saved for Later')} ({savedItems.length})
                </h3>
              </div>
              <div className="divide-y divide-border">
                {savedItems.map((item) => (
                  <div key={item.id || item.skuId} className="p-4 flex gap-4 opacity-75">
                    <div className="w-24 h-24 flex-shrink-0">
                      {item.productImageUrl ? (
                        <img
                          src={item.productImageUrl}
                          alt={item.productName}
                          className="w-full h-full object-cover rounded-md"
                        />
                      ) : (
                        <div className="w-full h-full bg-surface-hover rounded-md flex items-center justify-center">
                          <Package className="w-8 h-8 text-foreground-muted" />
                        </div>
                      )}
                    </div>
                    <div className="flex-1">
                      <Link
                        to={`/product/${item.productSlug || item.productId}`}
                        className="text-lg font-medium text-foreground hover:text-brand"
                      >
                        {item.productName}
                      </Link>
                      <p className="text-sm text-foreground-muted">{item.skuCode}</p>
                      <p className="text-lg font-semibold text-foreground mt-2">
                        ₴{item.unitPrice.toFixed(2)}
                      </p>
                      <button
                        onClick={() => handleMoveToCart(item.skuId)}
                        className="mt-2 text-sm text-brand hover:text-brand-dark"
                      >
                        {t('cart.moveToCart', 'Move to cart')}
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>

        {/* Order Summary */}
        <div className="lg:col-span-1">
          <div className="bg-surface rounded-lg shadow-sm border border-border p-6 sticky top-4">
            <h2 className="text-lg font-semibold text-foreground mb-4">
              {t('cart.orderSummary', 'Order Summary')}
            </h2>

            {/* Promo Code */}
            <div className="mb-4">
              <label className="block text-sm font-medium text-foreground mb-2">
                {t('cart.promoCode', 'Promo Code')}
              </label>
              <div className="flex gap-2">
                <input
                  type="text"
                  value={promoCode}
                  onChange={(e) => setPromoCode(e.target.value)}
                  placeholder={t('cart.enterPromoCode', 'Enter code')}
                  className="flex-1 px-3 py-2 border border-border rounded-md bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-brand"
                />
                <button
                  disabled={!promoCode.trim()}
                  className="px-4 py-2 bg-surface-hover text-foreground rounded-md hover:bg-surface disabled:opacity-50"
                >
                  {t('cart.apply', 'Apply')}
                </button>
              </div>
            </div>

            {/* Totals */}
            <div className="space-y-2 mb-4">
              <div className="flex justify-between text-sm">
                <span className="text-foreground-muted">
                  {t('cart.subtotal', 'Subtotal')}
                </span>
                <span className="font-medium text-foreground">₴{totalPrice.toFixed(2)}</span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-foreground-muted">
                  {t('cart.shipping', 'Shipping')}
                </span>
                <span className="font-medium text-success">
                  {t('cart.free', 'Free')}
                </span>
              </div>
              <div className="border-t border-border pt-2 mt-2">
                <div className="flex justify-between">
                  <span className="text-lg font-semibold text-foreground">
                    {t('cart.total', 'Total')}
                  </span>
                  <span className="text-xl font-bold text-foreground">
                    ₴{totalPrice.toFixed(2)}
                  </span>
                </div>
              </div>
            </div>

            {/* Checkout Button */}
            <button
              onClick={handleCheckout}
              disabled={isUpdating || activeItems.length === 0 || hasOutOfStockItems || Object.keys(stockErrors).length > 0}
              className="w-full flex items-center justify-center gap-2 px-6 py-3 bg-brand text-white font-medium rounded-md hover:bg-brand-dark disabled:opacity-50 disabled:cursor-not-allowed"
              title={hasOutOfStockItems ? t('cart.cannotCheckoutOutOfStock', 'Remove out of stock items to proceed') : undefined}
            >
              {t('cart.checkout', 'Proceed to Checkout')}
              <ArrowRight className="w-5 h-5" />
            </button>

            {/* Continue Shopping */}
            <Link
              to="/"
              className="mt-3 block text-center text-sm text-brand hover:text-brand-dark"
            >
              {t('cart.continueShopping', 'Continue Shopping')}
            </Link>
          </div>
        </div>
      </div>
    </div>
  )
}
