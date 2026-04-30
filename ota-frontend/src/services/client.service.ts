import api from '@/lib/api'
import { Client, ClientFilters, CreateClientRequest, UpdateClientRequest, ApiResponse, PaginatedResponse } from '@/types'

export const clientService = {
  async getNextCode(): Promise<string> {
    const response = await api.get<ApiResponse<string>>('/clients/next-code')
    return response.data.data
  },

  async createClient(data: CreateClientRequest): Promise<Client> {
    const response = await api.post<ApiResponse<Client>>('/clients', data)
    return response.data.data
  },

  async getClients(filters?: ClientFilters): Promise<PaginatedResponse<Client>> {
    const params = new URLSearchParams()
    if (filters?.search)   params.set('search',   filters.search)
    if (filters?.page)     params.set('page',     String(filters.page))
    if (filters?.pageSize) params.set('pageSize', String(filters.pageSize))

    const response = await api.get<ApiResponse<Client[]>>(`/clients?${params.toString()}`)
    return {
      items:      response.data.data,
      pagination: response.data.pagination!,
    }
  },

  async getClientById(id: string): Promise<Client> {
    const response = await api.get<ApiResponse<Client>>(`/clients/${id}`)
    return response.data.data
  },

  async updateClient(id: string, data: UpdateClientRequest): Promise<Client> {
    const response = await api.put<ApiResponse<Client>>(`/clients/${id}`, data)
    return response.data.data
  },

  async deleteClient(id: string): Promise<void> {
    await api.delete(`/clients/${id}`)
  },
}
