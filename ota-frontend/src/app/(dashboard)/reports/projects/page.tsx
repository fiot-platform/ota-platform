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
import { ProjectReport, PaginationInfo } from '@/types'

const FILTER_FIELDS: FilterField[] = [
  {
    key: 'isActive',
    label: 'Status',
    options: [
      { label: 'Active', value: 'true' },
      { label: 'Inactive', value: 'false' },
    ],
  },
]

const EMPTY_FILTERS: FilterValues = { isActive: '' }

export default function ProjectsReportPage() {
  const { toast } = useToast()
  const [exporting, setExporting] = React.useState(false)
  const [search, setSearch] = React.useState('')
  const [filters, setFilters] = React.useState<FilterValues>(EMPTY_FILTERS)
  const [page, setPage] = React.useState(1)
  const [pageSize, setPageSize] = React.useState(10)

  const { data: raw, isLoading } = useQuery({
    queryKey: ['report-projects'],
    queryFn: () => reportService.getProjectsReport(),
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
      (p) => p.name.toLowerCase().includes(q) || p.customerName.toLowerCase().includes(q),
    )
    if (filters.isActive !== '') rows = rows.filter((p) => String(p.isActive) === filters.isActive)
    return rows
  }, [raw, search, filters])

  const totalCount = filtered.length
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
  const pagedData = filtered.slice((page - 1) * pageSize, page * pageSize)
  const pagination: PaginationInfo = { page, pageSize, totalCount, totalPages, hasNextPage: page < totalPages, hasPreviousPage: page > 1 }

  React.useEffect(() => { setPage(1) }, [search, filters])

  const columns: Column<ProjectReport>[] = [
    { key: 'name', header: 'Project', cell: (p) => <span className="font-medium text-primary-800">{p.name}</span> },
    { key: 'customerName', header: 'Customer', cell: (p) => <span className="text-slate-500">{p.customerName}</span> },
    { key: 'repositoryCount', header: 'Repositories', headerClassName: 'text-right', className: 'text-right', accessor: 'repositoryCount' },
    { key: 'firmwareCount', header: 'Firmware', headerClassName: 'text-right', className: 'text-right', accessor: 'firmwareCount' },
    {
      key: 'activeRollouts',
      header: 'Active Rollouts',
      headerClassName: 'text-right',
      className: 'text-right',
      cell: (p) => (
        <span className={p.activeRollouts > 0 ? 'text-accent-600 font-semibold' : 'text-slate-400'}>
          {p.activeRollouts}
        </span>
      ),
    },
    { key: 'isActive', header: 'Status', cell: (p) => <StatusPill active={p.isActive} /> },
    { key: 'createdAt', header: 'Created', cell: (p) => <span className="text-slate-500">{formatDate(p.createdAt, 'dd MMM yyyy')}</span> },
  ]

  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        title="Projects Report"
        subtitle="All projects with repository and firmware summary"
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Reports', href: '/reports/device-status' },
          { label: 'Projects' },
        ]}
      />

      <div className="card p-6 space-y-4">
        <ReportHeader title="Projects Report" subtitle="All projects with repository and firmware summary"
          reportType="projects" exporting={exporting} onExport={handleExport} />

        <div className="flex flex-wrap items-center gap-3">
          <input
            type="search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search by project name or customer…"
            className="flex-1 min-w-[220px] max-w-sm px-3 py-2 text-sm border border-slate-300 rounded-lg outline-none focus:ring-2 focus:ring-accent-500 transition"
          />
          <FilterPopover fields={FILTER_FIELDS} values={filters} onChange={setFilters} onClear={() => setFilters(EMPTY_FILTERS)} />
          {(search || Object.values(filters).some(Boolean)) && (
            <span className="text-xs text-slate-500">{totalCount} result{totalCount !== 1 ? 's' : ''}</span>
          )}
        </div>

        <ActiveFilterChips fields={FILTER_FIELDS} values={filters} onChange={setFilters} />

        <DataTable<ProjectReport>
          flat
          columns={columns}
          data={pagedData}
          isLoading={isLoading}
          pagination={pagination}
          onPageChange={(p) => setPage(p)}
          onPageSizeChange={(s) => { setPageSize(s); setPage(1) }}
          keyExtractor={(p) => p.id}
          emptyMessage="No projects found"
        />
      </div>
    </div>
  )
}
