import api from '@/lib/api'
import {
  Device,
  DeviceFilters,
  UpdateDeviceRequest,
  ApiResponse,
  PaginatedResponse,
} from '@/types'

export const deviceService = {
  async getDevices(filters?: DeviceFilters): Promise<PaginatedResponse<Device>> {
    const params = new URLSearchParams()
    if (filters?.customerId) params.set('customerId', filters.customerId)
    if (filters?.siteId) params.set('siteId', filters.siteId)
    if (filters?.status) params.set('status', filters.status)
    if (filters?.model) params.set('model', filters.model)
    if (filters?.search) params.set('search', filters.search)
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

  async getDeviceOtaHistory(
    deviceId: string,
    params?: { page?: number; pageSize?: number }
  ): Promise<PaginatedResponse<{ id: string; rolloutName: string; status: string; completedAt?: string; firmwareVersion: string }>> {
    const searchParams = new URLSearchParams()
    if (params?.page) searchParams.set('page', String(params.page))
    if (params?.pageSize) searchParams.set('pageSize', String(params.pageSize))

    const response = await api.get<ApiResponse<{ id: string; rolloutName: string; status: string; completedAt?: string; firmwareVersion: string }[]>>(
      `/devices/${deviceId}/ota-history?${searchParams.toString()}`
    )
    return {
      items: response.data.data,
      pagination: response.data.pagination!,
    }
  },
}
