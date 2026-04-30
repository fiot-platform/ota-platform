'use client'

import * as React from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Search, RefreshCw, Eye, PauseCircle, XCircle, Plus, FileSpreadsheet,
  Pencil, Rocket, CheckSquare, X,
  Filter, Loader2,
} from 'lucide-react'
import Link from 'next/link'
import { deviceService, AvailableFirmware } from '@/services/device.service'
import { firmwareService } from '@/services/firmware.service'
import { projectService } from '@/services/project.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { DataTable, Column } from '@/components/ui/DataTable'
import { StatusBadge } from '@/components/ui/Badge'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
import { RegisterDeviceForm } from '@/components/forms/RegisterDeviceForm'
import { EditDeviceForm } from '@/components/forms/EditDeviceForm'
import { BulkDeviceUploadForm } from '@/components/forms/BulkDeviceUploadForm'
import { RoleGuard } from '@/components/role-access/RoleGuard'
import { useToast } from '@/components/ui/ToastProvider'
import { Device, DeviceStatus, FirmwareStatus, UpdateDeviceRequest } from '@/types'
import { formatRelativeTime, formatVersion } from '@/utils/formatters'
import { OtaProgressCell } from '@/components/ui/OtaProgressCell'
import { useProjectScope } from '@/hooks/useProjectScope'
import { ProjectScopeBanner } from '@/components/ui/ProjectScopeBanner'

// ─── Push OTA Modal ───────────────────────────────────────────────────────────

interface PushOtaModalProps {
  selectedDevices: Device[]
  onClose: () => void
  onSuccess: () => void
}

function PushOtaModal({ selectedDevices, onClose, onSuccess }: PushOtaModalProps) {
  const { toast } = useToast()
  const [firmwareId, setFirmwareId] = React.useState('')
  const [pushing, setPushing] = React.useState(false)

  const { data: firmwareData, isLoading: fwLoading } = useQuery({
    queryKey: ['firmware-approved-for-push'],
    queryFn: () => firmwareService.getFirmwareList({ status: FirmwareStatus.Approved, pageSize: 200 }),
    staleTime: 60_000,
  })
  const firmwareList = firmwareData?.items ?? []

  const handlePush = async () => {
    if (!firmwareId) return
    setPushing(true)
    const results = await Promise.allSettled(
      selectedDevices.map((d) => deviceService.pushFirmware(d.id, firmwareId))
    )
    const failed = results.filter((r) => r.status === 'rejected').length
    const succeeded = results.length - failed
    setPushing(false)
    if (failed === 0) {
      toast({ title: `OTA pushed to ${succeeded} device(s)`, variant: 'success' })
    } else {
      toast({ title: `${succeeded} succeeded, ${failed} failed`, variant: 'warning' })
    }
    onSuccess()
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/40 backdrop-blur-sm" onClick={onClose} />
      <div className="relative bg-white rounded-2xl shadow-xl w-full max-w-md p-6 z-10 animate-fade-in">
        <div className="flex items-center justify-between mb-5">
          <h2 className="text-lg font-bold text-primary-900 flex items-center gap-2">
            <Rocket className="w-5 h-5 text-accent-600" />
            Push OTA to {selectedDevices.length} Device{selectedDevices.length !== 1 ? 's' : ''}
          </h2>
          <button onClick={onClose} className="p-1.5 rounded-lg text-slate-400 hover:text-slate-600 hover:bg-slate-100 transition-colors">
            <X className="w-4 h-4" />
          </button>
        </div>

        <div className="mb-5">
          <label className="label">Select Firmware Version <span className="text-danger-500">*</span></label>
          {fwLoading ? (
            <div className="flex items-center gap-2 text-sm text-slate-500 py-2">
              <Loader2 className="w-4 h-4 animate-spin" /> Loading firmware…
            </div>
          ) : (
            <select
              className="input"
              value={firmwareId}
              onChange={(e) => setFirmwareId(e.target.value)}
            >
              <option value="">Select approved firmware…</option>
              {firmwareList.map((f) => (
                <option key={f.id} value={f.id}>
                  v{f.version} — {f.channel} {f.projectName ? `(${f.projectName})` : ''}
                </option>
              ))}
            </select>
          )}
          {firmwareList.length === 0 && !fwLoading && (
            <p className="text-xs text-warning-600 mt-1.5">No approved firmware available for rollout.</p>
          )}
        </div>

        {selectedDevices.length > 0 && (
          <div className="mb-5 max-h-40 overflow-y-auto rounded-xl border border-slate-200 divide-y divide-slate-100">
            {selectedDevices.map((d) => (
              <div key={d.id} className="px-3 py-2 flex items-center gap-2 text-sm">
                <span className="w-2 h-2 rounded-full bg-success-400 flex-shrink-0" />
                <span className="font-mono text-accent-700 text-xs">{d.macImeiIp}</span>
                <span className="text-slate-400">·</span>
                <span className="text-slate-500 truncate">{d.model}</span>
              </div>
            ))}
          </div>
        )}

        <div className="flex justify-end gap-3">
          <button onClick={onClose} className="btn-secondary" disabled={pushing}>Cancel</button>
          <button
            onClick={handlePush}
            disabled={!firmwareId || pushing}
            className="btn-primary"
          >
            {pushing
              ? <><Loader2 className="w-4 h-4 animate-spin" /> Pushing…</>
              : <><Rocket className="w-4 h-4" /> Push OTA</>}
          </button>
        </div>
      </div>
    </div>
  )
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function DevicesPage() {
  const queryClient = useQueryClient()
  const { toast } = useToast()
  const { scopedProjectId, scopedProjectName, isScoped } = useProjectScope()

  const [search, setSearch] = React.useState('')
  const [statusFilter, setStatusFilter] = React.useState('')
  const [projectFilter, setProjectFilter] = React.useState('')
  const [modelFilter, setModelFilter] = React.useState('')
  const [page, setPage] = React.useState(1)
  const [pageSize, setPageSize] = React.useState(25)
  const [suspendTarget, setSuspendTarget] = React.useState<Device | null>(null)
  const [decommissionTarget, setDecommissionTarget] = React.useState<Device | null>(null)
  const [editTarget, setEditTarget] = React.useState<Device | null>(null)
  const [registerOpen, setRegisterOpen] = React.useState(false)
  const [bulkOpen, setBulkOpen] = React.useState(false)
  const [selectedIds, setSelectedIds] = React.useState<Set<string>>(new Set())
  const [pushOtaOpen, setPushOtaOpen] = React.useState(false)
  const [showFilters, setShowFilters] = React.useState(false)

  const { data, isLoading } = useQuery({
    queryKey: ['devices', { search, status: statusFilter, projectId: isScoped ? scopedProjectId : (projectFilter || undefined), model: modelFilter || undefined, page, pageSize }],
    queryFn: () => deviceService.getDevices({
      search,
      status: (statusFilter as DeviceStatus) || undefined,
      projectId: isScoped ? (scopedProjectId ?? undefined) : (projectFilter || undefined),
      model: modelFilter || undefined,
      page,
      pageSize,
    }),
    refetchInterval: (query) => {
      const items = query.state.data?.items ?? []
      const hasActiveOta = items.some((d) => d.otaStatus === 'start' || d.otaStatus === 'inprogress')
      return hasActiveOta ? 5000 : 30000
    },
    staleTime: 15_000,
  })

  const { data: projectsData } = useQuery({
    queryKey: ['projects-filter'],
    queryFn: () => projectService.getProjects({ pageSize: 200 }),
    staleTime: 300_000,
  })

  // Unique models from current data for the model filter dropdown
  const models = React.useMemo(() => {
    const set = new Set<string>()
    data?.items.forEach((d) => { if (d.model) set.add(d.model) })
    return Array.from(set).sort()
  }, [data?.items])

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
    return Date.now() - new Date(lastHeartbeat).getTime() < 5 * 60 * 1000
  }

  const currentItems = data?.items ?? []

  const toggleSelect = (id: string) => {
    setSelectedIds((prev) => {
      const next = new Set(prev)
      next.has(id) ? next.delete(id) : next.add(id)
      return next
    })
  }

  const toggleSelectAll = () => {
    const activeIds = currentItems.filter((d) => d.status === DeviceStatus.Active).map((d) => d.id)
    const allSelected = activeIds.every((id) => selectedIds.has(id))
    if (allSelected) {
      setSelectedIds(new Set())
    } else {
      setSelectedIds(new Set(activeIds))
    }
  }

  const selectedDevices = currentItems.filter((d) => selectedIds.has(d.id))
  const activeItems = currentItems.filter((d) => d.status === DeviceStatus.Active)
  const allActiveSelected = activeItems.length > 0 && activeItems.every((d) => selectedIds.has(d.id))

  const columns: Column<Device>[] = [
    {
      key: 'select',
      header: (
        <input
          type="checkbox"
          checked={allActiveSelected}
          onChange={toggleSelectAll}
          disabled={activeItems.length === 0}
          className="w-4 h-4 rounded accent-accent-600 cursor-pointer disabled:cursor-not-allowed"
          title="Select all active devices"
        />
      ) as any,
      cell: (row) => (
        <input
          type="checkbox"
          checked={selectedIds.has(row.id)}
          onChange={() => toggleSelect(row.id)}
          disabled={row.status !== DeviceStatus.Active}
          onClick={(e) => e.stopPropagation()}
          className="w-4 h-4 rounded accent-accent-600 cursor-pointer disabled:cursor-not-allowed disabled:opacity-40"
        />
      ),
    },
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
      cell: (row) => <span className="text-sm text-primary-800">{row.projectName ?? '—'}</span>,
    },
    {
      key: 'repositoryName',
      header: 'Repository',
      cell: (row) => <span className="text-sm text-primary-800">{row.repositoryName ?? '—'}</span>,
    },
    {
      key: 'model',
      header: 'Model',
      cell: (row) => <span className="text-sm font-medium text-primary-800">{row.model}</span>,
    },
    {
      key: 'firmware',
      header: 'Firmware',
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
      key: 'newVersion',
      header: 'New Version',
      cell: (row) => (
        row.pendingFirmwareVersion ? (
          <code className="text-xs bg-warning-50 px-2 py-0.5 rounded text-warning-700 font-semibold border border-warning-200">
            {formatVersion(row.pendingFirmwareVersion)}
          </code>
        ) : (
          <span className="text-xs text-slate-400">—</span>
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
          <span className={`w-2 h-2 rounded-full ${isOnline(row.lastHeartbeatAt) ? 'bg-success-500' : 'bg-slate-300'}`} />
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

  const clearFilters = () => {
    setStatusFilter('')
    setProjectFilter('')
    setModelFilter('')
    setPage(1)
  }

  const hasActiveFilters = !!(statusFilter || projectFilter || modelFilter)

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

      {isScoped && scopedProjectId && (
        <ProjectScopeBanner projectId={scopedProjectId} projectName={scopedProjectName} />
      )}

      {/* ── Filters ───────────────────────────────────────────────────────── */}
      <div className="filter-bar">
        <div className="relative flex-1 min-w-[200px] max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
          <input
            type="text"
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1) }}
            placeholder="Search by serial, model, project…"
            className="input pl-9"
          />
        </div>

        <select value={statusFilter} onChange={(e) => { setStatusFilter(e.target.value); setPage(1) }} className="input w-auto">
          <option value="">All Statuses</option>
          {Object.values(DeviceStatus).map((s) => <option key={s} value={s}>{s}</option>)}
        </select>

        <button
          onClick={() => setShowFilters(!showFilters)}
          className={`btn-secondary gap-1.5 ${hasActiveFilters ? 'border-accent-400 text-accent-700 bg-accent-50' : ''}`}
        >
          <Filter className="w-4 h-4" />
          Filters
          {hasActiveFilters && (
            <span className="w-2 h-2 rounded-full bg-accent-500" />
          )}
        </button>

        {hasActiveFilters && (
          <button onClick={clearFilters} className="btn-secondary text-xs gap-1">
            <X className="w-3.5 h-3.5" /> Clear
          </button>
        )}

        <button onClick={() => queryClient.invalidateQueries({ queryKey: ['devices'] })} className="btn-secondary">
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

      {/* ── Advanced Filters ─────────────────────────────────────────────── */}
      {showFilters && (
        <div className="bg-white rounded-xl border border-slate-200 shadow-sm p-4">
          <p className="text-xs font-semibold text-slate-500 uppercase tracking-wide mb-3">Advanced Filters</p>
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div>
              <label className="label text-xs">Project</label>
              <select
                value={projectFilter}
                onChange={(e) => { setProjectFilter(e.target.value); setPage(1) }}
                className="input"
                disabled={isScoped}
              >
                <option value="">All Projects</option>
                {(projectsData?.items ?? []).map((p) => (
                  <option key={p.id} value={p.id}>{p.name}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="label text-xs">Model</label>
              <select
                value={modelFilter}
                onChange={(e) => { setModelFilter(e.target.value); setPage(1) }}
                className="input"
              >
                <option value="">All Models</option>
                {models.map((m) => <option key={m} value={m}>{m}</option>)}
              </select>
            </div>
          </div>
        </div>
      )}

      {/* ── Bulk OTA Action Bar ───────────────────────────────────────────── */}
      {selectedIds.size > 0 && (
        <RoleGuard module="OtaRollouts" action="create">
          <div className="sticky top-[72px] z-30 bg-accent-600 rounded-xl px-5 py-3 flex items-center gap-4 shadow-lg">
            <CheckSquare className="w-5 h-5 text-white flex-shrink-0" />
            <span className="text-white font-medium text-sm flex-1">
              {selectedIds.size} device{selectedIds.size !== 1 ? 's' : ''} selected
            </span>
            <button
              onClick={() => setSelectedIds(new Set())}
              className="text-accent-200 hover:text-white text-xs font-medium transition-colors"
            >
              Clear
            </button>
            <button
              onClick={() => setPushOtaOpen(true)}
              className="flex items-center gap-2 bg-white text-accent-700 px-4 py-2 rounded-lg text-sm font-semibold hover:bg-accent-50 transition-colors shadow-sm"
            >
              <Rocket className="w-4 h-4" />
              Push OTA
            </button>
          </div>
        </RoleGuard>
      )}

      {/* ── Table ─────────────────────────────────────────────────────────── */}
      <DataTable
        columns={columns}
        data={currentItems}
        pagination={data?.pagination}
        onPageChange={setPage}
        onPageSizeChange={(size) => { setPageSize(size); setPage(1) }}
        isLoading={isLoading}
        keyExtractor={(r) => r.id}
        emptyMessage="No devices registered"
      />

      {/* Modals */}
      {pushOtaOpen && (
        <PushOtaModal
          selectedDevices={selectedDevices}
          onClose={() => setPushOtaOpen(false)}
          onSuccess={() => {
            setPushOtaOpen(false)
            setSelectedIds(new Set())
            queryClient.invalidateQueries({ queryKey: ['devices'] })
          }}
        />
      )}

      <BulkDeviceUploadForm
        open={bulkOpen}
        onOpenChange={setBulkOpen}
        onUpload={async (devices) => {
          const result = await bulkMutation.mutateAsync(devices)
          return result
        }}
        isLoading={bulkMutation.isPending}
      />

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
            repositoryId: data.repositoryId || undefined,
          })
        }}
        isLoading={registerMutation.isPending}
      />

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
