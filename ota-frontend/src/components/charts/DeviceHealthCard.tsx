'use client'

import * as React from 'react'
import { PieChart, Pie, Cell, ResponsiveContainer } from 'recharts'
import { TrendingUp } from 'lucide-react'

interface DeviceHealthCardProps {
  totalDevices: number
  onlineDevices: number
  offlineDevices: number
  isLoading?: boolean
}

export function DeviceHealthCard({
  totalDevices,
  onlineDevices,
  offlineDevices,
  isLoading = false,
}: DeviceHealthCardProps) {
  const healthPct = totalDevices > 0 ? Math.round((onlineDevices / totalDevices) * 100) : 0

  const chartData = [
    { name: 'Online',  value: onlineDevices  || 0 },
    { name: 'Offline', value: offlineDevices || 0 },
  ].filter((d) => d.value > 0)

  if (isLoading) {
    return (
      <div className="card p-5 animate-pulse">
        <div className="flex items-center gap-6">
          <div className="flex-1 space-y-3">
            <div className="h-8 w-20 bg-slate-200 rounded" />
            <div className="h-4 w-28 bg-slate-200 rounded" />
            <div className="h-3 w-24 bg-slate-200 rounded" />
            <div className="h-3 w-24 bg-slate-200 rounded" />
          </div>
          <div className="w-28 h-28 rounded-full bg-slate-200 flex-shrink-0" />
        </div>
      </div>
    )
  }

  return (
    <div className="card p-5">
      <div className="flex items-center gap-4">

        {/* ── Left column: counts ────────────────────────────────────── */}
        <div className="flex-1 min-w-0 space-y-3">
          {/* Total + badge */}
          <div className="flex items-center gap-2">
            <span className="text-3xl font-bold text-primary-900 tracking-tight">
              {totalDevices.toLocaleString()}
            </span>
            <span className="inline-flex items-center gap-0.5 text-xs font-semibold text-success-700 bg-success-50 border border-success-200 px-2 py-0.5 rounded-full whitespace-nowrap">
              <TrendingUp className="w-3 h-3" />
              {healthPct}%
            </span>
          </div>

          {/* Label */}
          <p className="text-sm font-medium text-slate-500">Device Health</p>

          {/* Online / Offline */}
          <div className="space-y-1.5">
            <div className="flex items-center justify-between">
              <span className="flex items-center gap-1.5 text-sm text-slate-600">
                <span className="w-2.5 h-2.5 rounded-full bg-success-500 flex-shrink-0" />
                Online
              </span>
              <span className="text-sm font-semibold text-primary-900">
                {onlineDevices.toLocaleString()}
              </span>
            </div>
            <div className="flex items-center justify-between">
              <span className="flex items-center gap-1.5 text-sm text-slate-600">
                <span className="w-2.5 h-2.5 rounded-full bg-pink-400 flex-shrink-0" />
                Offline
              </span>
              <span className="text-sm font-semibold text-primary-900">
                {offlineDevices.toLocaleString()}
              </span>
            </div>
          </div>
        </div>

        {/* ── Right column: donut chart ──────────────────────────────── */}
        <div className="w-28 h-28 flex-shrink-0">
          {chartData.length > 0 ? (
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <Pie
                  data={chartData}
                  cx="50%"
                  cy="50%"
                  innerRadius={32}
                  outerRadius={52}
                  paddingAngle={3}
                  dataKey="value"
                  startAngle={90}
                  endAngle={-270}
                >
                  <Cell fill="#22c55e" stroke="#ffffff" strokeWidth={2} />
                  <Cell fill="#f472b6" stroke="#ffffff" strokeWidth={2} />
                </Pie>
              </PieChart>
            </ResponsiveContainer>
          ) : (
            <div className="w-full h-full rounded-full bg-slate-100 flex items-center justify-center">
              <span className="text-xs text-slate-400">—</span>
            </div>
          )}
        </div>

      </div>
    </div>
  )
}
