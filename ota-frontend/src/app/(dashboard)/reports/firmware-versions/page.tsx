'use client'

import * as React from 'react'
import { useQuery } from '@tanstack/react-query'
import { reportService } from '@/services/report.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { DataTable, Column } from '@/components/ui/DataTable'
import { FilterPopover, ActiveFilterChips, FilterValues, FilterField } from '@/components/ui/FilterPopover'
import { useToast } from '@/components/ui/ToastProvider'
import { downloadBlob, formatDate, formatFileSize } from '@/utils/formatters'
import { FwBadge, ChannelBadge, ReportHeader } from '@/components/reports/shared'
import { FirmwareVersionReport, PaginationInfo } from '@/types'

const FILTER_FIELDS: FilterField[] = [
  {
    key: 'status',
    label: 'Status',
    options: [
      { label: 'Draft', value: 'draft' },
      { label: 'Pending Approval', value: 'pendingApproval' },
      { label: 'Approved', value: 'approved' },
      { label: 'Rejected', value: 'rejected' },
      { label: 'Deprecated', value: 'deprecated' },
    ],
  },
  {
    key: 'channel',
    label: 'Channel',
    options: [
      { label: 'Stable', value: 'stable' },
      { label: 'Beta', value: 'beta' },
      { label: 'Alpha', value: 'alpha' },
      { label: 'Nightly', value: 'nightly' },
    ],
  },
]

const EMPTY_FILTERS: FilterValues = { status: '', channel: '' }

export default function FirmwareVersionsReportPage() {
  const { toast } = useToast()
  const [exporting, setExporting] = React.useState(false)
  const [search, setSearch] = React.useState('')
  const [filters, setFilters] = React.useState<FilterValues>(EMPTY_FILTERS)
  const [page, setPage] = React.useState(1)
  const [pageSize, setPageSize] = React.useState(10)

  const { data: raw, isLoading } = useQuery({
    queryKey: ['report-firmware-versions'],
    queryFn: () => reportService.getFirmwareVersionsReport(),
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
      (f) => f.version.toLowerCase().includes(q) || f.projectName.toLowerCase().includes(q) ||
             f.repositoryName.toLowerCase().includes(q) || (f.createdByName ?? '').toLowerCase().includes(q) ||
             (f.qaVerifiedByName ?? '').toLowerCase().includes(q) || (f.approvedByName ?? '').toLowerCase().includes(q),
    )
    if (filters.status) rows = rows.filter((f) => f.status.toLowerCase() === filters.status.toLowerCase())
    if (filters.channel) rows = rows.filter((f) => f.channel.toLowerCase() === filters.channel.toLowerCase())
    return rows
  }, [raw, search, filters])

  const totalCount = filtered.length
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
  const pagedData = filtered.slice((page - 1) * pageSize, page * pageSize)
  const pagination: PaginationInfo = { page, pageSize, totalCount, totalPages, hasNextPage: page < totalPages, hasPreviousPage: page > 1 }

  React.useEffect(() => { setPage(1) }, [search, filters])

  const columns: Column<FirmwareVersionReport>[] = [
    { key: 'version', header: 'Version', cell: (f) => <span className="font-mono font-semibold text-primary-800">{f.version}</span> },
    { key: 'projectName', header: 'Project', cell: (f) => <span className="text-slate-500">{f.projectName}</span> },
    { key: 'repositoryName', header: 'Repository Name', cell: (f) => <span className="text-slate-500">{f.repositoryName}</span> },
    { key: 'channel', header: 'Channel', cell: (f) => <ChannelBadge channel={f.channel} /> },
    { key: 'status', header: 'Status', cell: (f) => <FwBadge status={f.status} /> },
    { key: 'fileSizeBytes', header: 'Size', headerClassName: 'text-right', className: 'text-right', cell: (f) => <span className="text-slate-500">{f.fileSizeBytes ? formatFileSize(f.fileSizeBytes) : '—'}</span> },
    {
      key: 'createdByName',
      header: 'Created By (Date)',
      cell: (f) => f.createdByName
        ? <><span className="text-slate-700">{f.createdByName}</span><br /><span className="text-xs text-slate-400">{formatDate(f.createdAt, 'dd MMM yyyy')}</span></>
        : <span className="text-slate-400">— <span className="text-xs">({formatDate(f.createdAt, 'dd MMM yyyy')})</span></span>,
    },
    {
      key: 'qaVerifiedByName',
      header: 'QA Verify By (Date)',
      cell: (f) => f.qaVerifiedByName
        ? <><span className="text-slate-700">{f.qaVerifiedByName}</span><br /><span className="text-xs text-slate-400">{f.qaVerifiedAt ? formatDate(f.qaVerifiedAt, 'dd MMM yyyy') : '—'}</span></>
        : <span className="text-slate-400">—</span>,
    },
    {
      key: 'approvedByName',
      header: 'Approved By (Date)',
      cell: (f) => f.approvedByName
        ? <><span className="text-slate-700">{f.approvedByName}</span><br /><span className="text-xs text-slate-400">{f.approvedAt ? formatDate(f.approvedAt, 'dd MMM yyyy') : '—'}</span></>
        : <span className="text-slate-400">—</span>,
    },
    { key: 'deviceCount', header: 'Device Count', headerClassName: 'text-right', className: 'text-right', cell: (f) => <span className="font-semibold text-primary-800">{f.deviceCount}</span> },
    { key: 'createdAt', header: 'Created', cell: (f) => <span className="text-slate-500">{formatDate(f.createdAt, 'dd MMM yyyy')}</span> },
  ]

  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        title="Firmware Version Report"
        subtitle="All firmware versions across projects and repositories"
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Reports', href: '/reports/device-status' },
          { label: 'Firmware Versions' },
        ]}
      />

      <div className="card p-6 space-y-4">
        <ReportHeader title="Firmware Version Report" subtitle="All firmware versions across projects and repositories"
          reportType="firmware-versions" exporting={exporting} onExport={handleExport} />

        <div className="flex flex-wrap items-center gap-3">
          <input
            type="search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search by version, project, repository…"
            className="flex-1 min-w-[220px] max-w-sm px-3 py-2 text-sm border border-slate-300 rounded-lg outline-none focus:ring-2 focus:ring-accent-500 transition"
          />
          <FilterPopover fields={FILTER_FIELDS} values={filters} onChange={setFilters} onClear={() => setFilters(EMPTY_FILTERS)} />
          {(search || Object.values(filters).some(Boolean)) && (
            <span className="text-xs text-slate-500">{totalCount} result{totalCount !== 1 ? 's' : ''}</span>
          )}
        </div>

        <ActiveFilterChips fields={FILTER_FIELDS} values={filters} onChange={setFilters} />

        <DataTable<FirmwareVersionReport>
          flat
          columns={columns}
          data={pagedData}
          isLoading={isLoading}
          pagination={pagination}
          onPageChange={(p) => setPage(p)}
          onPageSizeChange={(s) => { setPageSize(s); setPage(1) }}
          keyExtractor={(f) => f.id}
          emptyMessage="No firmware versions found"
        />
      </div>
    </div>
  )
}
