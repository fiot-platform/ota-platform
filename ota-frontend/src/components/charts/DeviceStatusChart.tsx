'use client'

import * as React from 'react'
import {
  PieChart,
  Pie,
  Cell,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from 'recharts'

interface DeviceStatusData {
  status: string
  count: number
}

interface DeviceStatusChartProps {
  data: DeviceStatusData[]
  isLoading?: boolean
}

const STATUS_COLORS: Record<string, string> = {
  Active: '#22c55e',
  Inactive: '#94a3b8',
  Suspended: '#f59e0b',
  Decommissioned: '#ef4444',
  Pending: '#06b6d4',
  UpToDate: '#22c55e',
  UpdateAvailable: '#f59e0b',
  Updating: '#8b5cf6',
  Failed: '#ef4444',
  Offline: '#94a3b8',
}

const DEFAULT_COLORS = ['#22c55e', '#06b6d4', '#f59e0b', '#ef4444', '#8b5cf6', '#64748b']

function getColor(status: string, index: number): string {
  return STATUS_COLORS[status] ?? DEFAULT_COLORS[index % DEFAULT_COLORS.length]
}

function CustomTooltip({ active, payload }: {
  active?: boolean
  payload?: Array<{ name: string; value: number; payload: DeviceStatusData }>
}) {
  if (!active || !payload?.length) return null
  const item = payload[0]
  return (
    <div className="bg-white border border-slate-200 rounded-xl shadow-lg p-3 text-sm">
      <p className="font-semibold text-primary-900">{item.name}</p>
      <p className="text-slate-600 mt-1">
        Count: <span className="font-semibold text-primary-900">{item.value.toLocaleString()}</span>
      </p>
    </div>
  )
}

function CustomLegend({ payload }: { payload?: Array<{ value: string; color: string }> }) {
  if (!payload) return null
  return (
    <div className="flex flex-wrap justify-center gap-3 mt-3">
      {payload.map((entry) => (
        <div key={entry.value} className="flex items-center gap-1.5 text-xs text-slate-600">
          <span className="w-2.5 h-2.5 rounded-full flex-shrink-0" style={{ backgroundColor: entry.color }} />
          {entry.value}
        </div>
      ))}
    </div>
  )
}

export function DeviceStatusChart({ data, isLoading = false }: DeviceStatusChartProps) {
  if (isLoading) {
    return <div className="w-full h-64 bg-slate-100 rounded-xl animate-pulse" />
  }

  if (!data || data.length === 0) {
    return (
      <div className="w-full h-64 flex items-center justify-center bg-slate-50 rounded-xl border border-dashed border-slate-200">
        <p className="text-slate-400 text-sm">No device status data available</p>
      </div>
    )
  }

  const total = data.reduce((sum, d) => sum + d.count, 0)

  return (
    <div className="relative">
      <ResponsiveContainer width="100%" height={280}>
        <PieChart>
          <Pie
            data={data}
            cx="50%"
            cy="45%"
            innerRadius={65}
            outerRadius={100}
            paddingAngle={3}
            dataKey="count"
            nameKey="status"
          >
            {data.map((entry, index) => (
              <Cell
                key={`cell-${index}`}
                fill={getColor(entry.status, index)}
                strokeWidth={2}
                stroke="#ffffff"
              />
            ))}
          </Pie>
          <Tooltip content={<CustomTooltip />} />
          <Legend content={<CustomLegend />} />
        </PieChart>
      </ResponsiveContainer>

      {/* Center label */}
      <div className="absolute inset-0 flex items-center justify-center pointer-events-none" style={{ top: '-8px' }}>
        <div className="text-center">
          <p className="text-2xl font-bold text-primary-900">{total.toLocaleString()}</p>
          <p className="text-xs text-slate-500">Total Devices</p>
        </div>
      </div>
    </div>
  )
}
