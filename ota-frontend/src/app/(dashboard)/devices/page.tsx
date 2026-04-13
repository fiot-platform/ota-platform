'use client'

import * as React from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Search, RefreshCw, Eye, PauseCircle, XCircle } from 'lucide-react'
import Link from 'next/link'
import { deviceService } from '@/services/device.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { DataTable, Column } from '@/components/ui/DataTable'
import { StatusBadge } from '@/components/ui/Badge'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
import { RoleGuard } from '@/components/role-access/RoleGuard'
import { useToast } from '@/components/ui/ToastProvider'
import { Device, DeviceStatus } from '@/types'
import { formatRelativeTime, formatVersion } from '@/utils/formatters'

export default function DevicesPage() {
  const queryClient = useQueryClient()
  const { toast } = useToast()

  const [search, setSearch] = React.useState('')
  const [statusFilter, setStatusFilter] = React.useState('')
  const [page, setPage] = React.useState(1)
  const [pageSize, setPageSize] = React.useState(25)
  const [suspendTarget, setSuspendTarget] = React.useState<Device | null>(null)
  const [decommissionTarget, setDecommissionTarget] = React.useState<Device | null>(null)

  const { data, isLoading } = useQuery({
    queryKey: ['devices', { search, status: statusFilter, page, pageSize }],
    queryFn: () => deviceService.getDevices({
      search,
      status: (statusFilter as DeviceStatus) || undefined,
      page,
      pageSize,
    }),
  })

  const suspendMutation = useMutation({
    mutationFn: (id: string) => deviceService.suspendDevice(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['devices'] })
      toast({ title: 'Device suspended', variant: 'warning' })
      setSuspendTarget(null)
    },
    onError: () => toast({ title: 'Failed to suspend device', variant: 'error' }),
  })

  const decommissionMutation = useMutation({
    mutationFn: (id: string) => deviceService.decommissionDevice(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['devices'] })
      toast({ title: 'Device decommissioned', variant: 'success' })
      setDecommissionTarget(null)
    },
    onError: () => toast({ title: 'Failed to decommission device', variant: 'error' }),
  })

  const isOnline = (lastHeartbeat?: string) => {
    if (!lastHeartbeat) return false
    const diff = Date.now() - new Date(lastHeartbeat).getTime()
    return diff < 5 * 60 * 1000 // 5 minutes
  }

  const columns: Column<Device>[] = [
    {
      key: 'serialNumber',
      header: 'Serial Number',
      cell: (row) => (
        <div>
          <Link
            href={`/devices/${row.id}`}
            className="font-mono font-semibold text-accent-600 hover:text-accent-700 transition-colors text-sm"
          >
            {row.serialNumber}
          </Link>
          <p className="text-xs text-slate-500 mt-0.5">{row.customerName ?? row.customerId}</p>
        </div>
      ),
    },
    {
      key: 'model',
      header: 'Model',
      cell: (row) => (
        <div>
          <p className="text-sm font-medium text-primary-800">{row.model}</p>
          {row.hardwareRevision && (
            <p className="text-xs text-slate-500">HW Rev: {row.hardwareRevision}</p>
          )}
        </div>
      ),
    },
    {
      key: 'firmware',
      header: 'Current Firmware',
      cell: (row) => (
        row.currentFirmwareVersion ? (
          <code className="text-xs bg-slate-100 px-2 py-0.5 rounded text-accent-700 font-semibold">
            {formatVersion(row.currentFirmwareVersion)}
          </code>
        ) : (
          <span className="text-xs text-slate-400">Not set</span>
        )
      ),
    },
    {
      key: 'status',
      header: 'Status',
      cell: (row) => <StatusBadge status={row.status} dot />,
    },
    {
      key: 'heartbeat',
      header: 'Connectivity',
      cell: (row) => (
        <div className="flex items-center gap-2">
          <span
            className={`w-2 h-2 rounded-full ${isOnline(row.lastHeartbeatAt) ? 'bg-success-500' : 'bg-slate-300'}`}
          />
          <span className="text-xs text-slate-500">
            {row.lastHeartbeatAt ? formatRelativeTime(row.lastHeartbeatAt) : 'Never'}
          </span>
        </div>
      ),
    },
    {
      key: 'site',
      header: 'Site',
      cell: (row) => (
        <span className="text-sm text-slate-600">{row.siteName ?? row.siteId ?? '—'}</span>
      ),
    },
    {
      key: 'actions',
      header: 'Actions',
      cell: (row) => (
        <div className="flex items-center gap-1">
          <Link
            href={`/devices/${row.id}`}
            className="p-1.5 rounded-lg text-slate-400 hover:text-accent-600 hover:bg-accent-50 transition-colors"
            title="View"
          >
            <Eye className="w-4 h-4" />
          </Link>
          <RoleGuard module="Devices" action="update">
            {row.status === DeviceStatus.Active && (
              <button
                onClick={() => setSuspendTarget(row)}
                className="p-1.5 rounded-lg text-slate-400 hover:text-warning-600 hover:bg-warning-50 transition-colors"
                title="Suspend"
              >
                <PauseCircle className="w-4 h-4" />
              </button>
            )}
            {row.status !== DeviceStatus.Decommissioned && (
              <button
                onClick={() => setDecommissionTarget(row)}
                className="p-1.5 rounded-lg text-slate-400 hover:text-danger-600 hover:bg-danger-50 transition-colors"
                title="Decommission"
              >
                <XCircle className="w-4 h-4" />
              </button>
            )}
          </RoleGuard>
        </div>
      ),
    },
  ]

  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        title="Devices"
        subtitle="Monitor and manage registered IoT devices"
        breadcrumbs={[{ label: 'Dashboard', href: '/dashboard' }, { label: 'Devices' }]}
      />

      {/* Filters */}
      <div className="filter-bar">
        <div className="relative flex-1 min-w-[200px] max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
          <input
            type="text"
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1) }}
            placeholder="Search by serial, model..."
            className="input pl-9"
          />
        </div>

        <select
          value={statusFilter}
          onChange={(e) => { setStatusFilter(e.target.value); setPage(1) }}
          className="input w-auto"
        >
          <option value="">All Statuses</option>
          {Object.values(DeviceStatus).map((s) => (
            <option key={s} value={s}>{s}</option>
          ))}
        </select>

        <button
          onClick={() => queryClient.invalidateQueries({ queryKey: ['devices'] })}
          className="btn-secondary"
        >
          <RefreshCw className="w-4 h-4" />
          Refresh
        </button>
      </div>

      {/* Table */}
      <DataTable
        columns={columns}
        data={data?.items ?? []}
        pagination={data?.pagination}
        onPageChange={setPage}
        onPageSizeChange={(size) => { setPageSize(size); setPage(1) }}
        isLoading={isLoading}
        keyExtractor={(r) => r.id}
        emptyMessage="No devices registered"
      />

      {/* Suspend Confirm */}
      <ConfirmDialog
        open={Boolean(suspendTarget)}
        onOpenChange={(open) => !open && setSuspendTarget(null)}
        title="Suspend Device"
        message={`Suspend device ${suspendTarget?.serialNumber}? The device will not receive OTA updates while suspended.`}
        confirmLabel="Suspend"
        variant="warning"
        onConfirm={() => suspendTarget && suspendMutation.mutate(suspendTarget.id)}
        isLoading={suspendMutation.isPending}
      />

      {/* Decommission Confirm */}
      <ConfirmDialog
        open={Boolean(decommissionTarget)}
        onOpenChange={(open) => !open && setDecommissionTarget(null)}
        title="Decommission Device"
        message={`Permanently decommission device ${decommissionTarget?.serialNumber}? This action cannot be undone.`}
        confirmLabel="Decommission"
        variant="destructive"
        onConfirm={() => decommissionTarget && decommissionMutation.mutate(decommissionTarget.id)}
        isLoading={decommissionMutation.isPending}
      />
    </div>
  )
}
