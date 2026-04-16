'use client'

import * as React from 'react'
import { useQuery } from '@tanstack/react-query'
import { ChevronDown, ChevronRight, GitBranch } from 'lucide-react'
import { reportService } from '@/services/report.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { DataTable, Column } from '@/components/ui/DataTable'
import { FilterPopover, ActiveFilterChips, FilterValues, FilterField } from '@/components/ui/FilterPopover'
import { useToast } from '@/components/ui/ToastProvider'
import { downloadBlob, formatDate } from '@/utils/formatters'
import { FwBadge, ChannelBadge, ReportHeader } from '@/components/reports/shared'

type RepoFwRow = Awaited<ReturnType<typeof reportService.getProjectRepoFirmwareReport>>[number]

type RepoGroup = { repositoryId: string; repositoryName: string; firmware: RepoFwRow[] }
type ProjectGroup = { projectId: string; projectName: string; customerName: string; repos: RepoGroup[] }

const FILTER_FIELDS: FilterField[] = [
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
  {
    key: 'firmwareStatus',
    label: 'Firmware Status',
    options: [
      { label: 'Draft', value: 'draft' },
      { label: 'Pending Approval', value: 'pendingApproval' },
      { label: 'Approved', value: 'approved' },
      { label: 'Rejected', value: 'rejected' },
    ],
  },
]
const EMPTY_FILTERS: FilterValues = { channel: '', firmwareStatus: '' }

export default function ProjectRepoFirmwarePage() {
  const { toast } = useToast()
  const [exporting, setExporting] = React.useState(false)
  const [search, setSearch] = React.useState('')
  const [filters, setFilters] = React.useState<FilterValues>(EMPTY_FILTERS)
  const [expanded, setExpanded] = React.useState<Set<string>>(new Set())
  const [page, setPage] = React.useState(1)
  const [pageSize] = React.useState(10)

  const { data, isLoading } = useQuery({
    queryKey: ['report-project-repo-firmware'],
    queryFn: () => reportService.getProjectRepoFirmwareReport(),
  })

  const grouped: ProjectGroup[] = React.useMemo(() => {
    if (!data) return []
    const pMap = new Map<string, ProjectGroup>()
    for (const row of data) {
      if (!pMap.has(row.projectId))
        pMap.set(row.projectId, { projectId: row.projectId, projectName: row.projectName, customerName: row.customerName, repos: [] })
      const proj = pMap.get(row.projectId)!
      let repo = proj.repos.find((r) => r.repositoryId === row.repositoryId)
      if (!repo) { repo = { repositoryId: row.repositoryId, repositoryName: row.repositoryName, firmware: [] }; proj.repos.push(repo) }
      repo.firmware.push(row)
    }
    return Array.from(pMap.values())
  }, [data])

  const filteredProjects = React.useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return grouped
    return grouped.filter(
      (p) => p.projectName.toLowerCase().includes(q) || p.customerName.toLowerCase().includes(q),
    )
  }, [grouped, search])

  const totalCount = filteredProjects.length
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
  const pagedProjects = filteredProjects.slice((page - 1) * pageSize, page * pageSize)

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

  const fwColumns: Column<RepoFwRow>[] = [
    { key: 'firmwareVersion', header: 'Version', cell: (f) => <span className="font-mono">{f.firmwareVersion}</span> },
    { key: 'channel', header: 'Channel', cell: (f) => <ChannelBadge channel={f.channel} /> },
    { key: 'firmwareStatus', header: 'Status', cell: (f) => <FwBadge status={f.firmwareStatus} /> },
    { key: 'firmwareCreatedAt', header: 'Created', cell: (f) => <span className="text-slate-500">{formatDate(f.firmwareCreatedAt, 'dd MMM yyyy')}</span> },
  ]

  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        title="Project-wise Repositories & Firmware"
        subtitle="Each project's repositories and their associated firmware versions"
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Reports', href: '/reports/firmware-trends' },
          { label: 'Project Repos & Firmware' },
        ]}
      />

      <div className="card p-6 space-y-4">
        <ReportHeader title="Project-wise Repositories & Firmware Versions"
          subtitle="Expand a project to view its repositories and firmware"
          reportType="project-repo-firmware" exporting={exporting} onExport={handleExport} />

        {/* Search + Filter */}
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
            <span className="text-xs text-slate-500">{totalCount} project{totalCount !== 1 ? 's' : ''}</span>
          )}
        </div>

        <ActiveFilterChips fields={FILTER_FIELDS} values={filters} onChange={setFilters} />

        {isLoading ? (
          <div className="space-y-2">
            {Array.from({ length: 4 }).map((_, i) => (
              <div key={i} className="h-12 bg-slate-100 rounded-lg animate-pulse" />
            ))}
          </div>
        ) : !filteredProjects.length ? (
          <p className="text-sm text-slate-500 text-center py-10">No projects found.</p>
        ) : (
          <div className="space-y-2">
            {pagedProjects.map((project) => (
              <div key={project.projectId} className="border border-slate-200 rounded-lg overflow-hidden">
                <button
                  onClick={() => toggle(project.projectId)}
                  className="w-full flex items-center gap-3 px-4 py-3 bg-slate-50 hover:bg-slate-100 transition-colors text-left"
                >
                  {expanded.has(project.projectId)
                    ? <ChevronDown className="w-4 h-4 text-slate-400 shrink-0" />
                    : <ChevronRight className="w-4 h-4 text-slate-400 shrink-0" />}
                  <span className="font-semibold text-primary-800">{project.projectName}</span>
                  <span className="text-xs text-slate-400">{project.customerName}</span>
                  <span className="ml-auto text-xs text-slate-400 shrink-0">
                    {project.repos.length} repo{project.repos.length !== 1 ? 's' : ''}
                  </span>
                </button>

                {expanded.has(project.projectId) && (
                  <div className="divide-y divide-slate-100">
                    {project.repos.map((repo) => {
                      const firmware = (filters.channel || filters.firmwareStatus)
                        ? repo.firmware.filter((f) => {
                            if (filters.channel && f.channel.toLowerCase() !== filters.channel.toLowerCase()) return false
                            if (filters.firmwareStatus && f.firmwareStatus.toLowerCase() !== filters.firmwareStatus.toLowerCase()) return false
                            return true
                          })
                        : repo.firmware

                      return (
                        <div key={repo.repositoryId}>
                          <div className="flex items-center gap-2 px-6 py-2.5 bg-white text-sm">
                            <GitBranch className="w-3.5 h-3.5 text-slate-400 shrink-0" />
                            <span className="font-medium text-slate-700">{repo.repositoryName}</span>
                            <span className="ml-auto text-xs text-slate-400 shrink-0">
                              {firmware.length} version{firmware.length !== 1 ? 's' : ''}
                            </span>
                          </div>
                          {firmware.length > 0 && (
                            <div className="pl-10 pr-4 pb-3 bg-white">
                              <DataTable<RepoFwRow>
                                flat
                                columns={fwColumns}
                                data={firmware}
                                keyExtractor={(f, idx) => `${repo.repositoryId}-${idx}`}
                                emptyMessage="No firmware versions match the filter"
                              />
                            </div>
                          )}
                        </div>
                      )
                    })}
                  </div>
                )}
              </div>
            ))}
          </div>
        )}

        {/* Pagination for project groups */}
        {!isLoading && totalCount > pageSize && (
          <div className="border-t border-slate-200 pt-3">
            <div className="flex items-center justify-between text-sm text-slate-500">
              <span>
                Showing {(page - 1) * pageSize + 1}–{Math.min(page * pageSize, totalCount)} of {totalCount} projects
              </span>
              <div className="flex items-center gap-1">
                <button
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                  disabled={page === 1}
                  className="px-3 py-1 rounded-lg border border-slate-200 hover:bg-slate-50 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
                >
                  Prev
                </button>
                <span className="px-3 py-1 text-xs">{page} / {totalPages}</span>
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
