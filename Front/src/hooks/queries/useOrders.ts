import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  type CreateOrderRequest,
  ordersApi,
  type CancelOrderRequest,
  type OrderDetailDto,
  type OrderStatus,
  type PagedOrdersResult,
} from '../../api/ordersApi'
import { queryKeys } from './keys'
import { useServiceQuery } from './useServiceQuery'

interface UseOrdersParams {
  status?: OrderStatus | null
  sortBy?: string
  sortDescending?: boolean
  pageNumber?: number
  pageSize?: number
}

/** Loads paged order history with optional status and sorting filters. */
export function useOrders(params: UseOrdersParams = {}) {
  const {
    status,
    sortBy = 'CreatedAt',
    sortDescending = true,
    pageNumber = 1,
    pageSize = 10,
  } = params

  return useServiceQuery<PagedOrdersResult>({
    queryKey: queryKeys.orders.list({ status: status ?? null, sortBy, sortDescending, pageNumber, pageSize }),
    queryFn: () =>
      ordersApi.getOrders(status, null, null, sortBy, sortDescending, pageNumber, pageSize),
  })
}

/** Loads full order details by id. */
export function useOrder(orderId: string | null) {
  return useServiceQuery<OrderDetailDto>({
    queryKey: queryKeys.orders.detail(orderId ?? ''),
    queryFn: () => ordersApi.getOrder(orderId ?? ''),
    enabled: !!orderId,
  })
}

/** Cancels order and invalidates related order cache. */
export function useCancelOrder() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationKey: [...queryKeys.orders.all, 'cancel'],
    mutationFn: ({ orderId, data }: { orderId: string; data?: CancelOrderRequest }) =>
      ordersApi.cancelOrder(orderId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.orders.all })
    },
  })
}

/** Creates order from checkout payload and refreshes orders/cart caches. */
export function useCreateOrder() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationKey: [...queryKeys.orders.all, 'create'],
    mutationFn: (data: CreateOrderRequest) => ordersApi.createOrder(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.orders.all })
      queryClient.invalidateQueries({ queryKey: queryKeys.cart.all })
    },
  })
}
