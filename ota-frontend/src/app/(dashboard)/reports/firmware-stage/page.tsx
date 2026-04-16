'use client'

import * as React from 'react'
import { useQuery } from '@tanstack/react-query'
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Cell, LabelList } from 'recharts'
import { reportService } from '@/services/report.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { useToast } from '@/components/ui/ToastProvider'
import { downloadBlob, formatPercent } from '@/utils/formatters'
import { Th, Td, TableSkeleton, EmptyState, FwBadge, ReportHeader, STAGE_BAR_COLOR } from '@/components/reports/shared'

export default function FirmwareStagePage() {
  const { toast } = useToast()
  const [exporting, setExporting] = React.useState(false)

  const { data, isLoading } = useQuery({
    queryKey: ['report-firmware-stage'],
    queryFn: () => reportService.getFirmwareStageReport(),
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

  const rows = data ?? []

  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        title="Firmware Version Stage Report"
        subtitle="Distribution of firmware versions across lifecycle stages"
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Reports', href: '/reports/firmware-trends' },
          { label: 'Firmware Stage' },
        ]}
      />

      <div className="card p-6 space-y-6">
        <ReportHeader title="Firmware Version Stage Report"
          subtitle="Distribution of firmware versions across lifecycle stages"
          reportType="firmware-stage" exporting={exporting} onExport={handleExport} />

        {isLoading ? (
          <div className="h-64 bg-slate-100 rounded-lg animate-pulse" />
        ) : !rows.length ? <EmptyState /> : (
          <>
            <ResponsiveContainer width="100%" height={Math.max(240, rows.length * 44)}>
              <BarChart layout="vertical" data={rows} margin={{ top: 4, right: 56, left: 16, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#f1f5f9" horizontal={false} />
                <XAxis type="number" tick={{ fontSize: 11, fill: '#94a3b8' }} allowDecimals={false} />
                <YAxis type="category" dataKey="stage" tick={{ fontSize: 12, fill: '#475569' }} width={120} />
                <Tooltip contentStyle={{ fontSize: 12, borderRadius: 8, border: '1px solid #e2e8f0' }}
                  formatter={(value: number) => [`${value} version${value !== 1 ? 's' : ''}`, 'Count']} />
                <Bar dataKey="count" radius={[0, 4, 4, 0]} minPointSize={3}>
                  {rows.map((entry) => (
                    <Cell key={entry.stage} fill={STAGE_BAR_COLOR[entry.stage] ?? '#6366f1'} />
                  ))}
                  <LabelList dataKey="count" position="right" style={{ fontSize: 12, fill: '#475569', fontWeight: 600 }} />
                </Bar>
              </BarChart>
            </ResponsiveContainer>

            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead><tr><Th>Stage</Th><Th right>Count</Th><Th right>Percentage</Th></tr></thead>
                <tbody>
                  {rows.map((s) => (
                    <tr key={s.stage} className="hover:bg-slate-50">
                      <Td><FwBadge status={s.stage} /></Td>
                      <Td right><span className="font-semibold text-primary-800">{s.count}</span></Td>
                      <Td right>
                        <div className="flex items-center justify-end gap-2">
                          <div className="w-24 h-1.5 bg-slate-100 rounded-full overflow-hidden">
                            <div className="h-full bg-accent-500 rounded-full" style={{ width: `${s.percentage}%` }} />
                          </div>
                          <span className="text-slate-500 w-10 text-right">{formatPercent(s.percentage, 1)}</span>
                        </div>
                      </Td>
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
