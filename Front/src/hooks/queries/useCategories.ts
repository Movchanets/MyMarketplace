import { categoriesApi, type CategoryDto, type CategoryAvailableFiltersDto } from '../../api/catalogApi'
import { queryKeys } from './keys'
import { useServiceQuery } from './useServiceQuery'

/** Loads categories list for menus, filters, and admin forms. */
export function useCategories(params: { parentId?: string; topLevel?: boolean } = {}) {
  const { parentId, topLevel } = params

  return useServiceQuery<CategoryDto[]>({
    queryKey: queryKeys.categories.list({ parentId, topLevel }),
    queryFn: () => categoriesApi.getAll(parentId, topLevel),
  })
}

/** Loads category details by public slug. */
export function useCategoryBySlug(slug: string | undefined) {
  return useServiceQuery<CategoryDto>({
    queryKey: queryKeys.categories.slug(slug ?? ''),
    queryFn: () => categoriesApi.getBySlug(slug ?? ''),
    enabled: !!slug,
  })
}

/** Loads available dynamic filters for a category. */
export function useCategoryFilters(categoryId: string | undefined) {
  return useServiceQuery<CategoryAvailableFiltersDto>({
    queryKey: queryKeys.categories.filters(categoryId ?? ''),
    queryFn: () => categoriesApi.getAvailableFilters(categoryId ?? ''),
    enabled: !!categoryId,
  })
}
