import { create } from 'zustand'
import { favoritesApi } from '../api/catalogApi'
import { useAuthStore } from './authStore'

// Guest favorites storage key
const GUEST_FAVORITES_KEY = 'guestFavorites'

interface FavoritesState {
  favorites: Set<string>
  guestFavorites: Set<string>
  isLoading: boolean
  isToggling: Set<string>

  // Actions
  loadFavorites: () => Promise<void>
  toggleFavorite: (productId: string) => Promise<void>
  mergeGuestFavorites: () => Promise<void>
  clearGuestFavorites: () => void
}

export const useFavoritesStore = create<FavoritesState>((set, get) => ({
    favorites: new Set(),
  guestFavorites: new Set(),
  isLoading: false,
  isToggling: new Set(),

  loadFavorites: async () => {
    const isAuthenticated = useAuthStore.getState().isAuthenticated
    set({ isLoading: true })

    try {
      // Always load guest favorites from localStorage
      const storedGuestFavorites = localStorage.getItem(GUEST_FAVORITES_KEY)
      let guestFavorites = new Set<string>()
      if (storedGuestFavorites) {
        guestFavorites = new Set(JSON.parse(storedGuestFavorites))
        set({ guestFavorites })
      }

      // Load authenticated user favorites from API if authenticated
      if (isAuthenticated) {
        const response = await favoritesApi.getFavorites()
        if (response.isSuccess && response.payload) {
          const favorites = new Set(response.payload.map(f => f.id))
          set({ favorites })

          // Merge guest favorites if they exist
          if (guestFavorites.size > 0) {
            await get().mergeGuestFavorites()
          }
        }
      }
    } catch (error) {
      console.error('Failed to load favorites:', error)
    } finally {
      set({ isLoading: false })
    }
  },

  toggleFavorite: async (productId: string) => {
    const { favorites, guestFavorites, isToggling } = get()

    // Prevent multiple simultaneous toggles for the same product
    if (isToggling.has(productId)) return

    set({ isToggling: new Set([...isToggling, productId]) })

    try {
      // Check if user is authenticated
      const isAuthenticated = useAuthStore.getState().isAuthenticated

      if (!isAuthenticated) {
        // Guest user: Update localStorage immediately (optimistic)
        const newGuestFavorites = new Set(guestFavorites)
        if (newGuestFavorites.has(productId)) {
          newGuestFavorites.delete(productId)
        } else {
          newGuestFavorites.add(productId)
        }

        // Persist to localStorage
        localStorage.setItem(GUEST_FAVORITES_KEY, JSON.stringify([...newGuestFavorites]))
        set({ guestFavorites: newGuestFavorites })

      } else {
        // Authenticated user: Optimistic UI update
        const newFavorites = new Set(favorites)
        const wasFavorited = newFavorites.has(productId)

        // Immediate UI update (optimistic)
        if (wasFavorited) {
          newFavorites.delete(productId)
        } else {
          newFavorites.add(productId)
        }
        set({ favorites: newFavorites })

        try {
          // Make API call
          if (wasFavorited) {
            await favoritesApi.removeFromFavorites(productId)
          } else {
            await favoritesApi.addToFavorites(productId)
          }
        } catch (error) {
          // Revert on error (pessimistic fallback)
          console.error('Failed to toggle favorite:', error)

          const revertFavorites = new Set(favorites)
          if (wasFavorited) {
            revertFavorites.add(productId)
          } else {
            revertFavorites.delete(productId)
          }
          set({ favorites: revertFavorites })

          // You might want to show a toast notification here
          alert('Failed to update favorites. Please try again.')
        }
      }
    } finally {
      const newIsToggling = new Set(isToggling)
      newIsToggling.delete(productId)
      set({ isToggling: newIsToggling })
    }
  },

  mergeGuestFavorites: async () => {
    const { guestFavorites } = get()

    if (guestFavorites.size === 0) return

    try {
      const response = await favoritesApi.mergeGuestFavorites([...guestFavorites])
      if (response.isSuccess) {
        // Clear guest favorites and reload authenticated favorites
        localStorage.removeItem(GUEST_FAVORITES_KEY)
        set({ guestFavorites: new Set() })
        await get().loadFavorites()
      }
    } catch (error) {
      console.error('Failed to merge guest favorites:', error)
    }
  },

  clearGuestFavorites: () => {
    localStorage.removeItem(GUEST_FAVORITES_KEY)
    set({ guestFavorites: new Set() })
  }
}))

// Helper hook to check if a product is favorited
export const useIsFavorited = (productId: string) => {
  const { favorites, guestFavorites } = useFavoritesStore()
  const isAuthenticated = useAuthStore(state => state.isAuthenticated)

  if (isAuthenticated) {
    return favorites.has(productId)
  } else {
    return guestFavorites.has(productId)
  }
}

// Helper hook to get favorite count (if you want to show counts later)
export const useFavoriteCount = () => {
  const { favorites, guestFavorites } = useFavoritesStore()
  const isAuthenticated = useAuthStore(state => state.isAuthenticated)

  if (isAuthenticated) {
    return favorites.size
  } else {
    return guestFavorites.size
  }
}