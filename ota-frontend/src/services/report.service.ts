import api from '@/lib/api'
import {
  DashboardSummary,
  FirmwareApprovalTrend,
  RolloutSuccessRate,
  DeviceUpdateStatus,
  ApiResponse,
} from '@/types'

export const reportService = {
  async getDashboardSummary(): Promise<DashboardSummary> {
    const response = await api.get<ApiResponse<DashboardSummary>>('/reports/dashboard')
    return response.data.data
  },

  async getFirmwareTrends(days: number = 30): Promise<FirmwareApprovalTrend[]> {
    const response = await api.get<ApiResponse<FirmwareApprovalTrend[]>>(
      `/reports/firmware-approval-trend?days=${days}`
    )
    return response.data.data
  },

  async getRolloutSuccessRate(projectId?: string): Promise<RolloutSuccessRate[]> {
    const params = projectId ? `?projectId=${projectId}` : ''
    const response = await api.get<ApiResponse<RolloutSuccessRate[]>>(
      `/reports/rollout-success-rate${params}`
    )
    return response.data.data
  },

  async getDeviceUpdateStatus(customerId?: string): Promise<DeviceUpdateStatus[]> {
    const params = customerId ? `?customerId=${customerId}` : ''
    const response = await api.get<ApiResponse<DeviceUpdateStatus[]>>(
      `/reports/device-update-status${params}`
    )
    return response.data.data
  },

  async exportReport(reportType: string, params?: Record<string, string>): Promise<Blob> {
    const searchParams = new URLSearchParams(params)
    const response = await api.get(
      `/reports/export/${reportType}?${searchParams.toString()}`,
      { responseType: 'blob' }
    )
    return response.data as Blob
  },
}
