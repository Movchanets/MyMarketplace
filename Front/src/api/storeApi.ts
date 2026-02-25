import axiosClient from './axiosClient'
import type { ServiceResponse } from './types'

// Types
export interface StoreAdminDto {
  id: string
  name: string
  slug: string
  description: string | null
  userId: string
  ownerName: string | null
  ownerEmail: string | null
  isVerified: boolean
  isSuspended: boolean
  createdAt: string
  productCount: number
}

export interface MyStoreDto {
  id: string
  name: string
  slug: string
  description: string | null
  isVerified: boolean
  isSuspended: boolean
  createdAt: string
  productCount: number
}

export interface CreateStoreRequest {
  name: string
  description?: string | null
}

export interface UpdateStoreRequest {
  name: string
  description?: string | null
}

// Admin Stores API
export const storesAdminApi = {
  getAll: async (includeUnverified = true): Promise<ServiceResponse<StoreAdminDto[]>> => {
    const response = await axiosClient.get<ServiceResponse<StoreAdminDto[]>>(`/stores?includeUnverified=${includeUnverified}`)
    return response.data
  },

  verify: async (id: string): Promise<ServiceResponse<void>> => {
    const response = await axiosClient.post<ServiceResponse<void>>(`/stores/${id}/verify`)
    return response.data
  },

  suspend: async (id: string): Promise<ServiceResponse<void>> => {
    const response = await axiosClient.post<ServiceResponse<void>>(`/stores/${id}/suspend`)
    return response.data
  },

  unsuspend: async (id: string): Promise<ServiceResponse<void>> => {
    const response = await axiosClient.post<ServiceResponse<void>>(`/stores/${id}/unsuspend`)
    return response.data
  }
}

// User's Store API
export const myStoreApi = {
  get: async (): Promise<ServiceResponse<MyStoreDto | null>> => {
    const response = await axiosClient.get<ServiceResponse<MyStoreDto | null>>('/stores/my')
    return response.data
  },

  create: async (data: CreateStoreRequest): Promise<ServiceResponse<string>> => {
    const response = await axiosClient.post<ServiceResponse<string>>('/stores', data)
    return response.data
  },

  update: async (id: string, data: UpdateStoreRequest): Promise<ServiceResponse<void>> => {
    const response = await axiosClient.put<ServiceResponse<void>>(`/stores/${id}`, data)
    return response.data
  }
}
