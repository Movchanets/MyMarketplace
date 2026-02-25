import { myStoreApi, type MyStoreDto } from '../../api/storeApi'
import { storesApi, type PublicStoreDto } from '../../api/storesApi'
import { queryKeys } from './keys'
import { useServiceQuery } from './useServiceQuery'

/** Loads public store details by slug for storefront pages. */
export function useStoreBySlug(slug: string | undefined) {
  return useServiceQuery<PublicStoreDto>({
    queryKey: queryKeys.stores.slug(slug ?? ''),
    queryFn: () => storesApi.getBySlug(slug ?? ''),
    enabled: !!slug,
  })
}

/** Loads authenticated seller's own store profile. */
export function useMyStore() {
  return useServiceQuery<MyStoreDto | null>({
    queryKey: queryKeys.stores.my(),
    queryFn: () => myStoreApi.get(),
  })
}
