'use client'

import { useCallback } from 'react'
import { getCurrentRole } from '@/lib/auth'
import { canAccess, PermissionModule, PermissionAction } from '@/lib/permissions'
import { UserRole } from '@/types'

export interface PermissionsHook {
  can: (module: PermissionModule, action: PermissionAction) => boolean
  role: UserRole | null
  isRole: (roles: UserRole[]) => boolean
}

export function usePermissions(): PermissionsHook {
  const role = getCurrentRole()

  const can = useCallback(
    (module: PermissionModule, action: PermissionAction): boolean => {
      return canAccess(role, module, action)
    },
    [role]
  )

  const isRole = useCallback(
    (roles: UserRole[]): boolean => {
      if (!role) return false
      return roles.includes(role)
    },
    [role]
  )

  return { can, role, isRole }
}
