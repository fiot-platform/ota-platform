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
} from 'lucide-react'
import Link from 'next/link'
import { reportService } from '@/services/report.service'
import { auditService } from '@/services/audit.service'
import { authService } from '@/services/auth.service'
import { projectService } from '@/services/project.service'
import { firmwareService } from '@/services/firmware.service'
import { repositoryService } from '@/services/repository.service'
import { StatCard } from '@/components/ui/StatCard'
import { PageHeader } from '@/components/ui/PageHeader'
import { DataTable, Column } from '@/components/ui/DataTable'
import { StatusBadge, RoleBadge } from '@/components/ui/Badge'
import { FirmwareTrendChart } from '@/components/charts/FirmwareTrendChart'
import { RolloutSuccessChart } from '@/components/charts/RolloutSuccessChart'
import { DeviceStatusChart } from '@/components/charts/DeviceStatusChart'
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

      {/* Top stats */}
      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6 gap-4">
        <StatCard
          label="Projects"
          value={summary?.totalProjects ?? 0}
          icon={<FolderOpen className="w-5 h-5" />}
          accent="navy"
          isLoading={summaryLoading}
        />
        <StatCard
          label="Repositories"
          value={summary?.totalRepositories ?? 0}
          icon={<GitBranch className="w-5 h-5" />}
          accent="primary"
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
          label="Active Devices"
          value={summary?.activeDevices ?? 0}
          icon={<MonitorSmartphone className="w-5 h-5" />}
          accent="green"
          isLoading={summaryLoading}
        />
        <StatCard
          label="Active Rollouts"
          value={summary?.activeRollouts ?? 0}
          icon={<RefreshCw className="w-5 h-5" />}
          accent="amber"
          isLoading={summaryLoading}
        />
        {isSuperAdmin ? (
          <StatCard
            label="Platform Users"
            value={summary?.totalUsers ?? 0}
            icon={<Users className="w-5 h-5" />}
            accent="red"
            isLoading={summaryLoading}
          />
        ) : (
          <StatCard
            label="Pending Approvals"
            value={summary?.pendingApprovals ?? 0}
            icon={<Clock className="w-5 h-5" />}
            accent="red"
            isLoading={summaryLoading}
          />
        )}
      </div>

      {/* Health row */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
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
        <div className="card p-4 flex items-center gap-3">
          <div className="w-9 h-9 rounded-xl bg-slate-50 flex items-center justify-center flex-shrink-0">
            <WifiOff className="w-4 h-4 text-slate-400" />
          </div>
          <div>
            <p className="text-[10px] text-slate-400 font-semibold uppercase tracking-wide">Offline Devices</p>
            <p className="text-xl font-bold text-primary-900">{summaryLoading ? '—' : (summary?.offlineDevices ?? 0)}</p>
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
          <DataTable
            columns={auditColumns}
            data={recentAuditLogs?.items ?? []}
            isLoading={auditLoading}
            keyExtractor={(row) => row.id}
            emptyMessage="No recent audit events"
          />
        </div>
      </div>
    </div>
  )
}

// ─── Release Manager Dashboard ────────────────────────────────────────────────

function ReleaseManagerDashboard({ userName }: { userName: string }) {
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

  const { data: firmwareData, isLoading: firmwareLoading } = useQuery({
    queryKey: ['firmware-pending-attention'],
    queryFn: () => firmwareService.getFirmwareList({ pageSize: 50 }),
  })

  const pendingItems = firmwareData?.items.filter(
    (f) => f.status === FirmwareStatus.PendingApproval || f.status === FirmwareStatus.PendingQA
  ) ?? []

  const pendingColumns: Column<FirmwareVersion>[] = [
    {
      key: 'version',
      header: 'Version',
      cell: (row) => (
        <Link href={`/firmware/${row.id}`} className="font-mono font-bold text-accent-600 hover:text-accent-700 text-sm">
          {row.version}
        </Link>
      ),
    },
    {
      key: 'status',
      header: 'Status',
      cell: (row) => <StatusBadge status={row.status} dot />,
    },
    {
      key: 'channel',
      header: 'Channel',
      cell: (row) => <StatusBadge status={row.channel} />,
    },
    {
      key: 'createdAt',
      header: 'Submitted',
      cell: (row) => <span className="text-xs text-slate-500">{formatDate(row.createdAt)}</span>,
    },
    {
      key: 'actions',
      header: '',
      cell: (row) => (
        <Link href={`/firmware/${row.id}`} className="text-xs text-accent-600 hover:text-accent-700 font-medium">
          Review →
        </Link>
      ),
    },
  ]

  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        title={`${getGreeting()}, ${userName}`}
        subtitle="Release Manager — Firmware Lifecycle Overview"
      />

      {/* Primary KPIs */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
        {/* Pending Approvals — highlighted red */}
        <div className="card p-5 border-2 border-red-200 bg-red-50/30 col-span-2 sm:col-span-1">
          <div className="flex items-center gap-3 mb-2">
            <div className="w-9 h-9 rounded-xl bg-red-100 flex items-center justify-center">
              <Clock className="w-5 h-5 text-red-500" />
            </div>
            <span className="text-xs font-semibold text-red-600 uppercase tracking-wide">Needs Approval</span>
          </div>
          <p className="text-3xl font-black text-red-700">
            {summaryLoading ? '—' : (summary?.pendingApprovalFirmware ?? 0)}
          </p>
          <Link href="/firmware?status=PendingApproval" className="text-xs text-red-500 hover:text-red-600 mt-1 inline-block">
            Review now →
          </Link>
        </div>

        <StatCard
          label="Pending QA"
          value={summary?.pendingQAFirmware ?? 0}
          icon={<FlaskConical className="w-5 h-5" />}
          accent="amber"
          isLoading={summaryLoading}
        />
        <StatCard
          label="Active Rollouts"
          value={summary?.activeRollouts ?? 0}
          icon={<RefreshCw className="w-5 h-5" />}
          accent="green"
          isLoading={summaryLoading}
        />
        <StatCard
          label="Approved Firmware"
          value={summary?.approvedFirmware ?? 0}
          icon={<ShieldCheck className="w-5 h-5" />}
          accent="primary"
          isLoading={summaryLoading}
        />
      </div>

      {/* Secondary stats */}
      <div className="grid grid-cols-3 gap-4">
        <StatCard
          label="Total Firmware"
          value={summary?.totalFirmware ?? 0}
          icon={<Cpu className="w-5 h-5" />}
          accent="navy"
          isLoading={summaryLoading}
        />
        <StatCard
          label="Completed Rollouts"
          value={summary?.completedRollouts ?? 0}
          icon={<CheckCircle2 className="w-5 h-5" />}
          accent="green"
          isLoading={summaryLoading}
        />
        <StatCard
          label="Projects"
          value={summary?.totalProjects ?? 0}
          icon={<FolderOpen className="w-5 h-5" />}
          accent="purple"
          isLoading={summaryLoading}
        />
      </div>

      {/* Charts */}
      <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
        <div className="xl:col-span-2 card p-6">
          <div className="mb-5">
            <h3 className="section-title">Firmware Approval Trends</h3>
            <p className="text-muted">Last 30 days</p>
          </div>
          <FirmwareTrendChart data={firmwareTrends ?? []} isLoading={trendsLoading} />
        </div>
        <div className="card p-6">
          <div className="mb-4">
            <h3 className="section-title">Rollout Success Rate</h3>
            <p className="text-muted">By project</p>
          </div>
          <RolloutSuccessChart data={rolloutRates ?? []} isLoading={rolloutRatesLoading} />
        </div>
      </div>

      {/* Firmware needing attention */}
      <div className="card overflow-hidden">
        <div className="p-5 border-b border-slate-100 flex items-center justify-between">
          <div>
            <h3 className="section-title">Firmware Needing Attention</h3>
            <p className="text-muted">Pending approval or QA verification</p>
          </div>
          <Link href="/firmware" className="text-sm text-accent-600 hover:text-accent-700 font-medium">
            View all firmware
          </Link>
        </div>
        <DataTable
          columns={pendingColumns}
          data={pendingItems}
          isLoading={firmwareLoading}
          keyExtractor={(r) => r.id}
          emptyMessage="No firmware awaiting attention"
        />
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
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
        <StatCard
          label="Total Devices"
          value={summary?.totalDevices ?? 0}
          icon={<MonitorSmartphone className="w-5 h-5" />}
          accent="navy"
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

      <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
        <StatCard
          label="Projects"
          value={summary?.totalProjects ?? 0}
          icon={<FolderOpen className="w-5 h-5" />}
          accent="navy"
          isLoading={summaryLoading}
        />
        <StatCard
          label="Active Devices"
          value={summary?.activeDevices ?? 0}
          icon={<MonitorSmartphone className="w-5 h-5" />}
          accent="green"
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
