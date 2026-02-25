import axiosClient from './axiosClient'
import type { ServiceResponse } from './types'

// Types
export interface RoleDto {
  id: string
  name: string
  description: string
  permissions: string[]
  usersCount: number
}

export interface CreateRoleDto {
  name: string
  description: string
  permissions: string[]
}

export interface UpdateRoleDto {
  name?: string
  description?: string
  permissions?: string[]
}

export interface AdminUserDto {
  id: string
  username: string
  name: string
  surname: string
  email: string
  phoneNumber: string
  roles: string[]
  avatarUrl?: string
  isEmailConfirmed: boolean
  isLocked: boolean
  lockoutEnd?: string
  createdAt: string
}

export interface PermissionDto {
  name: string
  description: string
  category: string
}

export interface AssignUserRolesDto {
  roles: string[]
}

export interface LockUserDto {
  lockUntil?: string
}

// API Functions

// Roles
export const getRoles = async (): Promise<ServiceResponse<RoleDto[]>> => {
  const { data } = await axiosClient.get<ServiceResponse<RoleDto[]>>('/admin/roles')
  return data
}

export const getRoleById = async (id: string): Promise<ServiceResponse<RoleDto>> => {
  const { data } = await axiosClient.get<ServiceResponse<RoleDto>>(`/admin/roles/${id}`)
  return data
}

export const getAllPermissions = async (): Promise<ServiceResponse<Record<string, PermissionDto[]>>> => {
  const { data } = await axiosClient.get<ServiceResponse<Record<string, PermissionDto[]>>>('/admin/permissions')
  return data
}

export const createRole = async (dto: CreateRoleDto): Promise<ServiceResponse<RoleDto>> => {
  const { data } = await axiosClient.post<ServiceResponse<RoleDto>>('/admin/roles', dto)
  return data
}

export const updateRole = async (id: string, dto: UpdateRoleDto): Promise<ServiceResponse<RoleDto>> => {
  const { data } = await axiosClient.put<ServiceResponse<RoleDto>>(`/admin/roles/${id}`, dto)
  return data
}

export const deleteRole = async (id: string): Promise<ServiceResponse<void>> => {
  const { data } = await axiosClient.delete<ServiceResponse<void>>(`/admin/roles/${id}`)
  return data
}

// Users
export const getAdminUsers = async (): Promise<ServiceResponse<AdminUserDto[]>> => {
  const { data } = await axiosClient.get<ServiceResponse<AdminUserDto[]>>('/admin/users')
  return data
}

export const assignUserRoles = async (userId: string, dto: AssignUserRolesDto): Promise<ServiceResponse<AdminUserDto>> => {
  const { data } = await axiosClient.put<ServiceResponse<AdminUserDto>>(`/admin/users/${userId}/roles`, dto)
  return data
}

export const lockUser = async (userId: string, dto?: LockUserDto): Promise<ServiceResponse<void>> => {
  const { data } = await axiosClient.post<ServiceResponse<void>>(`/admin/users/${userId}/lock`, dto || {})
  return data
}

export const unlockUser = async (userId: string): Promise<ServiceResponse<void>> => {
  const { data } = await axiosClient.post<ServiceResponse<void>>(`/admin/users/${userId}/unlock`)
  return data
}
