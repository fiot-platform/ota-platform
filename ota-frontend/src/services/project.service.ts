import api from '@/lib/api'
import {
  Project,
  CreateProjectRequest,
  UpdateProjectRequest,
  ApiResponse,
  PaginatedResponse,
} from '@/types'

export const projectService = {
  async getProjects(
    params?: { search?: string; customerId?: string; isActive?: boolean; page?: number; pageSize?: number }
  ): Promise<PaginatedResponse<Project>> {
    const searchParams = new URLSearchParams()
    if (params?.search) searchParams.set('filter', params.search)
    if (params?.customerId) searchParams.set('customerId', params.customerId)
    if (params?.isActive !== undefined) searchParams.set('isActive', String(params.isActive))
    if (params?.page) searchParams.set('page', String(params.page))
    if (params?.pageSize) searchParams.set('pageSize', String(params.pageSize))

    const response = await api.get<ApiResponse<Project[]>>(`/projects?${searchParams.toString()}`)
    return {
      items: response.data.data,
      pagination: response.data.pagination!,
    }
  },

  async getProjectById(id: string): Promise<Project> {
    const response = await api.get<ApiResponse<Project>>(`/projects/${id}`)
    return response.data.data
  },

  async createProject(data: CreateProjectRequest): Promise<Project> {
    const response = await api.post<ApiResponse<Project>>('/projects', data)
    return response.data.data
  },

  async updateProject(id: string, data: UpdateProjectRequest): Promise<Project> {
    const response = await api.put<ApiResponse<Project>>(`/projects/${id}`, data)
    return response.data.data
  },

  async activateProject(id: string): Promise<void> {
    await api.post(`/projects/${id}/activate`)
  },

  async deactivateProject(id: string): Promise<void> {
    await api.post(`/projects/${id}/deactivate`)
  },

  async deleteProject(id: string): Promise<void> {
    await api.delete(`/projects/${id}`)
  },
}
