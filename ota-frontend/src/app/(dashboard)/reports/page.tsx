'use client'

import * as React from 'react'
import { useQuery } from '@tanstack/react-query'
import * as Tabs from '@radix-ui/react-tabs'
import { Download, TrendingUp, BarChart3, PieChart } from 'lucide-react'
import { reportService } from '@/services/report.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { FirmwareTrendChart } from '@/components/charts/FirmwareTrendChart'
import { RolloutSuccessChart } from '@/components/charts/RolloutSuccessChart'
import { DeviceStatusChart } from '@/components/charts/DeviceStatusChart'
import { RoleGuard } from '@/components/role-access/RoleGuard'
import { useToast } from '@/components/ui/ToastProvider'
import { downloadBlob, formatPercent } from '@/utils/formatters'

export default function ReportsPage() {
  const { toast } = useToast()
  const [firmwareDays, setFirmwareDays] = React.useState(30)
  const [exporting, setExporting] = React.useState(false)

  const { data: firmwareTrends, isLoading: trendsLoading } = useQuery({
    queryKey: ['firmware-trends', firmwareDays],
    queryFn: () => reportService.getFirmwareTrends(firmwareDays),
  })

  const { data: rolloutRates, isLoading: rolloutLoading } = useQuery({
    queryKey: ['rollout-success-rate'],
    queryFn: () => reportService.getRolloutSuccessRate(),
  })

  const { data: deviceStatus, isLoading: deviceLoading } = useQuery({
    queryKey: ['device-update-status'],
    queryFn: () => reportService.getDeviceUpdateStatus(),
  })

  const deviceStatusData = (deviceStatus ?? []).flatMap((d) => [
    { status: 'Up to Date', count: d.upToDate },
    { status: 'Update Available', count: d.updateAvailable },
    { status: 'Updating', count: d.updating },
    { status: 'Failed', count: d.failed },
    { status: 'Offline', count: d.offline },
  ]).filter((d) => d.count > 0)

  const handleExport = async (reportType: string) => {
    setExporting(true)
    try {
      const blob = await reportService.exportReport(reportType)
      downloadBlob(blob, `${reportType}-${new Date().toISOString().slice(0, 10)}.csv`)
      toast({ title: 'Report exported', variant: 'success' })
    } catch {
      toast({ title: 'Export failed', variant: 'error' })
    } finally {
      setExporting(false)
    }
  }

  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        title="Reports & Analytics"
        subtitle="Platform-wide analytics and performance metrics"
        breadcrumbs={[{ label: 'Dashboard', href: '/dashboard' }, { label: 'Reports' }]}
      />

      <Tabs.Root defaultValue="firmware">
        <Tabs.List className="flex gap-1 p-1 bg-slate-100 rounded-xl w-fit mb-6">
          <Tabs.Trigger
            value="firmware"
            className="flex items-center gap-2 px-4 py-2.5 text-sm font-medium rounded-lg transition-all text-slate-600 hover:text-primary-800 hover:bg-white data-[state=active]:bg-accent-600 data-[state=active]:text-white data-[state=active]:shadow-sm"
          >
            <TrendingUp className="w-4 h-4" />
            Firmware Trends
          </Tabs.Trigger>
          <Tabs.Trigger
            value="rollouts"
            className="flex items-center gap-2 px-4 py-2.5 text-sm font-medium rounded-lg transition-all text-slate-600 hover:text-primary-800 hover:bg-white data-[state=active]:bg-accent-600 data-[state=active]:text-white data-[state=active]:shadow-sm"
          >
            <BarChart3 className="w-4 h-4" />
            Rollout Success
          </Tabs.Trigger>
          <Tabs.Trigger
            value="devices"
            className="flex items-center gap-2 px-4 py-2.5 text-sm font-medium rounded-lg transition-all text-slate-600 hover:text-primary-800 hover:bg-white data-[state=active]:bg-accent-600 data-[state=active]:text-white data-[state=active]:shadow-sm"
          >
            <PieChart className="w-4 h-4" />
            Device Status
          </Tabs.Trigger>
        </Tabs.List>

        {/* Firmware Trends Tab */}
        <Tabs.Content value="firmware">
          <div className="card p-6 space-y-4">
            <div className="flex items-center justify-between">
              <div>
                <h3 className="section-title">Firmware Approval Trends</h3>
                <p className="text-muted">Submitted, approved, and rejected firmware over time</p>
              </div>
              <div className="flex items-center gap-3">
                <div className="flex items-center gap-1">
                  {[7, 30, 90].map((d) => (
                    <button
                      key={d}
                      onClick={() => setFirmwareDays(d)}
                      className={`px-3 py-1 rounded-lg text-sm font-medium transition-colors ${
                        firmwareDays === d
                          ? 'bg-accent-600 text-white'
                          : 'text-slate-600 hover:bg-slate-100'
                      }`}
                    >
                      {d}d
                    </button>
                  ))}
                </div>
                <RoleGuard module="Reports" action="export">
                  <button
                    onClick={() => handleExport('firmware-trends')}
                    disabled={exporting}
                    className="btn-secondary btn-sm"
                  >
                    <Download className="w-3.5 h-3.5" />
                    Export
                  </button>
                </RoleGuard>
              </div>
            </div>
            <FirmwareTrendChart data={firmwareTrends ?? []} isLoading={trendsLoading} />

            {/* Summary Stats */}
            {firmwareTrends && firmwareTrends.length > 0 && (
              <div className="grid grid-cols-3 gap-4 pt-4 border-t border-slate-100">
                {[
                  { label: 'Total Submitted', value: firmwareTrends.reduce((s, d) => s + d.submitted, 0), color: 'text-accent-600' },
                  { label: 'Total Approved', value: firmwareTrends.reduce((s, d) => s + d.approved, 0), color: 'text-success-600' },
                  { label: 'Total Rejected', value: firmwareTrends.reduce((s, d) => s + d.rejected, 0), color: 'text-danger-600' },
                ].map((stat) => (
                  <div key={stat.label} className="text-center p-3 bg-slate-50 rounded-lg">
                    <p className={`text-2xl font-bold ${stat.color}`}>{stat.value}</p>
                    <p className="text-xs text-slate-500 mt-0.5">{stat.label}</p>
                  </div>
                ))}
              </div>
            )}
          </div>
        </Tabs.Content>

        {/* Rollout Success Tab */}
        <Tabs.Content value="rollouts">
          <div className="card p-6 space-y-4">
            <div className="flex items-center justify-between">
              <div>
                <h3 className="section-title">Rollout Success Rate by Project</h3>
                <p className="text-muted">Successful vs failed rollout campaigns</p>
              </div>
              <RoleGuard module="Reports" action="export">
                <button
                  onClick={() => handleExport('rollout-success')}
                  disabled={exporting}
                  className="btn-secondary btn-sm"
                >
                  <Download className="w-3.5 h-3.5" />
                  Export
                </button>
              </RoleGuard>
            </div>
            <RolloutSuccessChart data={rolloutRates ?? []} isLoading={rolloutLoading} />

            {/* Table Breakdown */}
            {(rolloutRates ?? []).length > 0 && (
              <div className="overflow-x-auto mt-4">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-slate-200">
                      <th className="text-left py-2 px-3 text-xs font-semibold text-slate-500 uppercase">Project</th>
                      <th className="text-right py-2 px-3 text-xs font-semibold text-slate-500 uppercase">Total</th>
                      <th className="text-right py-2 px-3 text-xs font-semibold text-slate-500 uppercase">Successful</th>
                      <th className="text-right py-2 px-3 text-xs font-semibold text-slate-500 uppercase">Failed</th>
                      <th className="text-right py-2 px-3 text-xs font-semibold text-slate-500 uppercase">Success Rate</th>
                    </tr>
                  </thead>
                  <tbody>
                    {rolloutRates!.map((r) => (
                      <tr key={r.projectId} className="border-b border-slate-50 hover:bg-slate-50">
                        <td className="py-2 px-3 font-medium text-primary-800">{r.projectName}</td>
                        <td className="py-2 px-3 text-right text-slate-600">{r.totalRollouts}</td>
                        <td className="py-2 px-3 text-right text-success-600 font-semibold">{r.successfulRollouts}</td>
                        <td className="py-2 px-3 text-right text-danger-600 font-semibold">{r.failedRollouts}</td>
                        <td className="py-2 px-3 text-right">
                          <span className={`font-bold ${r.successRate >= 90 ? 'text-success-600' : r.successRate >= 70 ? 'text-warning-600' : 'text-danger-600'}`}>
                            {formatPercent(r.successRate)}
                          </span>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </Tabs.Content>

        {/* Device Status Tab */}
        <Tabs.Content value="devices">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <div className="card p-6">
              <div className="flex items-center justify-between mb-4">
                <div>
                  <h3 className="section-title">Device Update Status</h3>
                  <p className="text-muted">Distribution of device update states</p>
                </div>
                <RoleGuard module="Reports" action="export">
                  <button
                    onClick={() => handleExport('device-status')}
                    disabled={exporting}
                    className="btn-secondary btn-sm"
                  >
                    <Download className="w-3.5 h-3.5" />
                    Export
                  </button>
                </RoleGuard>
              </div>
              <DeviceStatusChart data={deviceStatusData} isLoading={deviceLoading} />
            </div>

            <div className="card p-6">
              <h3 className="section-title mb-4">Status Breakdown</h3>
              {deviceLoading ? (
                <div className="space-y-3">
                  {Array.from({ length: 5 }).map((_, i) => (
                    <div key={i} className="h-8 bg-slate-100 rounded animate-pulse" />
                  ))}
                </div>
              ) : (deviceStatus ?? []).map((customer) => (
                <div key={customer.customerId ?? 'all'} className="mb-6 last:mb-0">
                  {customer.customerName && (
                    <p className="text-sm font-semibold text-primary-800 mb-3">{customer.customerName}</p>
                  )}
                  <div className="space-y-2">
                    {[
                      { label: 'Up to Date', value: customer.upToDate, color: 'bg-success-500' },
                      { label: 'Update Available', value: customer.updateAvailable, color: 'bg-warning-500' },
                      { label: 'Updating', value: customer.updating, color: 'bg-accent-500' },
                      { label: 'Failed', value: customer.failed, color: 'bg-danger-500' },
                      { label: 'Offline', value: customer.offline, color: 'bg-slate-300' },
                    ].map((stat) => {
                      const pct = customer.total > 0 ? (stat.value / customer.total) * 100 : 0
                      return (
                        <div key={stat.label}>
                          <div className="flex items-center justify-between text-xs mb-1">
                            <span className="text-slate-600">{stat.label}</span>
                            <span className="font-semibold text-primary-800">{stat.value} ({formatPercent(pct, 0)})</span>
                          </div>
                          <div className="progress-bar">
                            <div className={`progress-bar-fill ${stat.color}`} style={{ width: `${pct}%` }} />
                          </div>
                        </div>
                      )
                    })}
                  </div>
                </div>
              ))}
            </div>
          </div>
        </Tabs.Content>
      </Tabs.Root>
    </div>
  )
}
