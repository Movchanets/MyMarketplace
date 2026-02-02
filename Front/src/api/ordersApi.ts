import axiosClient from './axiousClient'
import type { ServiceResponse } from './types'

// Types
export type OrderStatus = 'Pending' | 'Confirmed' | 'Processing' | 'Shipped' | 'Delivered' | 'Cancelled'
export type PaymentStatus = 'Pending' | 'Processing' | 'Paid' | 'Failed' | 'Refunded' | 'PartiallyRefunded'

export interface OrderItemDto {
  id: string
  productId: string
  productName: string
  productImageUrl: string | null
  skuId: string
  skuCode: string
  skuAttributes: string | null
  quantity: number
  priceAtPurchase: number
  subtotal: number
}

export interface OrderSummaryDto {
  id: string
  orderNumber: string
  status: OrderStatus
  paymentStatus: PaymentStatus
  totalPrice: number
  totalItems: number
  createdAt: string
  trackingNumber: string | null
  shippingCarrier: string | null
}

export interface ShippingAddressDto {
  firstName: string
  lastName: string
  phoneNumber: string
  email: string
  addressLine1: string
  addressLine2: string | null
  city: string
  state: string | null
  postalCode: string
  country: string
  fullName: string
  formattedAddress: string
}

export interface OrderDetailDto {
  id: string
  orderNumber: string
  userId: string
  items: OrderItemDto[]
  totalPrice: number
  subtotal: number
  shippingCost: number
  discountAmount: number
  status: OrderStatus
  paymentStatus: PaymentStatus
  shippingAddress: ShippingAddressDto
  deliveryMethod: string
  paymentMethod: string
  promoCode: string | null
  customerNotes: string | null
  createdAt: string
  updatedAt: string | null
  shippedAt: string | null
  deliveredAt: string | null
  cancelledAt: string | null
  cancellationReason: string | null
  trackingNumber: string | null
  shippingCarrier: string | null
}

export interface PagedOrdersResult {
  orders: OrderSummaryDto[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

export interface StatusHistoryEntry {
  status: string
  description: string
  timestamp: string
  isCurrentStatus: boolean
}

export interface OrderStatusHistoryResult {
  orderId: string
  orderNumber: string
  currentStatus: string
  paymentStatus: string
  history: StatusHistoryEntry[]
}

export interface ShippingAddressRequest {
  firstName: string
  lastName: string
  phoneNumber: string
  email: string
  addressLine1: string
  addressLine2?: string | null
  city: string
  state?: string | null
  postalCode: string
  country: string
}

export interface CreateOrderRequest {
  shippingAddress: ShippingAddressRequest
  deliveryMethod: string
  paymentMethod: string
  promoCode?: string | null
  customerNotes?: string | null
  idempotencyKey?: string | null
}

export interface CancelOrderRequest {
  reason?: string | null
}

// Orders API
export const ordersApi = {
  getOrders: async (
    status?: OrderStatus | null,
    fromDate?: string | null,
    toDate?: string | null,
    sortBy?: string | null,
    sortDescending: boolean = true,
    pageNumber: number = 1,
    pageSize: number = 20
  ): Promise<ServiceResponse<PagedOrdersResult>> => {
    const params = new URLSearchParams()
    if (status) params.append('status', status)
    if (fromDate) params.append('fromDate', fromDate)
    if (toDate) params.append('toDate', toDate)
    if (sortBy) params.append('sortBy', sortBy)
    params.append('sortDescending', sortDescending.toString())
    params.append('pageNumber', pageNumber.toString())
    params.append('pageSize', pageSize.toString())

    const query = params.toString()
    const response = await axiosClient.get<ServiceResponse<PagedOrdersResult>>(`/orders${query ? `?${query}` : ''}`)
    return response.data
  },

  getOrder: async (orderId: string): Promise<ServiceResponse<OrderDetailDto>> => {
    const response = await axiosClient.get<ServiceResponse<OrderDetailDto>>(`/orders/${orderId}`)
    return response.data
  },

  createOrder: async (data: CreateOrderRequest): Promise<ServiceResponse<OrderDetailDto>> => {
    const response = await axiosClient.post<ServiceResponse<OrderDetailDto>>('/orders', data)
    return response.data
  },

  cancelOrder: async (orderId: string, data?: CancelOrderRequest): Promise<ServiceResponse<boolean>> => {
    const response = await axiosClient.post<ServiceResponse<boolean>>(`/orders/${orderId}/cancel`, data)
    return response.data
  },

  getOrderStatusHistory: async (orderId: string): Promise<ServiceResponse<OrderStatusHistoryResult>> => {
    const response = await axiosClient.get<ServiceResponse<OrderStatusHistoryResult>>(`/orders/${orderId}/status-history`)
    return response.data
  },
}
