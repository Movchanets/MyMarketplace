import { useMutation, type UseMutationOptions, type UseMutationResult } from '@tanstack/react-query'
import { type ServiceResponse, unwrapServiceResponse } from '../../api/types'

type ServiceMutationOptions<TData, TVariables = void, TContext = unknown> = Omit<
  UseMutationOptions<TData, Error, TVariables, TContext>,
  'mutationFn'
> & {
  mutationFn: (variables: TVariables) => Promise<ServiceResponse<TData>>
}

/**
 * React Query mutation wrapper that unwraps backend ServiceResponse payloads
 * and keeps shared error logging in one place.
 */
export function useServiceMutation<TData, TVariables = void, TContext = unknown>(
  options: ServiceMutationOptions<TData, TVariables, TContext>,
): UseMutationResult<TData, Error, TVariables, TContext> {
  const { mutationFn, onError, ...rest } = options

  return useMutation<TData, Error, TVariables, TContext>({
    ...rest,
    mutationFn: async (variables: TVariables) => {
      const response = await mutationFn(variables)
      return unwrapServiceResponse(response)
    },
    onError: (error, variables, onMutateResult, context) => {
      console.error('Service mutation failed:', error)
      onError?.(error, variables, onMutateResult, context)
    },
  })
}
