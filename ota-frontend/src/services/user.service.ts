import api from '@/lib/api'
import {
  User,
  UserRole,
  CreateUserRequest,
  UpdateUserRequest,
  UserFilters,
  ApiResponse,
  PaginatedResponse,
} from '@/types'

export const userService = {
  async getUsers(
    filters?: UserFilters
  ): Promise<PaginatedResponse<User>> {
    const params = new URLSearchParams()
    if (filters?.role) params.set('role', filters.role)
    if (filters?.customerId) params.set('customerId', filters.customerId)
    if (filters?.isActive !== undefined) params.set('isActive', String(filters.isActive))
    if (filters?.search) params.set('search', filters.search)
    if (filters?.page) params.set('page', String(filters.page))
    if (filters?.pageSize) params.set('pageSize', String(filters.pageSize))

    const response = await api.get<ApiResponse<User[]>>(`/users?${params.toString()}`)
    return {
      items: response.data.data,
      pagination: response.data.pagination!,
    }
  },

  async getUserById(id: string): Promise<User> {
    const response = await api.get<ApiResponse<User>>(`/users/${id}`)
    return response.data.data
  },

  async createUser(data: CreateUserRequest): Promise<User> {
    const response = await api.post<ApiResponse<User>>('/users', data)
    return response.data.data
  },

  async updateUser(id: string, data: UpdateUserRequest): Promise<User> {
    const response = await api.put<ApiResponse<User>>(`/users/${id}`, data)
    return response.data.data
  },

  async deactivateUser(id: string): Promise<void> {
    await api.post(`/users/${id}/deactivate`)
  },

  async activateUser(id: string): Promise<void> {
    await api.post(`/users/${id}/activate`)
  },

  async assignRole(id: string, role: UserRole): Promise<User> {
    const response = await api.post<ApiResponse<User>>(`/users/${id}/role`, { role })
    return response.data.data
  },

  async deleteUser(id: string): Promise<void> {
    await api.delete(`/users/${id}`)
  },
}
