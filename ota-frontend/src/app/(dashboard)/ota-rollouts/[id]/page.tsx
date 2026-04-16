'use client'

import * as React from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useParams, useRouter } from 'next/navigation'
import {
  ArrowLeft,
  Play,
  Pause,
  StopCircle,
  RotateCcw,
  RefreshCw,
  BarChart3,
} from 'lucide-react'
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  Tooltip,
  ResponsiveContainer,
  Cell,
} from 'recharts'
import { otaService } from '@/services/ota.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { DataTable, Column } from '@/components/ui/DataTable'
import { StatusBadge } from '@/components/ui/Badge'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
import { RoleGuard } from '@/components/role-access/RoleGuard'
import { useToast } from '@/components/ui/ToastProvider'
import { OtaJob, OtaJobStatus, RolloutStatus } from '@/types'
import { formatDate, formatRelativeTime, formatPercent, truncateText } from '@/utils/formatters'

const JOB_STATUS_COLORS: Record<string, string> = {
  Succeeded: '#22c55e',
  Failed: '#ef4444',
  Pending: '#94a3b8',
  InProgress: '#3b82f6',
  Cancelled: '#f59e0b',
  Skipped: '#64748b',
  Retrying: '#8b5cf6',
}

export default function RolloutDetailPage() {
  const { id } = useParams<{ id: string }>()
  const router = useRouter()
  const queryClient = useQueryClient()
  const { toast } = useToast()

  const [jobPage, setJobPage] = React.useState(1)
  const [jobPageSize, setJobPageSize] = React.useState(25)
  const [confirmAction, setConfirmAction] = React.useState<'start' | 'pause' | 'resume' | 'cancel' | null>(null)

  const { data: rollout, isLoading: rolloutLoading } = useQuery({
    queryKey: ['rollout', id],
    queryFn: () => otaService.getRolloutById(id),
    refetchInterval: (query) =>
      query.state.data?.status === RolloutStatus.InProgress ? 10000 : false,
  })

  const { data: summary, isLoading: summaryLoading } = useQuery({
    queryKey: ['rollout-summary', id],
    queryFn: () => otaService.getRolloutSummary(id),
    refetchInterval: (query) =>
      query.state.data?.status === RolloutStatus.InProgress ? 10000 : false,
  })

  const { data: jobs, isLoading: jobsLoading } = useQuery({
    queryKey: ['rollout-jobs', id, jobPage, jobPageSize],
    queryFn: () => otaService.getRolloutJobs(id, { page: jobPage, pageSize: jobPageSize }),
  })

  const actionMutation = useMutation({
    mutationFn: (action: string) => {
      switch (action) {
        case 'start': return otaService.startRollout(id)
        case 'pause': return otaService.pauseRollout(id)
        case 'resume': return otaService.resumeRollout(id)
        case 'cancel': return otaService.cancelRollout(id)
        default: throw new Error('Unknown action')
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rollout', id] })
      queryClient.invalidateQueries({ queryKey: ['rollout-summary', id] })
      toast({ title: `Rollout ${confirmAction}ed`, variant: 'success' })
      setConfirmAction(null)
    },
    onError: () => toast({ title: 'Action failed', variant: 'error' }),
  })

  const retryMutation = useMutation({
    mutationFn: (jobId: string) => otaService.retryJob(jobId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rollout-jobs', id] })
      toast({ title: 'Job retry queued', variant: 'success' })
    },
    onError: () => toast({ title: 'Failed to retry job', variant: 'error' }),
  })

  const statusBreakdown = summary?.jobStatusBreakdown?.map((item) => ({
    name: item.status,
    value: item.count,
    color: JOB_STATUS_COLORS[item.status] ?? '#94a3b8',
  })) ?? []

  const jobColumns: Column<OtaJob>[] = [
    {
      key: 'device',
      header: 'Device',
      cell: (row) => (
        <div>
          <p className="font-mono text-sm text-accent-600">{row.deviceSerialNumber ?? row.deviceId}</p>
          {row.deviceModel && <p className="text-xs text-slate-500">{row.deviceModel}</p>}
        </div>
      ),
    },
    {
      key: 'status',
      header: 'Status',
      cell: (row) => <StatusBadge status={row.status} dot />,
    },
    {
      key: 'attempts',
      header: 'Attempts',
      cell: (row) => (
        <span className="text-sm text-slate-600">{row.attemptCount}/{row.maxAttempts}</span>
      ),
    },
    {
      key: 'lastAttempt',
      header: 'Last Attempt',
      cell: (row) => (
        <span className="text-sm text-slate-500">
          {row.lastAttemptAt ? formatRelativeTime(row.lastAttemptAt) : '—'}
        </span>
      ),
    },
    {
      key: 'failureReason',
      header: 'Failure Reason',
      cell: (row) => (
        row.failureReason ? (
          <span className="text-xs text-danger-600 font-medium" title={row.failureReason}>
            {truncateText(row.failureReason, 40)}
          </span>
        ) : (
          <span className="text-slate-300">—</span>
        )
      ),
    },
    {
      key: 'actions',
      header: 'Actions',
      cell: (row) => (
        <RoleGuard module="OtaRollouts" action="execute">
          {row.status === OtaJobStatus.Failed && (
            <button
              onClick={() => retryMutation.mutate(row.id)}
              disabled={retryMutation.isPending}
              className="inline-flex items-center gap-1.5 px-2.5 py-1 text-xs font-medium bg-accent-50 text-accent-700 hover:bg-accent-100 rounded-lg transition-colors disabled:opacity-50"
            >
              <RefreshCw className="w-3.5 h-3.5" />
              Retry
            </button>
          )}
        </RoleGuard>
      ),
    },
  ]

  if (rolloutLoading) {
    return (
      <div className="space-y-6 animate-pulse">
        <div className="h-10 bg-slate-200 rounded-lg w-64" />
        <div className="grid grid-cols-3 gap-6">
          <div className="col-span-2 card h-48" />
          <div className="card h-48" />
        </div>
      </div>
    )
  }

  if (!rollout) return (
    <div className="text-center py-20">
      <p className="text-slate-500">Rollout not found</p>
      <button onClick={() => router.back()} className="btn-secondary mt-4">
        <ArrowLeft className="w-4 h-4" /> Back
      </button>
    </div>
  )

  const total = rollout.totalDevices || 1
  const successPct = (rollout.succeededCount / total) * 100

  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        title={rollout.name}
        subtitle={rollout.description ?? 'OTA Rollout Details'}
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'OTA Rollouts', href: '/ota-rollouts' },
          { label: rollout.name },
        ]}
        actions={
          <div className="flex items-center gap-2">
            <button onClick={() => router.back()} className="btn-secondary">
              <ArrowLeft className="w-4 h-4" /> Back
            </button>
            <RoleGuard module="OtaRollouts" action="execute">
              {rollout.status === RolloutStatus.Draft && (
                <button onClick={() => setConfirmAction('start')} className="btn-success">
                  <Play className="w-4 h-4" /> Start Rollout
                </button>
              )}
              {rollout.status === RolloutStatus.InProgress && (
                <button onClick={() => setConfirmAction('pause')} className="btn-warning">
                  <Pause className="w-4 h-4" /> Pause
                </button>
              )}
              {rollout.status === RolloutStatus.Paused && (
                <button onClick={() => setConfirmAction('resume')} className="btn-primary">
                  <RotateCcw className="w-4 h-4" /> Resume
                </button>
              )}
              {[RolloutStatus.InProgress, RolloutStatus.Paused].includes(rollout.status as RolloutStatus) && (
                <button onClick={() => setConfirmAction('cancel')} className="btn-danger">
                  <StopCircle className="w-4 h-4" /> Cancel
                </button>
              )}
            </RoleGuard>
          </div>
        }
      />

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Summary Card */}
        <div className="lg:col-span-2 card p-6">
          <div className="flex items-center justify-between mb-4">
            <h3 className="section-title">Rollout Summary</h3>
            <StatusBadge status={rollout.status} dot />
          </div>

          {/* Progress Bar */}
          <div className="mb-6">
            <div className="flex items-center justify-between text-sm mb-2">
              <span className="font-medium text-primary-800">
                {rollout.succeededCount} / {rollout.totalDevices} devices updated
              </span>
              <span className="font-bold text-accent-600">{formatPercent(successPct)}</span>
            </div>
            <div className="progress-bar h-3">
              <div className="progress-bar-fill bg-success-500 h-3" style={{ width: `${successPct}%` }} />
            </div>
          </div>

          {/* Stats Grid */}
          <div className="grid grid-cols-3 sm:grid-cols-6 gap-3">
            {[
              { label: 'Total', value: rollout.totalDevices, color: 'text-primary-800' },
              { label: 'Pending', value: rollout.pendingCount, color: 'text-slate-600' },
              { label: 'In Progress', value: rollout.inProgressCount, color: 'text-accent-600' },
              { label: 'Succeeded', value: rollout.succeededCount, color: 'text-success-600' },
              { label: 'Failed', value: rollout.failedCount, color: 'text-danger-600' },
              { label: 'Skipped', value: rollout.skippedCount, color: 'text-slate-400' },
            ].map((stat) => (
              <div key={stat.label} className="text-center p-3 bg-slate-50 rounded-lg">
                <p className={`text-xl font-bold ${stat.color}`}>{stat.value}</p>
                <p className="text-xs text-slate-500 mt-0.5">{stat.label}</p>
              </div>
            ))}
          </div>

          {/* Meta */}
          <div className="grid grid-cols-2 gap-x-8 mt-5 pt-4 border-t border-slate-100">
            <div className="space-y-2">
              <div className="flex justify-between text-sm">
                <span className="text-slate-500">Project</span>
                <span className="font-medium">{rollout.projectName}</span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-slate-500">Target Type</span>
                <span className="font-medium">{rollout.targetType.replace(/([A-Z])/g, ' $1').trim()}</span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-slate-500">Created</span>
                <span className="font-medium">{formatDate(rollout.createdAt)}</span>
              </div>
            </div>
            <div className="space-y-2">
              {rollout.startedAt && (
                <div className="flex justify-between text-sm">
                  <span className="text-slate-500">Started</span>
                  <span className="font-medium">{formatRelativeTime(rollout.startedAt)}</span>
                </div>
              )}
              {rollout.completedAt && (
                <div className="flex justify-between text-sm">
                  <span className="text-slate-500">Completed</span>
                  <span className="font-medium">{formatRelativeTime(rollout.completedAt)}</span>
                </div>
              )}
              {rollout.firmwareVersion && (
                <div className="flex justify-between text-sm">
                  <span className="text-slate-500">Firmware</span>
                  <code className="font-bold text-accent-600">{rollout.firmwareVersion}</code>
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Job Status Chart */}
        <div className="card p-6">
          <h3 className="section-title flex items-center gap-2 mb-4">
            <BarChart3 className="w-4 h-4 text-accent-600" />
            Job Status Breakdown
          </h3>
          {summaryLoading ? (
            <div className="h-48 bg-slate-100 rounded-lg animate-pulse" />
          ) : statusBreakdown.length > 0 ? (
            <ResponsiveContainer width="100%" height={200}>
              <BarChart data={statusBreakdown} layout="vertical" margin={{ left: 0 }}>
                <XAxis type="number" tick={{ fontSize: 11, fill: '#94a3b8' }} axisLine={false} tickLine={false} />
                <YAxis dataKey="name" type="category" tick={{ fontSize: 11, fill: '#64748b' }} axisLine={false} tickLine={false} width={70} />
                <Tooltip formatter={(v) => [v, 'Devices']} />
                <Bar dataKey="value" radius={[0, 4, 4, 0]} maxBarSize={20}>
                  {statusBreakdown.map((item, idx) => (
                    <Cell key={idx} fill={item.color} />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          ) : (
            <div className="flex items-center justify-center h-48 text-slate-400 text-sm">
              No job data available
            </div>
          )}
        </div>
      </div>

      {/* Jobs Table */}
      <div>
        <div className="flex items-center justify-between mb-3">
          <h3 className="section-title">OTA Jobs ({jobs?.pagination?.totalCount ?? 0})</h3>
          <button
            onClick={() => queryClient.invalidateQueries({ queryKey: ['rollout-jobs', id] })}
            className="btn-secondary btn-sm"
          >
            <RefreshCw className="w-3.5 h-3.5" />
            Refresh
          </button>
        </div>
        <DataTable
          columns={jobColumns}
          data={jobs?.items ?? []}
          pagination={jobs?.pagination}
          onPageChange={setJobPage}
          onPageSizeChange={(s) => { setJobPageSize(s); setJobPage(1) }}
          isLoading={jobsLoading}
          keyExtractor={(r) => r.id}
          emptyMessage="No jobs found for this rollout"
        />
      </div>

      {/* Action Confirm */}
      <ConfirmDialog
        open={Boolean(confirmAction)}
        onOpenChange={(open) => !open && setConfirmAction(null)}
        title={`${confirmAction ? confirmAction.charAt(0).toUpperCase() + confirmAction.slice(1) : ''} Rollout`}
        message={`Are you sure you want to ${confirmAction} the rollout "${rollout.name}"?`}
        confirmLabel={confirmAction ? confirmAction.charAt(0).toUpperCase() + confirmAction.slice(1) : ''}
        variant={confirmAction === 'cancel' ? 'destructive' : confirmAction === 'pause' ? 'warning' : 'default'}
        onConfirm={() => confirmAction && actionMutation.mutate(confirmAction)}
        isLoading={actionMutation.isPending}
      />
    </div>
  )
}
