'use client'

import * as React from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Search, RefreshCw, RotateCcw, Loader2 } from 'lucide-react'
import api from '@/lib/api'
import { PageHeader } from '@/components/ui/PageHeader'
import { DataTable, Column } from '@/components/ui/DataTable'
import { StatusBadge } from '@/components/ui/Badge'
import { RoleGuard } from '@/components/role-access/RoleGuard'
import { useToast } from '@/components/ui/ToastProvider'
import { WebhookEvent, WebhookEventStatus, ApiResponse, PaginatedResponse } from '@/types'
import { formatDateTime, formatRelativeTime, truncateText } from '@/utils/formatters'
import { useQueryClient as useQC } from '@tanstack/react-query'

// ─── Service ──────────────────────────────────────────────────────────────────

async function getWebhookEvents(params: {
  status?: string
  eventType?: string
  page?: number
  pageSize?: number
}): Promise<PaginatedResponse<WebhookEvent>> {
  const searchParams = new URLSearchParams()
  if (params.status) searchParams.set('status', params.status)
  if (params.eventType) searchParams.set('eventType', params.eventType)
  if (params.page) searchParams.set('page', String(params.page))
  if (params.pageSize) searchParams.set('pageSize', String(params.pageSize))

  const response = await api.get<ApiResponse<WebhookEvent[]>>(`/webhook-events?${searchParams.toString()}`)
  return { items: response.data.data, pagination: response.data.pagination! }
}

async function reprocessWebhookEvent(id: string): Promise<WebhookEvent> {
  const response = await api.post<ApiResponse<WebhookEvent>>(`/webhook-events/${id}/reprocess`)
  return response.data.data
}

// ─── Event type options ───────────────────────────────────────────────────────

const EVENT_TYPES = [
  'push',
  'create',
  'delete',
  'release',
  'tag',
  'pull_request',
  'issues',
]

export default function WebhookEventsPage() {
  const queryClient = useQC()
  const { toast } = useToast()

  const [search, setSearch] = React.useState('')
  const [statusFilter, setStatusFilter] = React.useState('')
  const [eventTypeFilter, setEventTypeFilter] = React.useState('')
  const [page, setPage] = React.useState(1)
  const [pageSize, setPageSize] = React.useState(25)
  const [reprocessingId, setReprocessingId] = React.useState<string | null>(null)

  const { data, isLoading } = useQuery({
    queryKey: ['webhook-events', { status: statusFilter, eventType: eventTypeFilter, page, pageSize }],
    queryFn: () => getWebhookEvents({
      status: statusFilter || undefined,
      eventType: eventTypeFilter || undefined,
      page,
      pageSize,
    }),
    refetchInterval: 30_000,
  })

  const reprocessMutation = useMutation({
    mutationFn: (id: string) => reprocessWebhookEvent(id),
    onMutate: (id) => setReprocessingId(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['webhook-events'] })
      toast({ title: 'Event reprocessed', description: 'The webhook event has been queued for reprocessing.', variant: 'success' })
      setReprocessingId(null)
    },
    onError: () => {
      toast({ title: 'Reprocess failed', variant: 'error' })
      setReprocessingId(null)
    },
  })

  const columns: Column<WebhookEvent>[] = [
    {
      key: 'id',
      header: 'Event ID',
      cell: (row) => (
        <code className="text-xs text-slate-600 font-mono">{row.id.slice(0, 8)}...</code>
      ),
    },
    {
      key: 'repo',
      header: 'Repository',
      cell: (row) => (
        <div>
          <p className="font-mono text-sm text-accent-600">{row.giteaOwner}/{row.giteaRepo}</p>
        </div>
      ),
    },
    {
      key: 'eventType',
      header: 'Event Type',
      cell: (row) => (
        <span className="inline-flex items-center px-2.5 py-0.5 bg-primary-100 text-primary-700 rounded-full text-xs font-semibold font-mono">
          {row.eventType}
        </span>
      ),
    },
    {
      key: 'status',
      header: 'Status',
      cell: (row) => <StatusBadge status={row.status} dot />,
    },
    {
      key: 'receivedAt',
      header: 'Received',
      cell: (row) => (
        <div>
          <p className="text-sm text-primary-800">{formatDateTime(row.receivedAt)}</p>
          <p className="text-xs text-slate-400">{formatRelativeTime(row.receivedAt)}</p>
        </div>
      ),
    },
    {
      key: 'retryCount',
      header: 'Retries',
      cell: (row) => (
        <span className={`text-sm font-medium ${row.retryCount >= row.maxRetries ? 'text-danger-600' : 'text-slate-600'}`}>
          {row.retryCount}/{row.maxRetries}
        </span>
      ),
    },
    {
      key: 'error',
      header: 'Error',
      cell: (row) => (
        row.errorMessage ? (
          <span className="text-xs text-danger-600 font-medium" title={row.errorMessage}>
            {truncateText(row.errorMessage, 40)}
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
        <RoleGuard module="WebhookEvents" action="execute">
          {row.status === WebhookEventStatus.Failed && (
            <button
              onClick={() => reprocessMutation.mutate(row.id)}
              disabled={reprocessingId === row.id}
              className="inline-flex items-center gap-1.5 px-2.5 py-1 text-xs font-medium bg-accent-50 text-accent-700 hover:bg-accent-100 rounded-lg transition-colors disabled:opacity-50"
            >
              {reprocessingId === row.id ? (
                <Loader2 className="w-3.5 h-3.5 animate-spin" />
              ) : (
                <RotateCcw className="w-3.5 h-3.5" />
              )}
              Reprocess
            </button>
          )}
        </RoleGuard>
      ),
    },
  ]

  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        title="Webhook Events"
        subtitle="Monitor Gitea webhook events and processing status"
        breadcrumbs={[{ label: 'Dashboard', href: '/dashboard' }, { label: 'Webhook Events' }]}
      />

      {/* Filters */}
      <div className="filter-bar">
        <select
          value={statusFilter}
          onChange={(e) => { setStatusFilter(e.target.value); setPage(1) }}
          className="input w-auto"
        >
          <option value="">All Statuses</option>
          {Object.values(WebhookEventStatus).map((s) => (
            <option key={s} value={s}>{s}</option>
          ))}
        </select>

        <select
          value={eventTypeFilter}
          onChange={(e) => { setEventTypeFilter(e.target.value); setPage(1) }}
          className="input w-auto"
        >
          <option value="">All Event Types</option>
          {EVENT_TYPES.map((t) => (
            <option key={t} value={t}>{t}</option>
          ))}
        </select>

        <button
          onClick={() => queryClient.invalidateQueries({ queryKey: ['webhook-events'] })}
          className="btn-secondary"
        >
          <RefreshCw className="w-4 h-4" />
          Refresh
        </button>

        {/* Live indicator */}
        <div className="flex items-center gap-2 ml-auto text-xs text-slate-500">
          <span className="w-2 h-2 bg-success-500 rounded-full animate-pulse" />
          Auto-refreshes every 30s
        </div>
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
        emptyMessage="No webhook events found"
      />
    </div>
  )
}
