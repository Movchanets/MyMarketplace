import type { ProductSummaryDto } from '../../api/catalogApi'
import { searchApi, type PopularQueryDto } from '../../api/searchApi'
import { queryKeys } from './keys'
import { useServiceMutation } from './useServiceMutation'
import { useServiceQuery } from './useServiceQuery'

/** Loads popular search suggestions used by search UI components. */
export function usePopularQueries(limit = 10) {
  return useServiceQuery<PopularQueryDto[]>({
    queryKey: [...queryKeys.search.popular(), limit],
    queryFn: () => searchApi.getPopular(limit),
  })
}

/** Executes ad-hoc full-text product search by query. */
export function useSearchResults() {
  return useServiceMutation<ProductSummaryDto[], { query: string; limit?: number }>({
    mutationFn: ({ query, limit }) => searchApi.search(query, limit),
  })
}
