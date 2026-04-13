import api from '@/lib/api'
import {
  FirmwareVersion,
  FirmwareFilters,
  CreateFirmwareRequest,
  UpdateFirmwareRequest,
  ApproveFirmwareRequest,
  RejectFirmwareRequest,
  QAVerifyRequest,
  AssignChannelRequest,
  ApiResponse,
  PaginatedResponse,
} from '@/types'

export const firmwareService = {
  async getFirmwareList(filters?: FirmwareFilters): Promise<PaginatedResponse<FirmwareVersion>> {
    const params = new URLSearchParams()
    if (filters?.status) params.set('status', filters.status)
    if (filters?.channel) params.set('channel', filters.channel)
    if (filters?.projectId) params.set('projectId', filters.projectId)
    if (filters?.repositoryId) params.set('repositoryId', filters.repositoryId)
    if (filters?.search) params.set('search', filters.search)
    params.set('page', String(filters?.page ?? 1))
    params.set('pageSize', String(filters?.pageSize ?? 25))

    const response = await api.get<ApiResponse<FirmwareVersion[]>>(
      `/firmware?${params.toString()}`
    )
    return {
      items: response.data.data,
      pagination: response.data.pagination!,
    }
  },

  async getFirmwareById(id: string): Promise<FirmwareVersion> {
    const response = await api.get<ApiResponse<FirmwareVersion>>(`/firmware/${id}`)
    return response.data.data
  },

  async createFirmware(data: CreateFirmwareRequest): Promise<FirmwareVersion> {
    const response = await api.post<ApiResponse<FirmwareVersion>>('/firmware', data)
    return response.data.data
  },

  async updateFirmware(id: string, data: UpdateFirmwareRequest): Promise<FirmwareVersion> {
    const response = await api.put<ApiResponse<FirmwareVersion>>(`/firmware/${id}`, data)
    return response.data.data
  },

  async approveFirmware(id: string, data: ApproveFirmwareRequest): Promise<FirmwareVersion> {
    const response = await api.post<ApiResponse<FirmwareVersion>>(`/firmware/${id}/approve`, data)
    return response.data.data
  },

  async rejectFirmware(id: string, data: RejectFirmwareRequest): Promise<FirmwareVersion> {
    const response = await api.post<ApiResponse<FirmwareVersion>>(`/firmware/${id}/reject`, data)
    return response.data.data
  },

  async qaVerifyFirmware(id: string, data: QAVerifyRequest): Promise<FirmwareVersion> {
    const response = await api.post<ApiResponse<FirmwareVersion>>(`/firmware/${id}/qa-verify`, data)
    return response.data.data
  },

  async assignChannel(id: string, data: AssignChannelRequest): Promise<FirmwareVersion> {
    const response = await api.post<ApiResponse<FirmwareVersion>>(
      `/firmware/${id}/assign-channel`,
      data
    )
    return response.data.data
  },

  async deprecateFirmware(id: string): Promise<FirmwareVersion> {
    const response = await api.post<ApiResponse<FirmwareVersion>>(`/firmware/${id}/deprecate`)
    return response.data.data
  },

  async uploadFirmwareFile(file: File): Promise<{
    fileName: string
    storedFileName: string
    fileSizeBytes: number
    fileSha256: string
    downloadUrl: string
  }> {
    const formData = new FormData()
    formData.append('file', file)
    const response = await api.post<ApiResponse<{
      fileName: string
      storedFileName: string
      fileSizeBytes: number
      fileSha256: string
      downloadUrl: string
    }>>('/firmware/upload-file', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    return response.data.data
  },

  async syncFromGitea(repositoryId: string): Promise<{ synced: number; message: string }> {
    const response = await api.post<ApiResponse<{ synced: number; message: string }>>(
      `/firmware/sync/${repositoryId}`
    )
    return response.data.data
  },

  async getFirmwareStatusHistory(
    id: string
  ): Promise<{ status: string; changedBy: string; changedAt: string; notes?: string }[]> {
    const response = await api.get<
      ApiResponse<{ status: string; changedBy: string; changedAt: string; notes?: string }[]>
    >(`/firmware/${id}/history`)
    return response.data.data
  },
}
