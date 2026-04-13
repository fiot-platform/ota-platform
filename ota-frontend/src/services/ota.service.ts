import api from '@/lib/api'
import {
  Rollout,
  RolloutFilters,
  CreateRolloutRequest,
  OtaJob,
  RolloutSummary,
  ApiResponse,
  PaginatedResponse,
} from '@/types'

export const otaService = {
  async getRollouts(filters?: RolloutFilters): Promise<PaginatedResponse<Rollout>> {
    const params = new URLSearchParams()
    if (filters?.status) params.set('status', filters.status)
    if (filters?.projectId) params.set('projectId', filters.projectId)
    if (filters?.firmwareVersionId) params.set('firmwareVersionId', filters.firmwareVersionId)
    if (filters?.search) params.set('search', filters.search)
    if (filters?.page) params.set('page', String(filters.page))
    if (filters?.pageSize) params.set('pageSize', String(filters.pageSize))

    const response = await api.get<ApiResponse<Rollout[]>>(`/rollouts?${params.toString()}`)
    return {
      items: response.data.data,
      pagination: response.data.pagination!,
    }
  },

  async getRolloutById(id: string): Promise<Rollout> {
    const response = await api.get<ApiResponse<Rollout>>(`/rollouts/${id}`)
    return response.data.data
  },

  async createRollout(data: CreateRolloutRequest): Promise<Rollout> {
    const response = await api.post<ApiResponse<Rollout>>('/rollouts', data)
    return response.data.data
  },

  async startRollout(id: string): Promise<Rollout> {
    const response = await api.post<ApiResponse<Rollout>>(`/rollouts/${id}/start`)
    return response.data.data
  },

  async pauseRollout(id: string): Promise<Rollout> {
    const response = await api.post<ApiResponse<Rollout>>(`/rollouts/${id}/pause`)
    return response.data.data
  },

  async resumeRollout(id: string): Promise<Rollout> {
    const response = await api.post<ApiResponse<Rollout>>(`/rollouts/${id}/resume`)
    return response.data.data
  },

  async cancelRollout(id: string): Promise<Rollout> {
    const response = await api.post<ApiResponse<Rollout>>(`/rollouts/${id}/cancel`)
    return response.data.data
  },

  async getRolloutJobs(
    rolloutId: string,
    params?: { page?: number; pageSize?: number; status?: string }
  ): Promise<PaginatedResponse<OtaJob>> {
    const searchParams = new URLSearchParams()
    if (params?.page) searchParams.set('page', String(params.page))
    if (params?.pageSize) searchParams.set('pageSize', String(params.pageSize))
    if (params?.status) searchParams.set('status', params.status)

    const response = await api.get<ApiResponse<OtaJob[]>>(
      `/rollouts/${rolloutId}/jobs?${searchParams.toString()}`
    )
    return {
      items: response.data.data,
      pagination: response.data.pagination!,
    }
  },

  async retryJob(jobId: string): Promise<OtaJob> {
    const response = await api.post<ApiResponse<OtaJob>>(`/ota-jobs/${jobId}/retry`)
    return response.data.data
  },

  async getRolloutSummary(rolloutId: string): Promise<RolloutSummary> {
    const response = await api.get<ApiResponse<RolloutSummary>>(`/rollouts/${rolloutId}/summary`)
    return response.data.data
  },

  async getPolicies(
    params?: { page?: number; pageSize?: number }
  ): Promise<PaginatedResponse<{ id: string; name: string; description?: string }>> {
    const searchParams = new URLSearchParams()
    if (params?.page) searchParams.set('page', String(params.page))
    if (params?.pageSize) searchParams.set('pageSize', String(params.pageSize))

    const response = await api.get<ApiResponse<{ id: string; name: string; description?: string }[]>>(
      `/rollout-policies?${searchParams.toString()}`
    )
    return {
      items: response.data.data,
      pagination: response.data.pagination!,
    }
  },
}
