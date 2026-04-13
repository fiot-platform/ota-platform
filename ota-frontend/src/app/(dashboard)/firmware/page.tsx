'use client'

import * as React from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Search, RefreshCw, Eye, FlaskConical, CheckCircle, XCircle, Archive, Plus, Pencil } from 'lucide-react'
import Link from 'next/link'
import { firmwareService } from '@/services/firmware.service'
import { projectService } from '@/services/project.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { DataTable, Column } from '@/components/ui/DataTable'
import { StatusBadge } from '@/components/ui/Badge'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
import { RoleGuard } from '@/components/role-access/RoleGuard'
import { ApproveFirmwareDialog } from '@/components/dialogs/ApproveFirmwareDialog'
import { RejectFirmwareDialog } from '@/components/dialogs/RejectFirmwareDialog'
import { QAVerifyDialog } from '@/components/dialogs/QAVerifyDialog'
import { CreateFirmwareForm, EditFirmwareForm } from '@/components/forms/FirmwareForm'
import { useToast } from '@/components/ui/ToastProvider'
import { FirmwareVersion, FirmwareStatus, FirmwareChannel, CreateFirmwareRequest, UpdateFirmwareRequest } from '@/types'
import { formatFileSize, formatDate } from '@/utils/formatters'
import { UserRole } from '@/types'

export default function FirmwarePage() {
  const queryClient = useQueryClient()
  const { toast } = useToast()

  const [search, setSearch] = React.useState('')
  const [statusFilter, setStatusFilter] = React.useState('')
  const [channelFilter, setChannelFilter] = React.useState('')
  const [projectFilter, setProjectFilter] = React.useState('')
  const [page, setPage] = React.useState(1)
  const [pageSize, setPageSize] = React.useState(25)

  const [createOpen, setCreateOpen] = React.useState(false)
  const [editTarget, setEditTarget] = React.useState<FirmwareVersion | null>(null)
  const [approveTarget, setApproveTarget] = React.useState<FirmwareVersion | null>(null)
  const [rejectTarget, setRejectTarget] = React.useState<FirmwareVersion | null>(null)
  const [qaTarget, setQaTarget] = React.useState<FirmwareVersion | null>(null)
  const [deprecateTarget, setDeprecateTarget] = React.useState<FirmwareVersion | null>(null)

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
      cell: (row) => (
        <StatusBadge status={row.isQaVerified ? 'QAVerified' : 'Pending'} />
      ),
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

          <RoleGuard module="Firmware" action="approve" roles={[UserRole.QA, UserRole.PlatformAdmin, UserRole.SuperAdmin]}>
            {!row.isQaVerified && [FirmwareStatus.PendingQA, FirmwareStatus.Draft].includes(row.status as FirmwareStatus) && (
              <button
                onClick={() => setQaTarget(row)}
                className="p-1.5 rounded-lg text-slate-400 hover:text-accent-600 hover:bg-accent-50 transition-colors"
                title="QA Verify"
              >
                <FlaskConical className="w-4 h-4" />
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

          <RoleGuard module="Firmware" action="approve" roles={[UserRole.ReleaseManager, UserRole.PlatformAdmin, UserRole.SuperAdmin]}>
            {[FirmwareStatus.PendingApproval, FirmwareStatus.QAVerified, FirmwareStatus.PendingQA, FirmwareStatus.Draft].includes(row.status as FirmwareStatus) && (
              <button
                onClick={() => setRejectTarget(row)}
                className="p-1.5 rounded-lg text-slate-400 hover:text-danger-600 hover:bg-danger-50 transition-colors"
                title="Reject"
              >
                <XCircle className="w-4 h-4" />
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
            fileSha256: values.fileSha256 || undefined,
            fileSizeBytes: values.fileSizeBytes || 0,
            downloadUrl: values.downloadUrl || undefined,
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

      {/* Reject */}
      <RejectFirmwareDialog
        open={Boolean(rejectTarget)}
        onOpenChange={(open) => !open && setRejectTarget(null)}
        firmwareId={rejectTarget?.id ?? ''}
        firmwareVersion={rejectTarget?.version ?? ''}
      />

      {/* QA Verify */}
      <QAVerifyDialog
        open={Boolean(qaTarget)}
        onOpenChange={(open) => !open && setQaTarget(null)}
        firmwareId={qaTarget?.id ?? ''}
        firmwareVersion={qaTarget?.version ?? ''}
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
    </div>
  )
}
