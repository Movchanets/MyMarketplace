import axiosClient from './axiosClient'
import type { ServiceResponse } from './types'

export type AttributeDataType = 'string' | 'number' | 'boolean' | 'array'

export interface AttributeDefinitionDto {
  id: string
  code: string
  name: string
  dataType: AttributeDataType
  isRequired: boolean
  isVariant: boolean
  allowedValues: string[] | null
  displayOrder: number
  unit: string | null
  description: string | null
  isActive: boolean
  createdAt: string
  updatedAt: string | null
}

export interface CreateAttributeDefinitionRequest {
  code: string
  name: string
  dataType: AttributeDataType
  isRequired?: boolean
  isVariant?: boolean
  allowedValues?: string[]
  displayOrder?: number
  unit?: string
  description?: string
}

export interface UpdateAttributeDefinitionRequest {
  name: string
  dataType: AttributeDataType
  isRequired?: boolean
  isVariant?: boolean
  allowedValues?: string[]
  displayOrder?: number
  unit?: string
  description?: string
  isActive?: boolean
}

export const attributeDefinitionsApi = {
  getAll: async (): Promise<ServiceResponse<AttributeDefinitionDto[]>> => {
    const response = await axiosClient.get<ServiceResponse<AttributeDefinitionDto[]>>(
      '/attributedefinitions'
    )
    return response.data
  },

  create: async (
    request: CreateAttributeDefinitionRequest
  ): Promise<ServiceResponse<AttributeDefinitionDto>> => {
    const response = await axiosClient.post<ServiceResponse<AttributeDefinitionDto>>(
      '/attributedefinitions',
      request
    )
    return response.data
  },

  update: async (
    id: string,
    request: UpdateAttributeDefinitionRequest
  ): Promise<ServiceResponse<AttributeDefinitionDto>> => {
    const response = await axiosClient.put<ServiceResponse<AttributeDefinitionDto>>(
      `/attributedefinitions/${id}`,
      request
    )
    return response.data
  },

  delete: async (id: string): Promise<ServiceResponse<null>> => {
    const response = await axiosClient.delete<ServiceResponse<null>>(
      `/attributedefinitions/${id}`
    )
    return response.data
  },
}
