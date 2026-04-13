'use client'

import * as React from 'react'
import { usePermissions } from '@/hooks/usePermissions'
import { PermissionModule, PermissionAction } from '@/lib/permissions'
import { UserRole } from '@/types'

interface RoleGuardProps {
  module?: PermissionModule
  action?: PermissionAction
  roles?: UserRole[]
  fallback?: React.ReactNode
  children: React.ReactNode
}

export function RoleGuard({
  module,
  action,
  roles,
  fallback = null,
  children,
}: RoleGuardProps) {
  const { can, isRole } = usePermissions()

  let hasAccess = true

  if (module && action) {
    hasAccess = can(module, action)
  }

  if (roles && roles.length > 0) {
    hasAccess = hasAccess && isRole(roles)
  }

  if (!hasAccess) {
    return <>{fallback}</>
  }

  return <>{children}</>
}

// ─── HOC ──────────────────────────────────────────────────────────────────────

export function withRoleGuard<P extends object>(
  WrappedComponent: React.ComponentType<P>,
  guardProps: Omit<RoleGuardProps, 'children'>
): React.ComponentType<P> {
  const GuardedComponent = (props: P) => (
    <RoleGuard {...guardProps}>
      <WrappedComponent {...props} />
    </RoleGuard>
  )

  GuardedComponent.displayName = `withRoleGuard(${WrappedComponent.displayName || WrappedComponent.name || 'Component'})`

  return GuardedComponent
}
