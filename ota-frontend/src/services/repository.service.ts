import api from '@/lib/api'
import { Repository, ApiResponse, PaginatedResponse } from '@/types'
import { RegisterRepositoryPayload } from '@/components/forms/RepositoryForm'

export const repositoryService = {
  async getRepositories(
    params?: { projectId?: string; isActive?: boolean; search?: string; page?: number; pageSize?: number }
  ): Promise<PaginatedResponse<Repository>> {
    const searchParams = new URLSearchParams()
    if (params?.projectId) searchParams.set('projectId', params.projectId)
    if (params?.isActive !== undefined) searchParams.set('isActive', String(params.isActive))
    if (params?.search) searchParams.set('filter', params.search)
    if (params?.page) searchParams.set('page', String(params.page))
    if (params?.pageSize) searchParams.set('pageSize', String(params.pageSize))

    const response = await api.get<ApiResponse<Repository[]>>(
      `/repositories?${searchParams.toString()}`
    )
    return {
      items: response.data.data,
      pagination: response.data.pagination!,
    }
  },

  async getRepositoryById(id: string): Promise<Repository> {
    const response = await api.get<ApiResponse<Repository>>(`/repositories/${id}`)
    return response.data.data
  },

  async registerRepository(data: RegisterRepositoryPayload): Promise<Repository> {
    const response = await api.post<ApiResponse<Repository>>('/repositories', data)
    return response.data.data
  },

  async syncRepository(id: string): Promise<{ message: string }> {
    const response = await api.post<ApiResponse<null>>(`/repositories/${id}/sync`)
    return { message: response.data.message ?? 'Repository synchronised successfully.' }
  },

  async deactivateRepository(id: string): Promise<void> {
    await api.post(`/repositories/${id}/deactivate`)
  },

  async deleteRepository(id: string): Promise<void> {
    await api.delete(`/repositories/${id}`)
  },

  async activateRepository(id: string): Promise<void> {
    await api.post(`/repositories/${id}/activate`)
  },

  async configureWebhook(id: string): Promise<{ webhookUrl: string; secret: string }> {
    const response = await api.post<ApiResponse<{ webhookUrl: string; secret: string }>>(
      `/repositories/${id}/webhook`
    )
    return response.data.data
  },
}
