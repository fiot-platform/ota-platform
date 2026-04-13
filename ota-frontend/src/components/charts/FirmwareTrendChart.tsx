'use client'

import * as React from 'react'
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from 'recharts'
import { FirmwareApprovalTrend } from '@/types'
import { formatDate } from '@/utils/formatters'

interface FirmwareTrendChartProps {
  data: FirmwareApprovalTrend[]
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
      <p className="font-semibold text-primary-900 mb-2">{label}</p>
      {payload.map((entry) => (
        <div key={entry.name} className="flex items-center gap-2 py-0.5">
          <span className="w-2.5 h-2.5 rounded-full" style={{ backgroundColor: entry.color }} />
          <span className="text-slate-600 capitalize">{entry.name}:</span>
          <span className="font-semibold text-primary-900">{entry.value}</span>
        </div>
      ))}
    </div>
  )
}

export function FirmwareTrendChart({ data, isLoading = false }: FirmwareTrendChartProps) {
  if (isLoading) {
    return (
      <div className="w-full h-64 bg-slate-100 rounded-xl animate-pulse" />
    )
  }

  if (!data || data.length === 0) {
    return (
      <div className="w-full h-64 flex items-center justify-center bg-slate-50 rounded-xl border border-dashed border-slate-200">
        <p className="text-slate-400 text-sm">No trend data available</p>
      </div>
    )
  }

  const formattedData = data.map((d) => ({
    ...d,
    date: formatDate(d.date, 'MMM dd'),
  }))

  return (
    <ResponsiveContainer width="100%" height={280}>
      <LineChart data={formattedData} margin={{ top: 5, right: 20, left: 0, bottom: 5 }}>
        <CartesianGrid strokeDasharray="3 3" stroke="#f1f5f9" />
        <XAxis
          dataKey="date"
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
          formatter={(value) => <span className="capitalize text-slate-600">{value}</span>}
        />
        <Line
          type="monotone"
          dataKey="submitted"
          stroke="#3b82f6"
          strokeWidth={2}
          dot={false}
          activeDot={{ r: 4, fill: '#3b82f6' }}
          name="Submitted"
        />
        <Line
          type="monotone"
          dataKey="approved"
          stroke="#22c55e"
          strokeWidth={2}
          dot={false}
          activeDot={{ r: 4, fill: '#22c55e' }}
          name="Approved"
        />
        <Line
          type="monotone"
          dataKey="rejected"
          stroke="#ef4444"
          strokeWidth={2}
          dot={false}
          activeDot={{ r: 4, fill: '#ef4444' }}
          name="Rejected"
        />
      </LineChart>
    </ResponsiveContainer>
  )
}
