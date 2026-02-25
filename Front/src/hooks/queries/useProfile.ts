import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { authApi } from '../../api/authApi'
import { userApi, type User } from '../../api/userApi'
import { queryKeys } from './keys'
import { useAuthStore } from '../../store/authStore'

const refreshAuthFromProfileMutations = async () => {
  try {
    const tokens = await authApi.refreshTokens()
    useAuthStore.getState().setAuth(tokens.accessToken || '', tokens.refreshToken || '')
  } catch {
    // token refresh is optional here
  }
}

/** Loads current user profile for account pages and header UI. */
export function useProfile() {
  return useQuery({
    queryKey: queryKeys.profile.all,
    queryFn: () => userApi.getMyProfile(),
  })
}

/** Updates basic profile fields and syncs auth claims from refreshed token. */
export function useUpdateProfileInfo() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationKey: [...queryKeys.profile.all, 'updateInfo'],
    mutationFn: (payload: { name?: string; surname?: string; username?: string }) => userApi.updateMyInfo(payload),
    onSuccess: async (profile) => {
      queryClient.setQueryData<User>(queryKeys.profile.all, profile)
      await refreshAuthFromProfileMutations()
    },
  })
}

/** Updates phone and syncs auth claims from refreshed token. */
export function useUpdateProfilePhone() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationKey: [...queryKeys.profile.all, 'updatePhone'],
    mutationFn: (phoneNumber: string) => userApi.updateMyPhone({ phoneNumber }),
    onSuccess: async (profile) => {
      queryClient.setQueryData<User>(queryKeys.profile.all, profile)
      await refreshAuthFromProfileMutations()
    },
  })
}

/** Updates email and syncs auth claims from refreshed token. */
export function useUpdateProfileEmail() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationKey: [...queryKeys.profile.all, 'updateEmail'],
    mutationFn: (email: string) => userApi.updateMyEmail({ email }),
    onSuccess: async (profile) => {
      queryClient.setQueryData<User>(queryKeys.profile.all, profile)
      await refreshAuthFromProfileMutations()
    },
  })
}

/** Uploads profile avatar and updates cached profile data. */
export function useUploadAvatar() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationKey: [...queryKeys.profile.all, 'uploadAvatar'],
    mutationFn: (file: File) => userApi.uploadProfilePicture(file),
    onSuccess: async (profile) => {
      queryClient.setQueryData<User>(queryKeys.profile.all, profile)
      await refreshAuthFromProfileMutations()
    },
  })
}

/** Deletes profile avatar and updates cached profile data. */
export function useDeleteAvatar() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationKey: [...queryKeys.profile.all, 'deleteAvatar'],
    mutationFn: () => userApi.deleteProfilePicture(),
    onSuccess: async (profile) => {
      queryClient.setQueryData<User>(queryKeys.profile.all, profile)
      await refreshAuthFromProfileMutations()
    },
  })
}
