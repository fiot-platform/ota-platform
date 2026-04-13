import * as React from 'react'
import { clsx } from 'clsx'

export type BadgeVariant = 'success' | 'warning' | 'danger' | 'info' | 'neutral' | 'purple'

interface BadgeProps {
  variant?: BadgeVariant
  children: React.ReactNode
  className?: string
  dot?: boolean
}

const variantStyles: Record<BadgeVariant, string> = {
  success: 'bg-success-100 text-success-800 ring-success-200',
  warning: 'bg-warning-100 text-warning-800 ring-warning-200',
  danger: 'bg-danger-100 text-danger-800 ring-danger-200',
  info: 'bg-accent-100 text-accent-800 ring-accent-200',
  neutral: 'bg-slate-100 text-slate-700 ring-slate-200',
  purple: 'bg-purple-100 text-purple-800 ring-purple-200',
}

const dotStyles: Record<BadgeVariant, string> = {
  success: 'bg-success-500',
  warning: 'bg-warning-500',
  danger: 'bg-danger-500',
  info: 'bg-accent-500',
  neutral: 'bg-slate-500',
  purple: 'bg-purple-500',
}

export function Badge({ variant = 'neutral', children, className, dot = false }: BadgeProps) {
  return (
    <span
      className={clsx(
        'inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold ring-1 ring-inset',
        variantStyles[variant],
        className
      )}
    >
      {dot && (
        <span className={clsx('w-1.5 h-1.5 rounded-full flex-shrink-0', dotStyles[variant])} />
      )}
      {children}
    </span>
  )
}

// ─── Status-specific badge helpers ───────────────────────────────────────────

import { getStatusColor } from '@/utils/formatters'
import { formatStatus } from '@/utils/formatters'

interface StatusBadgeProps {
  status: string
  className?: string
  dot?: boolean
}

export function StatusBadge({ status, className, dot }: StatusBadgeProps) {
  const variant = getStatusColor(status)
  return (
    <Badge variant={variant} className={className} dot={dot}>
      {formatStatus(status)}
    </Badge>
  )
}

interface RoleBadgeProps {
  role: string
  className?: string
}

export function RoleBadge({ role, className }: RoleBadgeProps) {
  const roleVariants: Record<string, BadgeVariant> = {
    SuperAdmin: 'danger',
    PlatformAdmin: 'purple',
    ReleaseManager: 'info',
    QA: 'warning',
    DevOpsEngineer: 'info',
    SupportEngineer: 'neutral',
    CustomerAdmin: 'success',
    Viewer: 'neutral',
    Auditor: 'neutral',
    Device: 'neutral',
  }

  const variant = roleVariants[role] || 'neutral'
  const label = role.replace(/([A-Z])/g, ' $1').trim()

  return (
    <Badge variant={variant} className={className}>
      {label}
    </Badge>
  )
}
