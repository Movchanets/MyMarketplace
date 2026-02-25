import { type QueryKey, useQuery, type UseQueryOptions, type UseQueryResult } from '@tanstack/react-query'
import { type ServiceResponse, unwrapServiceResponse } from '../../api/types'

type ServiceQueryOptions<
  TData,
  TError = Error,
  TSelected = TData,
  TQueryKey extends QueryKey = QueryKey,
> = Omit<UseQueryOptions<TData, TError, TSelected, TQueryKey>, 'queryFn'> & {
  queryFn: () => Promise<ServiceResponse<TData>>
}

/**
 * React Query wrapper that unwraps backend ServiceResponse payloads
 * and throws on unsuccessful responses.
 */
export function useServiceQuery<
  TData,
  TError = Error,
  TSelected = TData,
  TQueryKey extends QueryKey = QueryKey,
>(options: ServiceQueryOptions<TData, TError, TSelected, TQueryKey>): UseQueryResult<TSelected, TError> {
  const { queryFn, ...rest } = options

  return useQuery<TData, TError, TSelected, TQueryKey>({
    ...rest,
    queryFn: async () => {
      const response = await queryFn()
      return unwrapServiceResponse(response)
    },
  })
}
