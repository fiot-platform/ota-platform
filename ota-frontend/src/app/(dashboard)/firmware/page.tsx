'use client'

import * as React from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Search, RefreshCw, Eye, CheckCircle, Archive, Plus, Pencil, Trash2 } from 'lucide-react'
import Link from 'next/link'
import { firmwareService } from '@/services/firmware.service'
import { projectService } from '@/services/project.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { DataTable, Column } from '@/components/ui/DataTable'
import { StatusBadge } from '@/components/ui/Badge'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
import { RoleGuard } from '@/components/role-access/RoleGuard'
import { ApproveFirmwareDialog } from '@/components/dialogs/ApproveFirmwareDialog'
import { CreateFirmwareForm, EditFirmwareForm } from '@/components/forms/FirmwareForm'
import { useToast } from '@/components/ui/ToastProvider'
import { FirmwareVersion, FirmwareStatus, FirmwareChannel, CreateFirmwareRequest, UpdateFirmwareRequest } from '@/types'
import { formatFileSize, formatDate } from '@/utils/formatters'
import { UserRole } from '@/types'
import { useAuth } from '@/hooks/useAuth'

export default function FirmwarePage() {
  const queryClient = useQueryClient()
  const { toast } = useToast()
  const { role } = useAuth()
  const canFilterByProject = role === UserRole.SuperAdmin || role === UserRole.PlatformAdmin
  const canSeeAllStatuses = role === UserRole.SuperAdmin || role === UserRole.PlatformAdmin
    || role === UserRole.ReleaseManager || role === UserRole.QA

  const [search, setSearch] = React.useState('')
  const [statusFilter, setStatusFilter] = React.useState(() =>
    (role === UserRole.SuperAdmin || role === UserRole.PlatformAdmin
      || role === UserRole.ReleaseManager || role === UserRole.QA)
      ? '' : FirmwareStatus.Approved
  )
  const [channelFilter, setChannelFilter] = React.useState('')
  const [projectFilter, setProjectFilter] = React.useState('')
  const [page, setPage] = React.useState(1)
  const [pageSize, setPageSize] = React.useState(25)

  const [createOpen, setCreateOpen] = React.useState(false)
  const [editTarget, setEditTarget] = React.useState<FirmwareVersion | null>(null)
  const [approveTarget, setApproveTarget] = React.useState<FirmwareVersion | null>(null)
  const [deprecateTarget, setDeprecateTarget] = React.useState<FirmwareVersion | null>(null)
  const [deleteTarget, setDeleteTarget] = React.useState<FirmwareVersion | null>(null)

  const { data, isLoading } = useQuery({
    queryKey: ['firmware', { search, status: statusFilter, channel: channelFilter, projectId: projectFilter, page, pageSize }],
    queryFn: () => firmwareService.getFirmwareList({
      search,
      status: (statusFilter as FirmwareStatus) || undefined,
      channel: (channelFilter as FirmwareChannel) || undefined,
      projectId: projectFilter || undefined,
      page,
      pageSize,
    }),
  })

  const { data: projects } = useQuery({
    queryKey: ['projects-all'],
    queryFn: () => projectService.getProjects({ pageSize: 200 }),
  })

  const createMutation = useMutation({
    mutationFn: (data: CreateFirmwareRequest) => firmwareService.createFirmware(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['firmware'] })
      toast({ title: 'Firmware created', description: 'New firmware version added in Draft status.', variant: 'success' })
      setCreateOpen(false)
    },
    onError: (err: any) => {
      toast({ title: 'Failed to create firmware', description: err?.response?.data?.message ?? 'Unknown error', variant: 'error' })
    },
  })

  const editMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateFirmwareRequest }) =>
      firmwareService.updateFirmware(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['firmware'] })
      toast({ title: 'Firmware updated', variant: 'success' })
      setEditTarget(null)
    },
    onError: (err: any) => {
      toast({ title: 'Failed to update firmware', description: err?.response?.data?.message ?? 'Unknown error', variant: 'error' })
    },
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => firmwareService.deleteFirmware(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['firmware'] })
      toast({ title: 'Firmware deleted', variant: 'success' })
      setDeleteTarget(null)
    },
    onError: (err: any) => toast({ title: 'Failed to delete firmware', description: err?.response?.data?.message ?? 'Only Draft and Rejected firmware can be deleted', variant: 'error' }),
  })

  const deprecateMutation = useMutation({
    mutationFn: (id: string) => firmwareService.deprecateFirmware(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['firmware'] })
      toast({ title: 'Firmware deprecated', variant: 'success' })
      setDeprecateTarget(null)
    },
    onError: () => toast({ title: 'Failed to deprecate firmware', variant: 'error' }),
  })

  // Editable statuses (not yet approved/deprecated/rejected)
  const isEditable = (fw: FirmwareVersion) =>
    [FirmwareStatus.Draft, FirmwareStatus.PendingQA].includes(fw.status as FirmwareStatus)

  const columns: Column<FirmwareVersion>[] = [
    {
      key: 'version',
      header: 'Version',
      cell: (row) => (
        <div>
          <Link
            href={`/firmware/${row.id}`}
            className="font-mono font-bold text-accent-600 hover:text-accent-700 transition-colors text-sm"
          >
            {row.version}
          </Link>
          <p className="text-xs text-slate-500 mt-0.5">{row.repositoryName ?? row.repositoryId}</p>
        </div>
      ),
    },
    {
      key: 'project',
      header: 'Project',
      cell: (row) => (
        <span className="text-sm text-primary-700">{row.projectName ?? '—'}</span>
      ),
    },
    {
      key: 'models',
      header: 'Models',
      cell: (row) => {
        const models = row.supportedModels ?? []
        if (models.length === 0) return <span className="text-sm text-slate-400">—</span>
        return (
          <div className="flex flex-wrap gap-1">
            {models.slice(0, 2).map((m) => (
              <span key={m} className="inline-block px-1.5 py-0.5 bg-slate-100 text-slate-600 rounded text-xs font-mono">
                {m}
              </span>
            ))}
            {models.length > 2 && (
              <span className="inline-block px-1.5 py-0.5 bg-slate-100 text-slate-500 rounded text-xs" title={models.slice(2).join(', ')}>
                +{models.length - 2}
              </span>
            )}
          </div>
        )
      },
    },
    {
      key: 'channel',
      header: 'Channel',
      cell: (row) => <StatusBadge status={row.channel} />,
    },
    {
      key: 'status',
      header: 'Status',
      cell: (row) => <StatusBadge status={row.status} dot />,
    },
    {
      key: 'qa',
      header: 'QA',
      cell: (row) => {
        if (row.qaSessionStatus) {
          const s = row.qaSessionStatus
          const label = s.replace(/([A-Z])/g, ' $1').trim()
          const cls =
            s === 'Complete'        ? 'bg-success-100 text-success-700' :
            s === 'Fail'            ? 'bg-danger-100 text-danger-600' :
            s === 'BugListRaised'   ? 'bg-warning-100 text-warning-700' :
            s === 'InProgress'      ? 'bg-accent-100 text-accent-700' :
                                      'bg-slate-100 text-slate-500'
          return <span className={`text-[11px] font-semibold px-2 py-0.5 rounded-full ${cls}`}>{label}</span>
        }
        return <span className="text-[11px] font-semibold px-2 py-0.5 rounded-full bg-slate-100 text-slate-400">Not Started</span>
      },
    },
    {
      key: 'fileSize',
      header: 'File Size',
      cell: (row) => (
        <span className="text-sm text-slate-500">{row.fileSizeBytes ? formatFileSize(row.fileSizeBytes) : '—'}</span>
      ),
    },
    {
      key: 'createdAt',
      header: 'Created',
      cell: (row) => <span className="text-sm text-slate-500">{formatDate(row.createdAt)}</span>,
    },
    {
      key: 'actions',
      header: 'Actions',
      cell: (row) => (
        <div className="flex items-center gap-1">
          <Link
            href={`/firmware/${row.id}`}
            className="p-1.5 rounded-lg text-slate-400 hover:text-accent-600 hover:bg-accent-50 transition-colors"
            title="View"
          >
            <Eye className="w-4 h-4" />
          </Link>

          <RoleGuard module="Firmware" action="update" roles={[UserRole.ReleaseManager, UserRole.PlatformAdmin, UserRole.SuperAdmin]}>
            {isEditable(row) && (
              <button
                onClick={() => setEditTarget(row)}
                className="p-1.5 rounded-lg text-slate-400 hover:text-accent-600 hover:bg-accent-50 transition-colors"
                title="Edit"
              >
                <Pencil className="w-4 h-4" />
              </button>
            )}
          </RoleGuard>

          <RoleGuard module="Firmware" action="approve" roles={[UserRole.ReleaseManager, UserRole.PlatformAdmin, UserRole.SuperAdmin]}>
            {[FirmwareStatus.PendingApproval, FirmwareStatus.QAVerified].includes(row.status as FirmwareStatus) && (
              <button
                onClick={() => setApproveTarget(row)}
                className="p-1.5 rounded-lg text-slate-400 hover:text-success-600 hover:bg-success-50 transition-colors"
                title="Approve"
              >
                <CheckCircle className="w-4 h-4" />
              </button>
            )}
          </RoleGuard>

          <RoleGuard module="Firmware" action="update" roles={[UserRole.ReleaseManager, UserRole.PlatformAdmin, UserRole.SuperAdmin]}>
            {row.status === FirmwareStatus.Approved && (
              <button
                onClick={() => setDeprecateTarget(row)}
                className="p-1.5 rounded-lg text-slate-400 hover:text-warning-600 hover:bg-warning-50 transition-colors"
                title="Deprecate"
              >
                <Archive className="w-4 h-4" />
              </button>
            )}
          </RoleGuard>

          <RoleGuard module="Firmware" action="delete">
            {[FirmwareStatus.Draft, FirmwareStatus.Rejected, FirmwareStatus.Deprecated].includes(row.status as FirmwareStatus) && (
              <button
                onClick={() => setDeleteTarget(row)}
                className="p-1.5 rounded-lg text-slate-400 hover:text-danger-600 hover:bg-danger-50 transition-colors"
                title="Delete"
              >
                <Trash2 className="w-4 h-4" />
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
        title="Firmware Versions"
        subtitle="Manage firmware lifecycle — from draft to deployment"
        breadcrumbs={[{ label: 'Dashboard', href: '/dashboard' }, { label: 'Firmware' }]}
        actions={
          <RoleGuard module="Firmware" action="create" roles={[UserRole.ReleaseManager, UserRole.PlatformAdmin, UserRole.SuperAdmin]}>
            <button onClick={() => setCreateOpen(true)} className="btn-primary">
              <Plus className="w-4 h-4" />
              Add Firmware
            </button>
          </RoleGuard>
        }
      />

      {/* Filters */}
      <div className="filter-bar">
        <div className="relative flex-1 min-w-[180px] max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
          <input
            type="text"
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1) }}
            placeholder="Search firmware..."
            className="input pl-9"
          />
        </div>

        {canSeeAllStatuses ? (
          <select
            value={statusFilter}
            onChange={(e) => { setStatusFilter(e.target.value); setPage(1) }}
            className="input w-auto"
          >
            <option value="">All Statuses</option>
            {Object.values(FirmwareStatus).filter(s => s !== FirmwareStatus.Active).map((s) => (
              <option key={s} value={s}>{s.replace(/([A-Z])/g, ' $1').trim()}</option>
            ))}
          </select>
        ) : (
          <span className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-success-50 border border-success-200 text-xs font-semibold text-success-700">
            <CheckCircle className="w-3.5 h-3.5" />
            Approved Only
          </span>
        )}

        <select
          value={channelFilter}
          onChange={(e) => { setChannelFilter(e.target.value); setPage(1) }}
          className="input w-auto"
        >
          <option value="">All Channels</option>
          {Object.values(FirmwareChannel).map((c) => (
            <option key={c} value={c}>{c}</option>
          ))}
        </select>

        {canFilterByProject && (
          <select
            value={projectFilter}
            onChange={(e) => { setProjectFilter(e.target.value); setPage(1) }}
            className="input w-auto"
          >
            <option value="">All Projects</option>
            {(projects?.items ?? []).map((p) => (
              <option key={p.id} value={p.id}>{p.name}</option>
            ))}
          </select>
        )}

        <button
          onClick={() => queryClient.invalidateQueries({ queryKey: ['firmware'] })}
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
        emptyMessage="No firmware versions found"
      />

      {/* Create Firmware */}
      <CreateFirmwareForm
        open={createOpen}
        onOpenChange={setCreateOpen}
        isLoading={createMutation.isPending}
        onSubmit={async (values) => {
          await createMutation.mutateAsync({
            repositoryId: values.repositoryId,
            version: values.version,
            giteaTagName: values.giteaTagName || undefined,
            channel: values.channel,
            releaseNotes: values.releaseNotes || undefined,
            fileName: values.fileName || undefined,
            storedFileName: values.storedFileName || undefined,
            fileSha256: values.fileSha256 || undefined,
            fileSizeBytes: values.fileSizeBytes || 0,
            isMandate: values.isMandate,
            minRequiredVersion: values.minRequiredVersion || undefined,
            maxAllowedVersion: values.maxAllowedVersion || undefined,
            supportedModels: values.supportedModels ?? [],
          })
        }}
      />

      {/* Edit Firmware */}
      {editTarget && (
        <EditFirmwareForm
          open={Boolean(editTarget)}
          onOpenChange={(open) => !open && setEditTarget(null)}
          firmware={editTarget}
          isLoading={editMutation.isPending}
          onSubmit={async (values) => {
            await editMutation.mutateAsync({
              id: editTarget.id,
              data: {
                releaseNotes: values.releaseNotes || undefined,
                isMandate: values.isMandate,
                minRequiredVersion: values.minRequiredVersion || undefined,
                maxAllowedVersion: values.maxAllowedVersion || undefined,
                supportedModels: values.supportedModels,
              },
            })
          }}
        />
      )}

      {/* Approve */}
      <ApproveFirmwareDialog
        open={Boolean(approveTarget)}
        onOpenChange={(open) => !open && setApproveTarget(null)}
        firmwareId={approveTarget?.id ?? ''}
        firmwareVersion={approveTarget?.version ?? ''}
      />

      {/* Deprecate */}
      <ConfirmDialog
        open={Boolean(deprecateTarget)}
        onOpenChange={(open) => !open && setDeprecateTarget(null)}
        title="Deprecate Firmware"
        message={`Deprecate "${deprecateTarget?.version}"? Deprecated firmware cannot be used in new rollouts.`}
        confirmLabel="Deprecate"
        variant="warning"
        onConfirm={() => deprecateTarget && deprecateMutation.mutate(deprecateTarget.id)}
        isLoading={deprecateMutation.isPending}
      />

      {/* Delete */}
      <ConfirmDialog
        open={Boolean(deleteTarget)}
        onOpenChange={(open) => !open && setDeleteTarget(null)}
        title="Delete Firmware"
        message={`Permanently delete firmware "${deleteTarget?.version}"? This cannot be undone.`}
        confirmLabel="Delete"
        variant="destructive"
        onConfirm={() => deleteTarget && deleteMutation.mutate(deleteTarget.id)}
        isLoading={deleteMutation.isPending}
      />
    </div>
  )
}
