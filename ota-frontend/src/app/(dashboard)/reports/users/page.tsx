'use client'

import * as React from 'react'
import { useQuery } from '@tanstack/react-query'
import { reportService } from '@/services/report.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { DataTable, Column } from '@/components/ui/DataTable'
import { FilterPopover, ActiveFilterChips, FilterValues, FilterField } from '@/components/ui/FilterPopover'
import { RoleBadge } from '@/components/ui/Badge'
import { useToast } from '@/components/ui/ToastProvider'
import { downloadBlob, formatDate } from '@/utils/formatters'
import { StatusPill, ReportHeader } from '@/components/reports/shared'
import { UserReport, PaginationInfo } from '@/types'

const FILTER_FIELDS: FilterField[] = [
  {
    key: 'role',
    label: 'Role',
    options: [
      { label: 'Super Admin', value: 'SuperAdmin' },
      { label: 'Platform Admin', value: 'PlatformAdmin' },
      { label: 'Release Manager', value: 'ReleaseManager' },
      { label: 'QA', value: 'QA' },
      { label: 'Customer Admin', value: 'CustomerAdmin' },
      { label: 'Viewer', value: 'Viewer' },
    ],
  },
  {
    key: 'isActive',
    label: 'Status',
    options: [
      { label: 'Active', value: 'true' },
      { label: 'Inactive', value: 'false' },
    ],
  },
]

const EMPTY_FILTERS: FilterValues = { role: '', isActive: '' }

export default function UsersReportPage() {
  const { toast } = useToast()
  const [exporting, setExporting] = React.useState(false)
  const [search, setSearch] = React.useState('')
  const [filters, setFilters] = React.useState<FilterValues>(EMPTY_FILTERS)
  const [page, setPage] = React.useState(1)
  const [pageSize, setPageSize] = React.useState(10)

  const { data: raw, isLoading } = useQuery({
    queryKey: ['report-users'],
    queryFn: () => reportService.getUsersReport(),
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
      (u) => u.name.toLowerCase().includes(q) || u.email.toLowerCase().includes(q) ||
             u.role.toLowerCase().includes(q) || (u.customerName ?? '').toLowerCase().includes(q),
    )
    if (filters.role) rows = rows.filter((u) => u.role === filters.role)
    if (filters.isActive !== '') rows = rows.filter((u) => String(u.isActive) === filters.isActive)
    return rows
  }, [raw, search, filters])

  const totalCount = filtered.length
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
  const pagedData = filtered.slice((page - 1) * pageSize, page * pageSize)
  const pagination: PaginationInfo = { page, pageSize, totalCount, totalPages, hasNextPage: page < totalPages, hasPreviousPage: page > 1 }

  React.useEffect(() => { setPage(1) }, [search, filters])

  const columns: Column<UserReport>[] = [
    { key: 'name', header: 'Name', cell: (u) => <span className="font-medium text-primary-800">{u.name}</span> },
    { key: 'email', header: 'Email', cell: (u) => <span className="text-slate-500">{u.email}</span> },
    { key: 'role', header: 'Role', cell: (u) => <RoleBadge role={u.role} /> },
    { key: 'customerName', header: 'Customer', cell: (u) => <span className="text-slate-500">{u.customerName ?? '—'}</span> },
    { key: 'isActive', header: 'Status', cell: (u) => <StatusPill active={u.isActive} /> },
    { key: 'lastLoginAt', header: 'Last Login', cell: (u) => <span className="text-slate-500">{u.lastLoginAt ? formatDate(u.lastLoginAt, 'dd MMM yyyy') : '—'}</span> },
    { key: 'createdAt', header: 'Created', cell: (u) => <span className="text-slate-500">{formatDate(u.createdAt, 'dd MMM yyyy')}</span> },
  ]

  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        title="Users Report"
        subtitle="All platform users with role and account status"
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Reports', href: '/reports/firmware-trends' },
          { label: 'Users' },
        ]}
      />

      <div className="card p-6 space-y-4">
        <ReportHeader title="Users Report" subtitle="All platform users with role and account status"
          reportType="users" exporting={exporting} onExport={handleExport} />

        {/* Search + Filter row */}
        <div className="flex flex-wrap items-center gap-3">
          <input
            type="search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search by name, email, role or customer…"
            className="flex-1 min-w-[220px] max-w-sm px-3 py-2 text-sm border border-slate-300 rounded-lg outline-none focus:ring-2 focus:ring-accent-500 transition"
          />
          <FilterPopover fields={FILTER_FIELDS} values={filters} onChange={setFilters} onClear={() => setFilters(EMPTY_FILTERS)} />
          {(search || Object.values(filters).some(Boolean)) && (
            <span className="text-xs text-slate-500">{totalCount} result{totalCount !== 1 ? 's' : ''}</span>
          )}
        </div>

        {/* Active filter chips */}
        <ActiveFilterChips fields={FILTER_FIELDS} values={filters} onChange={setFilters} />

        <DataTable<UserReport>
          flat
          columns={columns}
          data={pagedData}
          isLoading={isLoading}
          pagination={pagination}
          onPageChange={(p) => setPage(p)}
          onPageSizeChange={(s) => { setPageSize(s); setPage(1) }}
          keyExtractor={(u) => u.id}
          emptyMessage="No users found"
        />
      </div>
    </div>
  )
}
