import axiosClient from './axiousClient'
import type { ServiceResponse } from './types'

// Types
export interface CategoryDto {
  id: string
  name: string
  slug: string
  description: string | null
  parentCategoryId: string | null
  emoji: string | null
  isPrimary?: boolean
}

export interface TagDto {
  id: string
  name: string
  slug: string
  description: string | null
}

export interface CreateCategoryRequest {
  name: string
  description?: string | null
  parentCategoryId?: string | null
  emoji?: string | null
}

export interface UpdateCategoryRequest {
  name: string
  description?: string | null
  parentCategoryId?: string | null
  emoji?: string | null
}

export interface CreateTagRequest {
  name: string
  description?: string | null
}

export interface UpdateTagRequest {
  name: string
  description?: string | null
}

// Categories API
export const categoriesApi = {
  getAll: async (parentCategoryId?: string, topLevelOnly?: boolean): Promise<ServiceResponse<CategoryDto[]>> => {
    const params = new URLSearchParams()
    if (parentCategoryId) params.append('parentCategoryId', parentCategoryId)
    if (topLevelOnly) params.append('topLevelOnly', 'true')
    const query = params.toString()
    const response = await axiosClient.get<ServiceResponse<CategoryDto[]>>(`/categories${query ? `?${query}` : ''}`)
    return response.data
  },

  getById: async (id: string): Promise<ServiceResponse<CategoryDto>> => {
    const response = await axiosClient.get<ServiceResponse<CategoryDto>>(`/categories/${id}`)
    return response.data
  },

  getBySlug: async (slug: string): Promise<ServiceResponse<CategoryDto>> => {
    const response = await axiosClient.get<ServiceResponse<CategoryDto>>(`/categories/slug/${slug}`)
    return response.data
  },

  create: async (data: CreateCategoryRequest): Promise<ServiceResponse<string>> => {
    const response = await axiosClient.post<ServiceResponse<string>>('/categories', data)
    return response.data
  },

  update: async (id: string, data: UpdateCategoryRequest): Promise<ServiceResponse<void>> => {
    const response = await axiosClient.put<ServiceResponse<void>>(`/categories/${id}`, data)
    return response.data
  },

  delete: async (id: string): Promise<ServiceResponse<void>> => {
    const response = await axiosClient.delete<ServiceResponse<void>>(`/categories/${id}`)
    return response.data
  },

  getAvailableFilters: async (id: string): Promise<ServiceResponse<CategoryAvailableFiltersDto>> => {
    const response = await axiosClient.get<ServiceResponse<CategoryAvailableFiltersDto>>(`/categories/${id}/available-filters`)
    return response.data
  }
}

// Tags API
export const tagsApi = {
  getAll: async (): Promise<ServiceResponse<TagDto[]>> => {
    const response = await axiosClient.get<ServiceResponse<TagDto[]>>('/tags')
    return response.data
  },

  getById: async (id: string): Promise<ServiceResponse<TagDto>> => {
    const response = await axiosClient.get<ServiceResponse<TagDto>>(`/tags/${id}`)
    return response.data
  },

  getBySlug: async (slug: string): Promise<ServiceResponse<TagDto>> => {
    const response = await axiosClient.get<ServiceResponse<TagDto>>(`/tags/slug/${slug}`)
    return response.data
  },

  create: async (data: CreateTagRequest): Promise<ServiceResponse<string>> => {
    const response = await axiosClient.post<ServiceResponse<string>>('/tags', data)
    return response.data
  },

  update: async (id: string, data: UpdateTagRequest): Promise<ServiceResponse<void>> => {
    const response = await axiosClient.put<ServiceResponse<void>>(`/tags/${id}`, data)
    return response.data
  },

  delete: async (id: string): Promise<ServiceResponse<void>> => {
    const response = await axiosClient.delete<ServiceResponse<void>>(`/tags/${id}`)
    return response.data
  }
}

// Product types
export interface SkuDto {
  id: string
  skuCode: string
  price: number
  stockQuantity: number
  attributes: Record<string, unknown> | null
  mergedAttributes: Record<string, unknown> | null
  gallery: MediaImageDto[] | null
}

export interface MediaImageDto {
  id: string
  storageKey: string
  url: string
  mimeType: string
  width: number
  height: number
  altText: string | null
  galleryId?: string | null
}

export interface ProductSummaryDto {
  id: string
  storeId: string | null
  name: string
  slug: string
  baseImageUrl: string | null
  minPrice: number | null
  inStock: boolean
  isActive: boolean
  primaryCategory: CategoryDto | null
  categories: CategoryDto[]
  tags: TagDto[]
}

// Filter types
export interface AttributeValueOptionDto {
  value: string
  count: number
}

export interface NumberRangeDto {
  min: number
  max: number
  step?: number | null
}

export interface AttributeFilterDto {
  code: string
  name: string
  dataType: string
  unit: string | null
  displayOrder: number
  availableValues: AttributeValueOptionDto[] | null
  numberRange: NumberRangeDto | null
}

export interface PriceRangeDto {
  min: number
  max: number
}

export interface CategoryAvailableFiltersDto {
  categoryId: string
  categoryName: string
  attributes: AttributeFilterDto[]
  priceRange: PriceRangeDto | null
  totalProductCount: number
}

export interface AttributeFilterValue {
  in?: string[]
  equal?: string
  gte?: number
  lte?: number
  eq?: number
}

export const ProductSort = {
  Relevance: 'Relevance',
  Newest: 'Newest',
  PriceAsc: 'PriceAsc',
  PriceDesc: 'PriceDesc'
} as const

export type ProductSort = typeof ProductSort[keyof typeof ProductSort]

export interface ProductFilterRequest {
  categoryId?: string | null
  tagIds?: string[] | null
  minPrice?: number | null
  maxPrice?: number | null
  inStock?: boolean | null
  attributes?: Record<string, AttributeFilterValue> | null
  sort?: ProductSort
  page?: number
  pageSize?: number
}

export interface PagedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  total: number
}

export interface ProductDetailsDto {
  id: string
  storeId: string | null
  name: string
  slug: string
  description: string | null
  baseImageUrl: string | null
  attributes: Record<string, unknown> | null
  skus: SkuDto[]
  gallery: MediaImageDto[]
  primaryCategory: CategoryDto | null
  categories: CategoryDto[]
  tags: TagDto[]
}

export interface SkuRequest {
  price: number
  stockQuantity: number
  attributes?: Record<string, unknown>
}

export interface CreateProductRequest {
  name: string
  description?: string | null
  categoryIds?: string[]
  tagIds?: string[]
  skus?: SkuRequest[]
}

export interface AddSkuRequest {
  price: number
  stockQuantity: number
  attributes?: Record<string, unknown>
}

export interface UpdateSkuRequest {
  price: number
  stockQuantity: number
  attributes?: Record<string, unknown>
}

export interface GalleryUploadResponse {
  galleryId: string
  mediaImageId: string
  url: string
  storageKey: string
}

// Products API
export interface UpdateProductRequest {
  name: string
  description?: string | null
  categoryIds?: string[]
  tagIds?: string[]
  primaryCategoryId?: string
  galleryIdsToDelete?: string[]
}

export const productsApi = {
  getAll: async (): Promise<ServiceResponse<ProductSummaryDto[]>> => {
    const response = await axiosClient.get<ServiceResponse<ProductSummaryDto[]>>('/products')
    return response.data
  },

  getMy: async (): Promise<ServiceResponse<ProductSummaryDto[]>> => {
    const response = await axiosClient.get<ServiceResponse<ProductSummaryDto[]>>('/products/my')
    return response.data
  },

  getById: async (id: string): Promise<ServiceResponse<ProductDetailsDto>> => {
    const response = await axiosClient.get<ServiceResponse<ProductDetailsDto>>(`/products/${id}`)
    return response.data
  },

  getBySlug: async (productSlug: string, skuCode?: string): Promise<ServiceResponse<ProductDetailsDto>> => {
    const params = skuCode ? `?sku=${encodeURIComponent(skuCode)}` : ''
    const response = await axiosClient.get<ServiceResponse<ProductDetailsDto>>(`/products/s/${productSlug}${params}`)
    return response.data
  },

  getBySkuCode: async (skuCode: string): Promise<ServiceResponse<ProductDetailsDto>> => {
    const response = await axiosClient.get<ServiceResponse<ProductDetailsDto>>(`/products/by-sku/${skuCode}`)
    return response.data
  },

  getByCategory: async (categoryId: string): Promise<ServiceResponse<ProductSummaryDto[]>> => {
    const response = await axiosClient.get<ServiceResponse<ProductSummaryDto[]>>(`/products/by-category/${categoryId}`)
    return response.data
  },

  filter: async (request: ProductFilterRequest): Promise<ServiceResponse<PagedResponse<ProductSummaryDto>>> => {
    const response = await axiosClient.post<ServiceResponse<PagedResponse<ProductSummaryDto>>>('/products/filter', request)
    return response.data
  },

  create: async (data: CreateProductRequest): Promise<ServiceResponse<string>> => {
    const response = await axiosClient.post<ServiceResponse<string>>('/products', data)
    return response.data
  },

  update: async (productId: string, data: UpdateProductRequest, newImages?: File[]): Promise<ServiceResponse<void>> => {
    const formData = new FormData()
    formData.append('name', data.name)
    if (data.description) formData.append('description', data.description)
    if (data.categoryIds) {
      data.categoryIds.forEach(id => formData.append('categoryIds', id))
    }
    if (data.tagIds) {
      data.tagIds.forEach(id => formData.append('tagIds', id))
    }
    if (data.primaryCategoryId) {
      formData.append('primaryCategoryId', data.primaryCategoryId)
    }
    if (data.galleryIdsToDelete) {
      data.galleryIdsToDelete.forEach(id => formData.append('galleryIdsToDelete', id))
    }
    if (newImages) {
      newImages.forEach(file => formData.append('newGalleryImages', file))
    }
    
    const response = await axiosClient.put<ServiceResponse<void>>(
      `/products/${productId}/with-gallery`,
      formData,
      { headers: { 'Content-Type': 'multipart/form-data' } }
    )
    return response.data
  },

  addSku: async (productId: string, data: AddSkuRequest): Promise<ServiceResponse<string>> => {
    const response = await axiosClient.post<ServiceResponse<string>>(`/products/${productId}/skus`, data)
    return response.data
  },

  updateSku: async (productId: string, skuId: string, data: UpdateSkuRequest): Promise<ServiceResponse<void>> => {
    const response = await axiosClient.put<ServiceResponse<void>>(`/products/${productId}/skus/${skuId}`, data)
    return response.data
  },

  deleteSku: async (productId: string, skuId: string): Promise<ServiceResponse<void>> => {
    const response = await axiosClient.delete<ServiceResponse<void>>(`/products/${productId}/skus/${skuId}`)
    return response.data
  },

  uploadGalleryImage: async (productId: string, file: File, displayOrder: number = 0): Promise<ServiceResponse<GalleryUploadResponse>> => {
    const formData = new FormData()
    formData.append('file', file)
    formData.append('displayOrder', displayOrder.toString())
    const response = await axiosClient.post<ServiceResponse<GalleryUploadResponse>>(
      `/products/${productId}/gallery`,
      formData,
      { headers: { 'Content-Type': 'multipart/form-data' } }
    )
    return response.data
  },

  deleteGalleryImage: async (productId: string, galleryId: string): Promise<ServiceResponse<void>> => {
    const response = await axiosClient.delete<ServiceResponse<void>>(`/products/${productId}/gallery/${galleryId}`)
    return response.data
  },

  setBaseImage: async (productId: string, baseImageUrl: string | null): Promise<ServiceResponse<void>> => {
    const response = await axiosClient.patch<ServiceResponse<void>>(`/products/${productId}/base-image`, { baseImageUrl })
    return response.data
  },

  // SKU Gallery
  uploadSkuGalleryImage: async (productId: string, skuId: string, file: File, displayOrder: number = 0): Promise<ServiceResponse<GalleryUploadResponse>> => {
    const formData = new FormData()
    formData.append('file', file)
    formData.append('displayOrder', displayOrder.toString())
    const response = await axiosClient.post<ServiceResponse<GalleryUploadResponse>>(
      `/products/${productId}/skus/${skuId}/gallery`,
      formData,
      { headers: { 'Content-Type': 'multipart/form-data' } }
    )
    return response.data
  },

  deleteSkuGalleryImage: async (productId: string, skuId: string, galleryId: string): Promise<ServiceResponse<void>> => {
    const response = await axiosClient.delete<ServiceResponse<void>>(`/products/${productId}/skus/${skuId}/gallery/${galleryId}`)
    return response.data
  },

  toggleActive: async (productId: string, isActive: boolean): Promise<ServiceResponse<void>> => {
    const response = await axiosClient.patch<ServiceResponse<void>>(`/products/${productId}/active`, { isActive })
    return response.data
  },

   delete: async (productId: string): Promise<ServiceResponse<void>> => {
     const response = await axiosClient.delete<ServiceResponse<void>>(`/products/${productId}`)
     return response.data
   }
 }

// Favorites API
export interface FavoriteProductDto {
  id: string
  name: string
  slug: string
  baseImageUrl: string | null
  minPrice: number | null
  inStock: boolean
  favoritedAt: string
}

export interface MergeGuestFavoritesRequest {
  productIds: string[];
}

export const favoritesApi = {
  getFavorites: async (): Promise<ServiceResponse<FavoriteProductDto[]>> => {
    const response = await axiosClient.get<ServiceResponse<FavoriteProductDto[]>>('/favorites')
    return response.data
  },

  addToFavorites: async (productId: string): Promise<ServiceResponse<boolean>> => {
    const response = await axiosClient.post<ServiceResponse<boolean>>('/favorites', { productId })
    return response.data
  },

  removeFromFavorites: async (productId: string): Promise<ServiceResponse<boolean>> => {
    const response = await axiosClient.delete<ServiceResponse<boolean>>(`/favorites/${productId}`)
    return response.data
  },

  mergeGuestFavorites: async (productIds: string[]): Promise<ServiceResponse<number>> => {
    const response = await axiosClient.post<ServiceResponse<number>>('/favorites/merge-guest', { productIds })
    return response.data
  }
}
