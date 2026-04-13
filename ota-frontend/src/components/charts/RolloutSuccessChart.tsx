'use client'

import * as React from 'react'
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
  Cell,
} from 'recharts'
import { RolloutSuccessRate } from '@/types'
import { truncateText } from '@/utils/formatters'

interface RolloutSuccessChartProps {
  data: RolloutSuccessRate[]
  isLoading?: boolean
}

function CustomTooltip({ active, payload, label }: {
  active?: boolean
  payload?: Array<{ name: string; value: number; color: string }>
  label?: string
}) {
  if (!active || !payload?.length) return null
  return (
    <div className="bg-white border border-slate-200 rounded-xl shadow-lg p-3 text-sm">
      <p className="font-semibold text-primary-900 mb-2 max-w-[180px] truncate">{label}</p>
      {payload.map((entry) => (
        <div key={entry.name} className="flex items-center gap-2 py-0.5">
          <span className="w-2.5 h-2.5 rounded-full" style={{ backgroundColor: entry.color }} />
          <span className="text-slate-600">{entry.name}:</span>
          <span className="font-semibold text-primary-900">{entry.value}</span>
        </div>
      ))}
    </div>
  )
}

export function RolloutSuccessChart({ data, isLoading = false }: RolloutSuccessChartProps) {
  if (isLoading) {
    return <div className="w-full h-64 bg-slate-100 rounded-xl animate-pulse" />
  }

  if (!data || data.length === 0) {
    return (
      <div className="w-full h-64 flex items-center justify-center bg-slate-50 rounded-xl border border-dashed border-slate-200">
        <p className="text-slate-400 text-sm">No rollout data available</p>
      </div>
    )
  }

  const formattedData = data.map((d) => ({
    ...d,
    name: truncateText(d.projectName, 15),
    Successful: d.successfulRollouts,
    Failed: d.failedRollouts,
    Total: d.totalRollouts,
  }))

  return (
    <ResponsiveContainer width="100%" height={280}>
      <BarChart data={formattedData} margin={{ top: 5, right: 20, left: 0, bottom: 5 }}>
        <CartesianGrid strokeDasharray="3 3" stroke="#f1f5f9" vertical={false} />
        <XAxis
          dataKey="name"
          tick={{ fontSize: 11, fill: '#94a3b8' }}
          axisLine={{ stroke: '#e2e8f0' }}
          tickLine={false}
        />
        <YAxis
          tick={{ fontSize: 11, fill: '#94a3b8' }}
          axisLine={false}
          tickLine={false}
          allowDecimals={false}
        />
        <Tooltip content={<CustomTooltip />} />
        <Legend
          wrapperStyle={{ fontSize: '12px', paddingTop: '12px' }}
          formatter={(value) => <span className="text-slate-600">{value}</span>}
        />
        <Bar dataKey="Successful" fill="#22c55e" radius={[4, 4, 0, 0]} maxBarSize={40}>
          {formattedData.map((_, index) => (
            <Cell key={`cell-${index}`} fill="#22c55e" />
          ))}
        </Bar>
        <Bar dataKey="Failed" fill="#ef4444" radius={[4, 4, 0, 0]} maxBarSize={40}>
          {formattedData.map((_, index) => (
            <Cell key={`cell-${index}`} fill="#ef4444" />
          ))}
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  )
}
