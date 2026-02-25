import { useQueryClient } from '@tanstack/react-query'
import {
  categoriesApi,
  tagsApi,
  type CreateCategoryRequest,
  type UpdateCategoryRequest,
  type CreateTagRequest,
  type UpdateTagRequest,
} from '../../api/catalogApi'
import {
  attributeDefinitionsApi,
  type CreateAttributeDefinitionRequest,
  type UpdateAttributeDefinitionRequest,
} from '../../api/attributeDefinitionsApi'
import { queryKeys } from './keys'
import { useServiceMutation } from './useServiceMutation'
import { useServiceQuery } from './useServiceQuery'

/** Loads full category list for admin management screens. */
export function useAdminCategories() {
  return useServiceQuery({
    queryKey: queryKeys.categories.all,
    queryFn: () => categoriesApi.getAll(),
  })
}

export function useCreateCategory() {
  const queryClient = useQueryClient()

  return useServiceMutation<string, CreateCategoryRequest>({
    mutationFn: (payload) => categoriesApi.create(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.categories.all })
    },
  })
}

export function useUpdateCategory() {
  const queryClient = useQueryClient()

  return useServiceMutation<void, { id: string; data: UpdateCategoryRequest }>({
    mutationFn: ({ id, data }) => categoriesApi.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.categories.all })
    },
  })
}

export function useDeleteCategory() {
  const queryClient = useQueryClient()

  return useServiceMutation<void, string>({
    mutationFn: (id) => categoriesApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.categories.all })
    },
  })
}

/** Loads full tag list for admin management screens. */
export function useAdminTags() {
  return useServiceQuery({
    queryKey: queryKeys.tags.all,
    queryFn: () => tagsApi.getAll(),
  })
}

export function useCreateTag() {
  const queryClient = useQueryClient()

  return useServiceMutation<string, CreateTagRequest>({
    mutationFn: (payload) => tagsApi.create(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.tags.all })
    },
  })
}

export function useUpdateTag() {
  const queryClient = useQueryClient()

  return useServiceMutation<void, { id: string; data: UpdateTagRequest }>({
    mutationFn: ({ id, data }) => tagsApi.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.tags.all })
    },
  })
}

export function useDeleteTag() {
  const queryClient = useQueryClient()

  return useServiceMutation<void, string>({
    mutationFn: (id) => tagsApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.tags.all })
    },
  })
}

/** Loads all attribute definitions for admin management screens. */
export function useAdminAttributeDefinitions() {
  return useServiceQuery({
    queryKey: queryKeys.attributes.all,
    queryFn: () => attributeDefinitionsApi.getAll(),
  })
}

export function useCreateAttributeDefinition() {
  const queryClient = useQueryClient()

  return useServiceMutation({
    mutationFn: (payload: CreateAttributeDefinitionRequest) => attributeDefinitionsApi.create(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.attributes.all })
    },
  })
}

export function useUpdateAttributeDefinition() {
  const queryClient = useQueryClient()

  return useServiceMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateAttributeDefinitionRequest }) =>
      attributeDefinitionsApi.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.attributes.all })
    },
  })
}

export function useDeleteAttributeDefinition() {
  const queryClient = useQueryClient()

  return useServiceMutation({
    mutationFn: (id: string) => attributeDefinitionsApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.attributes.all })
    },
  })
}
