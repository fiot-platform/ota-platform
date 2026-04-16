'use client'

import * as React from 'react'
import { Download } from 'lucide-react'
import { RoleGuard } from '@/components/role-access/RoleGuard'

// ─── Table Skeleton ───────────────────────────────────────────────────────────

export function TableSkeleton({ rows = 5, cols = 5 }: { rows?: number; cols?: number }) {
  return (
    <div className="space-y-2">
      {Array.from({ length: rows }).map((_, i) => (
        <div key={i} className="flex gap-3">
          {Array.from({ length: cols }).map((_, j) => (
            <div key={j} className="h-8 bg-slate-100 rounded animate-pulse flex-1" />
          ))}
        </div>
      ))}
    </div>
  )
}

// ─── Empty State ──────────────────────────────────────────────────────────────

export function EmptyState({ message = 'No data available' }: { message?: string }) {
  return (
    <div className="flex items-center justify-center h-40 text-sm text-slate-400">
      {message}
    </div>
  )
}

// ─── Table Cells ──────────────────────────────────────────────────────────────

export function Th({ children, right }: { children: React.ReactNode; right?: boolean }) {
  return (
    <th className={`py-2 px-3 text-xs font-semibold text-slate-500 uppercase tracking-wide border-b border-slate-200 ${right ? 'text-right' : 'text-left'}`}>
      {children}
    </th>
  )
}

export function Td({ children, right, muted }: { children: React.ReactNode; right?: boolean; muted?: boolean }) {
  return (
    <td className={`py-2.5 px-3 text-sm border-b border-slate-50 ${right ? 'text-right' : ''} ${muted ? 'text-slate-400' : 'text-slate-700'}`}>
      {children}
    </td>
  )
}

// ─── Status Badges ────────────────────────────────────────────────────────────

export function StatusPill({ active }: { active: boolean }) {
  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${active ? 'bg-success-100 text-success-700' : 'bg-slate-100 text-slate-500'}`}>
      {active ? 'Active' : 'Inactive'}
    </span>
  )
}

const FW_STATUS_COLORS: Record<string, string> = {
  Draft:           'bg-slate-100 text-slate-600',
  PendingQA:       'bg-yellow-100 text-yellow-700',
  QAVerified:      'bg-cyan-100 text-cyan-700',
  PendingApproval: 'bg-orange-100 text-orange-700',
  Approved:        'bg-success-100 text-success-700',
  Rejected:        'bg-danger-100 text-danger-700',
  Deprecated:      'bg-slate-200 text-slate-500',
  Active:          'bg-success-100 text-success-700',
}
export function FwBadge({ status }: { status: string }) {
  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${FW_STATUS_COLORS[status] ?? 'bg-slate-100 text-slate-600'}`}>
      {status}
    </span>
  )
}

const CHANNEL_COLORS: Record<string, string> = {
  Alpha:      'bg-purple-100 text-purple-700',
  Beta:       'bg-blue-100 text-blue-700',
  Staging:    'bg-orange-100 text-orange-700',
  Production: 'bg-success-100 text-success-700',
}
export function ChannelBadge({ channel }: { channel: string }) {
  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${CHANNEL_COLORS[channel] ?? 'bg-slate-100 text-slate-600'}`}>
      {channel}
    </span>
  )
}

const DEVICE_STATUS_COLORS: Record<string, string> = {
  Active:         'bg-success-100 text-success-700',
  Inactive:       'bg-slate-100 text-slate-500',
  Suspended:      'bg-warning-100 text-warning-700',
  Decommissioned: 'bg-danger-100 text-danger-700',
  Pending:        'bg-blue-100 text-blue-700',
}
export function DeviceStatusBadge({ status }: { status: string }) {
  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${DEVICE_STATUS_COLORS[status] ?? 'bg-slate-100 text-slate-600'}`}>
      {status}
    </span>
  )
}

const JOB_STATUS_COLORS: Record<string, string> = {
  Pending:    'bg-slate-100 text-slate-600',
  Queued:     'bg-blue-100 text-blue-700',
  InProgress: 'bg-cyan-100 text-cyan-700',
  Succeeded:  'bg-success-100 text-success-700',
  Failed:     'bg-danger-100 text-danger-700',
  Cancelled:  'bg-slate-200 text-slate-500',
  Retrying:   'bg-warning-100 text-warning-700',
  Skipped:    'bg-slate-100 text-slate-500',
}
export function JobStatusBadge({ status }: { status: string }) {
  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${JOB_STATUS_COLORS[status] ?? 'bg-slate-100 text-slate-600'}`}>
      {status}
    </span>
  )
}

// ─── Stage bar colours ────────────────────────────────────────────────────────

export const STAGE_BAR_COLOR: Record<string, string> = {
  Draft:           '#6366f1',
  PendingQA:       '#f59e0b',
  QAVerified:      '#06b6d4',
  PendingApproval: '#f97316',
  Approved:        '#22c55e',
  Active:          '#22c55e',
  Rejected:        '#ef4444',
  Deprecated:      '#94a3b8',
}

// ─── Report Card Header ───────────────────────────────────────────────────────

export function ReportHeader({
  title, subtitle, reportType, exporting, onExport, children,
}: {
  title: string
  subtitle: string
  reportType: string
  exporting: boolean
  onExport: (type: string) => void
  children?: React.ReactNode
}) {
  return (
    <div className="flex items-start justify-between gap-4 flex-wrap">
      <div>
        <h3 className="section-title">{title}</h3>
        <p className="text-muted">{subtitle}</p>
      </div>
      <div className="flex items-center gap-2 flex-wrap">
        {children}
        <RoleGuard module="Reports" action="export">
          <button onClick={() => onExport(reportType)} disabled={exporting} className="btn-secondary btn-sm">
            <Download className="w-3.5 h-3.5" />Export
          </button>
        </RoleGuard>
      </div>
    </div>
  )
}
