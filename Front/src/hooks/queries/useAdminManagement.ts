import { useQueryClient } from '@tanstack/react-query'
import {
  assignUserRoles,
  createRole,
  deleteRole,
  getAdminUsers,
  getAllPermissions,
  getRoles,
  lockUser,
  unlockUser,
  updateRole,
  type AdminUserDto,
  type AssignUserRolesDto,
  type CreateRoleDto,
  type UpdateRoleDto,
} from '../../api/adminApi'
import { storesAdminApi } from '../../api/storeApi'
import { queryKeys } from './keys'
import { useServiceMutation } from './useServiceMutation'
import { useServiceQuery } from './useServiceQuery'

/** Loads users for admin access-control management. */
export function useAdminUsers() {
  return useServiceQuery({
    queryKey: queryKeys.admin.users.all,
    queryFn: () => getAdminUsers(),
  })
}

export function useAssignUserRoles() {
  const queryClient = useQueryClient()

  return useServiceMutation<AdminUserDto, { userId: string; data: AssignUserRolesDto }>({
    mutationFn: ({ userId, data }) => assignUserRoles(userId, data),
    onSuccess: (updatedUser) => {
      queryClient.setQueryData<AdminUserDto[]>(queryKeys.admin.users.all, (current = []) =>
        current.map((user) => (user.id === updatedUser.id ? updatedUser : user)),
      )
    },
  })
}

export function useLockUser() {
  const queryClient = useQueryClient()

  return useServiceMutation<void, string>({
    mutationFn: (userId) => lockUser(userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.admin.users.all })
    },
  })
}

export function useUnlockUser() {
  const queryClient = useQueryClient()

  return useServiceMutation<void, string>({
    mutationFn: (userId) => unlockUser(userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.admin.users.all })
    },
  })
}

/** Loads roles for role management pages. */
export function useAdminRoles() {
  return useServiceQuery({
    queryKey: queryKeys.admin.roles.all,
    queryFn: () => getRoles(),
  })
}

/** Loads all assignable permissions for role editing. */
export function useAdminPermissions() {
  return useServiceQuery({
    queryKey: queryKeys.admin.permissions.all,
    queryFn: () => getAllPermissions(),
  })
}

export function useCreateRole() {
  const queryClient = useQueryClient()

  return useServiceMutation({
    mutationFn: (payload: CreateRoleDto) => createRole(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.admin.roles.all })
    },
  })
}

export function useUpdateRole() {
  const queryClient = useQueryClient()

  return useServiceMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateRoleDto }) => updateRole(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.admin.roles.all })
      queryClient.invalidateQueries({ queryKey: queryKeys.admin.users.all })
    },
  })
}

export function useDeleteRole() {
  const queryClient = useQueryClient()

  return useServiceMutation({
    mutationFn: (id: string) => deleteRole(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.admin.roles.all })
      queryClient.invalidateQueries({ queryKey: queryKeys.admin.users.all })
    },
  })
}

/** Loads stores for verification/suspension workflows in admin panel. */
export function useAdminStores(includeUnverified = true) {
  return useServiceQuery({
    queryKey: [...queryKeys.stores.admin(), includeUnverified],
    queryFn: () => storesAdminApi.getAll(includeUnverified),
  })
}

export function useVerifyStore() {
  const queryClient = useQueryClient()

  return useServiceMutation<void, string>({
    mutationFn: (id) => storesAdminApi.verify(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.stores.admin() })
    },
  })
}

export function useSuspendStore() {
  const queryClient = useQueryClient()

  return useServiceMutation<void, string>({
    mutationFn: (id) => storesAdminApi.suspend(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.stores.admin() })
    },
  })
}

export function useUnsuspendStore() {
  const queryClient = useQueryClient()

  return useServiceMutation<void, string>({
    mutationFn: (id) => storesAdminApi.unsuspend(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.stores.admin() })
    },
  })
}
