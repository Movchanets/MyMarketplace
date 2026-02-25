import { useMutation, useQueryClient } from '@tanstack/react-query'
import { cartApi, type AddToCartRequest, type CartDto } from '../../api/cartApi'
import { queryKeys } from './keys'
import { useServiceQuery } from './useServiceQuery'
import { useAuthStore } from '../../store/authStore'
import { useCartStore } from '../../store/cartStore'

/**
 * Unified cart hook for authenticated and guest users.
 * Handles server cart queries, optimistic mutations, and guest-cart merge on login.
 */
export function useCart() {
  const queryClient = useQueryClient()
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated)
  const guestCart = useCartStore((state) => state.guestCart)
  const addToGuestCart = useCartStore((state) => state.addToGuestCart)
  const updateGuestCartQuantity = useCartStore((state) => state.updateGuestCartQuantity)
  const removeFromGuestCart = useCartStore((state) => state.removeFromGuestCart)
  const clearGuestCart = useCartStore((state) => state.clearGuestCart)

  const cartQuery = useServiceQuery<CartDto>({
    queryKey: queryKeys.cart.all,
    queryFn: () => cartApi.getCart(),
    enabled: isAuthenticated,
  })

  const addToCartMutation = useMutation({
    mutationKey: [...queryKeys.cart.all, 'add'],
    mutationFn: (data: AddToCartRequest) => cartApi.addToCart(data),
    onMutate: async (variables) => {
      await queryClient.cancelQueries({ queryKey: queryKeys.cart.all })

      const previousCart = queryClient.getQueryData<CartDto>(queryKeys.cart.all)

      if (previousCart) {
        const updatedItems = previousCart.items.map((item) => {
          if (item.skuId !== variables.skuId) {
            return item
          }

          const nextQuantity = item.quantity + variables.quantity
          return {
            ...item,
            quantity: nextQuantity,
            subtotal: item.unitPrice * nextQuantity,
          }
        })

        queryClient.setQueryData<CartDto>(queryKeys.cart.all, {
          ...previousCart,
          items: updatedItems,
          totalItems: updatedItems.reduce((sum, item) => sum + item.quantity, 0),
          totalPrice: updatedItems.reduce((sum, item) => sum + item.subtotal, 0),
        })
      }

      return { previousCart }
    },
    onError: (_error, _variables, context) => {
      if (context?.previousCart) {
        queryClient.setQueryData(queryKeys.cart.all, context.previousCart)
      }
    },
    onSuccess: (response) => {
      if (response.isSuccess && response.payload) {
        queryClient.setQueryData(queryKeys.cart.all, response.payload)
      }
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.cart.all })
    },
  })

  const mergeGuestCartMutation = useMutation({
    mutationKey: [...queryKeys.cart.all, 'mergeGuest'],
    mutationFn: (items: AddToCartRequest[]) => cartApi.mergeGuestCart(items),
    onSuccess: (response) => {
      if (response.isSuccess && response.payload) {
        queryClient.setQueryData(queryKeys.cart.all, response.payload)
        clearGuestCart()
      }
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.cart.all })
    },
  })

  const updateQuantityBySkuMutation = useMutation({
    mutationKey: [...queryKeys.cart.all, 'updateBySku'],
    mutationFn: ({ skuId, quantity }: { skuId: string; quantity: number }) =>
      cartApi.updateQuantityBySku(skuId, { quantity }),
    onSuccess: (response) => {
      if (response.isSuccess && response.payload) {
        queryClient.setQueryData(queryKeys.cart.all, response.payload)
      }
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.cart.all })
    },
  })

  const removeFromCartBySkuMutation = useMutation({
    mutationKey: [...queryKeys.cart.all, 'removeBySku'],
    mutationFn: (skuId: string) => cartApi.removeFromCartBySku(skuId),
    onSuccess: (response) => {
      if (response.isSuccess && response.payload) {
        queryClient.setQueryData(queryKeys.cart.all, response.payload)
      }
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.cart.all })
    },
  })

  const clearCartMutation = useMutation({
    mutationKey: [...queryKeys.cart.all, 'clear'],
    mutationFn: () => cartApi.clearCart(),
    onSuccess: (response) => {
      if (response.isSuccess) {
        queryClient.removeQueries({ queryKey: queryKeys.cart.all })
      }
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.cart.all })
    },
  })

  const loadCart = async () => {
    if (!isAuthenticated) {
      return
    }

    const cart = await cartQuery.refetch()

    if (!cart.error && guestCart.length > 0) {
      await mergeGuestCartMutation.mutateAsync(
        guestCart.map((item) => ({
          productId: item.productId,
          skuId: item.skuId,
          quantity: item.quantity,
        })),
      )
    }
  }

  const addToCart = async (productId: string, skuId: string, quantity: number): Promise<boolean> => {
    if (!isAuthenticated) {
      addToGuestCart(productId, skuId, quantity)
      return true
    }

    try {
      const response = await addToCartMutation.mutateAsync({ productId, skuId, quantity })
      return !!response.isSuccess
    } catch {
      return false
    }
  }

  const updateQuantityBySku = async (skuId: string, quantity: number): Promise<boolean> => {
    if (!isAuthenticated) {
      updateGuestCartQuantity(skuId, quantity)
      return true
    }

    try {
      const response = await updateQuantityBySkuMutation.mutateAsync({ skuId, quantity })
      return !!response.isSuccess
    } catch {
      return false
    }
  }

  const removeFromCartBySku = async (skuId: string): Promise<boolean> => {
    if (!isAuthenticated) {
      removeFromGuestCart(skuId)
      return true
    }

    try {
      const response = await removeFromCartBySkuMutation.mutateAsync(skuId)
      return !!response.isSuccess
    } catch {
      return false
    }
  }

  const clearCart = async (): Promise<boolean> => {
    if (!isAuthenticated) {
      clearGuestCart()
      return true
    }

    try {
      const response = await clearCartMutation.mutateAsync()
      return !!response.isSuccess
    } catch {
      return false
    }
  }

  const getItemCount = (skuId: string): number => {
    if (isAuthenticated) {
      const cart = cartQuery.data
      const item = cart?.items.find((cartItem) => cartItem.skuId === skuId)
      return item?.quantity ?? 0
    }

    const guestItem = guestCart.find((cartItem) => cartItem.skuId === skuId)
    return guestItem?.quantity ?? 0
  }

  const isInCart = (skuId: string): boolean => getItemCount(skuId) > 0

  const getTotalItems = (): number => {
    if (isAuthenticated) {
      return cartQuery.data?.totalItems ?? 0
    }

    return guestCart.reduce((sum, item) => sum + item.quantity, 0)
  }

  const getTotalPrice = (): number => {
    if (isAuthenticated) {
      return cartQuery.data?.totalPrice ?? 0
    }

    return 0
  }

  const resetLocalCart = () => {
    queryClient.removeQueries({ queryKey: queryKeys.cart.all })
  }

  const isUpdating =
    addToCartMutation.isPending ||
    mergeGuestCartMutation.isPending ||
    updateQuantityBySkuMutation.isPending ||
    removeFromCartBySkuMutation.isPending ||
    clearCartMutation.isPending

  return {
    cart: cartQuery.data ?? null,
    guestCart,
    isLoading: cartQuery.isPending,
    error: cartQuery.error,
    isUpdating,
    loadCart,
    addToCart,
    updateQuantityBySku,
    removeFromCartBySku,
    clearCart,
    clearGuestCart,
    getItemCount,
    isInCart,
    getTotalItems,
    getTotalPrice,
    resetLocalCart,
  }
}
