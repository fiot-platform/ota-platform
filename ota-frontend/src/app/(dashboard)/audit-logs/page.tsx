'use client'

import * as React from 'react'
import { useQuery } from '@tanstack/react-query'
import { Search, RefreshCw, Download, Filter } from 'lucide-react'
import { auditService } from '@/services/audit.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { DataTable, Column } from '@/components/ui/DataTable'
import { RoleBadge } from '@/components/ui/Badge'
import { RoleGuard } from '@/components/role-access/RoleGuard'
import { useToast } from '@/components/ui/ToastProvider'
import { AuditLog, AuditLogFilter, AuditAction } from '@/types'
import { formatDateTime, formatRelativeTime, downloadBlob } from '@/utils/formatters'
import { useQueryClient } from '@tanstack/react-query'

export default function AuditLogsPage() {
  const queryClient = useQueryClient()
  const { toast } = useToast()

  const [search, setSearch] = React.useState('')
  const [actionFilter, setActionFilter] = React.useState('')
  const [entityTypeFilter, setEntityTypeFilter] = React.useState('')
  const [startDate, setStartDate] = React.useState('')
  const [endDate, setEndDate] = React.useState('')
  const [page, setPage] = React.useState(1)
  const [pageSize, setPageSize] = React.useState(25)
  const [exporting, setExporting] = React.useState(false)

  const filter: AuditLogFilter = {
    action: actionFilter || undefined,
    entityType: entityTypeFilter || undefined,
    startDate: startDate || undefined,
    endDate: endDate || undefined,
    page,
    pageSize,
  }

  const { data, isLoading } = useQuery({
    queryKey: ['audit-logs', filter],
    queryFn: () => auditService.getAuditLogs(filter),
  })

  const handleExport = async () => {
    setExporting(true)
    try {
      const blob = await auditService.exportAuditLogs({
        action: actionFilter || undefined,
        entityType: entityTypeFilter || undefined,
        startDate: startDate || undefined,
        endDate: endDate || undefined,
      })
      downloadBlob(blob, `audit-logs-${new Date().toISOString().slice(0, 10)}.csv`)
      toast({ title: 'Export complete', variant: 'success' })
    } catch {
      toast({ title: 'Export failed', variant: 'error' })
    } finally {
      setExporting(false)
    }
  }

  const entityTypes = ['User', 'Project', 'Repository', 'Firmware', 'Device', 'Rollout', 'OtaJob', 'WebhookEvent', 'Policy', 'SystemConfig']

  const columns: Column<AuditLog>[] = [
    {
      key: 'timestamp',
      header: 'Timestamp',
      cell: (row) => (
        <div className="whitespace-nowrap">
          <p className="text-sm text-primary-800">{formatDateTime(row.timestamp)}</p>
          <p className="text-xs text-slate-400">{formatRelativeTime(row.timestamp)}</p>
        </div>
      ),
    },
    {
      key: 'action',
      header: 'Action',
      cell: (row) => (
        <span className="text-sm font-semibold text-primary-800">
          {row.action.replace(/([A-Z])/g, ' $1').trim()}
        </span>
      ),
    },
    {
      key: 'performedBy',
      header: 'Performed By',
      cell: (row) => (
        <div>
          <p className="text-sm font-medium text-primary-800">{row.performedByName ?? row.performedBy}</p>
          {row.performedByRole && <RoleBadge role={row.performedByRole} className="mt-0.5" />}
        </div>
      ),
    },
    {
      key: 'entity',
      header: 'Entity',
      cell: (row) => (
        <div>
          {row.entityType && (
            <p className="text-xs font-semibold text-slate-400 uppercase">{row.entityType}</p>
          )}
          {row.entityName && (
            <p className="text-sm text-primary-700 font-medium truncate max-w-[150px]">{row.entityName}</p>
          )}
          {!row.entityName && !row.entityType && <span className="text-slate-300">—</span>}
        </div>
      ),
    },
    {
      key: 'ipAddress',
      header: 'IP Address',
      cell: (row) => (
        <code className="text-xs text-slate-500">{row.ipAddress ?? '—'}</code>
      ),
    },
  ]

  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        title="Audit Logs"
        subtitle="Track all platform actions for compliance and security monitoring"
        breadcrumbs={[{ label: 'Dashboard', href: '/dashboard' }, { label: 'Audit Logs' }]}
        actions={
          <RoleGuard module="AuditLogs" action="export">
            <button
              onClick={handleExport}
              disabled={exporting}
              className="btn-secondary"
            >
              <Download className="w-4 h-4" />
              {exporting ? 'Exporting...' : 'Export CSV'}
            </button>
          </RoleGuard>
        }
      />

      {/* Filters */}
      <div className="filter-bar flex-wrap">
        <div className="relative flex-1 min-w-[200px] max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
          <input
            type="text"
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1) }}
            placeholder="Search by user or entity..."
            className="input pl-9"
          />
        </div>

        <select
          value={actionFilter}
          onChange={(e) => { setActionFilter(e.target.value); setPage(1) }}
          className="input w-auto"
        >
          <option value="">All Actions</option>
          {Object.values(AuditAction).map((a) => (
            <option key={a} value={a}>{a.replace(/([A-Z])/g, ' $1').trim()}</option>
          ))}
        </select>

        <select
          value={entityTypeFilter}
          onChange={(e) => { setEntityTypeFilter(e.target.value); setPage(1) }}
          className="input w-auto"
        >
          <option value="">All Entity Types</option>
          {entityTypes.map((e) => (
            <option key={e} value={e}>{e}</option>
          ))}
        </select>

        <div className="flex items-center gap-2">
          <input
            type="date"
            value={startDate}
            onChange={(e) => { setStartDate(e.target.value); setPage(1) }}
            className="input w-auto text-sm"
            placeholder="Start date"
          />
          <span className="text-slate-400 text-sm">to</span>
          <input
            type="date"
            value={endDate}
            onChange={(e) => { setEndDate(e.target.value); setPage(1) }}
            className="input w-auto text-sm"
            placeholder="End date"
          />
        </div>

        {(actionFilter || entityTypeFilter || startDate || endDate) && (
          <button
            onClick={() => {
              setActionFilter('')
              setEntityTypeFilter('')
              setStartDate('')
              setEndDate('')
              setPage(1)
            }}
            className="btn-ghost text-danger-600 text-sm"
          >
            <Filter className="w-4 h-4" />
            Clear Filters
          </button>
        )}

        <button
          onClick={() => queryClient.invalidateQueries({ queryKey: ['audit-logs'] })}
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
        emptyMessage="No audit log entries found"
      />
    </div>
  )
}
