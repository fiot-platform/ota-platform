'use client'

import * as React from 'react'
import { useQuery } from '@tanstack/react-query'
import { useParams, useRouter } from 'next/navigation'
import { ArrowLeft, Wifi, WifiOff, Calendar, Hash, Cpu, MapPin, User } from 'lucide-react'
import { deviceService } from '@/services/device.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { StatusBadge } from '@/components/ui/Badge'
import { DataTable, Column } from '@/components/ui/DataTable'
import { formatDate, formatRelativeTime, formatVersion } from '@/utils/formatters'

function InfoRow({ icon, label, value }: { icon: React.ReactNode; label: string; value: React.ReactNode }) {
  return (
    <div className="flex items-start gap-3 py-3 border-b border-slate-50 last:border-0">
      <span className="text-slate-400 mt-0.5 flex-shrink-0">{icon}</span>
      <div>
        <p className="text-xs font-semibold text-slate-400 uppercase tracking-wide mb-0.5">{label}</p>
        <div className="text-sm font-medium text-primary-800">{value}</div>
      </div>
    </div>
  )
}

interface OtaHistoryItem {
  id: string
  rolloutName: string
  status: string
  completedAt?: string
  firmwareVersion: string
}

export default function DeviceDetailPage() {
  const { id } = useParams<{ id: string }>()
  const router = useRouter()

  const { data: device, isLoading } = useQuery({
    queryKey: ['device', id],
    queryFn: () => deviceService.getDeviceById(id),
  })

  const { data: otaHistory, isLoading: historyLoading } = useQuery({
    queryKey: ['device-ota-history', id],
    queryFn: () => deviceService.getDeviceOtaHistory(id, { pageSize: 20 }),
  })

  const isOnline = device?.lastHeartbeatAt
    ? Date.now() - new Date(device.lastHeartbeatAt).getTime() < 5 * 60 * 1000
    : false

  const historyColumns: Column<OtaHistoryItem>[] = [
    {
      key: 'rolloutName',
      header: 'Rollout',
      cell: (row) => <span className="text-sm font-medium text-primary-800">{row.rolloutName}</span>,
    },
    {
      key: 'firmwareVersion',
      header: 'Firmware Version',
      cell: (row) => (
        <code className="text-xs bg-accent-50 px-2 py-0.5 rounded text-accent-700 font-semibold">
          {formatVersion(row.firmwareVersion)}
        </code>
      ),
    },
    {
      key: 'status',
      header: 'Status',
      cell: (row) => <StatusBadge status={row.status} dot />,
    },
    {
      key: 'completedAt',
      header: 'Completed',
      cell: (row) => (
        <span className="text-sm text-slate-500">
          {row.completedAt ? formatRelativeTime(row.completedAt) : '—'}
        </span>
      ),
    },
  ]

  if (isLoading) {
    return (
      <div className="space-y-6 animate-pulse">
        <div className="h-10 bg-slate-200 rounded-lg w-64" />
        <div className="grid grid-cols-3 gap-6">
          <div className="card p-6 h-64" />
          <div className="col-span-2 card p-6 h-64" />
        </div>
      </div>
    )
  }

  if (!device) {
    return (
      <div className="text-center py-20">
        <p className="text-slate-500 text-lg">Device not found</p>
        <button onClick={() => router.back()} className="btn-secondary mt-4">
          <ArrowLeft className="w-4 h-4" /> Go Back
        </button>
      </div>
    )
  }

  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        title={`Device: ${device.serialNumber}`}
        subtitle={`${device.model} — ${device.customerName ?? device.customerId}`}
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Devices', href: '/devices' },
          { label: device.serialNumber },
        ]}
        actions={
          <button onClick={() => router.back()} className="btn-secondary">
            <ArrowLeft className="w-4 h-4" /> Back
          </button>
        }
      />

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Device Info */}
        <div className="lg:col-span-1">
          <div className="card p-6">
            <div className="flex items-center justify-between mb-4">
              <h3 className="section-title">Device Info</h3>
              <StatusBadge status={device.status} dot />
            </div>

            {/* Online Status */}
            <div className={`flex items-center gap-3 p-3 rounded-lg mb-4 ${isOnline ? 'bg-success-50 border border-success-200' : 'bg-slate-50 border border-slate-200'}`}>
              {isOnline ? (
                <Wifi className="w-5 h-5 text-success-600" />
              ) : (
                <WifiOff className="w-5 h-5 text-slate-400" />
              )}
              <div>
                <p className={`text-sm font-semibold ${isOnline ? 'text-success-700' : 'text-slate-500'}`}>
                  {isOnline ? 'Online' : 'Offline'}
                </p>
                <p className="text-xs text-slate-500">
                  Last seen: {device.lastHeartbeatAt ? formatRelativeTime(device.lastHeartbeatAt) : 'Never'}
                </p>
              </div>
            </div>

            <InfoRow
              icon={<Hash className="w-4 h-4" />}
              label="Serial Number"
              value={<code className="text-accent-600">{device.serialNumber}</code>}
            />
            <InfoRow
              icon={<Cpu className="w-4 h-4" />}
              label="Model"
              value={device.model}
            />
            {device.hardwareRevision && (
              <InfoRow
                icon={<Hash className="w-4 h-4" />}
                label="Hardware Revision"
                value={device.hardwareRevision}
              />
            )}
            <InfoRow
              icon={<User className="w-4 h-4" />}
              label="Customer"
              value={
                <div>
                  <p>{device.customerName}</p>
                  <p className="text-xs text-slate-400 font-mono">{device.customerId}</p>
                </div>
              }
            />
            {device.siteName && (
              <InfoRow
                icon={<MapPin className="w-4 h-4" />}
                label="Site"
                value={device.siteName}
              />
            )}
            <InfoRow
              icon={<Cpu className="w-4 h-4" />}
              label="Current Firmware"
              value={
                device.currentFirmwareVersion ? (
                  <code className="text-accent-600 font-bold">
                    {formatVersion(device.currentFirmwareVersion)}
                  </code>
                ) : (
                  <span className="text-slate-400">Not set</span>
                )
              }
            />
            <InfoRow
              icon={<Calendar className="w-4 h-4" />}
              label="Registered"
              value={formatDate(device.registeredAt)}
            />

            {device.ipAddress && (
              <InfoRow
                icon={<Hash className="w-4 h-4" />}
                label="IP Address"
                value={<code className="text-slate-600 text-xs">{device.ipAddress}</code>}
              />
            )}
            {device.macAddress && (
              <InfoRow
                icon={<Hash className="w-4 h-4" />}
                label="MAC Address"
                value={<code className="text-slate-600 text-xs">{device.macAddress}</code>}
              />
            )}

            {device.metadata && Object.keys(device.metadata).length > 0 && (
              <div className="mt-4 pt-4 border-t border-slate-100">
                <p className="text-xs font-semibold text-slate-400 uppercase tracking-wide mb-2">Metadata</p>
                <div className="space-y-1">
                  {Object.entries(device.metadata).map(([key, value]) => (
                    <div key={key} className="flex items-center justify-between text-xs">
                      <span className="text-slate-500">{key}</span>
                      <span className="font-mono text-primary-700">{value}</span>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        </div>

        {/* OTA History */}
        <div className="lg:col-span-2">
          <div className="flex items-center justify-between mb-3">
            <h3 className="section-title">OTA Update History</h3>
            <span className="text-sm text-slate-500">{otaHistory?.pagination?.totalCount ?? 0} total updates</span>
          </div>
          <DataTable
            columns={historyColumns}
            data={otaHistory?.items ?? []}
            isLoading={historyLoading}
            keyExtractor={(r) => r.id}
            emptyMessage="No OTA update history for this device"
          />
        </div>
      </div>
    </div>
  )
}
