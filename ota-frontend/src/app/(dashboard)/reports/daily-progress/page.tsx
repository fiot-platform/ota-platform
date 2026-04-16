'use client'

import * as React from 'react'
import { useQuery } from '@tanstack/react-query'
import { Download } from 'lucide-react'
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts'
import { reportService } from '@/services/report.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { RoleGuard } from '@/components/role-access/RoleGuard'
import { useToast } from '@/components/ui/ToastProvider'
import { downloadBlob } from '@/utils/formatters'
import { Th, Td, TableSkeleton, EmptyState } from '@/components/reports/shared'

export default function DailyOtaProgressPage() {
  const { toast } = useToast()
  const [days, setDays] = React.useState(14)
  const [exporting, setExporting] = React.useState(false)

  const { data, isLoading } = useQuery({
    queryKey: ['report-daily-progress', days],
    queryFn: () => reportService.getDailyOtaProgress(days),
  })

  const handleExport = async () => {
    setExporting(true)
    try {
      const blob = await reportService.exportReport('daily-ota-progress', { days: String(days) })
      downloadBlob(blob, `daily-ota-progress-${new Date().toISOString().slice(0, 10)}.csv`)
      toast({ title: 'Report exported', variant: 'success' })
    } catch {
      toast({ title: 'Export failed', variant: 'error' })
    } finally {
      setExporting(false)
    }
  }

  const rows = data ?? []

  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        title="Daily OTA Progress Report"
        subtitle="Day-by-day OTA job activity across all rollouts"
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Reports', href: '/reports/firmware-trends' },
          { label: 'Daily OTA Progress' },
        ]}
      />

      <div className="card p-6 space-y-4">
        <div className="flex items-start justify-between gap-4 flex-wrap">
          <div>
            <h3 className="section-title">Daily OTA Job Activity</h3>
            <p className="text-muted">Stacked by job outcome per day</p>
          </div>
          <div className="flex items-center gap-2 flex-wrap">
            <div className="flex items-center gap-1">
              {[7, 14, 30].map((d) => (
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
        </div>

        {isLoading ? (
          <div className="h-72 bg-slate-100 rounded-lg animate-pulse" />
        ) : !rows.length ? <EmptyState /> : (
          <>
            <ResponsiveContainer width="100%" height={280}>
              <BarChart data={rows} margin={{ top: 4, right: 8, left: -16, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#f1f5f9" />
                <XAxis dataKey="date" tick={{ fontSize: 11, fill: '#94a3b8' }}
                  tickFormatter={(v) => { const d = new Date(v); return `${d.getDate()} ${d.toLocaleString('default', { month: 'short' })}` }} />
                <YAxis tick={{ fontSize: 11, fill: '#94a3b8' }} allowDecimals={false} />
                <Tooltip contentStyle={{ fontSize: 12, borderRadius: 8, border: '1px solid #e2e8f0' }}
                  labelFormatter={(v) => new Date(v).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })} />
                <Bar dataKey="succeeded"  name="Succeeded"   stackId="a" fill="#22c55e" />
                <Bar dataKey="failed"     name="Failed"      stackId="a" fill="#ef4444" />
                <Bar dataKey="inProgress" name="In Progress" stackId="a" fill="#06b6d4" />
                <Bar dataKey="queued"     name="Queued"      stackId="a" fill="#94a3b8" />
                <Bar dataKey="cancelled"  name="Cancelled"   stackId="a" fill="#cbd5e1" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>

            <div className="flex flex-wrap gap-4 pt-2 border-t border-slate-100">
              {[
                { label: 'Succeeded',   color: 'bg-success-500' },
                { label: 'Failed',      color: 'bg-danger-500' },
                { label: 'In Progress', color: 'bg-accent-500' },
                { label: 'Queued',      color: 'bg-slate-400' },
                { label: 'Cancelled',   color: 'bg-slate-300' },
              ].map((item) => (
                <div key={item.label} className="flex items-center gap-1.5 text-xs text-slate-600">
                  <div className={`w-3 h-3 rounded-sm ${item.color}`} />
                  {item.label}
                </div>
              ))}
            </div>

            <div className="overflow-x-auto pt-2">
              <table className="w-full text-sm">
                <thead>
                  <tr><Th>Date</Th><Th right>Total</Th><Th right>Succeeded</Th><Th right>Failed</Th><Th right>In Progress</Th><Th right>Queued</Th><Th right>Cancelled</Th></tr>
                </thead>
                <tbody>
                  {rows.map((row) => (
                    <tr key={row.date} className="hover:bg-slate-50">
                      <Td><span className="font-medium">{new Date(row.date).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })}</span></Td>
                      <Td right><span className="font-semibold text-primary-800">{row.total}</span></Td>
                      <Td right><span className="text-success-600">{row.succeeded}</span></Td>
                      <Td right><span className="text-danger-600">{row.failed}</span></Td>
                      <Td right><span className="text-accent-600">{row.inProgress}</span></Td>
                      <Td right muted>{row.queued}</Td>
                      <Td right muted>{row.cancelled}</Td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </>
        )}
      </div>
    </div>
  )
}
