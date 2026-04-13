'use client'

import * as React from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, Search, RefreshCw, Play, Pause, StopCircle, Eye, RotateCcw } from 'lucide-react'
import Link from 'next/link'
import { otaService } from '@/services/ota.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { DataTable, Column } from '@/components/ui/DataTable'
import { StatusBadge } from '@/components/ui/Badge'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
import { RoleGuard } from '@/components/role-access/RoleGuard'
import { CreateRolloutForm } from '@/components/forms/CreateRolloutForm'
import { useToast } from '@/components/ui/ToastProvider'
import { Rollout, RolloutStatus } from '@/types'
import { formatDate, formatPercent } from '@/utils/formatters'

export default function OtaRolloutsPage() {
  const queryClient = useQueryClient()
  const { toast } = useToast()

  const [search, setSearch] = React.useState('')
  const [statusFilter, setStatusFilter] = React.useState('')
  const [page, setPage] = React.useState(1)
  const [pageSize, setPageSize] = React.useState(25)
  const [createOpen, setCreateOpen] = React.useState(false)
  const [confirmAction, setConfirmAction] = React.useState<{ rollout: Rollout; action: 'start' | 'pause' | 'resume' | 'cancel' } | null>(null)

  const { data, isLoading } = useQuery({
    queryKey: ['rollouts', { search, status: statusFilter, page, pageSize }],
    queryFn: () => otaService.getRollouts({
      search,
      status: (statusFilter as RolloutStatus) || undefined,
      page,
      pageSize,
    }),
  })

  const actionMutation = useMutation({
    mutationFn: ({ action, id }: { action: string; id: string }) => {
      switch (action) {
        case 'start': return otaService.startRollout(id)
        case 'pause': return otaService.pauseRollout(id)
        case 'resume': return otaService.resumeRollout(id)
        case 'cancel': return otaService.cancelRollout(id)
        default: throw new Error('Unknown action')
      }
    },
    onSuccess: (_, { action }) => {
      queryClient.invalidateQueries({ queryKey: ['rollouts'] })
      toast({ title: `Rollout ${action}ed successfully`, variant: 'success' })
      setConfirmAction(null)
    },
    onError: () => toast({ title: 'Action failed', variant: 'error' }),
  })

  const getActionButtons = (row: Rollout) => (
    <div className="flex items-center gap-1">
      <Link
        href={`/ota-rollouts/${row.id}`}
        className="p-1.5 rounded-lg text-slate-400 hover:text-accent-600 hover:bg-accent-50 transition-colors"
        title="View"
      >
        <Eye className="w-4 h-4" />
      </Link>
      <RoleGuard module="OtaRollouts" action="execute">
        {row.status === RolloutStatus.Draft && (
          <button
            onClick={() => setConfirmAction({ rollout: row, action: 'start' })}
            className="p-1.5 rounded-lg text-slate-400 hover:text-success-600 hover:bg-success-50 transition-colors"
            title="Start"
          >
            <Play className="w-4 h-4" />
          </button>
        )}
        {row.status === RolloutStatus.InProgress && (
          <button
            onClick={() => setConfirmAction({ rollout: row, action: 'pause' })}
            className="p-1.5 rounded-lg text-slate-400 hover:text-warning-600 hover:bg-warning-50 transition-colors"
            title="Pause"
          >
            <Pause className="w-4 h-4" />
          </button>
        )}
        {row.status === RolloutStatus.Paused && (
          <button
            onClick={() => setConfirmAction({ rollout: row, action: 'resume' })}
            className="p-1.5 rounded-lg text-slate-400 hover:text-success-600 hover:bg-success-50 transition-colors"
            title="Resume"
          >
            <RotateCcw className="w-4 h-4" />
          </button>
        )}
        {[RolloutStatus.InProgress, RolloutStatus.Paused, RolloutStatus.Scheduled].includes(row.status as RolloutStatus) && (
          <button
            onClick={() => setConfirmAction({ rollout: row, action: 'cancel' })}
            className="p-1.5 rounded-lg text-slate-400 hover:text-danger-600 hover:bg-danger-50 transition-colors"
            title="Cancel"
          >
            <StopCircle className="w-4 h-4" />
          </button>
        )}
      </RoleGuard>
    </div>
  )

  const columns: Column<Rollout>[] = [
    {
      key: 'name',
      header: 'Rollout Name',
      cell: (row) => (
        <div>
          <Link
            href={`/ota-rollouts/${row.id}`}
            className="font-semibold text-accent-600 hover:text-accent-700 transition-colors"
          >
            {row.name}
          </Link>
          <p className="text-xs text-slate-500 mt-0.5">{row.projectName}</p>
        </div>
      ),
    },
    {
      key: 'firmware',
      header: 'Firmware Version',
      cell: (row) => (
        <code className="text-xs bg-slate-100 px-2 py-0.5 rounded font-semibold text-accent-700">
          {row.firmwareVersion ?? '—'}
        </code>
      ),
    },
    {
      key: 'status',
      header: 'Status',
      cell: (row) => <StatusBadge status={row.status} dot />,
    },
    {
      key: 'progress',
      header: 'Progress',
      cell: (row) => {
        const total = row.totalDevices
        const success = row.succeededCount
        const failed = row.failedCount
        const pct = total > 0 ? (success / total) * 100 : 0

        return (
          <div className="min-w-[120px]">
            <div className="flex items-center justify-between text-xs mb-1">
              <span className="text-success-600 font-medium">{success} done</span>
              {failed > 0 && <span className="text-danger-600 font-medium">{failed} failed</span>}
              <span className="text-slate-400">{total} total</span>
            </div>
            <div className="progress-bar">
              <div
                className="progress-bar-fill bg-success-500"
                style={{ width: `${pct}%` }}
              />
            </div>
            <p className="text-xs text-slate-500 mt-0.5">{formatPercent(pct)}</p>
          </div>
        )
      },
    },
    {
      key: 'target',
      header: 'Target',
      cell: (row) => (
        <div className="text-sm text-slate-600">
          <p>{row.targetType.replace(/([A-Z])/g, ' $1').trim()}</p>
          <p className="text-xs text-slate-400">{row.totalDevices} devices</p>
        </div>
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
      cell: getActionButtons,
    },
  ]

  const actionMessages: Record<string, string> = {
    start: 'Start this rollout? The firmware update process will begin for all targeted devices.',
    pause: 'Pause this rollout? In-progress updates will complete but no new updates will be dispatched.',
    resume: 'Resume this paused rollout? Update dispatch will continue for pending devices.',
    cancel: 'Cancel this rollout? All pending updates will be cancelled. This cannot be undone.',
  }

  const actionVariants: Record<string, 'default' | 'warning' | 'destructive'> = {
    start: 'default',
    pause: 'warning',
    resume: 'default',
    cancel: 'destructive',
  }

  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        title="OTA Rollouts"
        subtitle="Manage and monitor over-the-air firmware update campaigns"
        breadcrumbs={[{ label: 'Dashboard', href: '/dashboard' }, { label: 'OTA Rollouts' }]}
        actions={
          <RoleGuard module="OtaRollouts" action="create">
            <button onClick={() => setCreateOpen(true)} className="btn-primary">
              <Plus className="w-4 h-4" />
              Create Rollout
            </button>
          </RoleGuard>
        }
      />

      {/* Filters */}
      <div className="filter-bar">
        <div className="relative flex-1 min-w-[200px] max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
          <input
            type="text"
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1) }}
            placeholder="Search rollouts..."
            className="input pl-9"
          />
        </div>

        <select
          value={statusFilter}
          onChange={(e) => { setStatusFilter(e.target.value); setPage(1) }}
          className="input w-auto"
        >
          <option value="">All Statuses</option>
          {Object.values(RolloutStatus).map((s) => (
            <option key={s} value={s}>{s.replace(/([A-Z])/g, ' $1').trim()}</option>
          ))}
        </select>

        <button
          onClick={() => queryClient.invalidateQueries({ queryKey: ['rollouts'] })}
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
        emptyMessage="No rollouts found. Create your first rollout to deploy firmware."
      />

      {/* Create Form */}
      <CreateRolloutForm
        open={createOpen}
        onOpenChange={setCreateOpen}
      />

      {/* Action Confirm */}
      <ConfirmDialog
        open={Boolean(confirmAction)}
        onOpenChange={(open) => !open && setConfirmAction(null)}
        title={`${confirmAction?.action ? confirmAction.action.charAt(0).toUpperCase() + confirmAction.action.slice(1) : ''} Rollout`}
        message={confirmAction ? actionMessages[confirmAction.action] : ''}
        confirmLabel={confirmAction?.action ? confirmAction.action.charAt(0).toUpperCase() + confirmAction.action.slice(1) : ''}
        variant={confirmAction ? actionVariants[confirmAction.action] : 'default'}
        onConfirm={() => {
          if (confirmAction) {
            actionMutation.mutate({ action: confirmAction.action, id: confirmAction.rollout.id })
          }
        }}
        isLoading={actionMutation.isPending}
      />
    </div>
  )
}
