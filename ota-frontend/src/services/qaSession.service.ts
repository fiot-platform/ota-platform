import api from '@/lib/api'
import {
  QASession,
  QAEventLogItem,
  AddBugRequest,
  UpdateBugRequest,
  UpdateQAStatusRequest,
  CompleteQARequest,
  ApiResponse,
} from '@/types'

export const qaSessionService = {
  async getSession(firmwareId: string): Promise<QASession | null> {
    try {
      const response = await api.get<ApiResponse<QASession>>(`/qa/firmware/${firmwareId}`)
      return response.data.data
    } catch (err: any) {
      if (err?.response?.status === 404) return null
      throw err
    }
  },

  async startSession(firmwareId: string): Promise<QASession> {
    const response = await api.post<ApiResponse<QASession>>(`/qa/firmware/${firmwareId}/start`)
    return response.data.data
  },

  async updateStatus(firmwareId: string, data: UpdateQAStatusRequest): Promise<QASession> {
    const response = await api.put<ApiResponse<QASession>>(`/qa/firmware/${firmwareId}/status`, data)
    return response.data.data
  },

  async uploadDocument(firmwareId: string, file: File, type: 'testCase' | 'testResult'): Promise<QASession> {
    const formData = new FormData()
    formData.append('file', file)
    const response = await api.post<ApiResponse<QASession>>(
      `/qa/firmware/${firmwareId}/documents?type=${type}`,
      formData,
      { headers: { 'Content-Type': 'multipart/form-data' } }
    )
    return response.data.data
  },

  async removeDocument(firmwareId: string, documentId: string): Promise<void> {
    await api.delete(`/qa/firmware/${firmwareId}/documents/${documentId}`)
  },

  async addBug(firmwareId: string, data: AddBugRequest): Promise<QASession> {
    const response = await api.post<ApiResponse<QASession>>(`/qa/firmware/${firmwareId}/bugs`, data)
    return response.data.data
  },

  async updateBug(firmwareId: string, bugId: string, data: UpdateBugRequest): Promise<QASession> {
    const response = await api.put<ApiResponse<QASession>>(`/qa/firmware/${firmwareId}/bugs/${bugId}`, data)
    return response.data.data
  },

  async completeSession(firmwareId: string, data: CompleteQARequest): Promise<QASession> {
    const response = await api.post<ApiResponse<QASession>>(`/qa/firmware/${firmwareId}/complete`, data)
    return response.data.data
  },

  async getEventLog(firmwareId: string): Promise<QAEventLogItem[]> {
    const response = await api.get<ApiResponse<QAEventLogItem[]>>(`/qa/firmware/${firmwareId}/log`)
    return response.data.data
  },
}
