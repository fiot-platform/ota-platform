'use client'

import * as React from 'react'
import { useQuery } from '@tanstack/react-query'
import { Download } from 'lucide-react'
import { reportService } from '@/services/report.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { RolloutSuccessChart } from '@/components/charts/RolloutSuccessChart'
import { RoleGuard } from '@/components/role-access/RoleGuard'
import { useToast } from '@/components/ui/ToastProvider'
import { downloadBlob, formatPercent } from '@/utils/formatters'
import { Th, Td, TableSkeleton, EmptyState } from '@/components/reports/shared'

export default function RolloutSuccessReportPage() {
  const { toast } = useToast()
  const [exporting, setExporting] = React.useState(false)

  const { data, isLoading } = useQuery({
    queryKey: ['report-rollout-success'],
    queryFn: () => reportService.getRolloutSuccessRate(),
  })

  const handleExport = async () => {
    setExporting(true)
    try {
      const blob = await reportService.exportReport('rollout-success')
      downloadBlob(blob, `rollout-success-${new Date().toISOString().slice(0, 10)}.csv`)
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
        title="Rollout Success Rate"
        subtitle="Successful vs failed OTA rollout campaigns per project"
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Reports', href: '/reports/firmware-trends' },
          { label: 'Rollout Success' },
        ]}
      />

      <div className="card p-6 space-y-4">
        <div className="flex items-center justify-end">
          <RoleGuard module="Reports" action="export">
            <button onClick={handleExport} disabled={exporting} className="btn-secondary btn-sm">
              <Download className="w-3.5 h-3.5" />Export CSV
            </button>
          </RoleGuard>
        </div>

        <RolloutSuccessChart data={rows} isLoading={isLoading} />

        {isLoading ? <TableSkeleton rows={5} cols={5} /> : !rows.length ? <EmptyState /> : (
          <div className="overflow-x-auto border-t border-slate-100 pt-4">
            <table className="w-full text-sm">
              <thead>
                <tr><Th>Project</Th><Th right>Total</Th><Th right>Successful</Th><Th right>Failed</Th><Th right>Success Rate</Th></tr>
              </thead>
              <tbody>
                {rows.map((r) => (
                  <tr key={r.projectId} className="hover:bg-slate-50">
                    <Td><span className="font-medium text-primary-800">{r.projectName}</span></Td>
                    <Td right>{r.totalRollouts}</Td>
                    <Td right><span className="text-success-600 font-semibold">{r.successfulRollouts}</span></Td>
                    <Td right><span className="text-danger-600 font-semibold">{r.failedRollouts}</span></Td>
                    <Td right>
                      <span className={`font-bold ${r.successRate >= 90 ? 'text-success-600' : r.successRate >= 70 ? 'text-warning-600' : 'text-danger-600'}`}>
                        {formatPercent(r.successRate)}
                      </span>
                    </Td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}
