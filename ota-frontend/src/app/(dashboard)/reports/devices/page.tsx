'use client'

import * as React from 'react'
import { useQuery } from '@tanstack/react-query'
import { reportService } from '@/services/report.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { DataTable, Column } from '@/components/ui/DataTable'
import { FilterPopover, ActiveFilterChips, FilterValues, FilterField } from '@/components/ui/FilterPopover'
import { useToast } from '@/components/ui/ToastProvider'
import { downloadBlob, formatDate } from '@/utils/formatters'
import { DeviceStatusBadge, ReportHeader } from '@/components/reports/shared'
import { DeviceReport, PaginationInfo } from '@/types'

const FILTER_FIELDS: FilterField[] = [
  {
    key: 'status',
    label: 'Status',
    options: [
      { label: 'Online', value: 'online' },
      { label: 'Offline', value: 'offline' },
      { label: 'Suspended', value: 'suspended' },
    ],
  },
]

const EMPTY_FILTERS: FilterValues = { status: '' }

export default function DevicesReportPage() {
  const { toast } = useToast()
  const [exporting, setExporting] = React.useState(false)
  const [search, setSearch] = React.useState('')
  const [filters, setFilters] = React.useState<FilterValues>(EMPTY_FILTERS)
  const [page, setPage] = React.useState(1)
  const [pageSize, setPageSize] = React.useState(10)

  const { data: raw, isLoading } = useQuery({
    queryKey: ['report-devices'],
    queryFn: () => reportService.getDevicesReport(),
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
      (d) =>
        (d.macImeiIp ?? '').toLowerCase().includes(q) ||
        d.serialNumber.toLowerCase().includes(q) ||
        (d.customerName ?? '').toLowerCase().includes(q) ||
        d.projectName.toLowerCase().includes(q) ||
        (d.model ?? '').toLowerCase().includes(q) ||
        (d.currentFirmwareVersion ?? '').toLowerCase().includes(q),
    )
    if (filters.status) rows = rows.filter((d) => d.status.toLowerCase() === filters.status.toLowerCase())
    return rows
  }, [raw, search, filters])

  const totalCount = filtered.length
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
  const pagedData = filtered.slice((page - 1) * pageSize, page * pageSize)
  const pagination: PaginationInfo = { page, pageSize, totalCount, totalPages, hasNextPage: page < totalPages, hasPreviousPage: page > 1 }

  React.useEffect(() => { setPage(1) }, [search, filters])

  const columns: Column<DeviceReport>[] = [
    { key: 'macImeiIp',              header: 'MAC / IMEI / IP',         cell: (d) => <span className="font-mono text-primary-800">{d.macImeiIp ?? d.serialNumber}</span> },
    { key: 'customerName',           header: 'Client',                  cell: (d) => <span className="text-slate-500">{d.customerName ?? '—'}</span> },
    { key: 'projectName',            header: 'Project',                 cell: (d) => <span className="text-slate-500">{d.projectName}</span> },
    { key: 'model',                  header: 'Model',                   cell: (d) => <span className="text-slate-700 font-medium">{d.model ?? '—'}</span> },
    { key: 'currentFirmwareVersion', header: 'Current Firmware',        cell: (d) => <span className="font-mono text-xs">{d.currentFirmwareVersion ?? '—'}</span> },
    { key: 'status',                 header: 'Status',                  cell: (d) => <DeviceStatusBadge status={d.status} /> },
    { key: 'lastHeartbeatAt',        header: 'Last Heartbeat',          cell: (d) => <span className="text-slate-500">{d.lastHeartbeatAt ? formatDate(d.lastHeartbeatAt, 'dd MMM yyyy HH:mm') : '—'}</span> },
    { key: 'createdAt',              header: 'Registered',              cell: (d) => <span className="text-slate-500">{formatDate(d.createdAt, 'dd MMM yyyy')}</span> },
    { key: 'lastOtaAt',              header: 'Last OTA Date',           cell: (d) => <span className="text-slate-500">{d.lastOtaAt ? formatDate(d.lastOtaAt, 'dd MMM yyyy HH:mm') : '—'}</span> },
  ]

  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        title="Devices Report"
        subtitle="All registered devices with current firmware and status"
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Reports', href: '/reports/device-status' },
          { label: 'Devices' },
        ]}
      />

      <div className="card p-6 space-y-4">
        <ReportHeader title="Devices Report" subtitle="All registered devices with current firmware and status"
          reportType="devices" exporting={exporting} onExport={handleExport} />

        <div className="flex flex-wrap items-center gap-3">
          <input
            type="search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search by MAC/IMEI, client, project, model or firmware…"
            className="flex-1 min-w-[220px] max-w-sm px-3 py-2 text-sm border border-slate-300 rounded-lg outline-none focus:ring-2 focus:ring-accent-500 transition"
          />
          <FilterPopover fields={FILTER_FIELDS} values={filters} onChange={setFilters} onClear={() => setFilters(EMPTY_FILTERS)} />
          {(search || Object.values(filters).some(Boolean)) && (
            <span className="text-xs text-slate-500">{totalCount} result{totalCount !== 1 ? 's' : ''}</span>
          )}
        </div>

        <ActiveFilterChips fields={FILTER_FIELDS} values={filters} onChange={setFilters} />

        <DataTable<DeviceReport>
          flat
          columns={columns}
          data={pagedData}
          isLoading={isLoading}
          pagination={pagination}
          onPageChange={(p) => setPage(p)}
          onPageSizeChange={(s) => { setPageSize(s); setPage(1) }}
          keyExtractor={(d) => d.id}
          emptyMessage="No devices found"
        />
      </div>
    </div>
  )
}
