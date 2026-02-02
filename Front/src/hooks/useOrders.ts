import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { ordersApi, type OrderDetailDto, type OrderStatus, type PagedOrdersResult } from '../api/ordersApi'
import { unwrapServiceResponse } from '../api/types'

const ORDERS_QUERY_KEY = 'orders'

interface UseOrdersParams {
  status?: OrderStatus | null
  pageNumber?: number
  pageSize?: number
}

export function useOrders(params: UseOrdersParams = {}) {
  const queryClient = useQueryClient()
  const { status, pageNumber = 1, pageSize = 10 } = params

  const { data, isLoading, error } = useQuery<PagedOrdersResult | null>({
    queryKey: [ORDERS_QUERY_KEY, { status, pageNumber, pageSize }],
    queryFn: async () => {
      try {
        const response = await ordersApi.getOrders(status, null, null, 'CreatedAt', true, pageNumber, pageSize)
        return unwrapServiceResponse(response)
      } catch {
        return null
      }
    },
    staleTime: 1000 * 60 * 2, // 2 minutes
  })

  const cancelOrderMutation = useMutation({
    mutationFn: ({ orderId, reason }: { orderId: string; reason?: string }) =>
      ordersApi.cancelOrder(orderId, { reason }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [ORDERS_QUERY_KEY] })
    },
  })

  return {
    orders: data?.orders ?? [],
    totalCount: data?.totalCount ?? 0,
    totalPages: data?.totalPages ?? 1,
    isLoading,
    error,
    cancelOrder: cancelOrderMutation.mutateAsync,
    isCancelling: cancelOrderMutation.isPending,
  }
}

export function useOrder(orderId: string | null) {
  const { data: order, isLoading, error } = useQuery<OrderDetailDto | null>({
    queryKey: [ORDERS_QUERY_KEY, 'detail', orderId],
    queryFn: async () => {
      if (!orderId) return null
      try {
        const response = await ordersApi.getOrder(orderId)
        return unwrapServiceResponse(response)
      } catch {
        return null
      }
    },
    enabled: !!orderId,
    staleTime: 1000 * 60 * 2,
  })

  return { order, isLoading, error }
}

export function useOrderStatusHistory(orderId: string | null) {
  const { data: history, isLoading } = useQuery({
    queryKey: [ORDERS_QUERY_KEY, 'history', orderId],
    queryFn: async () => {
      if (!orderId) return null
      try {
        const response = await ordersApi.getOrderStatusHistory(orderId)
        return unwrapServiceResponse(response)
      } catch {
        return null
      }
    },
    enabled: !!orderId,
    staleTime: 1000 * 60 * 5,
  })

  return { history, isLoading }
}
