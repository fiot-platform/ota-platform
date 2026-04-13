import { format, formatDistanceToNow, parseISO } from 'date-fns'

// ─── Date Formatting ──────────────────────────────────────────────────────────

export function formatDate(date: string | Date | undefined | null, formatStr = 'MMM dd, yyyy'): string {
  if (!date) return 'N/A'
  try {
    const d = typeof date === 'string' ? parseISO(date) : date
    return format(d, formatStr)
  } catch {
    return 'Invalid date'
  }
}

export function formatDateTime(date: string | Date | undefined | null): string {
  return formatDate(date, 'MMM dd, yyyy HH:mm')
}

export function formatRelativeTime(date: string | Date | undefined | null): string {
  if (!date) return 'N/A'
  try {
    const d = typeof date === 'string' ? parseISO(date) : date
    return formatDistanceToNow(d, { addSuffix: true })
  } catch {
    return 'N/A'
  }
}

// ─── File Size Formatting ─────────────────────────────────────────────────────

export function formatFileSize(bytes: number | undefined | null): string {
  if (bytes === undefined || bytes === null) return 'N/A'
  if (bytes === 0) return '0 B'

  const units = ['B', 'KB', 'MB', 'GB', 'TB']
  const k = 1024
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  const value = bytes / Math.pow(k, i)

  return `${value.toFixed(i === 0 ? 0 : 1)} ${units[i]}`
}

// ─── Version Formatting ───────────────────────────────────────────────────────

export function formatVersion(version: string | undefined | null): string {
  if (!version) return 'N/A'
  // Ensure version starts with 'v' prefix
  if (!version.startsWith('v')) return `v${version}`
  return version
}

// ─── Status Formatting ────────────────────────────────────────────────────────

export function formatStatus(status: string | undefined | null): string {
  if (!status) return 'Unknown'

  // Split on capital letters and join with spaces
  return status
    .replace(/([A-Z])/g, ' $1')
    .replace(/^In /, 'In')
    .trim()
}

export function getStatusColor(
  status: string | undefined | null
): 'success' | 'warning' | 'danger' | 'info' | 'neutral' {
  if (!status) return 'neutral'

  const statusLower = status.toLowerCase()

  const successStatuses = [
    'active', 'approved', 'succeeded', 'completed', 'processed',
    'qaverified', 'qa_verified', 'online', 'uptodate', 'up_to_date',
  ]
  const warningStatuses = [
    'pending', 'pendingqa', 'pending_qa', 'pendingapproval', 'pending_approval',
    'inprogress', 'in_progress', 'scheduled', 'paused', 'retrying',
    'draft', 'queued', 'processing',
  ]
  const dangerStatuses = [
    'rejected', 'failed', 'deprecated', 'suspended', 'decommissioned',
    'cancelled', 'skipped', 'inactive', 'error',
  ]
  const infoStatuses = [
    'beta', 'staging', 'dev', 'hotfix', 'production', 'received',
  ]

  if (successStatuses.some(s => statusLower.includes(s.toLowerCase()))) return 'success'
  if (warningStatuses.some(s => statusLower.includes(s.toLowerCase()))) return 'warning'
  if (dangerStatuses.some(s => statusLower.includes(s.toLowerCase()))) return 'danger'
  if (infoStatuses.some(s => statusLower.includes(s.toLowerCase()))) return 'info'

  return 'neutral'
}

export function getStatusBgColor(status: string | undefined | null): string {
  const color = getStatusColor(status)
  const colorMap = {
    success: 'bg-success-100 text-success-800',
    warning: 'bg-warning-100 text-warning-800',
    danger: 'bg-danger-100 text-danger-800',
    info: 'bg-accent-100 text-accent-800',
    neutral: 'bg-primary-100 text-primary-700',
  }
  return colorMap[color]
}

// ─── Text Utilities ───────────────────────────────────────────────────────────

export function truncateText(text: string | undefined | null, length: number = 50): string {
  if (!text) return ''
  if (text.length <= length) return text
  return `${text.slice(0, length)}...`
}

export function capitalize(text: string | undefined | null): string {
  if (!text) return ''
  return text.charAt(0).toUpperCase() + text.slice(1).toLowerCase()
}

export function formatNumber(value: number | undefined | null): string {
  if (value === undefined || value === null) return '0'
  return new Intl.NumberFormat('en-US').format(value)
}

export function formatPercent(value: number | undefined | null, decimals = 1): string {
  if (value === undefined || value === null) return '0%'
  return `${value.toFixed(decimals)}%`
}

export function formatRole(role: string | undefined | null): string {
  if (!role) return 'Unknown'
  return role
    .replace(/([A-Z])/g, ' $1')
    .trim()
}

export function downloadBlob(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = filename
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  URL.revokeObjectURL(url)
}
