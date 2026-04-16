'use client'

import * as React from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Search, RefreshCw, Eye, PauseCircle, XCircle, Plus, FileSpreadsheet, Pencil } from 'lucide-react'
import Link from 'next/link'
import { deviceService } from '@/services/device.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { DataTable, Column } from '@/components/ui/DataTable'
import { StatusBadge } from '@/components/ui/Badge'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
import { RegisterDeviceForm } from '@/components/forms/RegisterDeviceForm'
import { EditDeviceForm } from '@/components/forms/EditDeviceForm'
import { BulkDeviceUploadForm } from '@/components/forms/BulkDeviceUploadForm'
import { RoleGuard } from '@/components/role-access/RoleGuard'
import { useToast } from '@/components/ui/ToastProvider'
import { Device, DeviceStatus, UpdateDeviceRequest } from '@/types'
import { formatRelativeTime, formatVersion } from '@/utils/formatters'
import { OtaProgressCell } from '@/components/ui/OtaProgressCell'
import { useProjectScope } from '@/hooks/useProjectScope'
import { ProjectScopeBanner } from '@/components/ui/ProjectScopeBanner'

export default function DevicesPage() {
  const queryClient = useQueryClient()
  const { toast } = useToast()
  const { scopedProjectId, scopedProjectName, isScoped } = useProjectScope()

  const [search, setSearch] = React.useState('')
  const [statusFilter, setStatusFilter] = React.useState('')
  const [page, setPage] = React.useState(1)
  const [pageSize, setPageSize] = React.useState(25)
  const [suspendTarget, setSuspendTarget] = React.useState<Device | null>(null)
  const [decommissionTarget, setDecommissionTarget] = React.useState<Device | null>(null)
  const [editTarget, setEditTarget] = React.useState<Device | null>(null)
  const [registerOpen, setRegisterOpen] = React.useState(false)
  const [bulkOpen, setBulkOpen] = React.useState(false)

  const { data, isLoading } = useQuery({
    queryKey: ['devices', { search, status: statusFilter, projectId: isScoped ? scopedProjectId : undefined, page, pageSize }],
    queryFn: () => deviceService.getDevices({
      search,
      status: (statusFilter as DeviceStatus) || undefined,
      projectId: isScoped ? (scopedProjectId ?? undefined) : undefined,
      page,
      pageSize,
    }),
    // Always poll every 30 s so new OTA status is picked up automatically.
    // Drops to 5 s while any device has an active OTA in progress.
    refetchInterval: (query) => {
      const items = query.state.data?.items ?? []
      const hasActiveOta = items.some(
        (d) => d.otaStatus === 'start' || d.otaStatus === 'inprogress'
      )
      return hasActiveOta ? 5000 : 30000
    },
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

  const registerMutation = useMutation({
    mutationFn: deviceService.registerDevice,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['devices'] })
      toast({ title: 'Device registered successfully', variant: 'success' })
      setRegisterOpen(false)
    },
    onError: (error: any) => {
      const msg = error?.response?.data?.message || 'Failed to register device'
      toast({ title: msg, variant: 'error' })
    },
  })

  const editMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateDeviceRequest }) =>
      deviceService.updateDevice(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['devices'] })
      toast({ title: 'Device updated successfully', variant: 'success' })
      setEditTarget(null)
    },
    onError: (error: any) => {
      const msg = error?.response?.data?.message || 'Failed to update device'
      toast({ title: msg, variant: 'error' })
    },
  })

  const bulkMutation = useMutation({
    mutationFn: deviceService.bulkRegisterDevices,
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ['devices'] })
      if (result.failed === 0) {
        toast({ title: `${result.succeeded} device(s) registered successfully`, variant: 'success' })
      } else {
        toast({ title: `${result.succeeded} registered, ${result.failed} failed`, variant: 'warning' })
      }
    },
    onError: (error: any) => {
      const msg = error?.response?.data?.message || 'Bulk upload failed'
      toast({ title: msg, variant: 'error' })
    },
  })

  const isOnline = (lastHeartbeat?: string) => {
    if (!lastHeartbeat) return false
    const diff = Date.now() - new Date(lastHeartbeat).getTime()
    return diff < 5 * 60 * 1000 // 5 minutes
  }

  const columns: Column<Device>[] = [
    {
      key: 'macImeiIp',
      header: 'MAC / IMEI / IP',
      cell: (row) => (
        <div>
          <Link
            href={`/devices/${row.id}`}
            className="font-mono font-semibold text-accent-600 hover:text-accent-700 transition-colors text-sm"
          >
            {row.macImeiIp ?? '—'}
          </Link>
          <p className="text-xs text-slate-500 mt-0.5">{row.customerName ?? row.customerId}</p>
        </div>
      ),
    },
    {
      key: 'projectName',
      header: 'Project',
      cell: (row) => (
        <span className="text-sm text-primary-800">{row.projectName ?? '—'}</span>
      ),
    },
    {
      key: 'model',
      header: 'Model',
      cell: (row) => (
        <span className="text-sm font-medium text-primary-800">{row.model}</span>
      ),
    },
    {
      key: 'firmware',
      header: 'Initial Firmware',
      cell: (row) => (
        row.currentFirmwareVersion ? (
          <code className="text-xs bg-slate-100 px-2 py-0.5 rounded text-accent-700 font-semibold">
            {formatVersion(row.currentFirmwareVersion)}
          </code>
        ) : (
          <span className="text-xs text-slate-400">0.0.0</span>
        )
      ),
    },
    {
      key: 'otaProgress',
      header: 'OTA Progress',
      cell: (row) => <OtaProgressCell device={row} />,
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
            <button
              onClick={() => setEditTarget(row)}
              className="p-1.5 rounded-lg text-slate-400 hover:text-primary-600 hover:bg-primary-50 transition-colors"
              title="Edit"
            >
              <Pencil className="w-4 h-4" />
            </button>
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
        actions={
          <RoleGuard module="Devices" action="create">
            <button onClick={() => setBulkOpen(true)} className="btn-secondary">
              <FileSpreadsheet className="w-4 h-4" />
              Bulk Upload
            </button>
            <button onClick={() => setRegisterOpen(true)} className="btn-primary">
              <Plus className="w-4 h-4" />
              Register Device
            </button>
          </RoleGuard>
        }
      />

      {/* Project scope banner — QA users only */}
      {isScoped && scopedProjectId && (
        <ProjectScopeBanner projectId={scopedProjectId} projectName={scopedProjectName} />
      )}

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
        {data?.items.some((d) => d.otaStatus === 'start' || d.otaStatus === 'inprogress') && (
          <span className="flex items-center gap-1.5 text-xs font-medium text-accent-600 bg-accent-50 border border-accent-200 px-2.5 py-1.5 rounded-lg">
            <span className="w-2 h-2 rounded-full bg-accent-500 animate-pulse" />
            OTA in progress — auto-refreshing
          </span>
        )}
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

      {/* Bulk Upload Modal */}
      <BulkDeviceUploadForm
        open={bulkOpen}
        onOpenChange={setBulkOpen}
        onUpload={async (devices) => {
          const result = await bulkMutation.mutateAsync(devices)
          return result
        }}
        isLoading={bulkMutation.isPending}
      />

      {/* Register Device Modal */}
      <RegisterDeviceForm
        open={registerOpen}
        onOpenChange={setRegisterOpen}
        onSubmit={async (data) => {
          await registerMutation.mutateAsync({
            projectName: data.projectName,
            customerCode: data.customerCode,
            macImeiIp: data.macImeiIp,
            model: data.model,
            currentFirmwareVersion: data.currentFirmwareVersion || undefined,
            publishTopic: data.publishTopic || undefined,
          })
        }}
        isLoading={registerMutation.isPending}
      />

      {/* Edit Device Modal */}
      {editTarget && (
        <EditDeviceForm
          open={Boolean(editTarget)}
          onOpenChange={(open) => !open && setEditTarget(null)}
          device={editTarget}
          onSubmit={async (data) => {
            await editMutation.mutateAsync({ id: editTarget.id, data })
          }}
          isLoading={editMutation.isPending}
        />
      )}

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
