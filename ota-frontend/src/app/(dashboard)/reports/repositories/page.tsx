'use client'

import * as React from 'react'
import { useQuery } from '@tanstack/react-query'
import { reportService } from '@/services/report.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { DataTable, Column } from '@/components/ui/DataTable'
import { FilterPopover, ActiveFilterChips, FilterValues, FilterField } from '@/components/ui/FilterPopover'
import { useToast } from '@/components/ui/ToastProvider'
import { downloadBlob, formatDate } from '@/utils/formatters'
import { StatusPill, ReportHeader } from '@/components/reports/shared'
import { RepositoryReport, PaginationInfo } from '@/types'

const FILTER_FIELDS: FilterField[] = [
  {
    key: 'isActive',
    label: 'Status',
    options: [
      { label: 'Active', value: 'true' },
      { label: 'Inactive', value: 'false' },
    ],
  },
  {
    key: 'webhookConfigured',
    label: 'Webhook',
    options: [
      { label: 'Configured', value: 'true' },
      { label: 'Not Set', value: 'false' },
    ],
  },
]

const EMPTY_FILTERS: FilterValues = { isActive: '', webhookConfigured: '' }

export default function RepositoriesReportPage() {
  const { toast } = useToast()
  const [exporting, setExporting] = React.useState(false)
  const [search, setSearch] = React.useState('')
  const [filters, setFilters] = React.useState<FilterValues>(EMPTY_FILTERS)
  const [page, setPage] = React.useState(1)
  const [pageSize, setPageSize] = React.useState(10)

  const { data: raw, isLoading } = useQuery({
    queryKey: ['report-repositories'],
    queryFn: () => reportService.getRepositoriesReport(),
  })

  const handleExport = async (type: string) => {
    setExporting(true)
    try {
      const blob = await reportService.exportReport(type)
      downloadBlob(blob, `${type}-${new Date().toISOString().slice(0, 10)}.csv`)
      toast({ title: 'Report exported', variant: 'success' })
    } catch {
      toast({ title: 'Export failed', variant: 'error' })
    } finally {
      setExporting(false)
    }
  }

  const filtered = React.useMemo(() => {
    let rows = raw ?? []
    const q = search.trim().toLowerCase()
    if (q) rows = rows.filter(
      (r) => r.name.toLowerCase().includes(q) || r.projectName.toLowerCase().includes(q) || (r.clientName ?? '').toLowerCase().includes(q),
    )
    if (filters.isActive !== '') rows = rows.filter((r) => String(r.isActive) === filters.isActive)
    if (filters.webhookConfigured !== '') rows = rows.filter((r) => String(r.webhookConfigured) === filters.webhookConfigured)
    return rows
  }, [raw, search, filters])

  const totalCount = filtered.length
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
  const pagedData = filtered.slice((page - 1) * pageSize, page * pageSize)
  const pagination: PaginationInfo = { page, pageSize, totalCount, totalPages, hasNextPage: page < totalPages, hasPreviousPage: page > 1 }

  React.useEffect(() => { setPage(1) }, [search, filters])

  const columns: Column<RepositoryReport>[] = [
    { key: 'name', header: 'Repository', cell: (r) => <span className="font-medium text-primary-800">{r.name}</span> },
    { key: 'projectName', header: 'Project', cell: (r) => <span className="text-slate-500">{r.projectName}</span> },
    { key: 'clientName', header: 'Client', cell: (r) => <span className="text-slate-500">{r.clientName ?? '—'}</span> },
    { key: 'firmwareCount', header: 'Firmware', headerClassName: 'text-right', className: 'text-right', accessor: 'firmwareCount' },
    {
      key: 'webhookConfigured',
      header: 'Webhook',
      cell: (r) => (
        <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${r.webhookConfigured ? 'bg-success-100 text-success-700' : 'bg-slate-100 text-slate-500'}`}>
          {r.webhookConfigured ? 'Configured' : 'Not Set'}
        </span>
      ),
    },
    { key: 'lastSyncedAt', header: 'Last Synced', cell: (r) => <span className="text-slate-500">{r.lastSyncedAt ? formatDate(r.lastSyncedAt, 'dd MMM yyyy HH:mm') : '—'}</span> },
    { key: 'isActive', header: 'Status', cell: (r) => <StatusPill active={r.isActive} /> },
    { key: 'createdAt', header: 'Created', cell: (r) => <span className="text-slate-500">{formatDate(r.createdAt, 'dd MMM yyyy')}</span> },
  ]

  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        title="Repositories Report"
        subtitle="All registered repositories with sync and firmware status"
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Reports', href: '/reports/device-status' },
          { label: 'Repositories' },
        ]}
      />

      <div className="card p-6 space-y-4">
        <ReportHeader title="Repositories Report" subtitle="All registered repositories with sync and firmware status"
          reportType="repositories" exporting={exporting} onExport={handleExport} />

        <div className="flex flex-wrap items-center gap-3">
          <input
            type="search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search by repository name, project or client…"
            className="flex-1 min-w-[220px] max-w-sm px-3 py-2 text-sm border border-slate-300 rounded-lg outline-none focus:ring-2 focus:ring-accent-500 transition"
          />
          <FilterPopover fields={FILTER_FIELDS} values={filters} onChange={setFilters} onClear={() => setFilters(EMPTY_FILTERS)} />
          {(search || Object.values(filters).some(Boolean)) && (
            <span className="text-xs text-slate-500">{totalCount} result{totalCount !== 1 ? 's' : ''}</span>
          )}
        </div>

        <ActiveFilterChips fields={FILTER_FIELDS} values={filters} onChange={setFilters} />

        <DataTable<RepositoryReport>
          flat
          columns={columns}
          data={pagedData}
          isLoading={isLoading}
          pagination={pagination}
          onPageChange={(p) => setPage(p)}
          onPageSizeChange={(s) => { setPageSize(s); setPage(1) }}
          keyExtractor={(r) => r.id}
          emptyMessage="No repositories found"
        />
      </div>
    </div>
  )
}
