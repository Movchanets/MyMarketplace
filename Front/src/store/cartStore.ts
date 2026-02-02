import { create } from 'zustand'
import { persist, createJSONStorage } from 'zustand/middleware'
import { cartApi, type CartDto, type CartItemDto } from '../api/cartApi'
import { useAuthStore } from './authStore'

// Guest cart storage key
const GUEST_CART_KEY = 'guestCart'

interface GuestCartItem {
  productId: string
  skuId: string
  quantity: number
  addedAt: string
}

// Snapshot for optimistic update rollback
interface CartSnapshot {
  cart: CartDto | null
  guestCart: GuestCartItem[]
}

interface CartState {
  cart: CartDto | null
  guestCart: GuestCartItem[]
  isLoading: boolean
  isUpdating: boolean
  error: string | null
  lastError: string | null // Keep track of last error for debugging

  // Computed selectors
  getTotalItems: () => number
  getTotalPrice: () => number
  getItemCount: (skuId: string) => number
  isInCart: (skuId: string) => boolean
  getCartItemBySkuId: (skuId: string) => CartItemDto | GuestCartItem | undefined

  // Actions
  loadCart: () => Promise<void>
  addToCart: (productId: string, skuId: string, quantity: number) => Promise<boolean>
  updateQuantity: (cartItemId: string, quantity: number) => Promise<boolean>
  updateQuantityBySku: (skuId: string, quantity: number) => Promise<boolean>
  removeFromCart: (cartItemId: string) => Promise<boolean>
  removeFromCartBySku: (skuId: string) => Promise<boolean>
  clearCart: () => Promise<boolean>
  clearError: () => void

  // Guest cart actions
  addToGuestCart: (productId: string, skuId: string, quantity: number) => void
  updateGuestCartQuantity: (skuId: string, quantity: number) => void
  removeFromGuestCart: (skuId: string) => void
  clearGuestCart: () => void
  mergeGuestCart: () => Promise<boolean>

  // Internal helpers
  _createSnapshot: () => CartSnapshot
  _restoreSnapshot: (snapshot: CartSnapshot) => void
}

export const useCartStore = create<CartState>()(
  persist(
    (set, get) => ({
      cart: null,
      guestCart: [],
      isLoading: false,
      isUpdating: false,
      error: null,
      lastError: null,

      // Internal helpers for optimistic updates
      _createSnapshot: () => ({
        cart: get().cart ? { ...get().cart!, items: [...get().cart!.items] } : null,
        guestCart: [...get().guestCart]
      }),

      _restoreSnapshot: (snapshot: CartSnapshot) => {
        set({ cart: snapshot.cart, guestCart: snapshot.guestCart })
      },

      // Computed selectors
      getTotalItems: () => {
        const state = get()
        if (state.cart) {
          return state.cart.totalItems
        }
        return state.guestCart.reduce((sum, item) => sum + item.quantity, 0)
      },

      getTotalPrice: () => {
        const state = get()
        if (state.cart) {
          return state.cart.totalPrice
        }
        // Guest cart doesn't have prices stored locally
        return 0
      },

      getItemCount: (skuId: string) => {
        const state = get()
        if (state.cart) {
          const item = state.cart.items.find(i => i.skuId === skuId)
          return item?.quantity ?? 0
        }
        const guestItem = state.guestCart.find(i => i.skuId === skuId)
        return guestItem?.quantity ?? 0
      },

      isInCart: (skuId: string) => {
        const state = get()
        if (state.cart) {
          return state.cart.items.some(i => i.skuId === skuId)
        }
        return state.guestCart.some(i => i.skuId === skuId)
      },

      getCartItemBySkuId: (skuId: string) => {
        const state = get()
        if (state.cart) {
          return state.cart.items.find(i => i.skuId === skuId)
        }
        return state.guestCart.find(i => i.skuId === skuId)
      },

      // Actions
      loadCart: async () => {
        const isAuthenticated = useAuthStore.getState().isAuthenticated
        set({ isLoading: true, error: null })

        try {
          if (isAuthenticated) {
            const response = await cartApi.getCart()
            if (response.isSuccess) {
              // Handle both null payload (empty cart) and valid payload
              const cartData = response.payload ?? {
                id: '',
                userId: '',
                items: [],
                totalItems: 0,
                totalPrice: 0
              }
              set({ cart: cartData })
              
              // Merge guest cart if exists
              const { guestCart, mergeGuestCart } = get()
              if (guestCart.length > 0) {
                await mergeGuestCart()
              }
            } else {
              console.error('Failed to load cart:', response.message)
              set({ error: response.message, lastError: response.message })
            }
          } else {
            // Load guest cart from localStorage (handled by persist middleware)
            // Just ensure we clear any stale server cart
            set({ cart: null })
          }
        } catch (error) {
          const errorMessage = error instanceof Error ? error.message : 'Failed to load cart'
          console.error('Failed to load cart:', error)
          set({ error: errorMessage, lastError: errorMessage })
        } finally {
          set({ isLoading: false })
        }
      },

      addToCart: async (productId: string, skuId: string, quantity: number): Promise<boolean> => {
        const isAuthenticated = useAuthStore.getState().isAuthenticated
        const snapshot = get()._createSnapshot()
        
        set({ isUpdating: true, error: null })

        try {
          if (isAuthenticated) {
            // Optimistic update
            const currentCart = get().cart
            if (currentCart) {
              const existingItemIndex = currentCart.items.findIndex(i => i.skuId === skuId)
              const updatedItems = [...currentCart.items]
              
              if (existingItemIndex >= 0) {
                // Update existing item
                const existingItem = { ...updatedItems[existingItemIndex] }
                existingItem.quantity += quantity
                existingItem.subtotal = existingItem.unitPrice * existingItem.quantity
                updatedItems[existingItemIndex] = existingItem
              }
              // Note: For new items, we don't have full item data, so we let the server response fill it
              
              set({ 
                cart: { 
                  ...currentCart, 
                  items: updatedItems,
                  totalItems: currentCart.totalItems + quantity,
                  totalPrice: updatedItems.reduce((sum, i) => sum + i.subtotal, 0)
                } 
              })
            }

            const response = await cartApi.addToCart({ productId, skuId, quantity })
            if (response.isSuccess && response.payload) {
              set({ cart: response.payload })
              return true
            } else {
              // Rollback on failure
              get()._restoreSnapshot(snapshot)
              const errorMessage = response.message || 'Failed to add to cart'
              set({ error: errorMessage, lastError: errorMessage })
              return false
            }
          } else {
            // Guest cart
            get().addToGuestCart(productId, skuId, quantity)
            return true
          }
        } catch (error) {
          // Rollback on error
          get()._restoreSnapshot(snapshot)
          const errorMessage = error instanceof Error ? error.message : 'Failed to add to cart'
          console.error('Failed to add to cart:', error)
          set({ error: errorMessage, lastError: errorMessage })
          return false
        } finally {
          set({ isUpdating: false })
        }
      },

      updateQuantity: async (cartItemId: string, quantity: number): Promise<boolean> => {
        if (!cartItemId) {
          console.error('updateQuantity called with empty cartItemId')
          set({ error: 'Invalid cart item', lastError: 'Invalid cart item' })
          return false
        }

        const snapshot = get()._createSnapshot()
        set({ isUpdating: true, error: null })

        try {
          // Optimistic update
          const currentCart = get().cart
          if (currentCart) {
            const itemIndex = currentCart.items.findIndex(i => i.id === cartItemId)
            if (itemIndex >= 0) {
              const updatedItems = [...currentCart.items]
              const item = { ...updatedItems[itemIndex] }
              const oldQuantity = item.quantity
              item.quantity = quantity
              item.subtotal = item.unitPrice * quantity
              updatedItems[itemIndex] = item
              
              const quantityDiff = quantity - oldQuantity
              set({ 
                cart: { 
                  ...currentCart, 
                  items: updatedItems,
                  totalItems: currentCart.totalItems + quantityDiff,
                  totalPrice: updatedItems.reduce((sum, i) => sum + i.subtotal, 0)
                } 
              })
            }
          }

          const response = await cartApi.updateQuantity(cartItemId, { quantity })
          if (response.isSuccess && response.payload) {
            set({ cart: response.payload })
            return true
          } else {
            // Rollback on failure
            get()._restoreSnapshot(snapshot)
            const errorMessage = response.message || 'Failed to update quantity'
            set({ error: errorMessage, lastError: errorMessage })
            return false
          }
        } catch (error) {
          // Rollback on error
          get()._restoreSnapshot(snapshot)
          const errorMessage = error instanceof Error ? error.message : 'Failed to update quantity'
          console.error('Failed to update quantity:', error)
          set({ error: errorMessage, lastError: errorMessage })
          return false
        } finally {
          set({ isUpdating: false })
        }
      },

      updateQuantityBySku: async (skuId: string, quantity: number): Promise<boolean> => {
        if (!skuId) {
          console.error('updateQuantityBySku called with empty skuId')
          set({ error: 'Invalid SKU', lastError: 'Invalid SKU' })
          return false
        }

        const isAuthenticated = useAuthStore.getState().isAuthenticated
        const snapshot = get()._createSnapshot()
        set({ isUpdating: true, error: null })

        try {
          if (isAuthenticated) {
            const response = await cartApi.updateQuantityBySku(skuId, { quantity })
            if (response.isSuccess && response.payload) {
              set({ cart: response.payload })
              return true
            } else {
              get()._restoreSnapshot(snapshot)
              const errorMessage = response.message || 'Failed to update quantity'
              set({ error: errorMessage, lastError: errorMessage })
              return false
            }
          } else {
            get().updateGuestCartQuantity(skuId, quantity)
            return true
          }
        } catch (error) {
          get()._restoreSnapshot(snapshot)
          const errorMessage = error instanceof Error ? error.message : 'Failed to update quantity'
          console.error('Failed to update quantity:', error)
          set({ error: errorMessage, lastError: errorMessage })
          return false
        } finally {
          set({ isUpdating: false })
        }
      },

      removeFromCart: async (cartItemId: string): Promise<boolean> => {
        if (!cartItemId) {
          console.error('removeFromCart called with empty cartItemId')
          set({ error: 'Invalid cart item', lastError: 'Invalid cart item' })
          return false
        }

        const snapshot = get()._createSnapshot()
        set({ isUpdating: true, error: null })

        try {
          // Optimistic update
          const currentCart = get().cart
          if (currentCart) {
            const updatedItems = currentCart.items.filter(i => i.id !== cartItemId)
            set({
              cart: {
                ...currentCart,
                items: updatedItems,
                totalItems: updatedItems.reduce((sum, i) => sum + i.quantity, 0),
                totalPrice: updatedItems.reduce((sum, i) => sum + i.subtotal, 0)
              }
            })
          }

          const response = await cartApi.removeFromCart(cartItemId)
          if (response.isSuccess && response.payload) {
            set({ cart: response.payload })
            return true
          } else {
            get()._restoreSnapshot(snapshot)
            const errorMessage = response.message || 'Failed to remove from cart'
            set({ error: errorMessage, lastError: errorMessage })
            return false
          }
        } catch (error) {
          get()._restoreSnapshot(snapshot)
          const errorMessage = error instanceof Error ? error.message : 'Failed to remove from cart'
          console.error('Failed to remove from cart:', error)
          set({ error: errorMessage, lastError: errorMessage })
          return false
        } finally {
          set({ isUpdating: false })
        }
      },

      removeFromCartBySku: async (skuId: string): Promise<boolean> => {
        if (!skuId) {
          console.error('removeFromCartBySku called with empty skuId')
          set({ error: 'Invalid SKU', lastError: 'Invalid SKU' })
          return false
        }

        const isAuthenticated = useAuthStore.getState().isAuthenticated
        const snapshot = get()._createSnapshot()
        set({ isUpdating: true, error: null })

        try {
          if (isAuthenticated) {
            // Optimistic update
            const currentCart = get().cart
            if (currentCart) {
              const updatedItems = currentCart.items.filter(i => i.skuId !== skuId)
              set({
                cart: {
                  ...currentCart,
                  items: updatedItems,
                  totalItems: updatedItems.reduce((sum, i) => sum + i.quantity, 0),
                  totalPrice: updatedItems.reduce((sum, i) => sum + i.subtotal, 0)
                }
              })
            }

            const response = await cartApi.removeFromCartBySku(skuId)
            if (response.isSuccess && response.payload) {
              set({ cart: response.payload })
              return true
            } else {
              get()._restoreSnapshot(snapshot)
              const errorMessage = response.message || 'Failed to remove from cart'
              set({ error: errorMessage, lastError: errorMessage })
              return false
            }
          } else {
            get().removeFromGuestCart(skuId)
            return true
          }
        } catch (error) {
          get()._restoreSnapshot(snapshot)
          const errorMessage = error instanceof Error ? error.message : 'Failed to remove from cart'
          console.error('Failed to remove from cart:', error)
          set({ error: errorMessage, lastError: errorMessage })
          return false
        } finally {
          set({ isUpdating: false })
        }
      },

      clearCart: async (): Promise<boolean> => {
        const isAuthenticated = useAuthStore.getState().isAuthenticated
        const snapshot = get()._createSnapshot()
        set({ isUpdating: true, error: null })

        try {
          if (isAuthenticated) {
            // Optimistic update
            set({ cart: { ...get().cart!, items: [], totalItems: 0, totalPrice: 0 } })

            const response = await cartApi.clearCart()
            if (response.isSuccess) {
              set({ cart: null })
              return true
            } else {
              get()._restoreSnapshot(snapshot)
              const errorMessage = response.message || 'Failed to clear cart'
              set({ error: errorMessage, lastError: errorMessage })
              return false
            }
          } else {
            get().clearGuestCart()
            return true
          }
        } catch (error) {
          get()._restoreSnapshot(snapshot)
          const errorMessage = error instanceof Error ? error.message : 'Failed to clear cart'
          console.error('Failed to clear cart:', error)
          set({ error: errorMessage, lastError: errorMessage })
          return false
        } finally {
          set({ isUpdating: false })
        }
      },

      clearError: () => set({ error: null }),

      // Guest cart actions
      addToGuestCart: (productId: string, skuId: string, quantity: number) => {
        const { guestCart } = get()
        const existingItem = guestCart.find(i => i.skuId === skuId)

        if (existingItem) {
          existingItem.quantity += quantity
          set({ guestCart: [...guestCart] })
        } else {
          set({ 
            guestCart: [...guestCart, { 
              productId, 
              skuId, 
              quantity, 
              addedAt: new Date().toISOString() 
            }] 
          })
        }
      },

      updateGuestCartQuantity: (skuId: string, quantity: number) => {
        const { guestCart } = get()
        
        if (quantity <= 0) {
          get().removeFromGuestCart(skuId)
          return
        }

        const item = guestCart.find(i => i.skuId === skuId)
        if (item) {
          item.quantity = quantity
          set({ guestCart: [...guestCart] })
        }
      },

      removeFromGuestCart: (skuId: string) => {
        const { guestCart } = get()
        set({ guestCart: guestCart.filter(i => i.skuId !== skuId) })
      },

      clearGuestCart: () => {
        set({ guestCart: [] })
        localStorage.removeItem(GUEST_CART_KEY)
      },

      mergeGuestCart: async (): Promise<boolean> => {
        const { guestCart } = get()
        if (guestCart.length === 0) return true

        set({ isUpdating: true, error: null })

        try {
          // Use the batch merge API instead of sequential individual calls
          // This prevents concurrency conflicts on the backend
          const itemsToMerge = guestCart.map(item => ({
            productId: item.productId,
            skuId: item.skuId,
            quantity: item.quantity
          }))

          const response = await cartApi.mergeGuestCart(itemsToMerge)
          
          if (response.isSuccess && response.payload) {
            // Clear guest cart after successful merge
            get().clearGuestCart()
            
            // Update cart with merged result from server
            set({ cart: response.payload })
            return true
          } else {
            const errorMessage = response.message || 'Failed to merge guest cart'
            console.error('Failed to merge guest cart:', errorMessage)
            set({ error: errorMessage, lastError: errorMessage })
            return false
          }
        } catch (error) {
          const errorMessage = error instanceof Error ? error.message : 'Failed to merge guest cart'
          console.error('Failed to merge guest cart:', error)
          set({ error: errorMessage, lastError: errorMessage })
          return false
        } finally {
          set({ isUpdating: false })
        }
      },
    }),
    {
      name: 'cart-storage',
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({ guestCart: state.guestCart }),
    }
  )
)
