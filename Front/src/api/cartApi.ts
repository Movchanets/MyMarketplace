import axiosClient from './axiosClient'
import type { ServiceResponse } from './types'

// Types
export interface CartItemDto {
  id: string
  productId: string
  productName: string
  productImageUrl: string | null
  skuId: string
  skuCode: string
  skuAttributes: string | null
  quantity: number
  unitPrice: number
  subtotal: number
  addedAt: string
}

export interface CartDto {
  id: string
  userId: string
  items: CartItemDto[]
  totalItems: number
  totalPrice: number
}

export interface AddToCartRequest {
  productId: string
  skuId: string
  quantity: number
}

export interface UpdateQuantityRequest {
  quantity: number
}

// Helper to format axios errors for logging
function formatAxiosError(err: any) {
  return {
    message: err?.message,
    code: err?.code,
    status: err?.response?.status,
    url: err?.config?.url,
    data: err?.response?.data
  }
}

// Cart API
export const cartApi = {
  getCart: async (): Promise<ServiceResponse<CartDto>> => {
    console.log('cartApi.getCart request')
    return axiosClient.get<ServiceResponse<CartDto>>('/cart')
      .then((response) => {
        console.log('cartApi.getCart response', { status: response.status, data: response.data })
        return response.data
      })
      .catch((err) => {
        console.error('cartApi.getCart error', formatAxiosError(err))
        throw err
      })
  },

  addToCart: async (data: AddToCartRequest): Promise<ServiceResponse<CartDto>> => {
    console.log('cartApi.addToCart request', data)
    return axiosClient.post<ServiceResponse<CartDto>>('/cart/items', data)
      .then((response) => {
        console.log('cartApi.addToCart response', { status: response.status, data: response.data })
        return response.data
      })
      .catch((err) => {
        console.error('cartApi.addToCart error', formatAxiosError(err))
        throw err
      })
  },

  updateQuantity: async (cartItemId: string, data: UpdateQuantityRequest): Promise<ServiceResponse<CartDto>> => {
    console.log('cartApi.updateQuantity request', { cartItemId, data })
    return axiosClient.put<ServiceResponse<CartDto>>(`/cart/items/${cartItemId}`, data)
      .then((response) => {
        console.log('cartApi.updateQuantity response', { status: response.status, data: response.data })
        return response.data
      })
      .catch((err) => {
        console.error('cartApi.updateQuantity error', formatAxiosError(err))
        throw err
      })
  },

  updateQuantityBySku: async (skuId: string, data: UpdateQuantityRequest): Promise<ServiceResponse<CartDto>> => {
    console.log('cartApi.updateQuantityBySku request', { skuId, data })
    return axiosClient.put<ServiceResponse<CartDto>>(`/cart/items/sku/${skuId}`, data)
      .then((response) => {
        console.log('cartApi.updateQuantityBySku response', { status: response.status, data: response.data })
        return response.data
      })
      .catch((err) => {
        console.error('cartApi.updateQuantityBySku error', formatAxiosError(err))
        throw err
      })
  },

  removeFromCart: async (cartItemId: string): Promise<ServiceResponse<CartDto>> => {
    console.log('cartApi.removeFromCart request', { cartItemId })
    return axiosClient.delete<ServiceResponse<CartDto>>(`/cart/items/${cartItemId}`)
      .then((response) => {
        console.log('cartApi.removeFromCart response', { status: response.status, data: response.data })
        return response.data
      })
      .catch((err) => {
        console.error('cartApi.removeFromCart error', formatAxiosError(err))
        throw err
      })
  },

  removeFromCartBySku: async (skuId: string): Promise<ServiceResponse<CartDto>> => {
    console.log('cartApi.removeFromCartBySku request', { skuId })
    return axiosClient.delete<ServiceResponse<CartDto>>(`/cart/items/sku/${skuId}`)
      .then((response) => {
        console.log('cartApi.removeFromCartBySku response', { status: response.status, data: response.data })
        return response.data
      })
      .catch((err) => {
        console.error('cartApi.removeFromCartBySku error', formatAxiosError(err))
        throw err
      })
  },

  clearCart: async (): Promise<ServiceResponse<boolean>> => {
    console.log('cartApi.clearCart request')
    return axiosClient.delete<ServiceResponse<boolean>>('/cart')
      .then((response) => {
        console.log('cartApi.clearCart response', { status: response.status, data: response.data })
        return response.data
      })
      .catch((err) => {
        console.error('cartApi.clearCart error', formatAxiosError(err))
        throw err
      })
  },

  /**
   * Merge multiple guest cart items into the authenticated user's cart.
   * This endpoint handles the merge server-side to avoid concurrency issues.
   */
  mergeGuestCart: async (items: AddToCartRequest[]): Promise<ServiceResponse<CartDto>> => {
    console.log('cartApi.mergeGuestCart request', { items })
    return axiosClient.post<ServiceResponse<CartDto>>('/cart/merge', { items })
      .then((response) => {
        console.log('cartApi.mergeGuestCart response', { status: response.status, data: response.data })
        return response.data
      })
      .catch((err) => {
        console.error('cartApi.mergeGuestCart error', formatAxiosError(err))
        throw err
      })
  },
}
