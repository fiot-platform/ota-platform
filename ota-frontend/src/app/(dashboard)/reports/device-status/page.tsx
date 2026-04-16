'use client'

import * as React from 'react'
import { useQuery } from '@tanstack/react-query'
import { Download } from 'lucide-react'
import { reportService } from '@/services/report.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { DeviceStatusChart } from '@/components/charts/DeviceStatusChart'
import { RoleGuard } from '@/components/role-access/RoleGuard'
import { useToast } from '@/components/ui/ToastProvider'
import { downloadBlob, formatPercent } from '@/utils/formatters'
import { TableSkeleton } from '@/components/reports/shared'

export default function DeviceStatusReportPage() {
  const { toast } = useToast()
  const [exporting, setExporting] = React.useState(false)

  const { data, isLoading } = useQuery({
    queryKey: ['report-device-update-status'],
    queryFn: () => reportService.getDeviceUpdateStatus(),
  })

  const chartData = (data ?? []).flatMap((d) => [
    { status: 'Up to Date',       count: d.upToDate },
    { status: 'Update Available', count: d.updateAvailable },
    { status: 'Updating',         count: d.updating },
    { status: 'Failed',           count: d.failed },
    { status: 'Offline',          count: d.offline },
  ]).filter((d) => d.count > 0)

  const handleExport = async () => {
    setExporting(true)
    try {
      const blob = await reportService.exportReport('device-status')
      downloadBlob(blob, `device-status-${new Date().toISOString().slice(0, 10)}.csv`)
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
        title="Device Update Status"
        subtitle="Distribution of device update states across the fleet"
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Reports', href: '/reports/firmware-trends' },
          { label: 'Device Status' },
        ]}
      />

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div className="card p-6">
          <div className="flex items-center justify-between mb-4">
            <div>
              <h3 className="section-title">Update Status Distribution</h3>
              <p className="text-muted">Current firmware update state of all devices</p>
            </div>
            <RoleGuard module="Reports" action="export">
              <button onClick={handleExport} disabled={exporting} className="btn-secondary btn-sm">
                <Download className="w-3.5 h-3.5" />Export CSV
              </button>
            </RoleGuard>
          </div>
          <DeviceStatusChart data={chartData} isLoading={isLoading} />
        </div>

        <div className="card p-6">
          <h3 className="section-title mb-4">Breakdown by Customer</h3>
          {isLoading ? <TableSkeleton rows={5} cols={1} /> : (data ?? []).map((customer) => (
            <div key={customer.customerId ?? 'all'} className="mb-6 last:mb-0">
              {customer.customerName && (
                <p className="text-sm font-semibold text-primary-800 mb-3">{customer.customerName}</p>
              )}
              <div className="space-y-2">
                {[
                  { label: 'Up to Date',       value: customer.upToDate,        color: 'bg-success-500' },
                  { label: 'Update Available',  value: customer.updateAvailable, color: 'bg-warning-500' },
                  { label: 'Updating',          value: customer.updating,        color: 'bg-accent-500' },
                  { label: 'Failed',            value: customer.failed,          color: 'bg-danger-500' },
                  { label: 'Offline',           value: customer.offline,         color: 'bg-slate-300' },
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
    </div>
  )
}
