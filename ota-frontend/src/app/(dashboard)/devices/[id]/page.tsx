'use client'

import * as React from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useParams, useRouter } from 'next/navigation'
import {
  ArrowLeft, Wifi, WifiOff, Calendar, Hash, Cpu, MapPin, User,
  PlayCircle, PauseCircle, XCircle, Upload, ChevronDown, RefreshCw, Pencil, Radio,
} from 'lucide-react'
import { deviceService } from '@/services/device.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { StatusBadge } from '@/components/ui/Badge'
import { DataTable, Column } from '@/components/ui/DataTable'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
import { EditDeviceForm } from '@/components/forms/EditDeviceForm'
import { RoleGuard } from '@/components/role-access/RoleGuard'
import { useToast } from '@/components/ui/ToastProvider'
import { DeviceStatus, UpdateDeviceRequest } from '@/types'
import { formatDate, formatRelativeTime, formatVersion } from '@/utils/formatters'
import { AvailableFirmware } from '@/services/device.service'

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
  rolloutName?: string
  status: string
  progress: number
  source: string      // "MQTT" | "Rollout"
  completedAt?: string
  timestamp: string
  firmwareVersion: string
}

// ── Push Firmware Modal ────────────────────────────────────────────────────────
function PushFirmwareModal({
  open,
  onClose,
  deviceId,
  deviceModel,
  currentVersion,
}: {
  open: boolean
  onClose: () => void
  deviceId: string
  deviceModel?: string
  currentVersion?: string
}) {
  const { toast } = useToast()
  const queryClient = useQueryClient()
  const [selected, setSelected] = React.useState('')

  const { data: firmwareList = [], isLoading } = useQuery({
    queryKey: ['device-available-firmware', deviceId],
    queryFn: () => deviceService.getAvailableFirmware(deviceId),
    enabled: open,
  })

  const pushMutation = useMutation({
    mutationFn: () => deviceService.pushFirmware(deviceId, selected),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['device-ota-history', deviceId] })
      toast({ title: 'Firmware push queued successfully', variant: 'success' })
      setSelected('')
      onClose()
    },
    onError: (error: any) => {
      const msg = error?.response?.data?.message || 'Failed to push firmware'
      toast({ title: msg, variant: 'error' })
    },
  })

  React.useEffect(() => {
    if (!open) setSelected('')
  }, [open])

  if (!open) return null

  const approvedList = firmwareList.filter(f => f.status?.toLowerCase() === 'approved')
  const selectedFw = approvedList.find(f => f.id === selected)

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/40 backdrop-blur-sm" onClick={onClose} />
      <div className="relative bg-white rounded-2xl shadow-2xl w-full max-w-lg animate-fade-in">
        <div className="flex items-center justify-between p-6 border-b border-slate-100">
          <div>
            <h2 className="text-lg font-bold text-primary-900">Push Firmware</h2>
            <p className="text-sm text-slate-500 mt-0.5">
              Select a firmware version to push to this device
            </p>
          </div>
          <button onClick={onClose} className="p-2 rounded-lg text-slate-400 hover:bg-slate-100">
            <XCircle className="w-5 h-5" />
          </button>
        </div>

        <div className="p-6 space-y-4">
          {/* Device context: model + current version */}
          <div className="bg-slate-50 rounded-lg p-3 grid grid-cols-2 gap-2">
            {deviceModel && (
              <div className="flex items-center justify-between">
                <span className="text-xs text-slate-500 font-medium">Device model</span>
                <code className="text-xs bg-white border border-slate-200 px-2 py-0.5 rounded text-primary-700 font-semibold">
                  {deviceModel}
                </code>
              </div>
            )}
            {currentVersion && (
              <div className="flex items-center justify-between">
                <span className="text-xs text-slate-500 font-medium">Current version</span>
                <code className="text-xs bg-white border border-slate-200 px-2 py-0.5 rounded text-accent-700 font-semibold">
                  {formatVersion(currentVersion)}
                </code>
              </div>
            )}
          </div>

          {deviceModel && (
            <p className="text-xs text-slate-400 -mt-2">
              Showing only approved firmware compatible with <span className="font-semibold text-slate-600">{deviceModel}</span>.
            </p>
          )}

          <div>
            <label className="block text-sm font-semibold text-primary-800 mb-2">
              Select Firmware Version
            </label>
            {isLoading ? (
              <div className="h-10 bg-slate-100 animate-pulse rounded-lg" />
            ) : approvedList.length === 0 ? (
              <div className="text-sm text-slate-500 bg-slate-50 rounded-lg p-4 text-center">
                No approved firmware versions available for model <strong>{deviceModel}</strong>.
              </div>
            ) : (
              <div className="space-y-2 max-h-56 overflow-y-auto pr-1">
                {approvedList.map(fw => (
                  <label
                    key={fw.id}
                    className={`flex items-start gap-3 p-3 rounded-xl border cursor-pointer transition-all select-none ${
                      selected === fw.id
                        ? 'bg-accent-50 border-accent-400'
                        : 'bg-white border-slate-200 hover:border-accent-300 hover:bg-accent-50'
                    }`}
                  >
                    <input
                      type="radio"
                      name="firmware-select"
                      value={fw.id}
                      checked={selected === fw.id}
                      onChange={() => setSelected(fw.id)}
                      className="mt-1 accent-accent-600 flex-shrink-0"
                    />
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 flex-wrap">
                        <code className="text-sm font-bold text-accent-700">{formatVersion(fw.version)}</code>
                        <span className="text-xs px-1.5 py-0.5 bg-success-100 text-success-700 rounded font-semibold">✓ Approved</span>
                        <span className="text-xs px-1.5 py-0.5 bg-slate-100 text-slate-600 rounded font-medium">{fw.channel}</span>
                        {fw.isMandate && (
                          <span className="text-xs px-1.5 py-0.5 bg-warning-100 text-warning-700 rounded font-semibold">★ Mandatory</span>
                        )}
                      </div>
                      {/* Supported models */}
                      {fw.supportedModels?.length > 0 && (
                        <div className="flex flex-wrap gap-1 mt-1.5">
                          {fw.supportedModels.map(m => (
                            <span
                              key={m}
                              className={`text-xs px-1.5 py-0.5 rounded font-mono border ${
                                m === deviceModel
                                  ? 'bg-success-50 border-success-300 text-success-700'
                                  : 'bg-slate-50 border-slate-200 text-slate-500'
                              }`}
                            >
                              {m}
                            </span>
                          ))}
                        </div>
                      )}
                      {fw.fileSizeBytes > 0 && (
                        <p className="text-xs text-slate-400 mt-1">{(fw.fileSizeBytes / 1024).toFixed(1)} KB</p>
                      )}
                      {fw.releaseNotes && (
                        <p className="text-xs text-slate-500 mt-1 truncate">{fw.releaseNotes}</p>
                      )}
                    </div>
                  </label>
                ))}
              </div>
            )}
          </div>
        </div>

        <div className="flex gap-3 p-6 pt-0">
          <button onClick={onClose} className="btn-secondary flex-1">Cancel</button>
          <button
            onClick={() => pushMutation.mutate()}
            disabled={!selected || pushMutation.isPending}
            className="btn-primary flex-1 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {pushMutation.isPending ? (
              <RefreshCw className="w-4 h-4 animate-spin" />
            ) : (
              <Upload className="w-4 h-4" />
            )}
            Push Firmware
          </button>
        </div>
      </div>
    </div>
  )
}

// ── Main Page ──────────────────────────────────────────────────────────────────
export default function DeviceDetailPage() {
  const { id } = useParams<{ id: string }>()
  const router = useRouter()
  const queryClient = useQueryClient()
  const { toast } = useToast()

  const [pushOpen, setPushOpen] = React.useState(false)
  const [editOpen, setEditOpen] = React.useState(false)
  const [activateConfirm, setActivateConfirm] = React.useState(false)
  const [suspendConfirm, setSuspendConfirm] = React.useState(false)
  const [blockConfirm, setBlockConfirm] = React.useState(false)

  const { data: device, isLoading } = useQuery({
    queryKey: ['device', id],
    queryFn: () => deviceService.getDeviceById(id),
  })

  const { data: otaHistory, isLoading: historyLoading } = useQuery({
    queryKey: ['device-ota-history', id],
    queryFn: () => deviceService.getDeviceOtaHistory(id, { pageSize: 20 }),
  })

  const activateMutation = useMutation({
    mutationFn: () => deviceService.activateDevice(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['device', id] })
      queryClient.invalidateQueries({ queryKey: ['devices'] })
      toast({ title: 'Device reactivated successfully', variant: 'success' })
      setActivateConfirm(false)
    },
    onError: (error: any) => {
      const msg = error?.response?.data?.message || 'Failed to activate device'
      toast({ title: msg, variant: 'error' })
    },
  })

  const suspendMutation = useMutation({
    mutationFn: () => deviceService.suspendDevice(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['device', id] })
      queryClient.invalidateQueries({ queryKey: ['devices'] })
      toast({ title: 'Device suspended', variant: 'warning' })
      setSuspendConfirm(false)
    },
    onError: (error: any) => {
      const msg = error?.response?.data?.message || 'Failed to suspend device'
      toast({ title: msg, variant: 'error' })
    },
  })

  const blockMutation = useMutation({
    mutationFn: () => deviceService.decommissionDevice(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['device', id] })
      queryClient.invalidateQueries({ queryKey: ['devices'] })
      toast({ title: 'Device blocked / decommissioned', variant: 'success' })
      setBlockConfirm(false)
    },
    onError: (error: any) => {
      const msg = error?.response?.data?.message || 'Failed to block device'
      toast({ title: msg, variant: 'error' })
    },
  })

  const editMutation = useMutation({
    mutationFn: (data: UpdateDeviceRequest) => deviceService.updateDevice(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['device', id] })
      queryClient.invalidateQueries({ queryKey: ['devices'] })
      toast({ title: 'Device updated successfully', variant: 'success' })
      setEditOpen(false)
    },
    onError: (error: any) => {
      const msg = error?.response?.data?.message || 'Failed to update device'
      toast({ title: msg, variant: 'error' })
    },
  })

  const isOnline = device?.lastHeartbeatAt
    ? Date.now() - new Date(device.lastHeartbeatAt).getTime() < 5 * 60 * 1000
    : false

  const statusColor = (s: string) => {
    switch (s.toLowerCase()) {
      case 'success':
      case 'succeeded': return 'bg-success-100 text-success-700'
      case 'failed':    return 'bg-danger-100 text-danger-700'
      case 'rollback':  return 'bg-warning-100 text-warning-700'
      case 'start':     return 'bg-accent-100 text-accent-700'
      case 'inprogress':return 'bg-accent-100 text-accent-700'
      default:          return 'bg-slate-100 text-slate-600'
    }
  }

  const historyColumns: Column<OtaHistoryItem>[] = [
    {
      key: 'source',
      header: 'Source',
      cell: (row) => (
        <span className={`text-xs font-semibold px-2 py-0.5 rounded-full ${
          row.source === 'MQTT' ? 'bg-purple-100 text-purple-700' : 'bg-blue-100 text-blue-700'
        }`}>
          {row.source}
        </span>
      ),
    },
    {
      key: 'firmwareVersion',
      header: 'Version',
      cell: (row) => row.firmwareVersion ? (
        <code className="text-xs bg-accent-50 px-2 py-0.5 rounded text-accent-700 font-semibold">
          {formatVersion(row.firmwareVersion)}
        </code>
      ) : <span className="text-slate-400 text-xs">—</span>,
    },
    {
      key: 'status',
      header: 'Status',
      cell: (row) => (
        <span className={`text-xs font-semibold px-2 py-0.5 rounded-full capitalize ${statusColor(row.status)}`}>
          {row.status}
        </span>
      ),
    },
    {
      key: 'progress',
      header: 'Progress',
      cell: (row) => (
        <div className="w-24 space-y-0.5">
          <div className="flex justify-between">
            <span className="text-xs text-slate-500 tabular-nums">{row.progress ?? 0}%</span>
          </div>
          <div className="w-full h-1.5 bg-slate-100 rounded-full overflow-hidden">
            <div
              className={`h-full rounded-full ${
                row.status.toLowerCase() === 'success' || row.status.toLowerCase() === 'succeeded'
                  ? 'bg-success-500'
                  : row.status.toLowerCase() === 'failed'
                  ? 'bg-danger-400'
                  : row.status.toLowerCase() === 'rollback'
                  ? 'bg-warning-400'
                  : 'bg-accent-500'
              }`}
              style={{ width: `${Math.min(100, row.progress ?? 0)}%` }}
            />
          </div>
        </div>
      ),
    },
    {
      key: 'rolloutName',
      header: 'Rollout',
      cell: (row) => (
        <span className="text-xs text-slate-500">{row.rolloutName ?? '—'}</span>
      ),
    },
    {
      key: 'timestamp',
      header: 'Time',
      cell: (row) => (
        <span className="text-xs text-slate-500">
          {row.timestamp ? formatRelativeTime(row.timestamp) : '—'}
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

  const isSuspended = device.status === DeviceStatus.Suspended
  const isActive = device.status === DeviceStatus.Active
  const isDecommissioned = device.status === DeviceStatus.Decommissioned

  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        title={`Device: ${device.macImeiIp ?? device.serialNumber ?? device.id}`}
        subtitle={`${device.model} — ${device.customerName ?? device.customerId}`}
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Devices', href: '/devices' },
          { label: device.macImeiIp ?? device.serialNumber ?? device.id },
        ]}
        actions={
          <button onClick={() => router.back()} className="btn-secondary">
            <ArrowLeft className="w-4 h-4" /> Back
          </button>
        }
      />

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* ── Left column: Device info + Actions ── */}
        <div className="lg:col-span-1 space-y-4">

          {/* Device Info Card */}
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
              label="MAC / IMEI / IP"
              value={<code className="text-accent-600">{device.macImeiIp ?? '—'}</code>}
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
                label="Site / Project"
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
            <InfoRow
              icon={<Radio className="w-4 h-4" />}
              label="Publish Topic"
              value={
                device.publishTopic ? (
                  <code className="text-xs text-primary-700 break-all">{device.publishTopic}</code>
                ) : (
                  <code className="text-xs text-slate-400">
                    {`OTA/${device.macImeiIp ?? device.serialNumber ?? '—'}/Status`}
                  </code>
                )
              }
            />

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

          {/* ── Actions Card ── */}
          <RoleGuard module="Devices" action="update">
            <div className="card p-5 space-y-3">
              <h3 className="section-title mb-1">Device Actions</h3>

              {/* Edit Device Details */}
              <button
                onClick={() => setEditOpen(true)}
                className="w-full flex items-center justify-center gap-2 px-4 py-2 rounded-xl text-sm font-semibold
                  bg-primary-50 text-primary-700 border border-primary-200
                  hover:bg-primary-100 transition-colors"
              >
                <Pencil className="w-4 h-4" />
                Edit Device Details
              </button>

              {/* Push Firmware — only when Active */}
              {isActive && (
                <button
                  onClick={() => setPushOpen(true)}
                  className="w-full btn-primary flex items-center justify-center gap-2"
                >
                  <Upload className="w-4 h-4" />
                  Choose &amp; Push Firmware
                </button>
              )}

              {/* Reactivate — when Suspended or Inactive */}
              {(isSuspended || device.status === DeviceStatus.Inactive) && (
                <button
                  onClick={() => setActivateConfirm(true)}
                  className="w-full flex items-center justify-center gap-2 px-4 py-2 rounded-xl text-sm font-semibold
                    bg-success-50 text-success-700 border border-success-200
                    hover:bg-success-100 transition-colors"
                >
                  <PlayCircle className="w-4 h-4" />
                  Reactivate Device
                </button>
              )}

              {/* Suspend — when Active */}
              {isActive && (
                <button
                  onClick={() => setSuspendConfirm(true)}
                  className="w-full flex items-center justify-center gap-2 px-4 py-2 rounded-xl text-sm font-semibold
                    bg-warning-50 text-warning-700 border border-warning-200
                    hover:bg-warning-100 transition-colors"
                >
                  <PauseCircle className="w-4 h-4" />
                  Suspend Device
                </button>
              )}

              {/* Block / Decommission — any state except already decommissioned */}
              {!isDecommissioned && (
                <button
                  onClick={() => setBlockConfirm(true)}
                  className="w-full flex items-center justify-center gap-2 px-4 py-2 rounded-xl text-sm font-semibold
                    bg-danger-50 text-danger-700 border border-danger-200
                    hover:bg-danger-100 transition-colors"
                >
                  <XCircle className="w-4 h-4" />
                  Block / Decommission
                </button>
              )}

              {isDecommissioned && (
                <div className="text-center text-sm text-slate-400 py-2">
                  This device has been decommissioned.
                </div>
              )}
            </div>
          </RoleGuard>
        </div>

        {/* ── Right column: OTA History ── */}
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

      {/* ── Push Firmware Modal ── */}
      <PushFirmwareModal
        open={pushOpen}
        onClose={() => setPushOpen(false)}
        deviceId={id}
        deviceModel={device.model}
        currentVersion={device.currentFirmwareVersion}
      />

      {/* ── Reactivate Confirm ── */}
      <ConfirmDialog
        open={activateConfirm}
        onOpenChange={(open) => !open && setActivateConfirm(false)}
        title="Reactivate Device"
        message={`Reactivate device ${device.macImeiIp ?? device.serialNumber}? It will resume receiving OTA updates.`}
        confirmLabel="Reactivate"
        variant="default"
        onConfirm={() => activateMutation.mutate()}
        isLoading={activateMutation.isPending}
      />

      {/* ── Suspend Confirm ── */}
      <ConfirmDialog
        open={suspendConfirm}
        onOpenChange={(open) => !open && setSuspendConfirm(false)}
        title="Suspend Device"
        message={`Suspend device ${device.macImeiIp ?? device.serialNumber}? It will stop receiving OTA updates until reactivated.`}
        confirmLabel="Suspend"
        variant="warning"
        onConfirm={() => suspendMutation.mutate()}
        isLoading={suspendMutation.isPending}
      />

      {/* ── Block Confirm ── */}
      <ConfirmDialog
        open={blockConfirm}
        onOpenChange={(open) => !open && setBlockConfirm(false)}
        title="Block / Decommission Device"
        message={`Permanently decommission device ${device.macImeiIp ?? device.serialNumber}? This cannot be undone.`}
        confirmLabel="Block & Decommission"
        variant="destructive"
        onConfirm={() => blockMutation.mutate()}
        isLoading={blockMutation.isPending}
      />

      {/* Edit Device Modal */}
      <EditDeviceForm
        open={editOpen}
        onOpenChange={setEditOpen}
        device={device}
        onSubmit={async (data) => { await editMutation.mutateAsync(data) }}
        isLoading={editMutation.isPending}
      />
    </div>
  )
}
