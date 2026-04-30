'use client'

import * as React from 'react'
import { useQuery } from '@tanstack/react-query'
import { Download, Search } from 'lucide-react'
import { deviceService } from '@/services/device.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { StatusBadge } from '@/components/ui/Badge'
import { RoleGuard } from '@/components/role-access/RoleGuard'
import { OtaProgressCell } from '@/components/ui/OtaProgressCell'
import { useToast } from '@/components/ui/ToastProvider'
import { downloadBlob, formatRelativeTime, formatVersion } from '@/utils/formatters'
import { TableSkeleton } from '@/components/reports/shared'
import { Device, DeviceStatus } from '@/types'

const HEARTBEAT_ONLINE_WINDOW_MS = 5 * 60 * 1000

function isOnline(lastHeartbeatAt?: string): boolean {
  if (!lastHeartbeatAt) return false
  return Date.now() - new Date(lastHeartbeatAt).getTime() < HEARTBEAT_ONLINE_WINDOW_MS
}

function otaStatusLabel(d: Device): string {
  if (d.hasActiveOtaJob) return 'Pending'
  if (d.otaStatus === 'success') return 'Done'
  if (d.otaStatus === 'failed') return 'Failed'
  if (d.otaStatus === 'rollback') return 'Rolled back'
  return 'Ready'
}

function otaStatusBadgeClasses(d: Device): string {
  if (d.hasActiveOtaJob)
    return 'bg-warning-50 text-warning-700 border-warning-200'
  if (d.otaStatus === 'success')
    return 'bg-success-50 text-success-700 border-success-200'
  if (d.otaStatus === 'failed' || d.otaStatus === 'rollback')
    return 'bg-danger-50 text-danger-700 border-danger-200'
  return 'bg-slate-100 text-slate-500 border-slate-200'
}

export default function DeviceStatusReportPage() {
  const { toast } = useToast()
  const [search, setSearch] = React.useState('')

  const { data, isLoading } = useQuery({
    queryKey: ['report-device-status-list'],
    queryFn: () => deviceService.getDevices({ pageSize: 500 }),
    refetchInterval: 30_000,
    staleTime: 10_000,
  })

  const devices: Device[] = data?.items ?? []

  const filtered = React.useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return devices
    return devices.filter((d) =>
      (d.macImeiIp ?? '').toLowerCase().includes(q) ||
      (d.serialNumber ?? '').toLowerCase().includes(q) ||
      d.model.toLowerCase().includes(q) ||
      (d.projectName ?? '').toLowerCase().includes(q) ||
      (d.repositoryName ?? '').toLowerCase().includes(q) ||
      (d.currentFirmwareVersion ?? '').toLowerCase().includes(q) ||
      (d.pendingFirmwareVersion ?? '').toLowerCase().includes(q)
    )
  }, [devices, search])

  const handleExport = () => {
    const header = [
      'MAC / IMEI / IP', 'Project', 'Repository', 'Model',
      'Current Firmware', 'New Firmware',
      'OTA Progress', 'OTA Status', 'Device Status', 'Connectivity',
    ]
    const escape = (v: string | number | undefined | null) => {
      const s = (v ?? '').toString()
      return /[",\n]/.test(s) ? `"${s.replace(/"/g, '""')}"` : s
    }
    const rows = filtered.map((d) => [
      d.macImeiIp ?? d.serialNumber ?? '',
      d.projectName ?? '',
      d.repositoryName ?? '',
      d.model,
      d.currentFirmwareVersion ?? '',
      d.pendingFirmwareVersion ?? '',
      d.otaStatus
        ? `${d.otaStatus} ${d.otaProgress ?? 0}%`
        : 'Idle',
      otaStatusLabel(d),
      d.status,
      isOnline(d.lastHeartbeatAt) ? 'Online' : 'Offline',
    ].map(escape).join(','))
    const csv = [header.join(','), ...rows].join('\n')
    downloadBlob(new Blob([csv], { type: 'text/csv;charset=utf-8' }),
      `device-status-${new Date().toISOString().slice(0, 10)}.csv`)
    toast({ title: 'Report exported', variant: 'success' })
  }

  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        title="Device Update Status"
        subtitle="Per-device firmware and connectivity report"
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Reports', href: '/reports/device-status' },
          { label: 'Device Status' },
        ]}
      />

      <div className="card p-6 space-y-4">
        <div className="flex items-center justify-between gap-3 flex-wrap">
          <div>
            <h3 className="section-title">Device Status</h3>
            <p className="text-muted">
              {isLoading ? 'Loading…' : `${filtered.length} of ${devices.length} device${devices.length === 1 ? '' : 's'}`}
            </p>
          </div>
          <div className="flex items-center gap-3 flex-wrap">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
              <input
                type="text"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Search MAC / project / model / firmware…"
                className="input pl-9 py-1.5 text-sm w-72"
              />
            </div>
            <RoleGuard module="Reports" action="export">
              <button onClick={handleExport} disabled={filtered.length === 0} className="btn-secondary btn-sm">
                <Download className="w-3.5 h-3.5" /> Export CSV
              </button>
            </RoleGuard>
          </div>
        </div>

        {isLoading ? (
          <TableSkeleton rows={8} cols={10} />
        ) : filtered.length === 0 ? (
          <p className="text-sm text-slate-400 text-center py-8">No devices match the current filter.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-slate-50 border-b border-slate-200">
                <tr>
                  {[
                    'MAC / IMEI / IP', 'Project', 'Repository', 'Model',
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
                {filtered.map((d) => (
                  <tr key={d.id} className="border-b border-slate-100 hover:bg-slate-50">
                    <td className="px-4 py-3">
                      <div>
                        <span className="font-mono font-semibold text-accent-600 text-sm">{d.macImeiIp ?? '—'}</span>
                        {d.customerName && <p className="text-xs text-slate-500 mt-0.5">{d.customerName}</p>}
                      </div>
                    </td>
                    <td className="px-4 py-3 text-sm text-primary-800">{d.projectName ?? '—'}</td>
                    <td className="px-4 py-3 text-sm text-primary-800">{d.repositoryName ?? '—'}</td>
                    <td className="px-4 py-3 text-sm font-medium text-primary-800">{d.model}</td>
                    <td className="px-4 py-3">
                      {d.currentFirmwareVersion ? (
                        <code className="text-xs bg-slate-100 px-2 py-0.5 rounded text-accent-700 font-semibold">
                          {formatVersion(d.currentFirmwareVersion)}
                        </code>
                      ) : (
                        <span className="text-xs text-slate-400">0.0.0</span>
                      )}
                    </td>
                    <td className="px-4 py-3">
                      {d.pendingFirmwareVersion ? (
                        <code className="text-xs bg-warning-50 px-2 py-0.5 rounded text-warning-700 font-semibold border border-warning-200">
                          {formatVersion(d.pendingFirmwareVersion)}
                        </code>
                      ) : (
                        <span className="text-xs text-slate-400">—</span>
                      )}
                    </td>
                    <td className="px-4 py-3"><OtaProgressCell device={d} /></td>
                    <td className="px-4 py-3">
                      <span className={`inline-flex items-center gap-1.5 text-xs font-semibold px-2.5 py-1 rounded-full border ${otaStatusBadgeClasses(d)}`}>
                        {otaStatusLabel(d)}
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      <StatusBadge status={d.status as DeviceStatus} dot />
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-2">
                        <span className={`w-2 h-2 rounded-full flex-shrink-0 ${isOnline(d.lastHeartbeatAt) ? 'bg-success-500' : 'bg-slate-300'}`} />
                        <span className="text-xs text-slate-500 whitespace-nowrap">
                          {d.lastHeartbeatAt ? formatRelativeTime(d.lastHeartbeatAt) : 'Never'}
                        </span>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}
