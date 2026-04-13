'use client'

import { useCallback } from 'react'
import { useRouter } from 'next/navigation'
import { getCurrentUser, isAuthenticated, getCurrentRole, logout as authLogout } from '@/lib/auth'
import { canAccess, PermissionModule, PermissionAction } from '@/lib/permissions'
import { UserRole, JwtPayload } from '@/types'

export interface AuthState {
  user: JwtPayload | null
  role: UserRole | null
  isAuthenticated: boolean
  canAccess: (module: PermissionModule, action: PermissionAction) => boolean
  logout: () => void
}

export function useAuth(): AuthState {
  const router = useRouter()
  const user = getCurrentUser()
  const role = getCurrentRole()
  const authenticated = isAuthenticated()

  const checkAccess = useCallback(
    (module: PermissionModule, action: PermissionAction): boolean => {
      return canAccess(role, module, action)
    },
    [role]
  )

  const logout = useCallback(() => {
    authLogout()
    router.push('/login')
  }, [router])

  return {
    user,
    role,
    isAuthenticated: authenticated,
    canAccess: checkAccess,
    logout,
  }
}
