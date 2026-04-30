'use client'

import * as React from 'react'
import { useParams, useRouter } from 'next/navigation'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  ArrowLeft, GitBranch, Globe, RefreshCw, Loader2,
  PowerOff, Zap, Calendar, FolderOpen, Hash, Building2,
} from 'lucide-react'
import Link from 'next/link'
import { repositoryService } from '@/services/repository.service'
import { firmwareService } from '@/services/firmware.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { StatusBadge } from '@/components/ui/Badge'
import { DataTable, Column } from '@/components/ui/DataTable'
import { RoleGuard } from '@/components/role-access/RoleGuard'
import { useToast } from '@/components/ui/ToastProvider'
import { FirmwareVersion } from '@/types'
import { formatDate, formatRelativeTime, formatFileSize } from '@/utils/formatters'

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

export default function RepositoryDetailPage() {
  const { id } = useParams<{ id: string }>()
  const router = useRouter()
  const queryClient = useQueryClient()
  const { toast } = useToast()
  const [syncingId, setSyncingId] = React.useState(false)

  const { data: repo, isLoading } = useQuery({
    queryKey: ['repository', id],
    queryFn: () => repositoryService.getRepositoryById(id),
  })

  const { data: firmware, isLoading: firmwareLoading } = useQuery({
    queryKey: ['firmware', { repositoryId: id }],
    queryFn: () => firmwareService.getFirmwareList({ repositoryId: id, pageSize: 100 }),
    enabled: !!id,
  })

  const syncMutation = useMutation({
    mutationFn: () => repositoryService.syncRepository(id),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ['repository', id] })
      queryClient.invalidateQueries({ queryKey: ['firmware'] })
      setSyncingId(false)
      toast({ title: 'Repository synced', description: result.message, variant: 'success' })
    },
    onError: () => {
      setSyncingId(false)
      toast({ title: 'Sync failed', description: 'Could not sync repository', variant: 'error' })
    },
  })

  const deactivateMutation = useMutation({
    mutationFn: () => repositoryService.deactivateRepository(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['repository', id] })
      queryClient.invalidateQueries({ queryKey: ['repositories'] })
      toast({ title: 'Repository deactivated', variant: 'success' })
    },
    onError: () => toast({ title: 'Failed to deactivate', variant: 'error' }),
  })

  const activateMutation = useMutation({
    mutationFn: () => repositoryService.activateRepository(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['repository', id] })
      queryClient.invalidateQueries({ queryKey: ['repositories'] })
      toast({ title: 'Repository activated', variant: 'success' })
    },
    onError: () => toast({ title: 'Failed to activate', variant: 'error' }),
  })

  const firmwareColumns: Column<FirmwareVersion>[] = [
    {
      key: 'version',
      header: 'Version',
      cell: (row) => (
        <Link
          href={`/firmware/${row.id}`}
          className="font-mono font-bold text-accent-600 hover:text-accent-700 text-sm transition-colors"
        >
          {row.version}
        </Link>
      ),
    },
    {
      key: 'channel',
      header: 'Channel',
      cell: (row) => <StatusBadge status={row.channel} />,
    },
    {
      key: 'status',
      header: 'Status',
      cell: (row) => <StatusBadge status={row.status} dot />,
    },
    {
      key: 'qa',
      header: 'QA',
      cell: (row) => <StatusBadge status={row.isQaVerified ? 'QAVerified' : 'Pending'} />,
    },
    {
      key: 'models',
      header: 'Models',
      cell: (row) => {
        const models = row.supportedModels ?? []
        if (models.length === 0) return <span className="text-slate-400 text-sm">—</span>
        return (
          <div className="flex flex-wrap gap-1">
            {models.slice(0, 2).map((m) => (
              <span key={m} className="text-xs px-1.5 py-0.5 bg-slate-100 text-slate-600 rounded font-mono border border-slate-200">
                {m}
              </span>
            ))}
            {models.length > 2 && (
              <span className="text-xs px-1.5 py-0.5 bg-slate-100 text-slate-500 rounded" title={models.slice(2).join(', ')}>
                +{models.length - 2}
              </span>
            )}
          </div>
        )
      },
    },
    {
      key: 'fileSize',
      header: 'Size',
      cell: (row) => (
        <span className="text-sm text-slate-500">{row.fileSizeBytes ? formatFileSize(row.fileSizeBytes) : '—'}</span>
      ),
    },
    {
      key: 'createdAt',
      header: 'Created',
      cell: (row) => <span className="text-sm text-slate-500">{formatDate(row.createdAt)}</span>,
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

  if (!repo) {
    return (
      <div className="text-center py-20">
        <p className="text-slate-500 text-lg">Repository not found</p>
        <button onClick={() => router.back()} className="btn-secondary mt-4">
          <ArrowLeft className="w-4 h-4" /> Go Back
        </button>
      </div>
    )
  }

  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        title={repo.name}
        subtitle={`${repo.giteaOwner}/${repo.giteaRepo}`}
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Repositories', href: '/repositories' },
          { label: repo.name },
        ]}
        actions={
          <button onClick={() => router.back()} className="btn-secondary">
            <ArrowLeft className="w-4 h-4" /> Back
          </button>
        }
      />

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* ── Left: Repository Info + Actions ── */}
        <div className="lg:col-span-1 space-y-4">

          {/* Info Card */}
          <div className="card p-6">
            <div className="flex items-center justify-between mb-4">
              <h3 className="section-title">Repository Info</h3>
              <StatusBadge status={repo.isActive ? 'Active' : 'Inactive'} dot />
            </div>

            <InfoRow
              icon={<Hash className="w-4 h-4" />}
              label="Repository"
              value={<code className="text-accent-600">{repo.giteaOwner}/{repo.giteaRepo}</code>}
            />
            {repo.projectName && (
              <InfoRow
                icon={<FolderOpen className="w-4 h-4" />}
                label="Project"
                value={repo.projectName}
              />
            )}
            {repo.clientName && (
              <InfoRow
                icon={<Building2 className="w-4 h-4" />}
                label="Client"
                value={repo.clientName}
              />
            )}
            <InfoRow
              icon={<GitBranch className="w-4 h-4" />}
              label="Default Branch"
              value={<code className="text-slate-700">{repo.defaultBranch}</code>}
            />
            <InfoRow
              icon={<Zap className="w-4 h-4" />}
              label="Webhook"
              value={<StatusBadge status={repo.webhookConfigured ? 'Active' : 'Inactive'} />}
            />
            {repo.giteaUrl && (
              <InfoRow
                icon={<Globe className="w-4 h-4" />}
                label="Gitea URL"
                value={
                  <a
                    href={repo.giteaUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-accent-600 hover:underline text-xs font-mono break-all"
                  >
                    {repo.giteaUrl}
                  </a>
                }
              />
            )}
            <InfoRow
              icon={<RefreshCw className="w-4 h-4" />}
              label="Last Synced"
              value={repo.lastSyncedAt ? formatRelativeTime(repo.lastSyncedAt) : 'Never'}
            />
            <InfoRow
              icon={<Calendar className="w-4 h-4" />}
              label="Registered"
              value={formatDate(repo.createdAt)}
            />
            {repo.description && (
              <InfoRow
                icon={<Hash className="w-4 h-4" />}
                label="Description"
                value={<span className="text-slate-600">{repo.description}</span>}
              />
            )}
          </div>

          {/* Actions Card */}
          <RoleGuard module="Repositories" action="execute">
            <div className="card p-5 space-y-3">
              <h3 className="section-title mb-1">Actions</h3>

              <button
                onClick={() => { setSyncingId(true); syncMutation.mutate() }}
                disabled={syncMutation.isPending}
                className="w-full btn-primary flex items-center justify-center gap-2 disabled:opacity-50"
              >
                {syncMutation.isPending ? (
                  <Loader2 className="w-4 h-4 animate-spin" />
                ) : (
                  <RefreshCw className="w-4 h-4" />
                )}
                Sync Repository
              </button>

              <RoleGuard module="Repositories" action="update">
                {repo.isActive ? (
                  <button
                    onClick={() => deactivateMutation.mutate()}
                    disabled={deactivateMutation.isPending}
                    className="w-full flex items-center justify-center gap-2 px-4 py-2 rounded-xl text-sm font-semibold
                      bg-warning-50 text-warning-700 border border-warning-200 hover:bg-warning-100 transition-colors disabled:opacity-50"
                  >
                    {deactivateMutation.isPending ? (
                      <Loader2 className="w-4 h-4 animate-spin" />
                    ) : (
                      <PowerOff className="w-4 h-4" />
                    )}
                    Deactivate
                  </button>
                ) : (
                  <button
                    onClick={() => activateMutation.mutate()}
                    disabled={activateMutation.isPending}
                    className="w-full flex items-center justify-center gap-2 px-4 py-2 rounded-xl text-sm font-semibold
                      bg-success-50 text-success-700 border border-success-200 hover:bg-success-100 transition-colors disabled:opacity-50"
                  >
                    {activateMutation.isPending ? (
                      <Loader2 className="w-4 h-4 animate-spin" />
                    ) : (
                      <Zap className="w-4 h-4" />
                    )}
                    Activate
                  </button>
                )}
              </RoleGuard>
            </div>
          </RoleGuard>
        </div>

        {/* ── Right: Firmware Versions ── */}
        <div className="lg:col-span-2">
          <div className="flex items-center justify-between mb-3">
            <h3 className="section-title">Firmware Versions</h3>
            <span className="text-sm text-slate-500">{firmware?.pagination?.totalCount ?? 0} total</span>
          </div>
          <DataTable
            columns={firmwareColumns}
            data={firmware?.items ?? []}
            isLoading={firmwareLoading}
            keyExtractor={(r) => r.id}
            emptyMessage="No firmware versions linked to this repository"
          />
        </div>
      </div>
    </div>
  )
}
