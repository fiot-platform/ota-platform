'use client'

import * as React from 'react'
import { useQuery } from '@tanstack/react-query'
import { ChevronDown, ChevronRight } from 'lucide-react'
import { reportService } from '@/services/report.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { DataTable, Column } from '@/components/ui/DataTable'
import { FilterPopover, ActiveFilterChips, FilterValues, FilterField } from '@/components/ui/FilterPopover'
import { useToast } from '@/components/ui/ToastProvider'
import { downloadBlob, formatDate } from '@/utils/formatters'
import { JobStatusBadge, ReportHeader } from '@/components/reports/shared'
import { PaginationInfo } from '@/types'

type OtaRow = Awaited<ReturnType<typeof reportService.getDeviceOtaHistory>>[number]

type DeviceGroup = {
  deviceId: string
  deviceSerial: string
  deviceName?: string
  projectName: string
  history: OtaRow[]
}

const FILTER_FIELDS: FilterField[] = [
  {
    key: 'jobStatus',
    label: 'Job Status',
    options: [
      { label: 'Pending', value: 'pending' },
      { label: 'In Progress', value: 'inProgress' },
      { label: 'Completed', value: 'completed' },
      { label: 'Failed', value: 'failed' },
      { label: 'Cancelled', value: 'cancelled' },
    ],
  },
]
const EMPTY_FILTERS: FilterValues = { jobStatus: '' }

export default function DeviceOtaHistoryPage() {
  const { toast } = useToast()
  const [exporting, setExporting] = React.useState(false)
  const [search, setSearch] = React.useState('')
  const [filters, setFilters] = React.useState<FilterValues>(EMPTY_FILTERS)
  const [expanded, setExpanded] = React.useState<Set<string>>(new Set())
  const [page, setPage] = React.useState(1)
  const [pageSize, setPageSize] = React.useState(10)

  const { data, isLoading } = useQuery({
    queryKey: ['report-device-ota-history'],
    queryFn: () => reportService.getDeviceOtaHistory(),
  })

  const grouped: DeviceGroup[] = React.useMemo(() => {
    if (!data) return []
    const dMap = new Map<string, DeviceGroup>()
    for (const row of data) {
      if (!dMap.has(row.deviceId))
        dMap.set(row.deviceId, { deviceId: row.deviceId, deviceSerial: row.deviceSerial, deviceName: row.deviceName, projectName: row.projectName, history: [] })
      dMap.get(row.deviceId)!.history.push(row)
    }
    return Array.from(dMap.values())
  }, [data])

  // Filter devices by search
  const filteredGroups = React.useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return grouped
    return grouped.filter(
      (d) => d.deviceSerial.toLowerCase().includes(q) ||
             (d.deviceName ?? '').toLowerCase().includes(q) ||
             d.projectName.toLowerCase().includes(q),
    )
  }, [grouped, search])

  // Paginate device groups
  const totalCount = filteredGroups.length
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
  const pagedGroups = filteredGroups.slice((page - 1) * pageSize, page * pageSize)
  const pagination: PaginationInfo = { page, pageSize, totalCount, totalPages, hasNextPage: page < totalPages, hasPreviousPage: page > 1 }

  React.useEffect(() => { setPage(1) }, [search])

  const toggle = (id: string) =>
    setExpanded((prev) => { const s = new Set(prev); s.has(id) ? s.delete(id) : s.add(id); return s })

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

  // Inner table columns (per-device job history)
  const jobColumns: Column<OtaRow>[] = [
    { key: 'firmwareVersion', header: 'Firmware Version', cell: (j) => <span className="font-mono">{j.firmwareVersion}</span> },
    { key: 'jobStatus', header: 'Status', cell: (j) => <JobStatusBadge status={j.jobStatus} /> },
    { key: 'startedAt', header: 'Started', cell: (j) => <span className="text-slate-500">{j.startedAt ? formatDate(j.startedAt, 'dd MMM yyyy HH:mm') : '—'}</span> },
    { key: 'completedAt', header: 'Completed', cell: (j) => <span className="text-slate-500">{j.completedAt ? formatDate(j.completedAt, 'dd MMM yyyy HH:mm') : '—'}</span> },
  ]

  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        title="Device-wise OTA History"
        subtitle="Per-device OTA job history across all rollouts"
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Reports', href: '/reports/firmware-trends' },
          { label: 'Device OTA History' },
        ]}
      />

      <div className="card p-6 space-y-4">
        <ReportHeader title="Device-wise OTA History" subtitle="Expand a device to view its full OTA job history"
          reportType="device-ota-history" exporting={exporting} onExport={handleExport} />

        {/* Search + Filter */}
        <div className="flex flex-wrap items-center gap-3">
          <input
            type="search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search by serial, device name or project…"
            className="flex-1 min-w-[220px] max-w-sm px-3 py-2 text-sm border border-slate-300 rounded-lg outline-none focus:ring-2 focus:ring-accent-500 transition"
          />
          <FilterPopover fields={FILTER_FIELDS} values={filters} onChange={setFilters} onClear={() => setFilters(EMPTY_FILTERS)} />
          {(search || Object.values(filters).some(Boolean)) && (
            <span className="text-xs text-slate-500">{totalCount} device{totalCount !== 1 ? 's' : ''}</span>
          )}
        </div>

        <ActiveFilterChips fields={FILTER_FIELDS} values={filters} onChange={setFilters} />

        {isLoading ? (
          <div className="space-y-2">
            {Array.from({ length: 4 }).map((_, i) => (
              <div key={i} className="h-12 bg-slate-100 rounded-lg animate-pulse" />
            ))}
          </div>
        ) : !filteredGroups.length ? (
          <p className="text-sm text-slate-500 text-center py-10">No devices found.</p>
        ) : (
          <div className="space-y-2">
            {pagedGroups.map((device) => {
              // Apply job status filter to history rows
              const history = filters.jobStatus
                ? device.history.filter((j) => j.jobStatus.toLowerCase() === filters.jobStatus.toLowerCase())
                : device.history

              return (
                <div key={device.deviceId} className="border border-slate-200 rounded-lg overflow-hidden">
                  <button
                    onClick={() => toggle(device.deviceId)}
                    className="w-full flex items-center gap-3 px-4 py-3 bg-slate-50 hover:bg-slate-100 transition-colors text-left"
                  >
                    {expanded.has(device.deviceId)
                      ? <ChevronDown className="w-4 h-4 text-slate-400 shrink-0" />
                      : <ChevronRight className="w-4 h-4 text-slate-400 shrink-0" />}
                    <span className="font-mono font-semibold text-primary-800">{device.deviceSerial}</span>
                    {device.deviceName && <span className="text-xs text-slate-500">{device.deviceName}</span>}
                    <span className="text-xs text-slate-400">{device.projectName}</span>
                    <span className="ml-auto text-xs text-slate-400 shrink-0">
                      {history.length} job{history.length !== 1 ? 's' : ''}
                    </span>
                  </button>

                  {expanded.has(device.deviceId) && (
                    <div className="px-4 pb-3 bg-white">
                      <DataTable<OtaRow>
                        flat
                        columns={jobColumns}
                        data={history}
                        keyExtractor={(j, idx) => `${device.deviceId}-${idx}`}
                        emptyMessage="No jobs match the current filter"
                      />
                    </div>
                  )}
                </div>
              )
            })}
          </div>
        )}

        {/* Pagination for device groups */}
        {!isLoading && totalCount > pageSize && (
          <div className="border-t border-slate-200 pt-3">
            <div className="flex items-center justify-between text-sm text-slate-500">
              <span>
                Showing {(page - 1) * pageSize + 1}–{Math.min(page * pageSize, totalCount)} of {totalCount} devices
              </span>
              <div className="flex items-center gap-1">
                <button
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                  disabled={page === 1}
                  className="px-3 py-1 rounded-lg border border-slate-200 hover:bg-slate-50 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
                >
                  Prev
                </button>
                <span className="px-3 py-1 text-xs">
                  {page} / {totalPages}
                </span>
                <button
                  onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                  disabled={page === totalPages}
                  className="px-3 py-1 rounded-lg border border-slate-200 hover:bg-slate-50 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
                >
                  Next
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
