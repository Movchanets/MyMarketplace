import { create } from 'zustand'
import { persist, createJSONStorage } from 'zustand/middleware'

interface GuestCartItem {
  productId: string
  skuId: string
  quantity: number
  addedAt: string
}

interface CartState {
  guestCart: GuestCartItem[]
  addToGuestCart: (productId: string, skuId: string, quantity: number) => void
  updateGuestCartQuantity: (skuId: string, quantity: number) => void
  removeFromGuestCart: (skuId: string) => void
  clearGuestCart: () => void
  getItemCount: (skuId: string) => number
  isInCart: (skuId: string) => boolean
}

export const useCartStore = create<CartState>()(
  persist(
    (set, get) => ({
      guestCart: [],

      addToGuestCart: (productId: string, skuId: string, quantity: number) => {
        if (quantity <= 0) return

        set((state) => {
          const existing = state.guestCart.find((item) => item.skuId === skuId)
          if (existing) {
            return {
              guestCart: state.guestCart.map((item) =>
                item.skuId === skuId ? { ...item, quantity: item.quantity + quantity } : item,
              ),
            }
          }

          return {
            guestCart: [
              ...state.guestCart,
              {
                productId,
                skuId,
                quantity,
                addedAt: new Date().toISOString(),
              },
            ],
          }
        })
      },

      updateGuestCartQuantity: (skuId: string, quantity: number) => {
        if (quantity <= 0) {
          get().removeFromGuestCart(skuId)
          return
        }

        set((state) => ({
          guestCart: state.guestCart.map((item) =>
            item.skuId === skuId ? { ...item, quantity } : item,
          ),
        }))
      },

      removeFromGuestCart: (skuId: string) => {
        set((state) => ({
          guestCart: state.guestCart.filter((item) => item.skuId !== skuId),
        }))
      },

      clearGuestCart: () => {
        set({ guestCart: [] })
      },

      getItemCount: (skuId: string) => {
        const item = get().guestCart.find((cartItem) => cartItem.skuId === skuId)
        return item?.quantity ?? 0
      },

      isInCart: (skuId: string) => {
        return get().getItemCount(skuId) > 0
      },
    }),
    {
      name: 'cart-storage',
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({ guestCart: state.guestCart }),
    },
  ),
)
