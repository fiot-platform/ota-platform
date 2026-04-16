import api from '@/lib/api'
import {
  Device,
  DeviceFilters,
  RegisterDeviceRequest,
  UpdateDeviceRequest,
  BulkRegisterResult,
  ApiResponse,
  PaginatedResponse,
} from '@/types'

export interface AvailableFirmware {
  id: string
  version: string
  channel: string
  status: string
  releaseNotes?: string
  isMandate: boolean
  downloadUrl?: string
  fileSizeBytes: number
  approvedAt?: string
  supportedModels: string[]
}

export const deviceService = {
  async registerDevice(data: RegisterDeviceRequest): Promise<Device> {
    const response = await api.post<ApiResponse<Device>>('/devices', data)
    return response.data.data
  },

  async bulkRegisterDevices(devices: RegisterDeviceRequest[]): Promise<BulkRegisterResult> {
    const response = await api.post<ApiResponse<BulkRegisterResult>>('/devices/bulk', { devices })
    return response.data.data
  },

  async getDevices(filters?: DeviceFilters): Promise<PaginatedResponse<Device>> {
    const params = new URLSearchParams()
    if (filters?.customerId) params.set('customerId', filters.customerId)
    if (filters?.siteId) params.set('siteId', filters.siteId)
    if (filters?.status) params.set('status', filters.status)
    if (filters?.model) params.set('model', filters.model)
    if (filters?.search) params.set('search', filters.search)
    if (filters?.projectId) params.set('projectId', filters.projectId)
    if (filters?.page) params.set('page', String(filters.page))
    if (filters?.pageSize) params.set('pageSize', String(filters.pageSize))

    const response = await api.get<ApiResponse<Device[]>>(`/devices?${params.toString()}`)
    return {
      items: response.data.data,
      pagination: response.data.pagination!,
    }
  },

  async getDeviceById(id: string): Promise<Device> {
    const response = await api.get<ApiResponse<Device>>(`/devices/${id}`)
    return response.data.data
  },

  async updateDevice(id: string, data: UpdateDeviceRequest): Promise<Device> {
    const response = await api.put<ApiResponse<Device>>(`/devices/${id}`, data)
    return response.data.data
  },

  async suspendDevice(id: string): Promise<void> {
    await api.post(`/devices/${id}/suspend`)
  },

  async decommissionDevice(id: string): Promise<void> {
    await api.post(`/devices/${id}/decommission`)
  },

  async activateDevice(id: string): Promise<void> {
    await api.post(`/devices/${id}/activate`)
  },

  async getAvailableFirmware(id: string): Promise<AvailableFirmware[]> {
    const response = await api.get<ApiResponse<AvailableFirmware[]>>(`/devices/${id}/available-firmware`)
    return response.data.data
  },

  async pushFirmware(id: string, firmwareVersionId: string): Promise<{ jobId: string }> {
    const response = await api.post<ApiResponse<{ jobId: string }>>(`/devices/${id}/push-firmware`, { firmwareVersionId })
    return response.data.data
  },

  async getDeviceOtaHistory(
    deviceId: string,
    params?: { page?: number; pageSize?: number }
  ): Promise<PaginatedResponse<{
    id: string
    rolloutName?: string
    status: string
    progress: number
    source: string
    completedAt?: string
    timestamp: string
    firmwareVersion: string
  }>> {
    const searchParams = new URLSearchParams()
    if (params?.page) searchParams.set('page', String(params.page))
    if (params?.pageSize) searchParams.set('pageSize', String(params.pageSize))

    const response = await api.get<ApiResponse<{
      id: string
      rolloutName?: string
      status: string
      progress: number
      source: string
      completedAt?: string
      timestamp: string
      firmwareVersion: string
    }[]>>(`/devices/${deviceId}/ota-history?${searchParams.toString()}`)
    return {
      items: response.data.data,
      pagination: response.data.pagination!,
    }
  },
}
