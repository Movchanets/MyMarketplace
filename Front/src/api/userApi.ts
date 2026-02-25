import axiosClient from './axiosClient'
import type { ServiceResponse } from './types'
import { unwrapServiceResponse } from './types'

export interface User {
  id: string
  email: string
  name?: string
  surname?: string
  phoneNumber?: string
  roles: string[]
  avatarUrl?: string
  [key: string]: unknown
}

export const userApi = {
  // Get all users (admin)
  getUsers: async (): Promise<User[]> => {
    const response = await axiosClient.get<User[]>('/users')
    return response.data
  },

  // Get user by id
  getUserById: async (id: string): Promise<User> => {
    const response = await axiosClient.get<User>(`/users/${encodeURIComponent(id)}`)
    return response.data
  },

  // Get user by email
  getUserByEmail: async (email: string): Promise<User> => {
    const response = await axiosClient.get<User>(`/users/by-email/${encodeURIComponent(email)}`)
    return response.data
  },

  // Update user (admin or self)
  updateUser: async (id: string, payload: Partial<User>): Promise<User> => {
    const response = await axiosClient.put<User>(`/users/${encodeURIComponent(id)}`, payload)
    return response.data
  },

  // Delete user (admin)
  deleteUser: async (id: string): Promise<{ message?: string }> => {
    const response = await axiosClient.delete(`/users/${encodeURIComponent(id)}`)
    return response.data
  },

  // ----- Profile (current user) -----
  // Get current user's profile
  getMyProfile: async (): Promise<User> => {
    const response = await axiosClient.get<ServiceResponse<User>>('/users/me')
    return unwrapServiceResponse(response.data)
  },

  // Update current user's phone
  updateMyPhone: async (payload: { phoneNumber: string }): Promise<User> => {
    const response = await axiosClient.put<ServiceResponse<User>>('/users/me/phone', payload)
    return unwrapServiceResponse(response.data)
  },

  // Update current user's email
  updateMyEmail: async (payload: { email: string }): Promise<User> => {
    const response = await axiosClient.put<ServiceResponse<User>>('/users/me/email', payload)
    return unwrapServiceResponse(response.data)
  },

  // Update current user's profile info (name, surname, username)
  updateMyInfo: async (payload: { name?: string; surname?: string; username?: string }): Promise<User> => {
    const response = await axiosClient.put<ServiceResponse<User>>('/users/me/info', payload)
    return unwrapServiceResponse(response.data)
  },

  // Change current user's password
  changePassword: async (data: { currentPassword: string; newPassword: string }): Promise<boolean> => {
    const response = await axiosClient.put<ServiceResponse>('/users/me/password', data)
    // change password returns a ServiceResponse with isSuccess/message
    const res = response.data
    if (!res || !res.isSuccess) throw new Error(res?.message || 'Failed to change password')
    return true
  },

  // Upload profile picture
  uploadProfilePicture: async (file: File): Promise<User> => {
    const formData = new FormData()
    formData.append('file', file)
    const response = await axiosClient.post<ServiceResponse<User>>('/users/me/picture', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    })
    return unwrapServiceResponse(response.data)
  },

  // Delete profile picture
  deleteProfilePicture: async (): Promise<User> => {
    const response = await axiosClient.delete<ServiceResponse<User>>('/users/me/picture')
    return unwrapServiceResponse(response.data)
  },
}
