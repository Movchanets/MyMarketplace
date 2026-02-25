import { create } from 'zustand'
import { persist, createJSONStorage } from 'zustand/middleware'

interface FavoritesState {
  guestFavorites: string[]
  toggleGuestFavorite: (productId: string) => void
  removeGuestFavorite: (productId: string) => void
  clearGuestFavorites: () => void
}

export const useFavoritesStore = create<FavoritesState>()(
  persist(
    (set) => ({
      guestFavorites: [],

      toggleGuestFavorite: (productId: string) => {
        set((state) => {
          const exists = state.guestFavorites.includes(productId)
          return {
            guestFavorites: exists
              ? state.guestFavorites.filter((id) => id !== productId)
              : [...state.guestFavorites, productId],
          }
        })
      },

      removeGuestFavorite: (productId: string) => {
        set((state) => ({
          guestFavorites: state.guestFavorites.filter((id) => id !== productId),
        }))
      },

      clearGuestFavorites: () => {
        set({ guestFavorites: [] })
      },
    }),
    {
      name: 'guestFavorites',
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({ guestFavorites: state.guestFavorites }),
    },
  ),
)

export const useIsFavorited = (productId: string) =>
  useFavoritesStore((state) => state.guestFavorites.includes(productId))

export const useFavoriteCount = () => useFavoritesStore((state) => state.guestFavorites.length)
