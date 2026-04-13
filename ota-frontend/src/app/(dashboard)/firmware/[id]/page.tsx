'use client'

import * as React from 'react'
import { useQuery } from '@tanstack/react-query'
import { useParams, useRouter } from 'next/navigation'
import {
  ArrowLeft,
  CheckCircle,
  XCircle,
  FlaskConical,
  Archive,
  Download,
  Calendar,
  Package,
  Hash,
  GitBranch,
  User,
} from 'lucide-react'
import { firmwareService } from '@/services/firmware.service'
import { QASessionPanel } from '@/components/qa/QASessionPanel'
import { PageHeader } from '@/components/ui/PageHeader'
import { StatusBadge } from '@/components/ui/Badge'
import { ApproveFirmwareDialog } from '@/components/dialogs/ApproveFirmwareDialog'
import { RejectFirmwareDialog } from '@/components/dialogs/RejectFirmwareDialog'
import { QAVerifyDialog } from '@/components/dialogs/QAVerifyDialog'
import { RoleGuard } from '@/components/role-access/RoleGuard'
import { FirmwareStatus, UserRole } from '@/types'
import { formatDate, formatFileSize, formatRelativeTime } from '@/utils/formatters'

function InfoItem({ label, value, icon }: { label: string; value: React.ReactNode; icon?: React.ReactNode }) {
  return (
    <div className="flex items-start gap-3 py-3 border-b border-slate-50 last:border-0">
      {icon && <span className="text-slate-400 mt-0.5 flex-shrink-0">{icon}</span>}
      <div className="flex-1 min-w-0">
        <p className="text-xs font-semibold text-slate-400 uppercase tracking-wide mb-0.5">{label}</p>
        <div className="text-sm text-primary-800 font-medium">{value}</div>
      </div>
    </div>
  )
}

function StatusStep({
  label,
  completed,
  timestamp,
  by,
  active,
  notes,
}: {
  label: string
  completed: boolean
  timestamp?: string | null
  by?: string | null
  active?: boolean
  notes?: string | null
}) {
  return (
    <div className="flex gap-4">
      <div className="flex flex-col items-center">
        <div className={`w-8 h-8 rounded-full flex items-center justify-center flex-shrink-0 ${
          completed ? 'bg-success-500' : active ? 'bg-accent-500' : 'bg-slate-200'
        }`}>
          {completed ? (
            <CheckCircle className="w-5 h-5 text-white" />
          ) : (
            <div className={`w-3 h-3 rounded-full ${active ? 'bg-white' : 'bg-slate-400'}`} />
          )}
        </div>
        <div className="w-0.5 flex-1 bg-slate-200 mt-1 mb-1 min-h-[20px]" />
      </div>
      <div className="flex-1 pb-4 min-w-0">
        <p className={`text-sm font-semibold ${completed ? 'text-success-700' : active ? 'text-accent-700' : 'text-slate-400'}`}>
          {label}
        </p>
        {timestamp && (
          <p className="text-xs text-slate-500 mt-0.5">
            {formatDate(timestamp)} by {by ?? 'System'}
          </p>
        )}
        {notes && (
          <p className="text-xs text-slate-500 mt-1 italic bg-slate-50 rounded p-2">{notes}</p>
        )}
      </div>
    </div>
  )
}

export default function FirmwareDetailPage() {
  const { id } = useParams<{ id: string }>()
  const router = useRouter()

  const [approveOpen, setApproveOpen] = React.useState(false)
  const [rejectOpen, setRejectOpen] = React.useState(false)
  const [qaOpen, setQaOpen] = React.useState(false)

  const { data: firmware, isLoading } = useQuery({
    queryKey: ['firmware', id],
    queryFn: () => firmwareService.getFirmwareById(id),
  })

  if (isLoading) {
    return (
      <div className="space-y-6 animate-pulse">
        <div className="h-10 bg-slate-200 rounded-lg w-64" />
        <div className="grid grid-cols-3 gap-6">
          <div className="col-span-2 card p-6 h-64" />
          <div className="card p-6 h-64" />
        </div>
      </div>
    )
  }

  if (!firmware) {
    return (
      <div className="text-center py-20">
        <p className="text-slate-500 text-lg">Firmware not found</p>
        <button onClick={() => router.back()} className="btn-secondary mt-4">
          <ArrowLeft className="w-4 h-4" /> Go Back
        </button>
      </div>
    )
  }

  const canApprove = [FirmwareStatus.PendingApproval, FirmwareStatus.QAVerified].includes(firmware.status as FirmwareStatus)
  const canReject = [FirmwareStatus.PendingApproval, FirmwareStatus.QAVerified, FirmwareStatus.PendingQA].includes(firmware.status as FirmwareStatus)
  const canQaVerify = !firmware.isQaVerified && [FirmwareStatus.Draft, FirmwareStatus.PendingQA].includes(firmware.status as FirmwareStatus)

  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        title={`Firmware: ${firmware.version}`}
        subtitle="Firmware version details and lifecycle management"
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Firmware', href: '/firmware' },
          { label: firmware.version },
        ]}
        actions={
          <div className="flex items-center gap-2">
            <button onClick={() => router.back()} className="btn-secondary">
              <ArrowLeft className="w-4 h-4" /> Back
            </button>

            <RoleGuard module="Firmware" action="approve" roles={[UserRole.QA, UserRole.PlatformAdmin, UserRole.SuperAdmin]}>
              {canQaVerify && (
                <button onClick={() => setQaOpen(true)} className="btn-primary bg-accent-600">
                  <FlaskConical className="w-4 h-4" /> QA Verify
                </button>
              )}
            </RoleGuard>

            <RoleGuard module="Firmware" action="approve" roles={[UserRole.ReleaseManager, UserRole.PlatformAdmin, UserRole.SuperAdmin]}>
              {canApprove && (
                <button onClick={() => setApproveOpen(true)} className="btn-primary bg-success-600 hover:bg-success-700">
                  <CheckCircle className="w-4 h-4" /> Approve
                </button>
              )}
              {canReject && (
                <button onClick={() => setRejectOpen(true)} className="btn-danger">
                  <XCircle className="w-4 h-4" /> Reject
                </button>
              )}
            </RoleGuard>
          </div>
        }
      />

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Main Info */}
        <div className="lg:col-span-2 space-y-6">
          {/* Details Card */}
          <div className="card p-6">
            <div className="flex items-center justify-between mb-4">
              <h3 className="section-title">Version Details</h3>
              <div className="flex items-center gap-2">
                <StatusBadge status={firmware.channel} />
                <StatusBadge status={firmware.status} dot />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-x-8">
              <div>
                <InfoItem
                  icon={<Hash className="w-4 h-4" />}
                  label="Version"
                  value={<code className="text-accent-600 font-bold">{firmware.version}</code>}
                />
                <InfoItem
                  icon={<GitBranch className="w-4 h-4" />}
                  label="Repository"
                  value={firmware.repositoryName ?? '—'}
                />
                <InfoItem
                  icon={<Package className="w-4 h-4" />}
                  label="File Size"
                  value={formatFileSize(firmware.fileSizeBytes)}
                />
                <InfoItem
                  icon={<Hash className="w-4 h-4" />}
                  label="Checksum (SHA256)"
                  value={
                    firmware.checksum ? (
                      <code className="text-xs text-slate-600 break-all">{firmware.checksum}</code>
                    ) : '—'
                  }
                />
              </div>
              <div>
                <InfoItem
                  icon={<Calendar className="w-4 h-4" />}
                  label="Created"
                  value={formatDate(firmware.createdAt)}
                />
                <InfoItem
                  icon={<Calendar className="w-4 h-4" />}
                  label="Last Updated"
                  value={formatDate(firmware.updatedAt)}
                />
                <InfoItem
                  icon={<Hash className="w-4 h-4" />}
                  label="Gitea Tag"
                  value={firmware.giteaTagName ? (
                    <code className="text-sm text-accent-600">{firmware.giteaTagName}</code>
                  ) : '—'}
                />
                <InfoItem
                  icon={<User className="w-4 h-4" />}
                  label="QA Status"
                  value={<StatusBadge status={firmware.isQaVerified ? 'QAVerified' : 'PendingQA'} />}
                />
              </div>
            </div>

            {firmware.releaseNotes && (
              <div className="mt-4 pt-4 border-t border-slate-100">
                <p className="label mb-2">Release Notes</p>
                <div className="text-sm text-slate-600 leading-relaxed whitespace-pre-wrap bg-slate-50 rounded-lg p-4">
                  {firmware.releaseNotes}
                </div>
              </div>
            )}
          </div>

          {/* Gitea Assets */}
          {(firmware.giteaAssets ?? []).length > 0 && (
            <div className="card p-6">
              <h3 className="section-title mb-4">Gitea Assets</h3>
              <div className="space-y-2">
                {firmware.giteaAssets!.map((asset) => (
                  <div
                    key={asset.id}
                    className="flex items-center justify-between p-3 bg-slate-50 rounded-lg"
                  >
                    <div>
                      <p className="text-sm font-medium text-primary-800">{asset.name}</p>
                      <p className="text-xs text-slate-500">{asset.contentType} — {formatFileSize(asset.size)}</p>
                    </div>
                    <a
                      href={asset.downloadUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-accent-600 bg-accent-50 hover:bg-accent-100 rounded-lg transition-colors"
                    >
                      <Download className="w-3.5 h-3.5" />
                      Download
                    </a>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* QA Details */}
          {firmware.isQaVerified && (
            <div className="card p-6 border-l-4 border-accent-400">
              <h3 className="section-title flex items-center gap-2 mb-3">
                <FlaskConical className="w-4 h-4 text-accent-600" />
                QA Verification
              </h3>
              <div className="grid grid-cols-2 gap-4 text-sm">
                <div>
                  <p className="text-xs text-slate-400 uppercase tracking-wide mb-1">Verified By</p>
                  <p className="font-medium">{firmware.qaVerifiedBy ?? '—'}</p>
                </div>
                <div>
                  <p className="text-xs text-slate-400 uppercase tracking-wide mb-1">Verified At</p>
                  <p className="font-medium">{formatDate(firmware.qaVerifiedAt)}</p>
                </div>
              </div>
              {firmware.qaRemarks && (
                <div className="mt-3 p-3 bg-slate-50 rounded-lg text-sm text-slate-600 italic">
                  {firmware.qaRemarks}
                </div>
              )}
            </div>
          )}
        </div>

        {/* Approval Timeline */}
        <div className="lg:col-span-1">
          <div className="card p-6">
            <h3 className="section-title mb-5">Approval Timeline</h3>
            <div className="space-y-0">
              <StatusStep
                label="Draft Created"
                completed={true}
                timestamp={firmware.createdAt}
                by="System"
              />
              <StatusStep
                label="QA Verification"
                completed={firmware.isQaVerified}
                active={!firmware.isQaVerified && firmware.status === FirmwareStatus.PendingQA}
                timestamp={firmware.qaVerifiedAt}
                by={firmware.qaVerifiedBy}
                notes={firmware.qaRemarks}
              />
              <StatusStep
                label="Pending Approval"
                completed={[FirmwareStatus.Approved, FirmwareStatus.Rejected, FirmwareStatus.Deprecated, FirmwareStatus.Active].includes(firmware.status as FirmwareStatus)}
                active={firmware.status === FirmwareStatus.PendingApproval}
              />
              <StatusStep
                label={firmware.status === FirmwareStatus.Rejected ? 'Rejected' : 'Approved'}
                completed={[FirmwareStatus.Approved, FirmwareStatus.Active].includes(firmware.status as FirmwareStatus)}
                timestamp={firmware.approvedAt ?? firmware.rejectedAt}
                by={firmware.approvedBy ?? firmware.rejectedBy}
                notes={firmware.approvalNotes ?? firmware.rejectionReason}
              />
              {firmware.status === FirmwareStatus.Deprecated && (
                <StatusStep
                  label="Deprecated"
                  completed={true}
                  timestamp={firmware.updatedAt}
                />
              )}
            </div>

            {firmware.status === FirmwareStatus.Rejected && firmware.rejectionReason && (
              <div className="mt-4 p-3 bg-danger-50 border border-danger-200 rounded-lg">
                <p className="text-xs font-semibold text-danger-700 uppercase mb-1">Rejection Reason</p>
                <p className="text-sm text-danger-600">{firmware.rejectionReason}</p>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* QA Session Panel */}
      <QASessionPanel firmwareId={id} firmwareVersion={firmware.version} />

      {/* Dialogs */}
      <ApproveFirmwareDialog
        open={approveOpen}
        onOpenChange={setApproveOpen}
        firmwareId={id}
        firmwareVersion={firmware.version}
      />
      <RejectFirmwareDialog
        open={rejectOpen}
        onOpenChange={setRejectOpen}
        firmwareId={id}
        firmwareVersion={firmware.version}
      />
      <QAVerifyDialog
        open={qaOpen}
        onOpenChange={setQaOpen}
        firmwareId={id}
        firmwareVersion={firmware.version}
      />
    </div>
  )
}
