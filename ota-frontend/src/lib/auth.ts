import { UserRole, JwtPayload } from '@/types'
import { canAccess, PermissionModule, PermissionAction } from './permissions'

const TOKEN_KEY = 'ota_access_token'
const REFRESH_TOKEN_KEY = 'ota_refresh_token'

// ─── Token Storage ────────────────────────────────────────────────────────────

export function getToken(): string | null {
  if (typeof window === 'undefined') return null
  try {
    // Try cookie first
    const cookieToken = getCookieValue('ota_token')
    if (cookieToken) return cookieToken
    // Fall back to localStorage
    return localStorage.getItem(TOKEN_KEY)
  } catch {
    return null
  }
}

export function getRefreshToken(): string | null {
  if (typeof window === 'undefined') return null
  try {
    return localStorage.getItem(REFRESH_TOKEN_KEY)
  } catch {
    return null
  }
}

export function setToken(token: string, refreshToken?: string): void {
  if (typeof window === 'undefined') return
  try {
    localStorage.setItem(TOKEN_KEY, token)
    // Also set as cookie for middleware access
    document.cookie = `ota_token=${token}; path=/; SameSite=Lax; max-age=${60 * 60 * 24}`
    if (refreshToken) {
      localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken)
    }
  } catch {
    // Silently fail
  }
}

export function clearToken(): void {
  if (typeof window === 'undefined') return
  try {
    localStorage.removeItem(TOKEN_KEY)
    localStorage.removeItem(REFRESH_TOKEN_KEY)
    document.cookie = 'ota_token=; path=/; max-age=0'
  } catch {
    // Silently fail
  }
}

function getCookieValue(name: string): string | null {
  if (typeof document === 'undefined') return null
  const match = document.cookie.match(new RegExp('(^| )' + name + '=([^;]+)'))
  return match ? decodeURIComponent(match[2]) : null
}

// ─── JWT Parsing ──────────────────────────────────────────────────────────────

function parseJwt(token: string): JwtPayload | null {
  try {
    const base64Url = token.split('.')[1]
    if (!base64Url) return null
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/')
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    )
    return JSON.parse(jsonPayload) as JwtPayload
  } catch {
    return null
  }
}

// ─── Auth Utilities ───────────────────────────────────────────────────────────

export function getCurrentUser(): JwtPayload | null {
  const token = getToken()
  if (!token) return null
  const payload = parseJwt(token)
  if (!payload) return null
  // Check if token is expired
  if (payload.exp && payload.exp * 1000 < Date.now()) {
    return null
  }
  return payload
}

export function isAuthenticated(): boolean {
  const user = getCurrentUser()
  return user !== null
}

export function getCurrentRole(): UserRole | null {
  const user = getCurrentUser()
  return user?.role ?? null
}

export function hasRole(roles: UserRole[]): boolean {
  const role = getCurrentRole()
  if (!role) return false
  return roles.includes(role)
}

export function hasPermission(module: PermissionModule, action: PermissionAction): boolean {
  const role = getCurrentRole()
  return canAccess(role, module, action)
}

export function logout(): void {
  clearToken()
  if (typeof window !== 'undefined') {
    window.location.href = '/login'
  }
}

export function getTokenExpiry(): Date | null {
  const token = getToken()
  if (!token) return null
  const payload = parseJwt(token)
  if (!payload?.exp) return null
  return new Date(payload.exp * 1000)
}

export function isTokenExpiringSoon(thresholdMinutes = 5): boolean {
  const expiry = getTokenExpiry()
  if (!expiry) return true
  const now = new Date()
  const diff = expiry.getTime() - now.getTime()
  return diff < thresholdMinutes * 60 * 1000
}
