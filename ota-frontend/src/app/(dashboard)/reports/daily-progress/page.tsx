'use client'

import * as React from 'react'
import { useQuery } from '@tanstack/react-query'
import { Download, Search } from 'lucide-react'
import { reportService } from '@/services/report.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { StatusBadge } from '@/components/ui/Badge'
import { RoleGuard } from '@/components/role-access/RoleGuard'
import { OtaProgressCell } from '@/components/ui/OtaProgressCell'
import { useToast } from '@/components/ui/ToastProvider'
import { downloadBlob, formatDate, formatRelativeTime, formatVersion } from '@/utils/formatters'
import { TableSkeleton } from '@/components/reports/shared'
import { Device, DeviceOtaHistoryRow, DeviceStatus } from '@/types'

const HEARTBEAT_ONLINE_WINDOW_MS = 5 * 60 * 1000

function isOnline(lastHeartbeatAt?: string) {
  if (!lastHeartbeatAt) return false
  return Date.now() - new Date(lastHeartbeatAt).getTime() < HEARTBEAT_ONLINE_WINDOW_MS
}

function otaStatusLabel(r: DeviceOtaHistoryRow) {
  // Prefer the OTA job's own outcome — that's the per-event truth
  if (r.jobStatus === 'Succeeded') return 'Done'
  if (r.jobStatus === 'Failed')    return 'Failed'
  if (r.jobStatus === 'Cancelled') return 'Cancelled'
  if (r.jobStatus === 'InProgress') return 'In Progress'
  return 'Pending'
}

function otaStatusBadgeClasses(r: DeviceOtaHistoryRow) {
  if (r.jobStatus === 'Succeeded') return 'bg-success-50 text-success-700 border-success-200'
  if (r.jobStatus === 'Failed')    return 'bg-danger-50 text-danger-700 border-danger-200'
  if (r.jobStatus === 'Cancelled') return 'bg-slate-100 text-slate-500 border-slate-200'
  if (r.jobStatus === 'InProgress') return 'bg-accent-50 text-accent-700 border-accent-200'
  return 'bg-warning-50 text-warning-700 border-warning-200'
}

// Build a minimal Device-shaped object for OtaProgressCell
function asDevice(r: DeviceOtaHistoryRow): Device {
  return {
    id: r.deviceId,
    deviceId: r.deviceId,
    serialNumber: r.deviceSerial,
    macImeiIp: r.macImeiIp,
    model: r.model,
    customerId: '',
    customerName: r.customerName,
    projectName: r.projectName,
    repositoryName: r.repositoryName,
    currentFirmwareVersion: r.currentFirmwareVersion,
    pendingFirmwareVersion: r.pendingFirmwareVersion,
    status: (r.deviceStatus as DeviceStatus) ?? DeviceStatus.Active,
    lastHeartbeatAt: r.lastHeartbeatAt,
    registeredAt: '',
    updatedAt: '',
    otaStatus: r.otaStatus,
    otaProgress: r.otaProgress,
    otaTargetVersion: r.firmwareVersion,
  }
}

export default function DailyOtaProgressPage() {
  const { toast } = useToast()

  // Default From/To to today
  const today = React.useMemo(() => {
    const d = new Date()
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
  }, [])

  const [search,   setSearch]   = React.useState('')
  const [fromDate, setFromDate] = React.useState(today)
  const [toDate,   setToDate]   = React.useState(today)

  const { data, isLoading } = useQuery({
    queryKey: ['report-daily-ota-progress-flat'],
    queryFn: () => reportService.getDeviceOtaHistory(),
    refetchInterval: 30_000,
    staleTime: 10_000,
  })

  const filtered = React.useMemo(() => {
    let rows = data ?? []

    // Date filter — match against OTA event timestamp (completed → started → pushed)
    if (fromDate || toDate) {
      const from = fromDate ? new Date(fromDate + 'T00:00:00').getTime() : -Infinity
      const to   = toDate   ? new Date(toDate   + 'T23:59:59').getTime() :  Infinity
      rows = rows.filter((r) => {
        const stamp = r.completedAt ?? r.startedAt ?? r.pushedAt
        if (!stamp) return false
        const t = new Date(stamp).getTime()
        return t >= from && t <= to
      })
    }

    const q = search.trim().toLowerCase()
    if (q) rows = rows.filter(
      (r) =>
        (r.macImeiIp ?? '').toLowerCase().includes(q) ||
        r.deviceSerial.toLowerCase().includes(q) ||
        (r.customerName ?? '').toLowerCase().includes(q) ||
        r.projectName.toLowerCase().includes(q) ||
        (r.repositoryName ?? '').toLowerCase().includes(q) ||
        (r.model ?? '').toLowerCase().includes(q) ||
        r.firmwareVersion.toLowerCase().includes(q) ||
        (r.currentFirmwareVersion ?? '').toLowerCase().includes(q),
    )
    return rows
  }, [data, search, fromDate, toDate])

  const handleExport = () => {
    const header = [
      'OTA Date', 'MAC / IMEI / IP', 'Project', 'Repository', 'Model',
      'Current Firmware', 'New Firmware',
      'OTA Progress', 'OTA Status', 'Device Status', 'Connectivity',
    ]
    const escape = (v: string | number | undefined | null) => {
      const s = (v ?? '').toString()
      return /[",\n]/.test(s) ? `"${s.replace(/"/g, '""')}"` : s
    }
    const rows = filtered.map((r) => {
      const otaDate = r.completedAt ?? r.startedAt ?? r.pushedAt
      return [
        otaDate ? formatDate(otaDate, 'dd MMM yyyy HH:mm') : '',
        r.macImeiIp ?? r.deviceSerial,
        r.projectName,
        r.repositoryName ?? '',
        r.model,
        r.currentFirmwareVersion ?? '',
        r.firmwareVersion,
        r.otaStatus ? `${r.otaStatus} ${r.otaProgress ?? 0}%` : 'Idle',
        otaStatusLabel(r),
        r.deviceStatus ?? '',
        isOnline(r.lastHeartbeatAt) ? 'Online' : 'Offline',
      ].map(escape).join(',')
    })
    const csv = [header.join(','), ...rows].join('\n')
    downloadBlob(new Blob([csv], { type: 'text/csv;charset=utf-8' }),
      `daily-ota-progress-${new Date().toISOString().slice(0, 10)}.csv`)
    toast({ title: 'Report exported', variant: 'success' })
  }

  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        title="Daily OTA Progress Report"
        subtitle="Per-OTA event activity across the fleet"
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Reports', href: '/reports/device-status' },
          { label: 'Daily OTA Progress' },
        ]}
      />

      <div className="card p-6 space-y-4">
        <div className="flex items-center justify-between gap-3 flex-wrap">
          <div>
            <h3 className="section-title">Daily OTA Progress</h3>
            <p className="text-muted">
              {isLoading ? 'Loading…' : `${filtered.length} OTA event${filtered.length === 1 ? '' : 's'}`}
            </p>
          </div>
          <div className="flex items-center gap-3 flex-wrap">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
              <input
                type="text"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Search MAC / project / repo / model / firmware…"
                className="input pl-9 py-1.5 text-sm w-72"
              />
            </div>
            <div className="flex items-center gap-1.5">
              <label className="text-xs font-medium text-slate-500">From</label>
              <input
                type="date"
                value={fromDate}
                max={toDate || undefined}
                onChange={(e) => setFromDate(e.target.value)}
                className="px-2.5 py-1.5 text-sm border border-slate-300 rounded-lg outline-none focus:ring-2 focus:ring-accent-500"
              />
            </div>
            <div className="flex items-center gap-1.5">
              <label className="text-xs font-medium text-slate-500">To</label>
              <input
                type="date"
                value={toDate}
                min={fromDate || undefined}
                onChange={(e) => setToDate(e.target.value)}
                className="px-2.5 py-1.5 text-sm border border-slate-300 rounded-lg outline-none focus:ring-2 focus:ring-accent-500"
              />
            </div>
            {(fromDate || toDate) && (
              <button
                type="button"
                onClick={() => { setFromDate(''); setToDate('') }}
                className="text-xs text-slate-500 hover:text-red-500 transition-colors"
              >
                Clear dates
              </button>
            )}
            <RoleGuard module="Reports" action="export">
              <button onClick={handleExport} disabled={filtered.length === 0} className="btn-secondary btn-sm">
                <Download className="w-3.5 h-3.5" /> Export CSV
              </button>
            </RoleGuard>
          </div>
        </div>

        {isLoading ? (
          <TableSkeleton rows={8} cols={11} />
        ) : filtered.length === 0 ? (
          <p className="text-sm text-slate-400 text-center py-8">No OTA events match the current filter.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-slate-50 border-b border-slate-200">
                <tr>
                  {[
                    'OTA Date', 'MAC / IMEI / IP', 'Project', 'Repository', 'Model',
                    'Current Firmware', 'New Firmware',
                    'OTA Progress', 'OTA Status', 'Device Status', 'Connectivity',
                  ].map((h) => (
                    <th
                      key={h}
                      className="px-4 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wide whitespace-nowrap"
                    >
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {filtered.map((r, idx) => {
                  const otaDate = r.completedAt ?? r.startedAt ?? r.pushedAt
                  return (
                    <tr key={`${r.deviceId}-${r.firmwareVersion}-${idx}`} className="border-b border-slate-100 hover:bg-slate-50">
                      <td className="px-4 py-3 text-xs text-slate-600 whitespace-nowrap">
                        {otaDate ? formatDate(otaDate, 'dd MMM yyyy HH:mm') : '—'}
                      </td>
                      <td className="px-4 py-3">
                        <span className="font-mono font-semibold text-accent-600 text-sm">
                          {r.macImeiIp ?? r.deviceSerial}
                        </span>
                        {r.customerName && <p className="text-xs text-slate-500 mt-0.5">{r.customerName}</p>}
                      </td>
                      <td className="px-4 py-3 text-sm text-primary-800">{r.projectName ?? '—'}</td>
                      <td className="px-4 py-3 text-sm text-primary-800">{r.repositoryName ?? '—'}</td>
                      <td className="px-4 py-3 text-sm font-medium text-primary-800">{r.model ?? '—'}</td>
                      <td className="px-4 py-3">
                        {r.currentFirmwareVersion ? (
                          <code className="text-xs bg-slate-100 px-2 py-0.5 rounded text-accent-700 font-semibold">
                            {formatVersion(r.currentFirmwareVersion)}
                          </code>
                        ) : (
                          <span className="text-xs text-slate-400">0.0.0</span>
                        )}
                      </td>
                      <td className="px-4 py-3">
                        <code className="text-xs bg-warning-50 px-2 py-0.5 rounded text-warning-700 font-semibold border border-warning-200">
                          {formatVersion(r.firmwareVersion)}
                        </code>
                      </td>
                      <td className="px-4 py-3"><OtaProgressCell device={asDevice(r)} /></td>
                      <td className="px-4 py-3">
                        <span className={`inline-flex items-center gap-1.5 text-xs font-semibold px-2.5 py-1 rounded-full border ${otaStatusBadgeClasses(r)}`}>
                          {otaStatusLabel(r)}
                        </span>
                      </td>
                      <td className="px-4 py-3">
                        {r.deviceStatus
                          ? <StatusBadge status={r.deviceStatus as DeviceStatus} dot />
                          : <span className="text-xs text-slate-400">—</span>}
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-2">
                          <span className={`w-2 h-2 rounded-full flex-shrink-0 ${isOnline(r.lastHeartbeatAt) ? 'bg-success-500' : 'bg-slate-300'}`} />
                          <span className="text-xs text-slate-500 whitespace-nowrap">
                            {r.lastHeartbeatAt ? formatRelativeTime(r.lastHeartbeatAt) : 'Never'}
                          </span>
                        </div>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}
