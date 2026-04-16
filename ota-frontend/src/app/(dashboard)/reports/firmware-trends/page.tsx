'use client'

import * as React from 'react'
import { useQuery } from '@tanstack/react-query'
import { Download } from 'lucide-react'
import { reportService } from '@/services/report.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { FirmwareTrendChart } from '@/components/charts/FirmwareTrendChart'
import { RoleGuard } from '@/components/role-access/RoleGuard'
import { useToast } from '@/components/ui/ToastProvider'
import { downloadBlob, formatPercent } from '@/utils/formatters'

export default function FirmwareTrendsReportPage() {
  const { toast } = useToast()
  const [days, setDays] = React.useState(30)
  const [exporting, setExporting] = React.useState(false)

  const { data, isLoading } = useQuery({
    queryKey: ['report-firmware-trends', days],
    queryFn: () => reportService.getFirmwareTrends(days),
  })

  const handleExport = async () => {
    setExporting(true)
    try {
      const blob = await reportService.exportReport('firmware-trends', { days: String(days) })
      downloadBlob(blob, `firmware-trends-${new Date().toISOString().slice(0, 10)}.csv`)
      toast({ title: 'Report exported', variant: 'success' })
    } catch {
      toast({ title: 'Export failed', variant: 'error' })
    } finally {
      setExporting(false)
    }
  }

  const rows = data ?? []
  const totalSubmitted = rows.reduce((s, d) => s + d.submitted, 0)
  const totalApproved  = rows.reduce((s, d) => s + d.approved, 0)
  const totalRejected  = rows.reduce((s, d) => s + d.rejected, 0)

  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        title="Firmware Approval Trends"
        subtitle="Submitted, approved, and rejected firmware over time"
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Reports', href: '/reports/firmware-trends' },
          { label: 'Firmware Trends' },
        ]}
      />

      <div className="card p-6 space-y-4">
        <div className="flex items-center justify-between flex-wrap gap-3">
          <div className="flex items-center gap-1">
            {[7, 30, 90].map((d) => (
              <button key={d} onClick={() => setDays(d)}
                className={`px-3 py-1 rounded-lg text-sm font-medium transition-colors ${days === d ? 'bg-accent-600 text-white' : 'text-slate-600 hover:bg-slate-100'}`}>
                {d}d
              </button>
            ))}
          </div>
          <RoleGuard module="Reports" action="export">
            <button onClick={handleExport} disabled={exporting} className="btn-secondary btn-sm">
              <Download className="w-3.5 h-3.5" />Export CSV
            </button>
          </RoleGuard>
        </div>

        <FirmwareTrendChart data={rows} isLoading={isLoading} />

        {rows.length > 0 && (
          <div className="grid grid-cols-3 gap-4 pt-4 border-t border-slate-100">
            {[
              { label: 'Total Submitted', value: totalSubmitted, color: 'text-accent-600' },
              { label: 'Total Approved',  value: totalApproved,  color: 'text-success-600' },
              { label: 'Total Rejected',  value: totalRejected,  color: 'text-danger-600' },
            ].map((s) => (
              <div key={s.label} className="text-center p-3 bg-slate-50 rounded-lg">
                <p className={`text-2xl font-bold ${s.color}`}>{s.value}</p>
                <p className="text-xs text-slate-500 mt-0.5">{s.label}</p>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
