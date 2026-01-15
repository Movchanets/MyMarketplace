import axiosClient from './axiousClient'
import type { ServiceResponse } from './types'
import type { ProductSummaryDto } from './catalogApi'

// Types
export interface PopularQueryDto {
  query: string
  searchCount: number
}

export interface SearchResultDto {
  products: ProductSummaryDto[]
  query: string
}

// Search API
export const searchApi = {
  /**
   * Search products by query text
   */
  search: async (query: string, limit: number = 20): Promise<ServiceResponse<ProductSummaryDto[]>> => {
    const params = new URLSearchParams()
    params.append('q', query)
    if (limit !== 20) params.append('limit', limit.toString())
    const response = await axiosClient.get<ServiceResponse<ProductSummaryDto[]>>(`/search?${params.toString()}`)
    return response.data
  },

  /**
   * Get popular search queries
   */
  getPopular: async (limit: number = 10): Promise<ServiceResponse<PopularQueryDto[]>> => {
    const params = limit !== 10 ? `?limit=${limit}` : ''
    const response = await axiosClient.get<ServiceResponse<PopularQueryDto[]>>(`/search/popular${params}`)
    return response.data
  }
}
