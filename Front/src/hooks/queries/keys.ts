/**
 * Centralized query key factory used across all TanStack Query hooks.
 * Keeps cache keys consistent for reads, invalidation, and optimistic updates.
 */
export const queryKeys = {
  categories: {
    all: ['categories'] as const,
    lists: () => [...queryKeys.categories.all, 'list'] as const,
    list: (filters: { parentId?: string; topLevel?: boolean }) =>
      [...queryKeys.categories.lists(), filters] as const,
    details: () => [...queryKeys.categories.all, 'detail'] as const,
    detail: (id: string) => [...queryKeys.categories.details(), id] as const,
    slug: (slug: string) => [...queryKeys.categories.all, 'slug', slug] as const,
    filters: (id: string) => [...queryKeys.categories.all, 'filters', id] as const,
  },
  products: {
    all: ['products'] as const,
    lists: () => [...queryKeys.products.all, 'list'] as const,
    my: () => [...queryKeys.products.all, 'my'] as const,
    detail: (id: string) => [...queryKeys.products.all, 'detail', id] as const,
    slug: (slug: string) => [...queryKeys.products.all, 'slug', slug] as const,
    filter: (params: Record<string, unknown>) => [...queryKeys.products.all, 'filter', params] as const,
  },
  cart: {
    all: ['cart'] as const,
  },
  orders: {
    all: ['orders'] as const,
    list: (params: Record<string, unknown>) => [...queryKeys.orders.all, 'list', params] as const,
    detail: (id: string) => [...queryKeys.orders.all, 'detail', id] as const,
    history: (id: string) => [...queryKeys.orders.all, 'history', id] as const,
  },
  favorites: {
    all: ['favorites'] as const,
  },
  search: {
    all: ['search'] as const,
    results: (query: string) => [...queryKeys.search.all, 'results', query] as const,
    popular: () => [...queryKeys.search.all, 'popular'] as const,
  },
  profile: {
    all: ['profile'] as const,
  },
  stores: {
    all: ['stores'] as const,
    slug: (slug: string) => [...queryKeys.stores.all, 'slug', slug] as const,
    my: () => [...queryKeys.stores.all, 'my'] as const,
    admin: () => [...queryKeys.stores.all, 'admin'] as const,
  },
  admin: {
    users: {
      all: ['admin', 'users'] as const,
    },
    roles: {
      all: ['admin', 'roles'] as const,
      detail: (id: string) => ['admin', 'roles', id] as const,
    },
    permissions: {
      all: ['admin', 'permissions'] as const,
    },
  },
  attributes: {
    all: ['attributes'] as const,
  },
  tags: {
    all: ['tags'] as const,
  },
} as const
