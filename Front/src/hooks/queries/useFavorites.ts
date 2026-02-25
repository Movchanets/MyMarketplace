import { useMemo } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { favoritesApi, type FavoriteProductDto } from '../../api/catalogApi'
import { queryKeys } from './keys'
import { useServiceQuery } from './useServiceQuery'
import { useAuthStore } from '../../store/authStore'

const GUEST_FAVORITES_KEY = 'guestFavorites'

const readGuestFavorites = (): string[] => {
  if (typeof window === 'undefined') {
    return []
  }

  const raw = localStorage.getItem(GUEST_FAVORITES_KEY)
  if (!raw) {
    return []
  }

  try {
    const parsed = JSON.parse(raw)
    return Array.isArray(parsed) ? parsed.filter((item): item is string => typeof item === 'string') : []
  } catch {
    return []
  }
}

const writeGuestFavorites = (favorites: string[]) => {
  localStorage.setItem(GUEST_FAVORITES_KEY, JSON.stringify(favorites))
}

/**
 * Unified favorites hook for authenticated and guest users.
 * Uses server state for signed-in users and localStorage-backed state for guests.
 */
export function useFavorites() {
  const queryClient = useQueryClient()
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated)

  const serverFavoritesQuery = useServiceQuery<FavoriteProductDto[]>({
    queryKey: queryKeys.favorites.all,
    queryFn: () => favoritesApi.getFavorites(),
    enabled: isAuthenticated,
  })

  const guestFavoritesQuery = useQuery({
    queryKey: [...queryKeys.favorites.all, 'guest'],
    queryFn: async () => readGuestFavorites(),
    enabled: !isAuthenticated,
    staleTime: Infinity,
  })

  const toggleFavoriteMutation = useMutation({
    mutationKey: [...queryKeys.favorites.all, 'toggle'],
    mutationFn: async ({ productId, isFavorited }: { productId: string; isFavorited: boolean }) => {
      if (!isAuthenticated) {
        const current = readGuestFavorites()
        const updated = isFavorited
          ? current.filter((id) => id !== productId)
          : [...new Set([...current, productId])]
        writeGuestFavorites(updated)
        return { isSuccess: true }
      }

      return isFavorited
        ? favoritesApi.removeFromFavorites(productId)
        : favoritesApi.addToFavorites(productId)
    },
    onMutate: async ({ productId, isFavorited }) => {
      const previousServer = queryClient.getQueryData<FavoriteProductDto[]>(queryKeys.favorites.all)
      const previousGuest = queryClient.getQueryData<string[]>([...queryKeys.favorites.all, 'guest'])

      if (!isAuthenticated) {
        const nextGuest = isFavorited
          ? (previousGuest ?? []).filter((id) => id !== productId)
          : [...new Set([...(previousGuest ?? []), productId])]

        queryClient.setQueryData([...queryKeys.favorites.all, 'guest'], nextGuest)
        writeGuestFavorites(nextGuest)
        return { previousServer, previousGuest }
      }

      const current = previousServer ?? []
      const nextServer = isFavorited
        ? current.filter((item) => item.id !== productId)
        : [
            ...current,
            {
              id: productId,
              name: '',
              slug: '',
              baseImageUrl: null,
              minPrice: null,
              inStock: false,
              favoritedAt: new Date().toISOString(),
            },
          ]

      queryClient.setQueryData(queryKeys.favorites.all, nextServer)
      return { previousServer, previousGuest }
    },
    onError: (_error, _variables, context) => {
      if (context?.previousServer) {
        queryClient.setQueryData(queryKeys.favorites.all, context.previousServer)
      }

      if (context?.previousGuest) {
        queryClient.setQueryData([...queryKeys.favorites.all, 'guest'], context.previousGuest)
        writeGuestFavorites(context.previousGuest)
      }
    },
    onSettled: () => {
      if (isAuthenticated) {
        queryClient.invalidateQueries({ queryKey: queryKeys.favorites.all })
      }
    },
  })

  const favoriteIds = useMemo(() => {
    if (isAuthenticated) {
      return new Set((serverFavoritesQuery.data ?? []).map((item) => item.id))
    }

    return new Set(guestFavoritesQuery.data ?? [])
  }, [isAuthenticated, serverFavoritesQuery.data, guestFavoritesQuery.data])

  const toggleFavorite = async (productId: string) => {
    const currentlyFavorited = favoriteIds.has(productId)
    await toggleFavoriteMutation.mutateAsync({ productId, isFavorited: currentlyFavorited })
  }

  const isToggling = useMemo(() => {
    if (!toggleFavoriteMutation.variables?.productId || !toggleFavoriteMutation.isPending) {
      return new Set<string>()
    }

    return new Set([toggleFavoriteMutation.variables.productId])
  }, [toggleFavoriteMutation.isPending, toggleFavoriteMutation.variables])

  return {
    favorites: favoriteIds,
    favoriteProducts: serverFavoritesQuery.data ?? [],
    isLoading: isAuthenticated ? serverFavoritesQuery.isPending : guestFavoritesQuery.isPending,
    isToggling,
    toggleFavorite,
    isFavorited: (productId: string) => favoriteIds.has(productId),
  }
}
