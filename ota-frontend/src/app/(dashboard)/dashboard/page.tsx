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
} from 'lucide-react'
import { reportService } from '@/services/report.service'
import { auditService } from '@/services/audit.service'
import { StatCard } from '@/components/ui/StatCard'
import { PageHeader } from '@/components/ui/PageHeader'
import { DataTable, Column } from '@/components/ui/DataTable'
import { StatusBadge, RoleBadge } from '@/components/ui/Badge'
import { FirmwareTrendChart } from '@/components/charts/FirmwareTrendChart'
import { RolloutSuccessChart } from '@/components/charts/RolloutSuccessChart'
import { DeviceStatusChart } from '@/components/charts/DeviceStatusChart'
import { useAuth } from '@/hooks/useAuth'
import { AuditLog } from '@/types'
import { formatRelativeTime } from '@/utils/formatters'

// ─── Dashboard Page ───────────────────────────────────────────────────────────

export default function DashboardPage() {
  const { user, role } = useAuth()

  // Queries
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

  // Audit log columns
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
          {row.entityType && (
            <span className="text-slate-500">{row.entityType}</span>
          )}
          {row.entityName && (
            <p className="text-primary-700 font-medium truncate max-w-[120px]">{row.entityName}</p>
          )}
        </div>
      ),
    },
  ]

  // Device status breakdown for chart
  const deviceStatusData = summary?.deviceStatusBreakdown?.map((d) => ({
    status: d.status,
    count: d.count,
  })) ?? deviceStatus?.flatMap((d) => [
    { status: 'Up to Date', count: d.upToDate },
    { status: 'Update Available', count: d.updateAvailable },
    { status: 'Updating', count: d.updating },
    { status: 'Failed', count: d.failed },
    { status: 'Offline', count: d.offline },
  ]).filter((d) => d.count > 0) ?? []

  const greeting = () => {
    const h = new Date().getHours()
    if (h < 12) return 'Good morning'
    if (h < 17) return 'Good afternoon'
    return 'Good evening'
  }

  return (
    <div className="space-y-6 animate-fade-in">
      {/* Page Header */}
      <PageHeader
        title={`${greeting()}, ${user?.email?.split('@')[0] ?? 'User'}`}
        subtitle={`${role?.replace(/([A-Z])/g, ' $1').trim()} — OTA Platform Overview`}
      />

      {/* Stats Grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6 gap-4">
        <StatCard
          label="Total Projects"
          value={summary?.totalProjects ?? 0}
          icon={<FolderOpen className="w-5 h-5" />}
          accent="navy"
          isLoading={summaryLoading}
        />
        <StatCard
          label="Repositories"
          value={summary?.totalRepositories ?? 0}
          icon={<GitBranch className="w-5 h-5" />}
          accent="blue"
          isLoading={summaryLoading}
        />
        <StatCard
          label="Firmware Versions"
          value={summary?.totalFirmwareVersions ?? 0}
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
        <StatCard
          label="Pending Approvals"
          value={summary?.pendingApprovals ?? 0}
          icon={<Clock className="w-5 h-5" />}
          accent="red"
          isLoading={summaryLoading}
        />
      </div>

      {/* Charts Row 1 */}
      <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
        {/* Firmware Trend */}
        <div className="xl:col-span-2 card p-6">
          <div className="flex items-center justify-between mb-5">
            <div>
              <h3 className="section-title">Firmware Approval Trends</h3>
              <p className="text-muted">Last 30 days</p>
            </div>
          </div>
          <FirmwareTrendChart data={firmwareTrends ?? []} isLoading={trendsLoading} />
        </div>

        {/* Device Status Donut */}
        <div className="card p-6">
          <div className="mb-4">
            <h3 className="section-title">Device Update Status</h3>
            <p className="text-muted">Current distribution</p>
          </div>
          <DeviceStatusChart data={deviceStatusData} isLoading={deviceStatusLoading} />
        </div>
      </div>

      {/* Charts Row 2 + Audit */}
      <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
        {/* Rollout Success Rate */}
        <div className="xl:col-span-1 card p-6">
          <div className="mb-4">
            <h3 className="section-title">Rollout Success Rate</h3>
            <p className="text-muted">By project</p>
          </div>
          <RolloutSuccessChart data={rolloutRates ?? []} isLoading={rolloutRatesLoading} />
        </div>

        {/* Recent Audit Events */}
        <div className="xl:col-span-2 card overflow-hidden">
          <div className="p-6 border-b border-slate-100">
            <div className="flex items-center justify-between">
              <div>
                <h3 className="section-title">Recent Audit Events</h3>
                <p className="text-muted">Last 10 system events</p>
              </div>
              <a
                href="/audit-logs"
                className="text-sm text-accent-600 hover:text-accent-700 font-medium transition-colors"
              >
                View all
              </a>
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
