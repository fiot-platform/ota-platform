import api from '@/lib/api'
import { AuditLog, AuditLogFilter, ApiResponse, PaginatedResponse } from '@/types'

export const auditService = {
  async getAuditLogs(filter?: AuditLogFilter): Promise<PaginatedResponse<AuditLog>> {
    const params = new URLSearchParams()
    if (filter?.action) params.set('action', filter.action)
    if (filter?.performedBy) params.set('performedBy', filter.performedBy)
    if (filter?.entityType) params.set('entityType', filter.entityType)
    if (filter?.entityId) params.set('entityId', filter.entityId)
    if (filter?.customerId) params.set('customerId', filter.customerId)
    if (filter?.startDate) params.set('startDate', filter.startDate)
    if (filter?.endDate) params.set('endDate', filter.endDate)
    if (filter?.page) params.set('page', String(filter.page))
    if (filter?.pageSize) params.set('pageSize', String(filter.pageSize))

    const response = await api.get<ApiResponse<AuditLog[]>>(`/audit?${params.toString()}`)
    return {
      items: response.data.data,
      pagination: response.data.pagination!,
    }
  },

  async exportAuditLogs(filter?: AuditLogFilter): Promise<Blob> {
    const params = new URLSearchParams()
    if (filter?.action) params.set('action', filter.action)
    if (filter?.performedBy) params.set('performedBy', filter.performedBy)
    if (filter?.entityType) params.set('entityType', filter.entityType)
    if (filter?.customerId) params.set('customerId', filter.customerId)
    if (filter?.startDate) params.set('startDate', filter.startDate)
    if (filter?.endDate) params.set('endDate', filter.endDate)

    const response = await api.get(`/audit/export?${params.toString()}`, {
      responseType: 'blob',
    })
    return response.data as Blob
  },

  async getAuditLogById(id: string): Promise<AuditLog> {
    const response = await api.get<ApiResponse<AuditLog>>(`/audit/${id}`)
    return response.data.data
  },
}
