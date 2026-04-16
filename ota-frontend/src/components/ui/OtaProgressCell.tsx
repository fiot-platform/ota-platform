'use client'

import { Device } from '@/types'
import { formatVersion } from '@/utils/formatters'

interface Props {
  device: Device
}

export function OtaProgressCell({ device }: Props) {
  const { otaStatus, otaProgress = 0, otaTargetVersion } = device
  const status = otaStatus?.toLowerCase() ?? ''
  const version = otaTargetVersion ? formatVersion(otaTargetVersion) : null

  // ── derive display values per state ──────────────────────────────────────
  const pct = (() => {
    switch (status) {
      case 'start':     return 5
      case 'inprogress': return Math.min(100, Math.max(1, otaProgress))
      case 'success':   return 100
      case 'failed':
      case 'rollback':  return Math.min(100, Math.max(1, otaProgress))
      default:          return 0
    }
  })()

  const barColor = (() => {
    switch (status) {
      case 'start':
      case 'inprogress': return 'bg-accent-500'
      case 'success':    return 'bg-success-500'
      case 'failed':     return 'bg-danger-400'
      case 'rollback':   return 'bg-warning-400'
      default:           return 'bg-slate-200'
    }
  })()

  const trackColor = (() => {
    switch (status) {
      case 'start':
      case 'inprogress': return 'bg-accent-100'
      case 'success':    return 'bg-success-100'
      case 'failed':     return 'bg-danger-100'
      case 'rollback':   return 'bg-warning-100'
      default:           return 'bg-slate-100'
    }
  })()

  const pctColor = (() => {
    switch (status) {
      case 'start':
      case 'inprogress': return 'text-accent-700'
      case 'success':    return 'text-success-700'
      case 'failed':     return 'text-danger-600'
      case 'rollback':   return 'text-warning-700'
      default:           return 'text-slate-400'
    }
  })()

  const statusLabel = (() => {
    switch (status) {
      case 'start':      return 'Starting…'
      case 'inprogress': return 'Updating'
      case 'success':    return version ? `Updated  ${version}` : 'Updated'
      case 'failed':     return 'Failed'
      case 'rollback':   return 'Rolled back'
      default:           return 'Idle'
    }
  })()

  return (
    <div className="w-40 space-y-1">
      {/* Status label + percentage */}
      <div className="flex items-center justify-between">
        <span className={`text-xs font-semibold ${pctColor}`}>
          {statusLabel}
        </span>
        <span className={`text-xs font-bold tabular-nums ${pctColor}`}>
          {pct}%
        </span>
      </div>

      {/* Bar track */}
      <div className={`w-full h-2 rounded-full overflow-hidden ${trackColor}`}>
        <div
          className={`h-full rounded-full transition-all duration-500 ${barColor} ${status === 'start' ? 'animate-pulse' : ''}`}
          style={{ width: `${pct}%` }}
        />
      </div>
    </div>
  )
}
