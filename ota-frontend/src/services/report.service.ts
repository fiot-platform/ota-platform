import api from '@/lib/api'
import {
  DashboardSummary,
  FirmwareApprovalTrend,
  RolloutSuccessRate,
  DeviceUpdateStatus,
  UserReport,
  ProjectReport,
  RepositoryReport,
  FirmwareVersionReport,
  DeviceReport,
  ProjectRepoFirmwareRow,
  DeviceOtaHistoryRow,
  DailyOtaProgress,
  FirmwareStageReport,
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

  // ── New reports ────────────────────────────────────────────────────────────

  async getUsersReport(): Promise<UserReport[]> {
    const response = await api.get<ApiResponse<UserReport[]>>('/reports/users')
    return response.data.data
  },

  async getProjectsReport(): Promise<ProjectReport[]> {
    const response = await api.get<ApiResponse<ProjectReport[]>>('/reports/projects')
    return response.data.data
  },

  async getRepositoriesReport(): Promise<RepositoryReport[]> {
    const response = await api.get<ApiResponse<RepositoryReport[]>>('/reports/repositories')
    return response.data.data
  },

  async getFirmwareVersionsReport(): Promise<FirmwareVersionReport[]> {
    const response = await api.get<ApiResponse<FirmwareVersionReport[]>>('/reports/firmware-versions')
    return response.data.data
  },

  async getDevicesReport(): Promise<DeviceReport[]> {
    const response = await api.get<ApiResponse<DeviceReport[]>>('/reports/devices')
    return response.data.data
  },

  async getProjectRepoFirmwareReport(): Promise<ProjectRepoFirmwareRow[]> {
    const response = await api.get<ApiResponse<ProjectRepoFirmwareRow[]>>('/reports/project-repo-firmware')
    return response.data.data
  },

  async getDeviceOtaHistory(deviceId?: string): Promise<DeviceOtaHistoryRow[]> {
    const params = deviceId ? `?deviceId=${deviceId}` : ''
    const response = await api.get<ApiResponse<DeviceOtaHistoryRow[]>>(
      `/reports/device-ota-history${params}`
    )
    return response.data.data
  },

  async getDailyOtaProgress(days: number = 14): Promise<DailyOtaProgress[]> {
    const response = await api.get<ApiResponse<DailyOtaProgress[]>>(
      `/reports/daily-ota-progress?days=${days}`
    )
    return response.data.data
  },

  async getFirmwareStageReport(): Promise<FirmwareStageReport[]> {
    const response = await api.get<ApiResponse<FirmwareStageReport[]>>('/reports/firmware-stage')
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
