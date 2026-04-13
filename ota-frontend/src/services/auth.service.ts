import api from '@/lib/api'
import { setToken, clearToken } from '@/lib/auth'
import { LoginRequest, LoginResponse, RefreshTokenRequest, ApiResponse, User } from '@/types'

export const authService = {
  async login(credentials: LoginRequest): Promise<LoginResponse> {
    const response = await api.post<ApiResponse<LoginResponse>>('/auth/login', credentials)
    const data = response.data.data
    setToken(data.accessToken, data.refreshToken)
    return data
  },

  async refreshToken(request?: RefreshTokenRequest): Promise<{ accessToken: string; refreshToken?: string }> {
    const refreshToken = request?.refreshToken
      ?? (typeof window !== 'undefined' ? localStorage.getItem('ota_refresh_token') : null)

    const response = await api.post<ApiResponse<{ accessToken: string; refreshToken?: string }>>(
      '/auth/refresh',
      { refreshToken }
    )
    const data = response.data.data
    if (data.accessToken) {
      setToken(data.accessToken, data.refreshToken)
    }
    return data
  },

  async logout(): Promise<void> {
    try {
      await api.post('/auth/logout')
    } catch {
      // Ignore errors on logout
    } finally {
      clearToken()
    }
  },

  async getCurrentUser(): Promise<User> {
    const response = await api.get<ApiResponse<User>>('/auth/me')
    return response.data.data
  },

  async changePassword(oldPassword: string, newPassword: string): Promise<void> {
    await api.post('/auth/change-password', { oldPassword, newPassword })
  },

  async forgotPassword(email: string): Promise<void> {
    await api.post('/auth/forgot-password', { email })
  },

  async resetPassword(token: string, newPassword: string): Promise<void> {
    await api.post('/auth/reset-password', { token, newPassword })
  },
}
