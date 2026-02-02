import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { cartApi, type CartDto, type AddToCartRequest } from '../api/cartApi'
import { unwrapServiceResponse } from '../api/types'

const CART_QUERY_KEY = 'cart'

export function useCart() {
  const queryClient = useQueryClient()

  const { data: cart, isLoading, error } = useQuery<CartDto | null>({
    queryKey: [CART_QUERY_KEY],
    queryFn: async () => {
      try {
        const response = await cartApi.getCart()
        return unwrapServiceResponse(response)
      } catch {
        return null
      }
    },
    staleTime: 1000 * 60 * 5, // 5 minutes
  })

  const addToCartMutation = useMutation({
    mutationFn: (data: AddToCartRequest) => cartApi.addToCart(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [CART_QUERY_KEY] })
    },
  })

  const updateQuantityMutation = useMutation({
    mutationFn: ({ cartItemId, quantity }: { cartItemId: string; quantity: number }) =>
      cartApi.updateQuantity(cartItemId, { quantity }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [CART_QUERY_KEY] })
    },
  })

  const removeFromCartMutation = useMutation({
    mutationFn: (cartItemId: string) => cartApi.removeFromCart(cartItemId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [CART_QUERY_KEY] })
    },
  })

  const clearCartMutation = useMutation({
    mutationFn: () => cartApi.clearCart(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [CART_QUERY_KEY] })
    },
  })

  return {
    cart,
    isLoading,
    error,
    addToCart: addToCartMutation.mutateAsync,
    isAddingToCart: addToCartMutation.isPending,
    updateQuantity: updateQuantityMutation.mutateAsync,
    isUpdatingQuantity: updateQuantityMutation.isPending,
    removeFromCart: removeFromCartMutation.mutateAsync,
    isRemovingFromCart: removeFromCartMutation.isPending,
    clearCart: clearCartMutation.mutateAsync,
    isClearingCart: clearCartMutation.isPending,
  }
}
