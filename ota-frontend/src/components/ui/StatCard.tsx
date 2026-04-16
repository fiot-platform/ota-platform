import * as React from 'react'
import { TrendingUp, TrendingDown, Minus } from 'lucide-react'
import { clsx } from 'clsx'

interface StatCardProps {
  label: string
  value: string | number
  icon: React.ReactNode
  trend?: {
    value: number
    label?: string
    direction: 'up' | 'down' | 'neutral'
  }
  accent?: 'primary' | 'green' | 'amber' | 'red' | 'purple' | 'navy'
  isLoading?: boolean
  onClick?: () => void
  className?: string
}

const accentStyles: Record<string, { bg: string; icon: string; value: string }> = {
  primary: {
    bg: 'bg-accent-50 ring-accent-200',
    icon: 'text-accent-600',
    value: 'text-accent-700',
  },
  green: {
    bg: 'bg-success-50 ring-success-200',
    icon: 'text-success-600',
    value: 'text-success-700',
  },
  amber: {
    bg: 'bg-warning-50 ring-warning-200',
    icon: 'text-warning-600',
    value: 'text-warning-700',
  },
  red: {
    bg: 'bg-danger-50 ring-danger-200',
    icon: 'text-danger-600',
    value: 'text-danger-700',
  },
  purple: {
    bg: 'bg-purple-50 ring-purple-200',
    icon: 'text-purple-600',
    value: 'text-purple-700',
  },
  navy: {
    bg: 'bg-primary-50 ring-primary-200',
    icon: 'text-primary-600',
    value: 'text-primary-700',
  },
}

export function StatCard({
  label,
  value,
  icon,
  trend,
  accent = 'primary',
  isLoading = false,
  onClick,
  className,
}: StatCardProps) {
  const styles = accentStyles[accent]

  if (isLoading) {
    return (
      <div className={clsx('card p-5 space-y-3', className)}>
        <div className="flex items-center justify-between">
          <div className="h-4 bg-slate-200 rounded animate-pulse w-24" />
          <div className="w-10 h-10 bg-slate-200 rounded-lg animate-pulse" />
        </div>
        <div className="h-8 bg-slate-200 rounded animate-pulse w-20" />
        <div className="h-4 bg-slate-200 rounded animate-pulse w-32" />
      </div>
    )
  }

  return (
    <div
      className={clsx(
        'card p-5 transition-all duration-200',
        onClick && 'cursor-pointer hover:shadow-card-hover hover:-translate-y-0.5',
        className
      )}
      onClick={onClick}
      role={onClick ? 'button' : undefined}
      tabIndex={onClick ? 0 : undefined}
      onKeyDown={onClick ? (e) => e.key === 'Enter' && onClick() : undefined}
    >
      <div className="flex items-start justify-between">
        <div className="space-y-3 flex-1">
          <p className="text-sm font-medium text-slate-500">{label}</p>
          <p className="text-3xl font-bold text-primary-900 tracking-tight">
            {typeof value === 'number' ? value.toLocaleString() : value}
          </p>
          {trend && (
            <div className="flex items-center gap-1.5">
              <span
                className={clsx(
                  'flex items-center gap-0.5 text-xs font-semibold',
                  trend.direction === 'up' && 'text-success-600',
                  trend.direction === 'down' && 'text-danger-600',
                  trend.direction === 'neutral' && 'text-slate-500'
                )}
              >
                {trend.direction === 'up' && <TrendingUp className="w-3.5 h-3.5" />}
                {trend.direction === 'down' && <TrendingDown className="w-3.5 h-3.5" />}
                {trend.direction === 'neutral' && <Minus className="w-3.5 h-3.5" />}
                {Math.abs(trend.value)}%
              </span>
              {trend.label && (
                <span className="text-xs text-slate-400">{trend.label}</span>
              )}
            </div>
          )}
        </div>
        <div className={clsx('w-11 h-11 flex items-center justify-center rounded-xl ring-1', styles.bg)}>
          <span className={styles.icon}>{icon}</span>
        </div>
      </div>
    </div>
  )
}
