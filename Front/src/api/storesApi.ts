import axiosClient from './axiosClient'
import type { ServiceResponse } from './types'
import type { ProductSummaryDto } from './catalogApi'

// Types
export interface PublicStoreDto {
  id: string
  name: string
  slug: string
  description: string | null
  isVerified: boolean
  createdAt: string
  products: ProductSummaryDto[]
}

// Stores API
export const storesApi = {
  getBySlug: async (slug: string): Promise<ServiceResponse<PublicStoreDto>> => {
    const response = await axiosClient.get<ServiceResponse<PublicStoreDto>>(`/stores/slug/${slug}`)
    return response.data
  }
}
