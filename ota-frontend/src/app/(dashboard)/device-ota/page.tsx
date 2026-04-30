'use client'

import * as React from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  FlaskConical, CheckCircle2, Loader2,
  Play,
  ChevronDown, Search, Inbox,
  Clock, CheckCheck, Cpu,
} from 'lucide-react'
import { projectService }    from '@/services/project.service'
import { firmwareService }   from '@/services/firmware.service'
import { deviceService }     from '@/services/device.service'
import { repositoryService } from '@/services/repository.service'
import { PageHeader }        from '@/components/ui/PageHeader'
import { StatusBadge }       from '@/components/ui/Badge'
import { useToast }          from '@/components/ui/ToastProvider'
import { RoleGuard }         from '@/components/role-access/RoleGuard'
import { OtaProgressCell }   from '@/components/ui/OtaProgressCell'
import {
  FirmwareStatus, FirmwareVersion, Device, DeviceStatus,
  ProjectClientRef, Repository, PaginatedResponse,
} from '@/types'
import { formatVersion, formatRelativeTime } from '@/utils/formatters'

type DeviceTab = 'all' | 'pending' | 'done'

// ─── Device Selection Table — 3 permanent tabs ────────────────────────────────

const BASE_COLS = 9

interface DeviceTableProps {
  devices: Device[]
  firmwareVersion: string
  isLoading: boolean
  selectedIds: Set<string>
  onToggle: (id: string) => void
  onToggleMany: (ids: string[], select: boolean) => void
  search: string
  onSearchChange: (v: string) => void
}

function DeviceSelectionTable({
  devices, firmwareVersion, isLoading, selectedIds, onToggle, onToggleMany, search, onSearchChange,
}: DeviceTableProps) {
  const [activeTab, setActiveTab] = React.useState<DeviceTab>('all')
  const isOnline = (last?: string) => !!last && Date.now() - new Date(last).getTime() < 5 * 60 * 1000

  // A device is in the Pending tab when the backend's denormalised flag says so AND
  // its current firmware version is not yet the target — independent of MQTT timing.
  const doneDevices    = devices.filter((d) => d.currentFirmwareVersion === firmwareVersion)
  const pendingDevices = devices.filter(
    (d) => d.hasActiveOtaJob === true && d.currentFirmwareVersion !== firmwareVersion
  )

  const tabDevices = activeTab === 'done'
    ? doneDevices
    : activeTab === 'pending'
    ? pendingDevices
    : devices

  const filtered = React.useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return tabDevices
    return tabDevices.filter((d) =>
      (d.macImeiIp ?? '').toLowerCase().includes(q) ||
      d.model.toLowerCase().includes(q) ||
      (d.customerName ?? '').toLowerCase().includes(q) ||
      (d.projectName ?? '').toLowerCase().includes(q) ||
      (d.currentFirmwareVersion ?? '').toLowerCase().includes(q)
    )
  }, [tabDevices, search])

  const tabs = [
    {
      key: 'all'     as DeviceTab,
      label: 'Total Devices',
      count: devices.length,
      Icon: Cpu,
      activeColor: 'text-primary-700',
      activeBorder: 'border-primary-500',
    },
    {
      key: 'pending' as DeviceTab,
      label: 'Pending OTA',
      count: pendingDevices.length,
      Icon: Clock,
      activeColor: 'text-warning-600',
      activeBorder: 'border-warning-500',
    },
    {
      key: 'done'    as DeviceTab,
      label: 'OTA Done',
      count: doneDevices.length,
      Icon: CheckCheck,
      activeColor: 'text-success-600',
      activeBorder: 'border-success-500',
    },
  ]

  const emptyMessage =
    devices.length === 0         ? 'No devices found for this project'
    : activeTab === 'done'       ? 'No devices have this firmware version yet'
    : activeTab === 'pending'    ? 'No devices have an active OTA job'
    : 'No devices match the search'

  const isSelectableTab = activeTab === 'all'
  const selectableVisibleIds = filtered
    .filter((d) => d.status === DeviceStatus.Active)
    .map((d) => d.id)
  const allVisibleSelected =
    isSelectableTab &&
    selectableVisibleIds.length > 0 &&
    selectableVisibleIds.every((id) => selectedIds.has(id))
  const someVisibleSelected =
    isSelectableTab &&
    selectableVisibleIds.some((id) => selectedIds.has(id)) &&
    !allVisibleSelected

  return (
    <div className="rounded-xl border border-slate-200 overflow-hidden">

      {/* ── 3 Tabs ────────────────────────────────────────────────────── */}
      <div className="flex border-b border-slate-200 bg-white">
        {tabs.map(({ key, label, count, Icon, activeColor, activeBorder }) => (
          <button
            key={key}
            onClick={() => setActiveTab(key)}
            className={`flex items-center gap-2 px-5 py-3 text-sm font-medium border-b-2 transition-colors flex-1 justify-center
              ${activeTab === key
                ? `${activeBorder} ${activeColor} bg-slate-50`
                : 'border-transparent text-slate-500 hover:text-slate-700 hover:bg-slate-50'}`}
          >
            <Icon className={`w-4 h-4 ${activeTab === key ? activeColor : 'text-slate-400'}`} />
            {label}
            <span className={`text-xs font-bold px-2 py-0.5 rounded-full
              ${activeTab === key ? 'bg-white border border-slate-200 text-primary-800' : 'bg-slate-100 text-slate-500'}`}>
              {count}
            </span>
          </button>
        ))}
      </div>

      {/* ── Search bar ────────────────────────────────────────────────── */}
      <div className="flex items-center gap-3 px-4 py-3 bg-slate-50 border-b border-slate-200">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
          <input
            type="text"
            value={search}
            onChange={(e) => onSearchChange(e.target.value)}
            placeholder="Search by MAC / IMEI / IP, model, project, customer…"
            className="input pl-9 py-1.5 text-sm"
          />
        </div>
        {selectedIds.size > 0 && (
          <span className="text-xs font-medium text-accent-700 bg-accent-50 border border-accent-200 px-2.5 py-1 rounded-full whitespace-nowrap">
            {selectedIds.size} device{selectedIds.size === 1 ? '' : 's'} selected
          </span>
        )}
      </div>

      {/* ── Table ─────────────────────────────────────────────────────── */}
      <div className="overflow-x-auto max-h-[360px] overflow-y-auto">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 border-b border-slate-200 sticky top-0 z-10">
            <tr>
              <th className="px-4 py-3 w-10">
                {isSelectableTab && (
                  <input
                    type="checkbox"
                    aria-label="Select all visible"
                    checked={allVisibleSelected}
                    ref={(el) => { if (el) el.indeterminate = someVisibleSelected }}
                    onChange={() => onToggleMany(selectableVisibleIds, !allVisibleSelected)}
                    disabled={selectableVisibleIds.length === 0}
                    className="w-4 h-4 rounded accent-accent-600 cursor-pointer disabled:cursor-not-allowed"
                  />
                )}
              </th>
              {[
                'MAC / IMEI / IP', 'Project', 'Model', 'Current FW',
                ...(activeTab === 'pending' ? ['New FW'] : []),
                'OTA Progress', 'OTA Status', 'Device Status', 'Connectivity',
              ].map((h) => (
                <th key={h} className="px-4 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wide whitespace-nowrap">{h}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {isLoading ? (
              Array.from({ length: 5 }).map((_, i) => (
                <tr key={i} className="border-b border-slate-100">
                  {Array.from({ length: BASE_COLS + (activeTab === 'pending' ? 1 : 0) }).map((_, j) => (
                    <td key={j} className="px-4 py-3">
                      <div className="h-4 bg-slate-200 rounded animate-pulse" style={{ width: `${55 + (j * 7) % 35}%` }} />
                    </td>
                  ))}
                </tr>
              ))
            ) : filtered.length === 0 ? (
              <tr>
                <td colSpan={BASE_COLS + (activeTab === 'pending' ? 1 : 0)} className="px-4 py-12 text-center">
                  <div className="flex flex-col items-center gap-2">
                    <Inbox className="w-9 h-9 text-slate-300" />
                    <p className="text-sm text-slate-400 font-medium">{emptyMessage}</p>
                  </div>
                </td>
              </tr>
            ) : (
              filtered.map((device) => {
                const isSelected   = selectedIds.has(device.id)
                const isActive    = device.status === DeviceStatus.Active
                const isOtaDone   = device.currentFirmwareVersion === firmwareVersion
                const hasActiveJob = device.hasActiveOtaJob === true && !isOtaDone
                const canSelect   = isSelectableTab && isActive
                return (
                  <tr
                    key={device.id}
                    onClick={() => canSelect && onToggle(device.id)}
                    className={`border-b border-slate-100 last:border-b-0 transition-colors duration-100
                      ${canSelect ? 'cursor-pointer' : !isActive ? 'opacity-40 cursor-not-allowed' : 'cursor-default'}
                      ${isSelected ? 'bg-accent-50 hover:bg-accent-50' : canSelect ? 'hover:bg-slate-50' : ''}`}
                  >
                    <td className="px-4 py-3 w-10">
                      <input
                        type="checkbox"
                        checked={isSelected}
                        disabled={!canSelect}
                        onChange={() => canSelect && onToggle(device.id)}
                        onClick={(e) => e.stopPropagation()}
                        className="w-4 h-4 rounded accent-accent-600 cursor-pointer disabled:cursor-not-allowed"
                      />
                    </td>
                    <td className="px-4 py-3">
                      <span className={`font-mono text-sm font-semibold ${isSelected ? 'text-accent-700' : 'text-accent-600'}`}>
                        {device.macImeiIp ?? '—'}
                      </span>
                      {device.customerName && <p className="text-xs text-slate-500 mt-0.5">{device.customerName}</p>}
                    </td>
                    <td className="px-4 py-3 text-sm text-primary-800">{device.projectName ?? '—'}</td>
                    <td className="px-4 py-3 text-sm font-medium text-primary-800">{device.model}</td>
                    <td className="px-4 py-3">
                      {device.currentFirmwareVersion
                        ? <code className="text-xs bg-slate-100 px-2 py-0.5 rounded text-accent-700 font-semibold">{formatVersion(device.currentFirmwareVersion)}</code>
                        : <span className="text-xs text-slate-400">0.0.0</span>}
                    </td>
                    {activeTab === 'pending' && (
                      <td className="px-4 py-3">
                        {device.pendingFirmwareVersion
                          ? <code className="text-xs bg-warning-50 px-2 py-0.5 rounded text-warning-700 font-semibold border border-warning-200">{formatVersion(device.pendingFirmwareVersion)}</code>
                          : <span className="text-xs text-slate-400">—</span>}
                      </td>
                    )}
                    <td className="px-4 py-3"><OtaProgressCell device={device} /></td>
                    <td className="px-4 py-3">
                      {isOtaDone ? (
                        <span className="inline-flex items-center gap-1.5 text-xs font-semibold px-2.5 py-1 rounded-full bg-success-50 text-success-700 border border-success-200">
                          <CheckCheck className="w-3 h-3" /> Done
                        </span>
                      ) : hasActiveJob ? (
                        <span className="inline-flex items-center gap-1.5 text-xs font-semibold px-2.5 py-1 rounded-full bg-warning-50 text-warning-700 border border-warning-200">
                          <Clock className="w-3 h-3" /> Pending
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1.5 text-xs font-semibold px-2.5 py-1 rounded-full bg-slate-100 text-slate-500 border border-slate-200">
                          Ready
                        </span>
                      )}
                    </td>
                    <td className="px-4 py-3"><StatusBadge status={device.status} dot /></td>
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-2">
                        <span className={`w-2 h-2 rounded-full flex-shrink-0 ${isOnline(device.lastHeartbeatAt) ? 'bg-success-500' : 'bg-slate-300'}`} />
                        <span className="text-xs text-slate-500 whitespace-nowrap">
                          {device.lastHeartbeatAt ? formatRelativeTime(device.lastHeartbeatAt) : 'Never'}
                        </span>
                      </div>
                    </td>
                  </tr>
                )
              })
            )}
          </tbody>
        </table>
      </div>

      {!isLoading && devices.length > 0 && (
        <div className="px-4 py-2 border-t border-slate-100 bg-slate-50 text-xs text-slate-400 flex items-center gap-3">
          <span>{filtered.length} of {tabDevices.length} shown</span>
          <span className="text-slate-300">·</span>
          <span className="text-success-600 font-medium">{doneDevices.length} done</span>
          <span className="text-slate-300">·</span>
          <span className="text-warning-600 font-medium">{pendingDevices.length} in progress</span>
          {selectedIds.size === 0 && isSelectableTab && (
            <span className="ml-auto text-amber-600 font-medium">Select one or more devices from Total Devices to push OTA</span>
          )}
          {!isSelectableTab && (
            <span className="ml-auto text-slate-400 italic">Read-only view — switch to Total Devices to select</span>
          )}
        </div>
      )}
    </div>
  )
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function DeviceOtaPage() {
  const { toast }   = useToast()
  const queryClient = useQueryClient()

  // ── Configure selections ──────────────────────────────────────────────────
  const [selectedProjectId,    setSelectedProjectId]    = React.useState('')
  const [projectClients,       setProjectClients]       = React.useState<ProjectClientRef[]>([])
  const [selectedClientCode,   setSelectedClientCode]   = React.useState('')
  const [selectedRepositoryId, setSelectedRepositoryId] = React.useState('')
  const [selectedFirmware,     setSelectedFirmware]     = React.useState<FirmwareVersion | null>(null)
  const [selectedDeviceIds,    setSelectedDeviceIds]    = React.useState<Set<string>>(new Set())
  const [deviceSearch,         setDeviceSearch]         = React.useState('')

  const toggleDeviceId = React.useCallback((id: string) => {
    setSelectedDeviceIds((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id); else next.add(id)
      return next
    })
  }, [])

  const toggleManyDeviceIds = React.useCallback((ids: string[], select: boolean) => {
    setSelectedDeviceIds((prev) => {
      const next = new Set(prev)
      ids.forEach((id) => { if (select) next.add(id); else next.delete(id) })
      return next
    })
  }, [])

  // ── Data fetches ──────────────────────────────────────────────────────────

  const { data: projectsData, isLoading: projectsLoading } = useQuery({
    queryKey: ['projects-device-ota'],
    queryFn: () => projectService.getProjects({ isActive: true, pageSize: 200 }),
  })
  const projects = projectsData?.items ?? []

  const { data: repositoriesData, isLoading: repositoriesLoading } = useQuery({
    queryKey: ['repositories-device-ota', selectedProjectId],
    queryFn: () => repositoryService.getRepositories({ projectId: selectedProjectId, isActive: true, pageSize: 200 }),
    enabled: !!selectedProjectId && !!selectedClientCode,
  })
  const repositories: Repository[] = repositoriesData?.items ?? []

  const { data: firmwareData, isLoading: firmwareLoading } = useQuery({
    queryKey: ['firmware-device-ota', selectedProjectId, selectedRepositoryId],
    queryFn: () => firmwareService.getFirmwareList({
      projectId: selectedProjectId,
      repositoryId: selectedRepositoryId || undefined,
      status: FirmwareStatus.Approved,
      pageSize: 200,
    }),
    enabled: !!selectedProjectId && !!selectedRepositoryId,
    staleTime: 60_000,
  })
  const firmwareList = firmwareData?.items ?? []

  // Devices load as soon as firmware is selected
  const { data: devicesData, isLoading: devicesLoading } = useQuery({
    queryKey: ['devices-device-ota', selectedProjectId],
    queryFn: () => deviceService.getDevices({
      projectId: selectedProjectId || undefined,
      pageSize: 500,
    }),
    enabled: !!selectedFirmware,
    staleTime: 0,
    refetchInterval: 30_000,
  })
  const devices = devicesData?.items ?? []

  // ── Mutations ─────────────────────────────────────────────────────────────

  const startOtaMutation = useMutation({
    mutationFn: async () => {
      if (selectedDeviceIds.size === 0 || !selectedFirmware)
        throw new Error('Select at least one device and a firmware.')

      const ids = Array.from(selectedDeviceIds)
      const results = await Promise.allSettled(
        ids.map((id) => deviceService.pushFirmware(id, selectedFirmware.id))
      )

      const succeededIds: string[] = []
      const failures: { deviceId: string; reason: string }[] = []
      results.forEach((r, i) => {
        if (r.status === 'fulfilled') succeededIds.push(ids[i])
        else failures.push({
          deviceId: ids[i],
          reason: (r.reason as any)?.response?.data?.message ?? (r.reason as any)?.message ?? 'Push failed',
        })
      })
      return { succeededIds, failures }
    },
    onSuccess: ({ succeededIds, failures }) => {
      // Clear the selection so the table reflects the post-push state and the user can
      // queue another batch. The Pending OTA tab will pick the devices up automatically.
      setSelectedDeviceIds(new Set())

      // Optimistically flip hasActiveOtaJob=true on the cached device list so the
      // pushed devices jump into the Pending OTA tab immediately, without waiting
      // for the next refetch to round-trip.
      if (succeededIds.length > 0) {
        const succeededSet = new Set(succeededIds)
        queryClient.setQueryData(
          ['devices-device-ota', selectedProjectId],
          (old: PaginatedResponse<Device> | undefined) => {
            if (!old) return old
            return {
              ...old,
              items: old.items.map((d) =>
                succeededSet.has(d.id) ? { ...d, hasActiveOtaJob: true } : d
              ),
            }
          }
        )
      }
      // Then trigger a refetch so the cache catches up to authoritative server state.
      queryClient.invalidateQueries({ queryKey: ['devices-device-ota', selectedProjectId] })

      if (failures.length === 0) {
        toast({ title: `OTA started on ${succeededIds.length} device(s)`, variant: 'success' })
      } else if (succeededIds.length === 0) {
        const firstReason = failures[0]?.reason ?? 'Push failed'
        toast({
          title: `Failed to start OTA on ${failures.length} device(s)`,
          description: firstReason,
          variant: 'error',
        })
      } else {
        toast({
          title: `OTA started on ${succeededIds.length} device(s)`,
          description: `${failures.length} push${failures.length === 1 ? '' : 'es'} failed`,
          variant: 'warning',
        })
      }
    },
    onError: (e: any) => {
      const msg = e?.response?.data?.message ?? e?.message ?? 'Failed to start OTA'
      toast({ title: msg, variant: 'error' })
    },
  })

  // ── Render ────────────────────────────────────────────────────────────────

  return (
    <RoleGuard module="OtaRollouts" action="execute">
      <div className="space-y-6 animate-fade-in">
        <PageHeader
          title="Device OTA"
          subtitle="Push a firmware update to one or more devices"
          breadcrumbs={[
            { label: 'Dashboard',    href: '/dashboard'    },
            { label: 'OTA Rollouts', href: '/ota-rollouts' },
            { label: 'Device OTA'                          },
          ]}
        />

        {/* ── Configure card ──────────────────────────────────────────────── */}
        <div className="bg-white rounded-2xl border border-slate-200 shadow-sm">
          <div className="px-6 py-4 border-b border-slate-100">
            <h3 className="font-semibold text-primary-900 flex items-center gap-2">
              <FlaskConical className="w-4 h-4 text-accent-600" />
              Configure
            </h3>
            <p className="text-sm text-slate-500 mt-0.5">
              Select a project, client, repository and firmware — then pick a device.
            </p>
          </div>

          <div className="p-6 space-y-5">

              {/* Step 1–3 ── Project + Client + Repository */}
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                {/* Project */}
                <div>
                  <label className="label">
                    <span className="inline-flex items-center gap-1.5">
                      <span className="w-5 h-5 rounded-full bg-accent-600 text-white text-[10px] font-bold flex items-center justify-center flex-shrink-0">1</span>
                      Project <span className="text-danger-500">*</span>
                    </span>
                  </label>
                  <div className="relative">
                    <select
                      className="input appearance-none pr-9"
                      value={selectedProjectId}
                      disabled={projectsLoading}
                      onChange={(e) => {
                        const matched = projects.find((p) => p.id === e.target.value)
                        setSelectedProjectId(e.target.value)
                        setProjectClients(matched?.clients ?? [])
                        setSelectedClientCode('')
                        setSelectedRepositoryId('')
                        setSelectedFirmware(null)
                        setSelectedDeviceIds(new Set())
                        setDeviceSearch('')
                      }}
                    >
                      <option value="">{projectsLoading ? 'Loading…' : 'Select a project'}</option>
                      {projects.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
                    </select>
                    <span className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-slate-400">
                      {projectsLoading ? <Loader2 className="w-4 h-4 animate-spin" /> : <ChevronDown className="w-4 h-4" />}
                    </span>
                  </div>
                </div>

                {/* Client */}
                <div>
                  <label className="label">
                    <span className="inline-flex items-center gap-1.5">
                      <span className="w-5 h-5 rounded-full bg-accent-600 text-white text-[10px] font-bold flex items-center justify-center flex-shrink-0">2</span>
                      Client <span className="text-danger-500">*</span>
                    </span>
                  </label>
                  <div className="relative">
                    <select
                      className="input appearance-none pr-9"
                      value={selectedClientCode}
                      disabled={!selectedProjectId}
                      onChange={(e) => {
                        setSelectedClientCode(e.target.value)
                        setSelectedRepositoryId('')
                        setSelectedFirmware(null)
                        setSelectedDeviceIds(new Set())
                        setDeviceSearch('')
                      }}
                    >
                      <option value="">
                        {!selectedProjectId ? 'Select a project first' : projectClients.length === 0 ? 'No clients' : 'Select a client'}
                      </option>
                      {projectClients.map((c) => <option key={c.code} value={c.code}>{c.name} — {c.code}</option>)}
                    </select>
                    <span className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-slate-400">
                      <ChevronDown className="w-4 h-4" />
                    </span>
                  </div>
                </div>

                {/* Repository */}
                <div>
                  <label className="label">
                    <span className="inline-flex items-center gap-1.5">
                      <span className="w-5 h-5 rounded-full bg-accent-600 text-white text-[10px] font-bold flex items-center justify-center flex-shrink-0">3</span>
                      Repository <span className="text-danger-500">*</span>
                    </span>
                  </label>
                  <div className="relative">
                    <select
                      className="input appearance-none pr-9"
                      value={selectedRepositoryId}
                      disabled={!selectedClientCode || repositoriesLoading}
                      onChange={(e) => {
                        setSelectedRepositoryId(e.target.value)
                        setSelectedFirmware(null)
                        setSelectedDeviceIds(new Set())
                        setDeviceSearch('')
                      }}
                    >
                      <option value="">
                        {!selectedClientCode ? 'Select a client first' : repositoriesLoading ? 'Loading…' : repositories.length === 0 ? 'No repositories' : 'Select a repository'}
                      </option>
                      {repositories.map((r) => <option key={r.id} value={r.id}>{r.name}</option>)}
                    </select>
                    <span className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-slate-400">
                      {repositoriesLoading && selectedClientCode ? <Loader2 className="w-4 h-4 animate-spin" /> : <ChevronDown className="w-4 h-4" />}
                    </span>
                  </div>
                </div>
              </div>

              {/* Step 4 ── Firmware Version (visible after repository is selected) */}
              {selectedRepositoryId && (
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div>
                    <label className="label">
                      <span className="inline-flex items-center gap-1.5">
                        <span className="w-5 h-5 rounded-full bg-accent-600 text-white text-[10px] font-bold flex items-center justify-center flex-shrink-0">4</span>
                        Firmware Version <span className="text-danger-500">*</span>
                      </span>
                    </label>
                    <div className="relative">
                      <select
                        className="input appearance-none pr-9"
                        value={selectedFirmware?.id ?? ''}
                        disabled={firmwareLoading}
                        onChange={(e) => {
                          setSelectedFirmware(firmwareList.find((f) => f.id === e.target.value) ?? null)
                          setSelectedDeviceIds(new Set())
                          setDeviceSearch('')
                        }}
                      >
                        <option value="">
                          {firmwareLoading
                            ? 'Loading…'
                            : firmwareList.length === 0
                            ? 'No approved firmware available'
                            : 'Select firmware version'}
                        </option>
                        {firmwareList.map((f) => (
                          <option key={f.id} value={f.id}>v{f.version} — {f.channel}</option>
                        ))}
                      </select>
                      <span className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-slate-400">
                        {firmwareLoading ? <Loader2 className="w-4 h-4 animate-spin" /> : <ChevronDown className="w-4 h-4" />}
                      </span>
                    </div>
                    {selectedFirmware?.releaseNotes && (
                      <p className="text-xs text-slate-400 mt-1.5 truncate">{selectedFirmware.releaseNotes}</p>
                    )}
                  </div>
                </div>
              )}

              {/* Step 5 ── Device list with 3 tabs (visible after firmware is selected) */}
              {selectedFirmware && (
                <div>
                  <div className="flex items-center justify-between mb-2">
                    <label className="label mb-0">
                      <span className="inline-flex items-center gap-1.5">
                        <span className="w-5 h-5 rounded-full bg-accent-600 text-white text-[10px] font-bold flex items-center justify-center flex-shrink-0">5</span>
                        Select Device <span className="text-danger-500">*</span>
                      </span>
                    </label>
                    <span className="text-xs text-slate-400">Only Active devices can be selected</span>
                  </div>
                  <DeviceSelectionTable
                    devices={devices}
                    firmwareVersion={selectedFirmware.version}
                    isLoading={devicesLoading}
                    selectedIds={selectedDeviceIds}
                    onToggle={toggleDeviceId}
                    onToggleMany={toggleManyDeviceIds}
                    search={deviceSearch}
                    onSearchChange={setDeviceSearch}
                  />
                </div>
              )}

              {/* Start OTA — shown when at least one device is selected */}
              {selectedDeviceIds.size > 0 && selectedFirmware && (
                <div className="flex items-center justify-between gap-4 p-4 bg-accent-50 border border-accent-200 rounded-xl">
                  <div className="flex items-center gap-3 text-sm min-w-0">
                    <CheckCircle2 className="w-4 h-4 text-accent-600 flex-shrink-0" />
                    <span className="text-accent-800 text-xs truncate">
                      <strong>{selectedDeviceIds.size}</strong> device{selectedDeviceIds.size === 1 ? '' : 's'} selected
                      {' → '}<code className="bg-accent-200 px-1 rounded font-semibold">v{selectedFirmware.version}</code>
                    </span>
                  </div>
                  <button
                    onClick={() => startOtaMutation.mutate()}
                    disabled={startOtaMutation.isPending}
                    className="btn-primary flex-shrink-0"
                  >
                    {startOtaMutation.isPending
                      ? <><Loader2 className="w-4 h-4 animate-spin" /> Starting…</>
                      : <><Play className="w-4 h-4" /> Start OTA</>}
                  </button>
                </div>
              )}
          </div>
        </div>
      </div>
    </RoleGuard>
  )
}
