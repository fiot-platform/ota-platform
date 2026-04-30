'use client'

import * as React from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  FolderOpen,
  GitBranch,
  Cpu,
  MonitorSmartphone,
  RefreshCw,
  Clock,
  FlaskConical,
  Tag,
  Building2,
  CheckCircle2,
  XCircle,
  AlertCircle,
  Users,
  ShieldCheck,
  Wifi,
  WifiOff,
  Loader2,
  ArrowRight,
  ChevronRight,
  Bell,
} from 'lucide-react'
import Link from 'next/link'
import { reportService } from '@/services/report.service'
import { auditService } from '@/services/audit.service'
import { authService } from '@/services/auth.service'
import { projectService } from '@/services/project.service'
import { firmwareService } from '@/services/firmware.service'
import { repositoryService } from '@/services/repository.service'
import { deviceService } from '@/services/device.service'
import { clientService } from '@/services/client.service'
import NotificationService from '@/services/notification.service'
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer,
} from 'recharts'
import { StatCard } from '@/components/ui/StatCard'
import { PageHeader } from '@/components/ui/PageHeader'
import { DataTable, Column } from '@/components/ui/DataTable'
import { StatusBadge, RoleBadge } from '@/components/ui/Badge'
import { FirmwareTrendChart } from '@/components/charts/FirmwareTrendChart'
import { RolloutSuccessChart } from '@/components/charts/RolloutSuccessChart'
import { DeviceStatusChart } from '@/components/charts/DeviceStatusChart'
import { DeviceHealthCard } from '@/components/charts/DeviceHealthCard'
import { useAuth } from '@/hooks/useAuth'
import { AuditLog, FirmwareStatus, FirmwareVersion, Repository, UserRole } from '@/types'
import { formatRelativeTime, formatDate } from '@/utils/formatters'

// ─── Greeting helper ──────────────────────────────────────────────────────────

function getGreeting() {
  const h = new Date().getHours()
  if (h < 12) return 'Good morning'
  if (h < 17) return 'Good afternoon'
  return 'Good evening'
}

// ─── SuperAdmin / PlatformAdmin Dashboard ────────────────────────────────────

function SuperAdminDashboard({ userName, role }: { userName: string; role: UserRole }) {
  const isSuperAdmin = role === UserRole.SuperAdmin

  const { data: summary, isLoading: summaryLoading } = useQuery({
    queryKey: ['dashboard-summary'],
    queryFn: () => reportService.getDashboardSummary(),
    refetchInterval: 60_000,
  })

  const { data: firmwareTrends, isLoading: trendsLoading } = useQuery({
    queryKey: ['firmware-trends', 30],
    queryFn: () => reportService.getFirmwareTrends(30),
  })

  const { data: rolloutRates, isLoading: rolloutRatesLoading } = useQuery({
    queryKey: ['rollout-success-rate'],
    queryFn: () => reportService.getRolloutSuccessRate(),
  })

  const { data: deviceStatus, isLoading: deviceStatusLoading } = useQuery({
    queryKey: ['device-update-status'],
    queryFn: () => reportService.getDeviceUpdateStatus(),
  })

  const { data: recentAuditLogs, isLoading: auditLoading } = useQuery({
    queryKey: ['recent-audit-logs'],
    queryFn: () => auditService.getAuditLogs({ pageSize: 10, page: 1 }),
  })

  const auditColumns: Column<AuditLog>[] = [
    {
      key: 'timestamp',
      header: 'Time',
      cell: (row) => (
        <span className="text-xs text-slate-500 whitespace-nowrap">
          {formatRelativeTime(row.timestamp)}
        </span>
      ),
    },
    {
      key: 'action',
      header: 'Action',
      cell: (row) => (
        <span className="text-sm font-medium text-primary-800">
          {row.action.replace(/([A-Z])/g, ' $1').trim()}
        </span>
      ),
    },
    {
      key: 'performedBy',
      header: 'Performed By',
      cell: (row) => (
        <div>
          <p className="text-sm text-primary-800">{row.performedByName ?? row.performedBy}</p>
          {row.performedByRole && <RoleBadge role={row.performedByRole} className="mt-0.5" />}
        </div>
      ),
    },
    {
      key: 'entity',
      header: 'Entity',
      cell: (row) => (
        <div className="text-sm">
          {row.entityType && <span className="text-slate-500">{row.entityType}</span>}
          {row.entityName && (
            <p className="text-primary-700 font-medium truncate max-w-[120px]">{row.entityName}</p>
          )}
        </div>
      ),
    },
  ]

  const deviceStatusData =
    deviceStatus
      ?.flatMap((d) => [
        { status: 'Up to Date', count: d.upToDate },
        { status: 'Update Available', count: d.updateAvailable },
        { status: 'Updating', count: d.updating },
        { status: 'Failed', count: d.failed },
        { status: 'Offline', count: d.offline },
      ])
      .filter((d) => d.count > 0) ?? []

  const roleName = role === UserRole.SuperAdmin ? 'Super Admin' : 'Platform Admin'

  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        title={`${getGreeting()}, ${userName}`}
        subtitle={`${roleName} — Full Platform Overview`}
      />

      {/* Pending Approvals Marquee */}
      {!summaryLoading && (summary?.pendingApprovalFirmware ?? 0) > 0 && (
        <div className="bg-orange-50 border border-orange-200 rounded-xl px-4 py-3 flex items-center gap-3 overflow-hidden">
          <AlertCircle className="w-4 h-4 text-orange-500 flex-shrink-0 animate-pulse" />
          <div className="flex-1 overflow-hidden">
            <div className="whitespace-nowrap animate-[marquee_18s_linear_infinite]">
              <span className="text-sm font-semibold text-orange-800">
                ⚠️ Action Required: {summary?.pendingApprovalFirmware} firmware version{(summary?.pendingApprovalFirmware ?? 0) > 1 ? 's' : ''} pending your approval. &nbsp;&nbsp;&nbsp;
                Please review and approve or reject to unblock the release pipeline. &nbsp;&nbsp;&nbsp;
                ⚠️ Action Required: {summary?.pendingApprovalFirmware} firmware version{(summary?.pendingApprovalFirmware ?? 0) > 1 ? 's' : ''} pending your approval.
              </span>
            </div>
          </div>
          <Link href="/firmware" className="flex-shrink-0 text-xs font-semibold text-orange-700 bg-orange-100 hover:bg-orange-200 px-3 py-1.5 rounded-lg transition-colors whitespace-nowrap">
            Review Now →
          </Link>
        </div>
      )}

      {/* Top stats */}
      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6 gap-4">
        <StatCard
          label="Projects"
          value={summary?.totalProjects ?? 0}
          icon={<FolderOpen className="w-5 h-5" />}
          accent="navy"
          isLoading={summaryLoading}
          href="/projects"
        />
        <StatCard
          label="Repositories"
          value={summary?.totalRepositories ?? 0}
          icon={<GitBranch className="w-5 h-5" />}
          accent="primary"
          isLoading={summaryLoading}
          href="/repositories"
        />
        <StatCard
          label="Firmware Versions"
          value={summary?.totalFirmware ?? 0}
          icon={<Cpu className="w-5 h-5" />}
          accent="purple"
          isLoading={summaryLoading}
          href="/firmware"
        />
        <StatCard
          label="Active Devices"
          value={summary?.activeDevices ?? 0}
          icon={<MonitorSmartphone className="w-5 h-5" />}
          accent="green"
          isLoading={summaryLoading}
          href="/devices"
        />
        <StatCard
          label="Active Rollouts"
          value={summary?.activeRollouts ?? 0}
          icon={<RefreshCw className="w-5 h-5" />}
          accent="amber"
          isLoading={summaryLoading}
          href="/ota-rollouts"
        />
        {isSuperAdmin ? (
          <StatCard
            label="Platform Users"
            value={summary?.totalUsers ?? 0}
            icon={<Users className="w-5 h-5" />}
            accent="red"
            isLoading={summaryLoading}
            href="/users"
          />
        ) : (
          <StatCard
            label="Pending Approvals"
            value={summary?.pendingApprovals ?? 0}
            icon={<Clock className="w-5 h-5" />}
            accent="red"
            isLoading={summaryLoading}
            href="/firmware"
          />
        )}
      </div>

      {/* Health row */}
      <div className="grid grid-cols-1 sm:grid-cols-4 gap-4">
        <DeviceHealthCard
          totalDevices={summary?.totalDevices ?? 0}
          onlineDevices={summary?.activeDevices ?? 0}
          offlineDevices={summary?.offlineDevices ?? 0}
          isLoading={summaryLoading}
        />
        <div className="card p-4 flex items-center gap-3">
          <div className="w-9 h-9 rounded-xl bg-orange-50 flex items-center justify-center flex-shrink-0">
            <AlertCircle className="w-4 h-4 text-orange-500" />
          </div>
          <div>
            <p className="text-[10px] text-slate-400 font-semibold uppercase tracking-wide">Pending Approvals</p>
            <p className="text-xl font-bold text-primary-900">{summaryLoading ? '—' : (summary?.pendingApprovalFirmware ?? 0)}</p>
          </div>
        </div>
        <div className="card p-4 flex items-center gap-3">
          <div className="w-9 h-9 rounded-xl bg-warning-50 flex items-center justify-center flex-shrink-0">
            <FlaskConical className="w-4 h-4 text-warning-500" />
          </div>
          <div>
            <p className="text-[10px] text-slate-400 font-semibold uppercase tracking-wide">Pending QA</p>
            <p className="text-xl font-bold text-primary-900">{summaryLoading ? '—' : (summary?.pendingQAFirmware ?? 0)}</p>
          </div>
        </div>
        <div className="card p-4 flex items-center gap-3">
          <div className="w-9 h-9 rounded-xl bg-blue-50 flex items-center justify-center flex-shrink-0">
            <Loader2 className="w-4 h-4 text-blue-500" />
          </div>
          <div>
            <p className="text-[10px] text-slate-400 font-semibold uppercase tracking-wide">Devices Updating</p>
            <p className="text-xl font-bold text-primary-900">{summaryLoading ? '—' : (summary?.devicesUpdating ?? 0)}</p>
          </div>
        </div>
      </div>

      {/* Charts row */}
      <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
        <div className="xl:col-span-2 card p-6">
          <div className="flex items-center justify-between mb-5">
            <div>
              <h3 className="section-title">Firmware Approval Trends</h3>
              <p className="text-muted">Last 30 days</p>
            </div>
          </div>
          <FirmwareTrendChart data={firmwareTrends ?? []} isLoading={trendsLoading} />
        </div>
        <div className="card p-6">
          <div className="mb-4">
            <h3 className="section-title">Device Update Status</h3>
            <p className="text-muted">Current distribution</p>
          </div>
          <DeviceStatusChart data={deviceStatusData} isLoading={deviceStatusLoading} />
        </div>
      </div>

      {/* Rollout success + Audit */}
      <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
        <div className="xl:col-span-1 card p-6">
          <div className="mb-4">
            <h3 className="section-title">Rollout Success Rate</h3>
            <p className="text-muted">By project</p>
          </div>
          <RolloutSuccessChart data={rolloutRates ?? []} isLoading={rolloutRatesLoading} />
        </div>
        <div className="xl:col-span-2 card overflow-hidden">
          <div className="p-6 border-b border-slate-100">
            <div className="flex items-center justify-between">
              <div>
                <h3 className="section-title">Recent Audit Events</h3>
                <p className="text-muted">Last 10 system events</p>
              </div>
              <Link href="/audit-logs" className="text-sm text-accent-600 hover:text-accent-700 font-medium transition-colors">
                View all
              </Link>
            </div>
          </div>
          <div className="overflow-y-auto max-h-[200px] [&::-webkit-scrollbar]:w-1.5 [&::-webkit-scrollbar-track]:bg-slate-50 [&::-webkit-scrollbar-thumb]:bg-slate-300 [&::-webkit-scrollbar-thumb]:rounded-full">
            <DataTable
              columns={auditColumns}
              data={recentAuditLogs?.items ?? []}
              isLoading={auditLoading}
              keyExtractor={(row) => row.id}
              emptyMessage="No recent audit events"
              stickyHeader
              flat
            />
          </div>
        </div>
      </div>
    </div>
  )
}

// ─── Release Manager Dashboard ────────────────────────────────────────────────

function ReleaseManagerDashboard({ userName }: { userName: string }) {
  const [otaWindow, setOtaWindow]   = React.useState<'today' | 'week' | 'month'>('today')
  const [trendWindow, setTrendWindow] = React.useState<'today' | 'week' | 'month' | 'year'>('year')

  // ── Counts (section 1) — projects, repositories, firmwares, devices, clients ─
  const { data: summary, isLoading: summaryLoading } = useQuery({
    queryKey: ['rm-dashboard-summary'],
    queryFn: () => reportService.getDashboardSummary(),
    refetchInterval: 60_000,
  })
  const { data: clientsData, isLoading: clientsLoading } = useQuery({
    queryKey: ['rm-dashboard-clients'],
    queryFn: () => clientService.getClients({ pageSize: 1 }),
  })
  const clientCount = clientsData?.pagination?.totalCount ?? clientsData?.items.length ?? 0

  // Per-device snapshot — drives every fleet-related stat.
  const { data: devicesData, isLoading: devicesLoading } = useQuery({
    queryKey: ['rm-dashboard-devices'],
    queryFn: () => deviceService.getDevices({ pageSize: 500 }),
    refetchInterval: 30_000,
  })
  const devices = devicesData?.items ?? []

  // 14-day OTA outcome trend
  const { data: daily, isLoading: dailyLoading } = useQuery({
    queryKey: ['rm-dashboard-daily', 14],
    queryFn: () => reportService.getDailyOtaProgress(14),
  })

  // Per-event history — used only for "Today" totals
  const { data: history, isLoading: historyLoading } = useQuery({
    queryKey: ['rm-dashboard-ota-history'],
    queryFn: () => reportService.getDeviceOtaHistory(),
    refetchInterval: 60_000,
  })

  // Approved firmware — for "latest version" lookup
  const { data: firmwareData, isLoading: firmwareLoading } = useQuery({
    queryKey: ['rm-dashboard-approved-firmware'],
    queryFn: () => firmwareService.getFirmwareList({ status: FirmwareStatus.Approved, pageSize: 50 }),
  })
  const approvedFirmware = firmwareData?.items ?? []

  // ── "Latest approved version" per model + global fallback ───────────────
  const normalizeVersion = (v: string | undefined | null) =>
    (v ?? '').trim().toLowerCase().replace(/^v/, '')
  const isNewer = (a: string, b: string) =>
    a.localeCompare(b, undefined, { numeric: true }) > 0

  const { latestPerModel, latestGlobal } = React.useMemo(() => {
    const perModel = new Map<string, string>()
    let global: string | null = null
    for (const f of approvedFirmware) {
      const v = normalizeVersion(f.version)
      if (!v) continue
      if (!global || isNewer(v, global)) global = v
      const models = f.supportedModels?.length ? f.supportedModels : []
      for (const model of models) {
        const cur = perModel.get(model)
        if (!cur || isNewer(v, cur)) perModel.set(model, v)
      }
    }
    return { latestPerModel: perModel, latestGlobal: global }
  }, [approvedFirmware])

  const targetFor = React.useCallback((deviceModel: string) =>
    latestPerModel.get(deviceModel) ?? latestGlobal ?? null,
    [latestPerModel, latestGlobal])

  // ── Yearly OTA trend — pull 365 days of daily progress, group by month ──
  const { data: yearly, isLoading: yearlyLoading } = useQuery({
    queryKey: ['rm-dashboard-yearly', 365],
    queryFn: () => reportService.getDailyOtaProgress(365),
  })

  // ── Inbox notifications (latest 5) ──────────────────────────────────────
  const { data: inbox, isLoading: inboxLoading } = useQuery({
    queryKey: ['rm-dashboard-inbox'],
    queryFn: () => NotificationService.getInbox(5),
    refetchInterval: 60_000,
  })

  // ── Section 1: counts ──────────────────────────────────────────────────
  const counts = {
    projects:     summary?.totalProjects     ?? 0,
    repositories: summary?.totalRepositories ?? 0,
    firmwares:    summary?.totalFirmware     ?? 0,
    devices:      summary?.totalDevices      ?? devices.length,
    clients:      clientCount,
  }

  // ── Section 2: Last OTA push progress — counts within selected window ──
  const windowStart = React.useMemo(() => {
    const t = new Date()
    if (otaWindow === 'today') {
      t.setHours(0, 0, 0, 0)
    } else if (otaWindow === 'week') {
      // last 7 days inclusive of today
      t.setHours(0, 0, 0, 0)
      t.setDate(t.getDate() - 6)
    } else {
      // last 30 days inclusive of today
      t.setHours(0, 0, 0, 0)
      t.setDate(t.getDate() - 29)
    }
    return t.getTime()
  }, [otaWindow])

  const otaWindowEvents = (history ?? []).filter((r) => {
    const t = r.completedAt ?? r.startedAt ?? r.pushedAt
    return t && new Date(t).getTime() >= windowStart
  })
  const otaWindowComplete = otaWindowEvents.filter((r) => r.jobStatus === 'Succeeded').length
  const otaWindowFailed   = otaWindowEvents.filter((r) => r.jobStatus === 'Failed' || r.jobStatus === 'Cancelled').length
  // Pending = active jobs right now (not historical for the window)
  const otaWindowPending  = devices.filter((d) => d.hasActiveOtaJob === true).length
  const otaWindowTotal    = otaWindowComplete + otaWindowFailed + otaWindowPending
  const pct = (n: number) => otaWindowTotal > 0 ? Math.round((n / otaWindowTotal) * 100) : 0

  // ── Section 4: OTA status trend — buckets per selected window ──────────
  // Today  → 24 hourly buckets (00:00 → 23:00)
  // Week   → 7 daily buckets (last 7 days, today inclusive)
  // Month  → 1 bar per day for the current month so far
  // Year   → 12 monthly buckets (Jan–Dec for the current year)
  //
  // Today/Week/Month buckets come from per-event `history` data so we can split
  // by hour/day. Year still uses the daily aggregate (`yearly`) since per-event
  // history may not span a whole year.
  type TrendBucket = {
    label: string
    succeeded: number
    failed: number
    inProgress: number
    queued: number
    cancelled: number
  }

  const trendBuckets: TrendBucket[] = React.useMemo(() => {
    const events = history ?? []

    if (trendWindow === 'today') {
      const buckets: TrendBucket[] = Array.from({ length: 24 }, (_, h) => ({
        label: String(h).padStart(2, '0'),
        succeeded: 0, failed: 0, inProgress: 0, queued: 0, cancelled: 0,
      }))
      const startOfDay = new Date(); startOfDay.setHours(0, 0, 0, 0)
      const startMs = startOfDay.getTime()
      for (const r of events) {
        const stamp = r.completedAt ?? r.startedAt ?? r.pushedAt
        if (!stamp) continue
        const t = new Date(stamp).getTime()
        if (t < startMs) continue
        const hour = new Date(t).getHours()
        const b = buckets[hour]
        if (r.jobStatus === 'Succeeded')  b.succeeded++
        else if (r.jobStatus === 'Failed') b.failed++
        else if (r.jobStatus === 'InProgress') b.inProgress++
        else if (r.jobStatus === 'Queued' || r.jobStatus === 'Pending') b.queued++
        else if (r.jobStatus === 'Cancelled') b.cancelled++
      }
      return buckets
    }

    if (trendWindow === 'week') {
      // Last 7 calendar days, today on the right
      const buckets: TrendBucket[] = []
      const day0 = new Date(); day0.setHours(0, 0, 0, 0)
      day0.setDate(day0.getDate() - 6)
      for (let i = 0; i < 7; i++) {
        const d = new Date(day0); d.setDate(day0.getDate() + i)
        buckets.push({
          label: `${d.getDate()} ${d.toLocaleString('default', { month: 'short' })}`,
          succeeded: 0, failed: 0, inProgress: 0, queued: 0, cancelled: 0,
        })
      }
      for (const r of events) {
        const stamp = r.completedAt ?? r.startedAt ?? r.pushedAt
        if (!stamp) continue
        const t = new Date(stamp); t.setHours(0, 0, 0, 0)
        const idx = Math.round((t.getTime() - day0.getTime()) / (24 * 60 * 60 * 1000))
        if (idx < 0 || idx > 6) continue
        const b = buckets[idx]
        if (r.jobStatus === 'Succeeded')  b.succeeded++
        else if (r.jobStatus === 'Failed') b.failed++
        else if (r.jobStatus === 'InProgress') b.inProgress++
        else if (r.jobStatus === 'Queued' || r.jobStatus === 'Pending') b.queued++
        else if (r.jobStatus === 'Cancelled') b.cancelled++
      }
      return buckets
    }

    if (trendWindow === 'month') {
      // 1 bar per day for the current calendar month
      const now = new Date()
      const year = now.getFullYear()
      const month = now.getMonth()
      const daysInMonth = new Date(year, month + 1, 0).getDate()
      const monthStart = new Date(year, month, 1).getTime()
      const buckets: TrendBucket[] = Array.from({ length: daysInMonth }, (_, i) => ({
        label: String(i + 1),
        succeeded: 0, failed: 0, inProgress: 0, queued: 0, cancelled: 0,
      }))
      for (const r of events) {
        const stamp = r.completedAt ?? r.startedAt ?? r.pushedAt
        if (!stamp) continue
        const t = new Date(stamp)
        if (t.getFullYear() !== year || t.getMonth() !== month) continue
        const dayIdx = t.getDate() - 1
        const b = buckets[dayIdx]
        if (r.jobStatus === 'Succeeded')  b.succeeded++
        else if (r.jobStatus === 'Failed') b.failed++
        else if (r.jobStatus === 'InProgress') b.inProgress++
        else if (r.jobStatus === 'Queued' || r.jobStatus === 'Pending') b.queued++
        else if (r.jobStatus === 'Cancelled') b.cancelled++
      }
      // suppress unused-variable warning for monthStart
      void monthStart
      return buckets
    }

    // year — group the daily aggregate into 12 month buckets
    const monthLabels = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec']
    const currentYear = new Date().getFullYear()
    const buckets: TrendBucket[] = monthLabels.map((label) => ({
      label,
      succeeded: 0, failed: 0, inProgress: 0, queued: 0, cancelled: 0,
    }))
    for (const d of (yearly ?? [])) {
      const dt = new Date(d.date)
      if (dt.getFullYear() !== currentYear) continue
      const b = buckets[dt.getMonth()]
      b.succeeded  += d.succeeded  ?? 0
      b.failed     += d.failed     ?? 0
      b.inProgress += d.inProgress ?? 0
      b.queued     += d.queued     ?? 0
      b.cancelled  += d.cancelled  ?? 0
    }
    return buckets
  }, [trendWindow, history, yearly])

  const trendCardSubtitle =
    trendWindow === 'today' ? 'Hourly breakdown for today'
    : trendWindow === 'week'  ? 'Daily breakdown for the last 7 days'
    : trendWindow === 'month' ? `Daily breakdown for ${new Date().toLocaleString('default', { month: 'long' })}`
    :                            `Monthly breakdown across all rollouts (${new Date().getFullYear()})`

  return (
    <div className="space-y-6 animate-fade-in">

      <PageHeader
        title={`${getGreeting()}, ${userName}`}
        subtitle="Release Manager — Fleet OTA Overview"
      />

      {/* ── 1. COUNTS ─────────────────────────────────────────────────────── */}
      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-4 [&>*]:h-full">
        <StatCard
          label="Projects"
          value={counts.projects}
          icon={<FolderOpen className="w-5 h-5" />}
          accent="purple"
          href="/projects"
          isLoading={summaryLoading}
        />
        <StatCard
          label="Repositories"
          value={counts.repositories}
          icon={<GitBranch className="w-5 h-5" />}
          accent="primary"
          href="/repositories"
          isLoading={summaryLoading}
        />
        <StatCard
          label="Firmwares"
          value={counts.firmwares}
          icon={<Cpu className="w-5 h-5" />}
          accent="navy"
          href="/firmware"
          isLoading={summaryLoading}
        />
        <StatCard
          label="Devices"
          value={counts.devices}
          icon={<MonitorSmartphone className="w-5 h-5" />}
          accent="green"
          href="/devices"
          isLoading={summaryLoading || devicesLoading}
        />
        <StatCard
          label="Clients"
          value={counts.clients}
          icon={<Building2 className="w-5 h-5" />}
          accent="amber"
          href="/clients"
          isLoading={clientsLoading}
        />
      </div>

      {/* ── 2 + 3 — OTA progress (with toggle) + Firmware approvals ───────── */}
      <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
        {/* Last OTA push progress */}
        <div className="xl:col-span-2 card p-6">
          <div className="flex items-center justify-between flex-wrap gap-3 mb-5">
            <div>
              <h3 className="section-title">Last OTA push progress</h3>
              <p className="text-muted">Pending · Complete · Failed for the selected window</p>
            </div>
            <div className="inline-flex p-0.5 rounded-lg bg-slate-100">
              {(['today', 'week', 'month'] as const).map((w) => (
                <button
                  key={w}
                  onClick={() => setOtaWindow(w)}
                  className={`px-3 py-1.5 text-xs font-medium rounded-md transition-colors ${
                    otaWindow === w
                      ? 'bg-white text-primary-900 shadow-sm'
                      : 'text-slate-500 hover:text-primary-700'
                  }`}
                >
                  {w === 'today' ? 'Today' : w === 'week' ? 'This week' : 'This month'}
                </button>
              ))}
            </div>
          </div>

          {historyLoading || devicesLoading ? (
            <div className="space-y-4">
              {Array.from({ length: 3 }).map((_, i) =>
                <div key={i} className="h-12 bg-slate-100 rounded animate-pulse" />
              )}
            </div>
          ) : (
            <div className="space-y-5">
              {/* Pending */}
              <div>
                <div className="flex items-center justify-between mb-1.5">
                  <span className="flex items-center gap-2 text-sm font-medium text-primary-800">
                    <Loader2 className="w-4 h-4 text-accent-600" /> Pending
                  </span>
                  <span className="text-sm font-semibold text-accent-700 tabular-nums">
                    {otaWindowPending} <span className="text-xs text-slate-400 font-normal">({pct(otaWindowPending)}%)</span>
                  </span>
                </div>
                <div className="h-2.5 bg-slate-100 rounded-full overflow-hidden">
                  <div className="h-full bg-accent-500 rounded-full transition-all duration-500" style={{ width: `${pct(otaWindowPending)}%` }} />
                </div>
              </div>

              {/* Complete */}
              <div>
                <div className="flex items-center justify-between mb-1.5">
                  <span className="flex items-center gap-2 text-sm font-medium text-primary-800">
                    <CheckCircle2 className="w-4 h-4 text-success-600" /> Complete
                  </span>
                  <span className="text-sm font-semibold text-success-700 tabular-nums">
                    {otaWindowComplete} <span className="text-xs text-slate-400 font-normal">({pct(otaWindowComplete)}%)</span>
                  </span>
                </div>
                <div className="h-2.5 bg-slate-100 rounded-full overflow-hidden">
                  <div className="h-full bg-success-500 rounded-full transition-all duration-500" style={{ width: `${pct(otaWindowComplete)}%` }} />
                </div>
              </div>

              {/* Failed */}
              <div>
                <div className="flex items-center justify-between mb-1.5">
                  <span className="flex items-center gap-2 text-sm font-medium text-primary-800">
                    <XCircle className="w-4 h-4 text-danger-600" /> Failed
                  </span>
                  <span className="text-sm font-semibold text-danger-700 tabular-nums">
                    {otaWindowFailed} <span className="text-xs text-slate-400 font-normal">({pct(otaWindowFailed)}%)</span>
                  </span>
                </div>
                <div className="h-2.5 bg-slate-100 rounded-full overflow-hidden">
                  <div className="h-full bg-danger-500 rounded-full transition-all duration-500" style={{ width: `${pct(otaWindowFailed)}%` }} />
                </div>
              </div>

              <div className="pt-2 border-t border-slate-100 text-xs text-slate-500">
                <strong className="text-primary-800">{otaWindowTotal}</strong> total OTA event{otaWindowTotal === 1 ? '' : 's'} in this window
              </div>
            </div>
          )}
        </div>

        {/* Firmware approval count */}
        <Link
          href="/firmware?status=PendingApproval"
          className="card p-6 border-2 border-warning-200 bg-warning-50/40 hover:bg-warning-50 transition-colors group"
        >
          <div className="flex items-start justify-between">
            <div className="w-12 h-12 rounded-xl bg-warning-100 ring-1 ring-warning-200 flex items-center justify-center">
              <ShieldCheck className="w-6 h-6 text-warning-700" />
            </div>
            <span className="text-xs font-semibold text-warning-700 uppercase tracking-wide">Action needed</span>
          </div>
          <p className="text-sm font-semibold text-primary-900 mt-5">Firmware approvals</p>
          <p className="text-4xl font-black text-warning-700 mt-1 tabular-nums">
            {summaryLoading ? '—' : (summary?.pendingApprovalFirmware ?? 0)}
          </p>
          <p className="text-xs text-slate-600 mt-1.5 leading-snug">
            Firmware version{(summary?.pendingApprovalFirmware ?? 0) === 1 ? ' is' : 's are'} waiting for your sign-off
          </p>
          <span className="inline-flex items-center gap-1 text-xs font-semibold text-warning-700 mt-4 group-hover:text-warning-800">
            Review now <ArrowRight className="w-3 h-3" />
          </span>
        </Link>
      </div>

      {/* ── 4 + 5 — Yearly trend + Latest notifications side-by-side ──────── */}
      <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
        {/* OTA status trend — 2/3 width, with Today/Week/Month/Year toggle */}
        <div className="xl:col-span-2 card p-6">
          <div className="flex items-center justify-between flex-wrap gap-3 mb-4">
            <div>
              <h3 className="section-title">OTA status trend</h3>
              <p className="text-muted">{trendCardSubtitle}</p>
            </div>
            <div className="inline-flex p-0.5 rounded-lg bg-slate-100">
              {(['today', 'week', 'month', 'year'] as const).map((w) => (
                <button
                  key={w}
                  onClick={() => setTrendWindow(w)}
                  className={`px-3 py-1.5 text-xs font-medium rounded-md transition-colors ${
                    trendWindow === w
                      ? 'bg-white text-primary-900 shadow-sm'
                      : 'text-slate-500 hover:text-primary-700'
                  }`}
                >
                  {w === 'today' ? 'Today' : w === 'week' ? 'This week' : w === 'month' ? 'This month' : 'This year'}
                </button>
              ))}
            </div>
          </div>
          {((trendWindow === 'year' && yearlyLoading) || (trendWindow !== 'year' && historyLoading)) ? (
            <div className="h-72 bg-slate-100 rounded-lg animate-pulse" />
          ) : (
            <ResponsiveContainer width="100%" height={300}>
              <BarChart data={trendBuckets} margin={{ top: 4, right: 8, left: -16, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#f1f5f9" />
                <XAxis dataKey="label" tick={{ fontSize: 11, fill: '#94a3b8' }} />
                <YAxis tick={{ fontSize: 11, fill: '#94a3b8' }} allowDecimals={false} />
                <Tooltip contentStyle={{ fontSize: 12, borderRadius: 8, border: '1px solid #e2e8f0' }} />
                <Bar dataKey="succeeded"  name="Succeeded"   fill="#22c55e" />
                <Bar dataKey="failed"     name="Failed"      fill="#ef4444" />
                <Bar dataKey="inProgress" name="In Progress" fill="#06b6d4" />
                <Bar dataKey="queued"     name="Queued"      fill="#94a3b8" />
                <Bar dataKey="cancelled"  name="Cancelled"   fill="#cbd5e1" />
              </BarChart>
            </ResponsiveContainer>
          )}
          <div className="flex flex-wrap gap-4 pt-4 border-t border-slate-100 mt-4 text-xs">
            {[
              { label: 'Succeeded',   color: 'bg-success-500' },
              { label: 'Failed',      color: 'bg-danger-500' },
              { label: 'In Progress', color: 'bg-cyan-500' },
              { label: 'Queued',      color: 'bg-slate-400' },
              { label: 'Cancelled',   color: 'bg-slate-300' },
            ].map((item) => (
              <div key={item.label} className="flex items-center gap-1.5 text-slate-600">
                <div className={`w-3 h-3 rounded-sm ${item.color}`} />
                {item.label}
              </div>
            ))}
          </div>
        </div>

        {/* Latest notifications — 1/3 width, sits to the right of the chart */}
        <div className="card overflow-hidden flex flex-col">
          <div className="p-5 border-b border-slate-100 flex items-center justify-between flex-shrink-0">
            <div className="flex items-center gap-2.5">
              <div className="w-9 h-9 rounded-xl bg-accent-50 ring-1 ring-accent-200 flex items-center justify-center">
                <Bell className="w-4 h-4 text-accent-600" />
              </div>
              <div>
                <h3 className="section-title">Latest notifications</h3>
                <p className="text-muted">
                  {inbox?.unreadCount ? `${inbox.unreadCount} unread` : 'You\'re caught up'}
                </p>
              </div>
            </div>
          </div>
          {inboxLoading ? (
            <div className="p-5 space-y-2">
              {Array.from({ length: 3 }).map((_, i) =>
                <div key={i} className="h-12 bg-slate-100 rounded animate-pulse" />
              )}
            </div>
          ) : !inbox?.notifications.length ? (
            <div className="p-10 text-center flex-1 flex flex-col items-center justify-center">
              <Bell className="w-9 h-9 text-slate-300 mb-2" />
              <p className="text-sm font-medium text-slate-700">No notifications yet</p>
              <p className="text-xs text-slate-400 mt-1">You'll see firmware push, approval, and rollout activity here</p>
            </div>
          ) : (
            <div className="divide-y divide-slate-100 flex-1 overflow-y-auto">
              {inbox.notifications.slice(0, 5).map((n) => (
                <div
                  key={n.id}
                  className={`flex items-start gap-3 px-5 py-3.5 ${
                    !n.isRead ? 'bg-accent-50/40' : ''
                  }`}
                >
                  <div className={`w-2 h-2 rounded-full mt-2 flex-shrink-0 ${!n.isRead ? 'bg-accent-500' : 'bg-slate-200'}`} />
                  <div className="flex-1 min-w-0">
                    <p className={`text-sm truncate ${!n.isRead ? 'font-semibold text-primary-900' : 'text-slate-700'}`}>
                      {n.title}
                    </p>
                    {n.body && (
                      <p className="text-xs text-slate-500 mt-0.5 line-clamp-2">{n.body}</p>
                    )}
                  </div>
                  <span className="text-xs text-slate-400 whitespace-nowrap flex-shrink-0">
                    {formatRelativeTime(n.createdAt)}
                  </span>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  )
}

// ─── Customer Admin Dashboard ─────────────────────────────────────────────────

function CustomerAdminDashboard({ userName }: { userName: string }) {
  const { data: summary, isLoading: summaryLoading } = useQuery({
    queryKey: ['dashboard-summary'],
    queryFn: () => reportService.getDashboardSummary(),
    refetchInterval: 30_000,
  })

  const { data: deviceStatus, isLoading: deviceStatusLoading } = useQuery({
    queryKey: ['device-update-status'],
    queryFn: () => reportService.getDeviceUpdateStatus(),
  })

  const deviceStatusData =
    deviceStatus
      ?.flatMap((d) => [
        { status: 'Up to Date', count: d.upToDate },
        { status: 'Update Available', count: d.updateAvailable },
        { status: 'Updating', count: d.updating },
        { status: 'Failed', count: d.failed },
        { status: 'Offline', count: d.offline },
      ])
      .filter((d) => d.count > 0) ?? []

  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        title={`${getGreeting()}, ${userName}`}
        subtitle="Customer Admin — Your Device Fleet"
      />

      {/* Fleet KPIs */}
      <div className="grid grid-cols-1 sm:grid-cols-4 gap-4">
        <DeviceHealthCard
          totalDevices={summary?.totalDevices ?? 0}
          onlineDevices={summary?.activeDevices ?? 0}
          offlineDevices={summary?.offlineDevices ?? 0}
          isLoading={summaryLoading}
        />
        <StatCard
          label="Active Devices"
          value={summary?.activeDevices ?? 0}
          icon={<Wifi className="w-5 h-5" />}
          accent="green"
          isLoading={summaryLoading}
        />
        <StatCard
          label="Devices Updating"
          value={summary?.devicesUpdating ?? 0}
          icon={<Loader2 className="w-5 h-5" />}
          accent="primary"
          isLoading={summaryLoading}
        />
        <StatCard
          label="Offline Devices"
          value={summary?.offlineDevices ?? 0}
          icon={<WifiOff className="w-5 h-5" />}
          accent="red"
          isLoading={summaryLoading}
        />
      </div>

      {/* OTA status row */}
      <div className="grid grid-cols-2 sm:grid-cols-3 gap-4">
        <StatCard
          label="Active Rollouts"
          value={summary?.activeRollouts ?? 0}
          icon={<RefreshCw className="w-5 h-5" />}
          accent="amber"
          isLoading={summaryLoading}
        />
        <StatCard
          label="Approved Firmware"
          value={summary?.approvedFirmware ?? 0}
          icon={<ShieldCheck className="w-5 h-5" />}
          accent="purple"
          isLoading={summaryLoading}
        />
        <StatCard
          label="Projects"
          value={summary?.totalProjects ?? 0}
          icon={<FolderOpen className="w-5 h-5" />}
          accent="navy"
          isLoading={summaryLoading}
        />
      </div>

      {/* Device status chart */}
      <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
        <div className="card p-6">
          <div className="mb-4">
            <h3 className="section-title">Device Update Status</h3>
            <p className="text-muted">Your fleet distribution</p>
          </div>
          <DeviceStatusChart data={deviceStatusData} isLoading={deviceStatusLoading} />
        </div>

        {/* Quick links */}
        <div className="card p-6">
          <h3 className="section-title mb-4">Quick Actions</h3>
          <div className="space-y-3">
            <Link
              href="/devices"
              className="flex items-center gap-3 p-4 rounded-xl bg-slate-50 hover:bg-accent-50 transition-colors group"
            >
              <div className="w-9 h-9 rounded-lg bg-white flex items-center justify-center shadow-sm group-hover:bg-accent-100 transition-colors">
                <MonitorSmartphone className="w-4 h-4 text-accent-600" />
              </div>
              <div>
                <p className="text-sm font-semibold text-primary-800">Manage Devices</p>
                <p className="text-xs text-slate-500">View and control your device fleet</p>
              </div>
            </Link>
            <Link
              href="/ota-rollouts"
              className="flex items-center gap-3 p-4 rounded-xl bg-slate-50 hover:bg-accent-50 transition-colors group"
            >
              <div className="w-9 h-9 rounded-lg bg-white flex items-center justify-center shadow-sm group-hover:bg-accent-100 transition-colors">
                <RefreshCw className="w-4 h-4 text-accent-600" />
              </div>
              <div>
                <p className="text-sm font-semibold text-primary-800">OTA Rollouts</p>
                <p className="text-xs text-slate-500">Monitor active firmware rollouts</p>
              </div>
            </Link>
            <Link
              href="/firmware"
              className="flex items-center gap-3 p-4 rounded-xl bg-slate-50 hover:bg-accent-50 transition-colors group"
            >
              <div className="w-9 h-9 rounded-lg bg-white flex items-center justify-center shadow-sm group-hover:bg-accent-100 transition-colors">
                <Cpu className="w-4 h-4 text-accent-600" />
              </div>
              <div>
                <p className="text-sm font-semibold text-primary-800">Firmware Versions</p>
                <p className="text-xs text-slate-500">Browse approved firmware for your devices</p>
              </div>
            </Link>
          </div>
        </div>
      </div>
    </div>
  )
}

// ─── Viewer Dashboard ─────────────────────────────────────────────────────────

function ViewerDashboard({ userName }: { userName: string }) {
  const { data: summary, isLoading: summaryLoading } = useQuery({
    queryKey: ['dashboard-summary'],
    queryFn: () => reportService.getDashboardSummary(),
    refetchInterval: 60_000,
  })

  const { data: firmwareTrends, isLoading: trendsLoading } = useQuery({
    queryKey: ['firmware-trends', 14],
    queryFn: () => reportService.getFirmwareTrends(14),
  })

  const { data: deviceStatus, isLoading: deviceStatusLoading } = useQuery({
    queryKey: ['device-update-status'],
    queryFn: () => reportService.getDeviceUpdateStatus(),
  })

  const deviceStatusData =
    deviceStatus
      ?.flatMap((d) => [
        { status: 'Up to Date', count: d.upToDate },
        { status: 'Update Available', count: d.updateAvailable },
        { status: 'Updating', count: d.updating },
        { status: 'Failed', count: d.failed },
        { status: 'Offline', count: d.offline },
      ])
      .filter((d) => d.count > 0) ?? []

  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        title={`${getGreeting()}, ${userName}`}
        subtitle="Viewer — Platform At a Glance"
      />

      <div className="grid grid-cols-1 sm:grid-cols-4 gap-4">
        <DeviceHealthCard
          totalDevices={summary?.totalDevices ?? 0}
          onlineDevices={summary?.activeDevices ?? 0}
          offlineDevices={summary?.offlineDevices ?? 0}
          isLoading={summaryLoading}
        />
        <StatCard
          label="Projects"
          value={summary?.totalProjects ?? 0}
          icon={<FolderOpen className="w-5 h-5" />}
          accent="navy"
          isLoading={summaryLoading}
        />
        <StatCard
          label="Firmware Versions"
          value={summary?.totalFirmware ?? 0}
          icon={<Cpu className="w-5 h-5" />}
          accent="purple"
          isLoading={summaryLoading}
        />
        <StatCard
          label="Active Rollouts"
          value={summary?.activeRollouts ?? 0}
          icon={<RefreshCw className="w-5 h-5" />}
          accent="amber"
          isLoading={summaryLoading}
        />
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
        <div className="card p-6">
          <div className="mb-5">
            <h3 className="section-title">Firmware Approval Trends</h3>
            <p className="text-muted">Last 14 days</p>
          </div>
          <FirmwareTrendChart data={firmwareTrends ?? []} isLoading={trendsLoading} />
        </div>
        <div className="card p-6">
          <div className="mb-4">
            <h3 className="section-title">Device Update Status</h3>
            <p className="text-muted">Current distribution</p>
          </div>
          <DeviceStatusChart data={deviceStatusData} isLoading={deviceStatusLoading} />
        </div>
      </div>

      {/* Informational tiles */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
        <div className="card p-4 text-center">
          <p className="text-2xl font-black text-primary-900">{summaryLoading ? '—' : (summary?.approvedFirmware ?? 0)}</p>
          <p className="text-xs text-slate-500 mt-0.5">Approved Firmware</p>
        </div>
        <div className="card p-4 text-center">
          <p className="text-2xl font-black text-primary-900">{summaryLoading ? '—' : (summary?.completedRollouts ?? 0)}</p>
          <p className="text-xs text-slate-500 mt-0.5">Completed Rollouts</p>
        </div>
        <div className="card p-4 text-center">
          <p className="text-2xl font-black text-primary-900">{summaryLoading ? '—' : (summary?.offlineDevices ?? 0)}</p>
          <p className="text-xs text-slate-500 mt-0.5">Offline Devices</p>
        </div>
        <div className="card p-4 text-center">
          <p className="text-2xl font-black text-primary-900">{summaryLoading ? '—' : (summary?.totalRepositories ?? 0)}</p>
          <p className="text-xs text-slate-500 mt-0.5">Repositories</p>
        </div>
      </div>
    </div>
  )
}

// ─── QA Project Dashboard ─────────────────────────────────────────────────────

function QADashboard({ projectId, userName }: { projectId: string; userName: string }) {
  const { data: project, isLoading: projectLoading } = useQuery({
    queryKey: ['project', projectId],
    queryFn: () => projectService.getProjectById(projectId),
  })

  const { data: firmware, isLoading: firmwareLoading } = useQuery({
    queryKey: ['firmware', { projectId }],
    queryFn: () => firmwareService.getFirmwareList({ projectId, pageSize: 50 }),
    enabled: !!projectId,
  })

  const { data: repos, isLoading: reposLoading } = useQuery({
    queryKey: ['repositories', { projectId }],
    queryFn: () => repositoryService.getRepositories({ projectId }),
    enabled: !!projectId,
  })

  const firmwareColumns: Column<FirmwareVersion>[] = [
    {
      key: 'version',
      header: 'Version',
      cell: (row) => (
        <Link
          href={`/firmware/${row.id}`}
          className="font-mono font-bold text-accent-600 hover:text-accent-700 text-sm transition-colors"
        >
          {row.version}
        </Link>
      ),
    },
    {
      key: 'channel',
      header: 'Channel',
      cell: (row) => <StatusBadge status={row.channel} />,
    },
    {
      key: 'status',
      header: 'Status',
      cell: (row) => <StatusBadge status={row.status} dot />,
    },
    {
      key: 'qa',
      header: 'QA Status',
      cell: (row) => (
        <div className="flex items-center gap-1.5">
          {row.isQaVerified ? (
            <>
              <CheckCircle2 className="w-3.5 h-3.5 text-success-500" />
              <span className="text-xs text-success-600 font-medium">Verified</span>
            </>
          ) : (
            <>
              <AlertCircle className="w-3.5 h-3.5 text-warning-500" />
              <span className="text-xs text-warning-600 font-medium">Pending</span>
            </>
          )}
        </div>
      ),
    },
    {
      key: 'createdAt',
      header: 'Created',
      cell: (row) => <span className="text-xs text-slate-500">{formatDate(row.createdAt)}</span>,
    },
    {
      key: 'actions',
      header: '',
      cell: (row) => (
        <Link
          href={`/firmware/${row.id}`}
          className="text-xs text-accent-600 hover:text-accent-700 font-medium transition-colors whitespace-nowrap"
        >
          Review →
        </Link>
      ),
    },
  ]

  const repoColumns: Column<Repository>[] = [
    {
      key: 'name',
      header: 'Repository',
      cell: (row) => (
        <Link
          href={`/repositories/${row.id}`}
          className="text-sm font-semibold text-primary-800 hover:text-accent-600 transition-colors"
        >
          {row.name}
        </Link>
      ),
    },
    {
      key: 'owner',
      header: 'Gitea',
      cell: (row) => (
        <code className="text-xs text-slate-500 bg-slate-100 px-1.5 py-0.5 rounded">
          {row.giteaOwner}/{row.giteaRepo}
        </code>
      ),
    },
    {
      key: 'branch',
      header: 'Branch',
      cell: (row) => <code className="text-xs text-slate-600">{row.defaultBranch}</code>,
    },
    {
      key: 'status',
      header: 'Status',
      cell: (row) => <StatusBadge status={row.isActive ? 'Active' : 'Inactive'} dot />,
    },
  ]

  const pendingQACount = firmware?.items.filter((f) => !f.isQaVerified).length ?? 0
  const verifiedQACount = firmware?.items.filter((f) => f.isQaVerified).length ?? 0

  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        title={`${getGreeting()}, ${userName}`}
        subtitle="QA Engineer — Your Assigned Project"
      />

      {/* Project Details Card */}
      {projectLoading ? (
        <div className="card p-6 animate-pulse">
          <div className="h-5 bg-slate-200 rounded w-48 mb-4" />
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
            {[1, 2, 3, 4].map((i) => (
              <div key={i} className="h-16 bg-slate-100 rounded-xl" />
            ))}
          </div>
        </div>
      ) : project ? (
        <div className="card p-6">
          <div className="flex items-start justify-between mb-5">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-accent-50 flex items-center justify-center">
                <FolderOpen className="w-5 h-5 text-accent-600" />
              </div>
              <div>
                <h2 className="text-lg font-bold text-primary-900">{project.name}</h2>
                {project.description && (
                  <p className="text-sm text-slate-500 mt-0.5">{project.description}</p>
                )}
              </div>
            </div>
            <StatusBadge status={project.isActive ? 'Active' : 'Inactive'} dot />
          </div>

          <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-3">
            {project.customerName && (
              <div className="flex items-center gap-2.5 p-3 bg-slate-50 rounded-xl col-span-2 sm:col-span-1">
                <div className="w-8 h-8 rounded-lg bg-white flex items-center justify-center shadow-sm flex-shrink-0">
                  <Building2 className="w-4 h-4 text-primary-500" />
                </div>
                <div className="min-w-0">
                  <p className="text-[10px] text-slate-400 font-semibold uppercase tracking-wide">Customer</p>
                  <p className="text-sm font-semibold text-primary-800 truncate">{project.customerName}</p>
                </div>
              </div>
            )}
            {project.businessUnit && (
              <div className="flex items-center gap-2.5 p-3 bg-slate-50 rounded-xl col-span-2 sm:col-span-1">
                <div className="w-8 h-8 rounded-lg bg-white flex items-center justify-center shadow-sm flex-shrink-0">
                  <Building2 className="w-4 h-4 text-navy-500" />
                </div>
                <div className="min-w-0">
                  <p className="text-[10px] text-slate-400 font-semibold uppercase tracking-wide">Business Unit</p>
                  <p className="text-sm font-semibold text-primary-800 truncate">{project.businessUnit}</p>
                </div>
              </div>
            )}
            <div className="flex items-center gap-2.5 p-3 bg-slate-50 rounded-xl">
              <div className="w-8 h-8 rounded-lg bg-white flex items-center justify-center shadow-sm flex-shrink-0">
                <Cpu className="w-4 h-4 text-purple-500" />
              </div>
              <div>
                <p className="text-[10px] text-slate-400 font-semibold uppercase tracking-wide">Firmware</p>
                <p className="text-sm font-semibold text-primary-800">{firmware?.pagination?.totalCount ?? 0}</p>
              </div>
            </div>
            <div className="flex items-center gap-2.5 p-3 bg-slate-50 rounded-xl">
              <div className="w-8 h-8 rounded-lg bg-white flex items-center justify-center shadow-sm flex-shrink-0">
                <FlaskConical className="w-4 h-4 text-warning-500" />
              </div>
              <div>
                <p className="text-[10px] text-slate-400 font-semibold uppercase tracking-wide">Pending QA</p>
                <p className="text-sm font-semibold text-primary-800">{pendingQACount}</p>
              </div>
            </div>
            <div className="flex items-center gap-2.5 p-3 bg-slate-50 rounded-xl">
              <div className="w-8 h-8 rounded-lg bg-white flex items-center justify-center shadow-sm flex-shrink-0">
                <CheckCircle2 className="w-4 h-4 text-success-500" />
              </div>
              <div>
                <p className="text-[10px] text-slate-400 font-semibold uppercase tracking-wide">QA Verified</p>
                <p className="text-sm font-semibold text-primary-800">{verifiedQACount}</p>
              </div>
            </div>
            <div className="flex items-center gap-2.5 p-3 bg-slate-50 rounded-xl">
              <div className="w-8 h-8 rounded-lg bg-white flex items-center justify-center shadow-sm flex-shrink-0">
                <GitBranch className="w-4 h-4 text-accent-500" />
              </div>
              <div>
                <p className="text-[10px] text-slate-400 font-semibold uppercase tracking-wide">Repos</p>
                <p className="text-sm font-semibold text-primary-800">{repos?.items.length ?? 0}</p>
              </div>
            </div>
          </div>

          {project.tags && project.tags.length > 0 && (
            <div className="flex items-center gap-2 mt-4 pt-4 border-t border-slate-100">
              <Tag className="w-3.5 h-3.5 text-slate-400 flex-shrink-0" />
              <div className="flex flex-wrap gap-1">
                {project.tags.map((tag) => (
                  <span
                    key={tag}
                    className="text-xs px-2 py-0.5 bg-accent-50 text-accent-700 border border-accent-100 rounded-full font-medium"
                  >
                    {tag}
                  </span>
                ))}
              </div>
            </div>
          )}
        </div>
      ) : (
        <div className="card p-8 flex flex-col items-center text-center gap-3">
          <XCircle className="w-10 h-10 text-slate-300" />
          <p className="text-slate-500">Assigned project not found. Contact your administrator.</p>
        </div>
      )}

      <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
        <div className="xl:col-span-2 card overflow-hidden">
          <div className="p-5 border-b border-slate-100 flex items-center justify-between">
            <div>
              <h3 className="section-title">Firmware Versions</h3>
              <p className="text-muted">All firmware for this project</p>
            </div>
            <Link href="/firmware" className="text-sm text-accent-600 hover:text-accent-700 font-medium transition-colors">
              View all
            </Link>
          </div>
          <DataTable
            columns={firmwareColumns}
            data={firmware?.items ?? []}
            isLoading={firmwareLoading}
            keyExtractor={(r) => r.id}
            emptyMessage="No firmware versions for this project yet"
          />
        </div>

        <div className="card overflow-hidden">
          <div className="p-5 border-b border-slate-100">
            <h3 className="section-title">Repositories</h3>
            <p className="text-muted">Linked Gitea repositories</p>
          </div>
          <DataTable
            columns={repoColumns}
            data={repos?.items ?? []}
            isLoading={reposLoading}
            keyExtractor={(r) => r.id}
            emptyMessage="No repositories linked"
          />
        </div>
      </div>
    </div>
  )
}

// ─── No-project fallback for QA ───────────────────────────────────────────────

function QANoProject({ userName }: { userName: string }) {
  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        title={`${getGreeting()}, ${userName}`}
        subtitle="QA Engineer — No project assigned"
      />
      <div className="card p-10 flex flex-col items-center text-center gap-4">
        <div className="w-16 h-16 rounded-2xl bg-warning-50 flex items-center justify-center">
          <AlertCircle className="w-8 h-8 text-warning-400" />
        </div>
        <div>
          <p className="text-base font-semibold text-primary-800">No project assigned</p>
          <p className="text-sm text-slate-500 mt-1">
            You have not been assigned to any project yet. Please contact your administrator.
          </p>
        </div>
      </div>
    </div>
  )
}

// ─── Root page — picks the right dashboard by role ────────────────────────────

export default function DashboardPage() {
  const { user, role } = useAuth()

  const userName = user?.email?.split('@')[0] ?? 'User'
  const isQA = role === UserRole.QA

  // Fetch the live user profile from the database so projectScope is always fresh,
  // not stale from the JWT (which only updates on login).
  const { data: liveUser, isLoading: liveUserLoading } = useQuery({
    queryKey: ['auth-me'],
    queryFn: () => authService.getCurrentUser(),
    enabled: isQA,
    staleTime: 30_000,
  })

  if (isQA) {
    if (liveUserLoading) {
      return (
        <div className="space-y-6 animate-pulse">
          <div className="h-10 bg-slate-200 rounded-lg w-64" />
          <div className="h-40 bg-slate-100 rounded-2xl" />
        </div>
      )
    }
    const assignedProjectId = liveUser?.projectScope?.[0] ?? null
    if (assignedProjectId) {
      return <QADashboard projectId={assignedProjectId} userName={userName} />
    }
    return <QANoProject userName={userName} />
  }

  if (role === UserRole.ReleaseManager) {
    return <ReleaseManagerDashboard userName={userName} />
  }

  if (role === UserRole.CustomerAdmin) {
    return <CustomerAdminDashboard userName={userName} />
  }

  if (role === UserRole.Viewer) {
    return <ViewerDashboard userName={userName} />
  }

  // SuperAdmin and PlatformAdmin get the full platform overview
  return <SuperAdminDashboard userName={userName} role={role ?? UserRole.PlatformAdmin} />
}
