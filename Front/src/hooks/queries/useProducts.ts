import { keepPreviousData, useQueryClient } from '@tanstack/react-query'
import {
  productsApi,
  type PagedResponse,
  type ProductFilterRequest,
  type ProductDetailsDto,
  type ProductSummaryDto,
  type CreateProductRequest,
  type UpdateProductRequest,
  type AddSkuRequest,
  type UpdateSkuRequest,
  type GalleryUploadResponse,
} from '../../api/catalogApi'
import { queryKeys } from './keys'
import { useServiceMutation } from './useServiceMutation'
import { useServiceQuery } from './useServiceQuery'

/** Loads product details by public slug (and optional SKU preselection). */
export function useProductBySlug(productSlug: string | undefined, skuCode?: string) {
  return useServiceQuery<ProductDetailsDto>({
    queryKey: [...queryKeys.products.slug(productSlug ?? ''), skuCode ?? null],
    queryFn: () => productsApi.getBySlug(productSlug ?? '', skuCode),
    enabled: !!productSlug,
  })
}

/** Loads current seller's products for store management pages. */
export function useMyProducts(enabled = true) {
  return useServiceQuery<ProductSummaryDto[]>({
    queryKey: queryKeys.products.my(),
    queryFn: () => productsApi.getMy(),
    enabled,
  })
}

/** Loads product details by id for seller edit flows. */
export function useProductById(productId: string | undefined) {
  return useServiceQuery<ProductDetailsDto>({
    queryKey: queryKeys.products.detail(productId ?? ''),
    queryFn: () => productsApi.getById(productId ?? ''),
    enabled: !!productId,
  })
}

/** Returns paged catalog results with stable previous-page placeholder data. */
export function useFilterProducts(request: ProductFilterRequest, enabled = true) {
  return useServiceQuery<PagedResponse<ProductSummaryDto>>({
    queryKey: queryKeys.products.filter(request as Record<string, unknown>),
    queryFn: () => productsApi.filter(request),
    enabled,
    placeholderData: keepPreviousData,
  })
}

/** Creates a new seller product and invalidates product caches. */
export function useCreateProduct() {
  const queryClient = useQueryClient()

  return useServiceMutation<string, CreateProductRequest>({
    mutationFn: (payload) => productsApi.create(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.products.all })
    },
  })
}

export function useUpdateProduct() {
  const queryClient = useQueryClient()

  return useServiceMutation<void, { productId: string; data: UpdateProductRequest; newImages?: File[] }>({
    mutationFn: ({ productId, data, newImages }) => productsApi.update(productId, data, newImages),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.products.detail(variables.productId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.products.my() })
      queryClient.invalidateQueries({ queryKey: queryKeys.products.all })
    },
  })
}

export function useDeleteProduct() {
  const queryClient = useQueryClient()

  return useServiceMutation<void, string>({
    mutationFn: (productId) => productsApi.delete(productId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.products.my() })
      queryClient.invalidateQueries({ queryKey: queryKeys.products.all })
    },
  })
}

export function useToggleProductActive() {
  const queryClient = useQueryClient()

  return useServiceMutation<void, { productId: string; isActive: boolean }>({
    mutationFn: ({ productId, isActive }) => productsApi.toggleActive(productId, isActive),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.products.detail(variables.productId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.products.my() })
      queryClient.invalidateQueries({ queryKey: queryKeys.products.all })
    },
  })
}

export function useSetProductBaseImage() {
  const queryClient = useQueryClient()

  return useServiceMutation<void, { productId: string; baseImageUrl: string | null }>({
    mutationFn: ({ productId, baseImageUrl }) => productsApi.setBaseImage(productId, baseImageUrl),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.products.detail(variables.productId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.products.my() })
    },
  })
}

export function useUploadProductGalleryImage() {
  const queryClient = useQueryClient()

  return useServiceMutation<GalleryUploadResponse, { productId: string; file: File; displayOrder?: number }>({
    mutationFn: ({ productId, file, displayOrder }) => productsApi.uploadGalleryImage(productId, file, displayOrder),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.products.detail(variables.productId) })
    },
  })
}

export function useAddSku() {
  const queryClient = useQueryClient()

  return useServiceMutation<string, { productId: string; data: AddSkuRequest }>({
    mutationFn: ({ productId, data }) => productsApi.addSku(productId, data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.products.detail(variables.productId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.products.my() })
    },
  })
}

export function useUpdateSku() {
  const queryClient = useQueryClient()

  return useServiceMutation<void, { productId: string; skuId: string; data: UpdateSkuRequest }>({
    mutationFn: ({ productId, skuId, data }) => productsApi.updateSku(productId, skuId, data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.products.detail(variables.productId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.products.my() })
    },
  })
}

export function useDeleteSku() {
  const queryClient = useQueryClient()

  return useServiceMutation<void, { productId: string; skuId: string }>({
    mutationFn: ({ productId, skuId }) => productsApi.deleteSku(productId, skuId),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.products.detail(variables.productId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.products.my() })
    },
  })
}

export function useUploadSkuGalleryImage() {
  const queryClient = useQueryClient()

  return useServiceMutation<GalleryUploadResponse, { productId: string; skuId: string; file: File; displayOrder?: number }>({
    mutationFn: ({ productId, skuId, file, displayOrder }) =>
      productsApi.uploadSkuGalleryImage(productId, skuId, file, displayOrder),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.products.detail(variables.productId) })
    },
  })
}

export function useDeleteSkuGalleryImage() {
  const queryClient = useQueryClient()

  return useServiceMutation<void, { productId: string; skuId: string; galleryId: string }>({
    mutationFn: ({ productId, skuId, galleryId }) => productsApi.deleteSkuGalleryImage(productId, skuId, galleryId),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.products.detail(variables.productId) })
    },
  })
}
