'use client'

import * as React from 'react'
import { useQuery } from '@tanstack/react-query'
import { reportService } from '@/services/report.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { DataTable, Column } from '@/components/ui/DataTable'
import { FilterPopover, ActiveFilterChips, FilterValues, FilterField } from '@/components/ui/FilterPopover'
import { useToast } from '@/components/ui/ToastProvider'
import { downloadBlob, formatDate } from '@/utils/formatters'
import { JobStatusBadge, ReportHeader } from '@/components/reports/shared'
import { DeviceOtaHistoryRow, PaginationInfo } from '@/types'

const STATUS_OPTIONS: { label: string; value: string }[] = [
  { label: 'Pending',     value: 'pending' },
  { label: 'In Progress', value: 'inProgress' },
  { label: 'Completed',   value: 'completed' },
  { label: 'Failed',      value: 'failed' },
  { label: 'Cancelled',   value: 'cancelled' },
]
const EMPTY_FILTERS: FilterValues = {
  jobStatus: '',
  projectName: '',
  customerName: '',
  firmwareVersion: '',
}

export default function DeviceOtaHistoryPage() {
  const { toast } = useToast()
  // Default both date filters to today so the report opens scoped to today's events.
  const today = React.useMemo(() => {
    const d = new Date()
    const yyyy = d.getFullYear()
    const mm = String(d.getMonth() + 1).padStart(2, '0')
    const dd = String(d.getDate()).padStart(2, '0')
    return `${yyyy}-${mm}-${dd}`
  }, [])

  const [exporting, setExporting] = React.useState(false)
  const [search, setSearch] = React.useState('')
  const [filters, setFilters] = React.useState<FilterValues>(EMPTY_FILTERS)
  const [fromDate, setFromDate] = React.useState(today)
  const [toDate,   setToDate]   = React.useState(today)
  const [page, setPage] = React.useState(1)
  const [pageSize, setPageSize] = React.useState(25)

  const { data, isLoading } = useQuery({
    queryKey: ['report-device-ota-history-flat'],
    queryFn: () => reportService.getDeviceOtaHistory(),
  })

  // Dynamic option lists from loaded data — keeps dropdowns in sync with what
  // actually exists in the report instead of a hardcoded list.
  const FILTER_FIELDS: FilterField[] = React.useMemo(() => {
    const rows = data ?? []
    const uniq = (arr: (string | undefined | null)[]) =>
      Array.from(new Set(arr.map((s) => (s ?? '').trim()).filter(Boolean))).sort()

    const projects  = uniq(rows.map((r) => r.projectName))
    const customers = uniq(rows.map((r) => r.customerName))
    const versions  = uniq(rows.map((r) => r.firmwareVersion))

    return [
      { key: 'jobStatus',       label: 'Job Status',      options: STATUS_OPTIONS },
      { key: 'projectName',     label: 'Project',         options: projects.map((v)  => ({ label: v, value: v })) },
      { key: 'customerName',    label: 'Client',          options: customers.map((v) => ({ label: v, value: v })) },
      { key: 'firmwareVersion', label: 'Updated Version', options: versions.map((v)  => ({ label: v, value: v })) },
    ]
  }, [data])

  const filtered = React.useMemo(() => {
    let rows = data ?? []
    const q = search.trim().toLowerCase()
    if (q) rows = rows.filter(
      (r) =>
        (r.macImeiIp ?? '').toLowerCase().includes(q) ||
        r.deviceSerial.toLowerCase().includes(q) ||
        (r.customerName ?? '').toLowerCase().includes(q) ||
        r.projectName.toLowerCase().includes(q) ||
        (r.model ?? '').toLowerCase().includes(q) ||
        (r.oldFirmwareVersion ?? '').toLowerCase().includes(q) ||
        r.firmwareVersion.toLowerCase().includes(q) ||
        (r.pushedByName ?? '').toLowerCase().includes(q),
    )
    if (filters.jobStatus)
      rows = rows.filter((r) => r.jobStatus.toLowerCase() === filters.jobStatus.toLowerCase())
    if (filters.projectName)
      rows = rows.filter((r) => r.projectName === filters.projectName)
    if (filters.customerName)
      rows = rows.filter((r) => (r.customerName ?? '') === filters.customerName)
    if (filters.firmwareVersion)
      rows = rows.filter((r) => r.firmwareVersion === filters.firmwareVersion)

    // Date range — match against the OTA event date (completedAt → startedAt → pushedAt)
    if (fromDate || toDate) {
      const from = fromDate ? new Date(fromDate + 'T00:00:00').getTime() : -Infinity
      const to   = toDate   ? new Date(toDate   + 'T23:59:59').getTime() :  Infinity
      rows = rows.filter((r) => {
        const stamp = r.completedAt ?? r.startedAt ?? r.pushedAt
        if (!stamp) return false
        const t = new Date(stamp).getTime()
        return t >= from && t <= to
      })
    }

    return rows
  }, [data, search, filters, fromDate, toDate])

  const totalCount = filtered.length
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
  const pagedData = filtered.slice((page - 1) * pageSize, page * pageSize)
  const pagination: PaginationInfo = { page, pageSize, totalCount, totalPages, hasNextPage: page < totalPages, hasPreviousPage: page > 1 }

  React.useEffect(() => { setPage(1) }, [search, filters, fromDate, toDate])

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

  const renderUserDate = (name: string | null | undefined, when: string | null | undefined) => {
    if (!name && !when) return <span className="text-slate-300">—</span>
    return (
      <div className="leading-tight">
        <p className="text-sm text-slate-700">{name ?? '—'}</p>
        {when && <p className="text-xs text-slate-400">{formatDate(when, 'dd MMM yyyy HH:mm')}</p>}
      </div>
    )
  }

  const columns: Column<DeviceOtaHistoryRow>[] = [
    { key: 'macImeiIp',          header: 'MAC / IMEI / IP',  cell: (r) => <span className="font-mono font-semibold text-accent-600 text-sm">{r.macImeiIp ?? r.deviceSerial}</span> },
    { key: 'customerName',       header: 'Client',           cell: (r) => <span className="text-slate-500">{r.customerName ?? '—'}</span> },
    { key: 'projectName',        header: 'Project',          cell: (r) => <span className="text-slate-500">{r.projectName}</span> },
    { key: 'model',              header: 'Model',            cell: (r) => <span className="text-slate-700 font-medium">{r.model ?? '—'}</span> },
    { key: 'oldFirmwareVersion', header: 'Old Version',      cell: (r) => r.oldFirmwareVersion
        ? <code className="text-xs bg-slate-100 px-2 py-0.5 rounded text-slate-700 font-semibold">{r.oldFirmwareVersion}</code>
        : <span className="text-slate-300">—</span> },
    { key: 'firmwareVersion',    header: 'Updated Version',  cell: (r) => <code className="text-xs bg-accent-50 px-2 py-0.5 rounded text-accent-700 font-semibold border border-accent-200">{r.firmwareVersion}</code> },
    { key: 'completedAt',        header: 'OTA Date',         cell: (r) => <span className="text-slate-500 text-xs">
        {r.completedAt
          ? formatDate(r.completedAt, 'dd MMM yyyy HH:mm')
          : r.startedAt ? formatDate(r.startedAt, 'dd MMM yyyy HH:mm') : '—'}
      </span> },
    { key: 'pushedBy',           header: 'Push By (Date)',   cell: (r) => renderUserDate(r.pushedByName, r.pushedAt) },
    { key: 'jobStatus',          header: 'Status',           cell: (r) => <JobStatusBadge status={r.jobStatus} /> },
  ]

  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        title="Device-wise OTA History"
        subtitle="One row per OTA event — push details, target version, and outcome"
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Reports', href: '/reports/device-status' },
          { label: 'Device OTA History' },
        ]}
      />

      <div className="card p-6 space-y-4">
        <ReportHeader
          title="Device-wise OTA History"
          subtitle="Per-device OTA events"
          reportType="device-ota-history"
          exporting={exporting}
          onExport={handleExport}
        />

        <div className="flex flex-wrap items-center gap-3">
          <input
            type="search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search by MAC, client, project, model, version or pusher…"
            className="flex-1 min-w-[260px] max-w-md px-3 py-2 text-sm border border-slate-300 rounded-lg outline-none focus:ring-2 focus:ring-accent-500 transition"
          />

          <div className="flex items-center gap-1.5">
            <label className="text-xs font-medium text-slate-500">From</label>
            <input
              type="date"
              value={fromDate}
              max={toDate || undefined}
              onChange={(e) => setFromDate(e.target.value)}
              className="px-2.5 py-1.5 text-sm border border-slate-300 rounded-lg outline-none focus:ring-2 focus:ring-accent-500"
            />
          </div>
          <div className="flex items-center gap-1.5">
            <label className="text-xs font-medium text-slate-500">To</label>
            <input
              type="date"
              value={toDate}
              min={fromDate || undefined}
              onChange={(e) => setToDate(e.target.value)}
              className="px-2.5 py-1.5 text-sm border border-slate-300 rounded-lg outline-none focus:ring-2 focus:ring-accent-500"
            />
          </div>
          {(fromDate || toDate) && (
            <button
              type="button"
              onClick={() => { setFromDate(''); setToDate('') }}
              className="text-xs text-slate-500 hover:text-red-500 transition-colors"
            >
              Clear dates
            </button>
          )}

          <FilterPopover fields={FILTER_FIELDS} values={filters} onChange={setFilters} onClear={() => setFilters(EMPTY_FILTERS)} />
          {(search || fromDate || toDate || Object.values(filters).some(Boolean)) && (
            <span className="text-xs text-slate-500">{totalCount} result{totalCount !== 1 ? 's' : ''}</span>
          )}
        </div>

        <ActiveFilterChips fields={FILTER_FIELDS} values={filters} onChange={setFilters} />

        <DataTable<DeviceOtaHistoryRow>
          flat
          columns={columns}
          data={pagedData}
          isLoading={isLoading}
          pagination={pagination}
          onPageChange={(p) => setPage(p)}
          onPageSizeChange={(s) => { setPageSize(s); setPage(1) }}
          keyExtractor={(r, idx) => `${r.deviceId}-${r.firmwareVersion}-${idx}`}
          emptyMessage="No OTA history found"
        />
      </div>
    </div>
  )
}
