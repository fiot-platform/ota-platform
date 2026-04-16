'use client'

import * as React from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, Search, RefreshCw, Loader2, PowerOff, Eye, Trash2 } from 'lucide-react'
import Link from 'next/link'
import { repositoryService } from '@/services/repository.service'
import { projectService } from '@/services/project.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { DataTable, Column } from '@/components/ui/DataTable'
import { StatusBadge } from '@/components/ui/Badge'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
import { RepositoryForm, RegisterRepositoryPayload } from '@/components/forms/RepositoryForm'
import { RoleGuard } from '@/components/role-access/RoleGuard'
import { useToast } from '@/components/ui/ToastProvider'
import { Repository, UserRole } from '@/types'
import { formatRelativeTime } from '@/utils/formatters'
import { useAuth } from '@/hooks/useAuth'

export default function RepositoriesPage() {
  const queryClient = useQueryClient()
  const { toast } = useToast()
  const { role } = useAuth()
  const canFilterByProject = role === UserRole.SuperAdmin || role === UserRole.PlatformAdmin

  const [search, setSearch] = React.useState('')
  const [projectId, setProjectId] = React.useState('')
  const [page, setPage] = React.useState(1)
  const [pageSize, setPageSize] = React.useState(25)
  const [syncingId, setSyncingId] = React.useState<string | null>(null)
  const [confirmDeactivate, setConfirmDeactivate] = React.useState<Repository | null>(null)
  const [confirmDelete, setConfirmDelete] = React.useState<Repository | null>(null)
  const [repoFormOpen, setRepoFormOpen] = React.useState(false)

  const { data, isLoading } = useQuery({
    queryKey: ['repositories', { search, projectId, page, pageSize }],
    queryFn: () => repositoryService.getRepositories({ search, projectId: projectId || undefined, page, pageSize }),
  })

  const { data: projects } = useQuery({
    queryKey: ['projects-all'],
    queryFn: () => projectService.getProjects({ pageSize: 200 }),
  })

  const syncMutation = useMutation({
    mutationFn: (id: string) => repositoryService.syncRepository(id),
    onSuccess: (result, id) => {
      queryClient.invalidateQueries({ queryKey: ['repositories'] })
      setSyncingId(null)
      toast({ title: 'Repository synced', description: result.message, variant: 'success' })
    },
    onError: () => {
      setSyncingId(null)
      toast({ title: 'Sync failed', description: 'Could not sync repository', variant: 'error' })
    },
  })

  const deactivateMutation = useMutation({
    mutationFn: (id: string) => repositoryService.deactivateRepository(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['repositories'] })
      toast({ title: 'Repository deactivated', variant: 'success' })
      setConfirmDeactivate(null)
    },
    onError: () => toast({ title: 'Failed to deactivate', variant: 'error' }),
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => repositoryService.deleteRepository(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['repositories'] })
      toast({ title: 'Repository deleted', variant: 'success' })
      setConfirmDelete(null)
    },
    onError: () => toast({ title: 'Failed to delete repository', variant: 'error' }),
  })

  const registerMutation = useMutation({
    mutationFn: (data: RegisterRepositoryPayload) => repositoryService.registerRepository(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['repositories'] })
      toast({ title: 'Repository registered', description: 'Repository connected successfully', variant: 'success' })
      setRepoFormOpen(false)
    },
    onError: (err: any) => {
      const msg = err?.response?.data?.message ?? 'Could not register repository'
      toast({ title: 'Registration failed', description: msg, variant: 'error' })
    },
  })

  const handleSync = (id: string) => {
    setSyncingId(id)
    syncMutation.mutate(id)
  }

  const columns: Column<Repository>[] = [
    {
      key: 'name',
      header: 'Repository',
      cell: (row) => (
        <div>
          <p className="font-semibold text-primary-900">{row.name}</p>
          <p className="text-xs text-slate-500 font-mono">{row.giteaOwner}/{row.giteaRepo}</p>
        </div>
      ),
    },
    {
      key: 'project',
      header: 'Project',
      cell: (row) => (
        <span className="text-sm text-primary-700 font-medium">
          {row.projectName ?? '—'}
        </span>
      ),
    },
    {
      key: 'defaultBranch',
      header: 'Default Branch',
      cell: (row) => (
        <code className="text-xs bg-slate-100 px-2 py-0.5 rounded text-slate-700">{row.defaultBranch}</code>
      ),
    },
    {
      key: 'webhook',
      header: 'Webhook',
      cell: (row) => (
        <StatusBadge status={row.webhookConfigured ? 'Active' : 'Inactive'} />
      ),
    },
    {
      key: 'status',
      header: 'Status',
      cell: (row) => <StatusBadge status={row.isActive ? 'Active' : 'Inactive'} />,
    },
    {
      key: 'lastSyncedAt',
      header: 'Last Synced',
      cell: (row) => (
        <span className="text-sm text-slate-500">
          {row.lastSyncedAt ? formatRelativeTime(row.lastSyncedAt) : 'Never'}
        </span>
      ),
    },
    {
      key: 'actions',
      header: 'Actions',
      cell: (row) => (
        <div className="flex items-center gap-1">
          <Link
            href={`/repositories/${row.id}`}
            className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg text-xs font-medium bg-slate-50 text-slate-700 hover:bg-slate-100 transition-colors"
          >
            <Eye className="w-3.5 h-3.5" />
            View
          </Link>
          <RoleGuard module="Repositories" action="execute">
            <button
              onClick={() => handleSync(row.id)}
              disabled={syncingId === row.id}
              className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg text-xs font-medium bg-accent-50 text-accent-700 hover:bg-accent-100 transition-colors disabled:opacity-50"
            >
              {syncingId === row.id ? (
                <Loader2 className="w-3.5 h-3.5 animate-spin" />
              ) : (
                <RefreshCw className="w-3.5 h-3.5" />
              )}
              Sync
            </button>
          </RoleGuard>
          <RoleGuard module="Repositories" action="update">
            {row.isActive && (
              <button
                onClick={() => setConfirmDeactivate(row)}
                className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg text-xs font-medium bg-warning-50 text-warning-700 hover:bg-warning-100 transition-colors"
              >
                <PowerOff className="w-3.5 h-3.5" />
                Deactivate
              </button>
            )}
          </RoleGuard>
          <RoleGuard module="Repositories" action="delete">
            <button
              onClick={() => setConfirmDelete(row)}
              className="p-1.5 rounded-lg text-slate-400 hover:text-danger-600 hover:bg-danger-50 transition-colors"
              title="Delete"
            >
              <Trash2 className="w-3.5 h-3.5" />
            </button>
          </RoleGuard>
        </div>
      ),
    },
  ]

  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        title="Repositories"
        subtitle="Manage Gitea repository connections and synchronization"
        breadcrumbs={[{ label: 'Dashboard', href: '/dashboard' }, { label: 'Repositories' }]}
        actions={
          <RoleGuard module="Repositories" action="create">
            <button onClick={() => setRepoFormOpen(true)} className="btn-primary">
              <Plus className="w-4 h-4" />
              Register Repository
            </button>
          </RoleGuard>
        }
      />

      {/* Filters */}
      <div className="filter-bar">
        <div className="relative flex-1 min-w-[200px] max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
          <input
            type="text"
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1) }}
            placeholder="Search repositories..."
            className="input pl-9"
          />
        </div>

        {canFilterByProject && (
          <select
            value={projectId}
            onChange={(e) => { setProjectId(e.target.value); setPage(1) }}
            className="input w-auto"
          >
            <option value="">All Projects</option>
            {(projects?.items ?? []).map((p) => (
              <option key={p.id} value={p.id}>{p.name}</option>
            ))}
          </select>
        )}

        <button
          onClick={() => queryClient.invalidateQueries({ queryKey: ['repositories'] })}
          className="btn-secondary"
        >
          <RefreshCw className="w-4 h-4" />
          Refresh
        </button>
      </div>

      {/* Table */}
      <DataTable
        columns={columns}
        data={data?.items ?? []}
        pagination={data?.pagination}
        onPageChange={setPage}
        onPageSizeChange={(size) => { setPageSize(size); setPage(1) }}
        isLoading={isLoading}
        keyExtractor={(r) => r.id}
        emptyMessage="No repositories found"
      />

      {/* Deactivate Confirm */}
      <ConfirmDialog
        open={Boolean(confirmDeactivate)}
        onOpenChange={(open) => !open && setConfirmDeactivate(null)}
        title="Deactivate Repository"
        message={`Deactivate "${confirmDeactivate?.name}"? Webhooks will stop processing events for this repository.`}
        confirmLabel="Deactivate"
        variant="warning"
        onConfirm={() => confirmDeactivate && deactivateMutation.mutate(confirmDeactivate.id)}
        isLoading={deactivateMutation.isPending}
      />

      {/* Delete Confirm */}
      <ConfirmDialog
        open={Boolean(confirmDelete)}
        onOpenChange={(open) => !open && setConfirmDelete(null)}
        title="Delete Repository"
        message={`Permanently delete "${confirmDelete?.name}"? This cannot be undone and will remove all associated data.`}
        confirmLabel="Delete"
        variant="destructive"
        onConfirm={() => confirmDelete && deleteMutation.mutate(confirmDelete.id)}
        isLoading={deleteMutation.isPending}
      />

      {/* Register Repository Form */}
      <RepositoryForm
        open={repoFormOpen}
        onOpenChange={setRepoFormOpen}
        onSubmit={async (data) => { await registerMutation.mutateAsync(data) }}
        isLoading={registerMutation.isPending}
      />
    </div>
  )
}
