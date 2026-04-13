import axios, { AxiosError, AxiosInstance, InternalAxiosRequestConfig } from 'axios'
import { getToken, clearToken } from './auth'

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api'

const api: AxiosInstance = axios.create({
  baseURL: API_URL,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
    Accept: 'application/json',
  },
})

// ─── Request Interceptor ──────────────────────────────────────────────────────

api.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = getToken()
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error: AxiosError) => {
    return Promise.reject(error)
  }
)

// ─── Response Interceptor ─────────────────────────────────────────────────────

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean }

    if (error.response?.status === 401) {
      if (!originalRequest._retry) {
        originalRequest._retry = true
        // Try to refresh the token
        const refreshToken = typeof window !== 'undefined'
          ? localStorage.getItem('ota_refresh_token')
          : null

        if (refreshToken) {
          try {
            const response = await axios.post(`${API_URL}/auth/refresh`, {
              refreshToken,
            })
            const { accessToken } = response.data.data || response.data
            if (accessToken) {
              if (typeof window !== 'undefined') {
                localStorage.setItem('ota_access_token', accessToken)
                document.cookie = `ota_token=${accessToken}; path=/; SameSite=Lax; max-age=${60 * 60 * 24}`
              }
              originalRequest.headers.Authorization = `Bearer ${accessToken}`
              return api(originalRequest)
            }
          } catch {
            // Refresh failed - clear tokens and redirect
          }
        }
      }
      // Clear token and redirect to login
      clearToken()
      if (typeof window !== 'undefined') {
        window.location.href = '/login'
      }
      return Promise.reject(error)
    }

    if (error.response?.status === 403) {
      if (typeof window !== 'undefined') {
        window.location.href = '/unauthorized'
      }
      return Promise.reject(error)
    }

    return Promise.reject(error)
  }
)

export default api
