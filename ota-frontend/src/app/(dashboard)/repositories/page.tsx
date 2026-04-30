'use client'

import * as React from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, Search, RefreshCw, Loader2, PowerOff, Eye, Trash2, Pencil, MoreVertical } from 'lucide-react'
import { useRouter } from 'next/navigation'
import * as DropdownMenu from '@radix-ui/react-dropdown-menu'
import { repositoryService } from '@/services/repository.service'
import { projectService } from '@/services/project.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { DataTable, Column } from '@/components/ui/DataTable'
import { StatusBadge } from '@/components/ui/Badge'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
import { RepositoryForm, RegisterRepositoryPayload } from '@/components/forms/RepositoryForm'
import { EditRepositoryForm, UpdateRepositoryPayload } from '@/components/forms/EditRepositoryForm'
import { RoleGuard } from '@/components/role-access/RoleGuard'
import { useToast } from '@/components/ui/ToastProvider'
import { Repository, UserRole } from '@/types'
import { formatRelativeTime } from '@/utils/formatters'
import { useAuth } from '@/hooks/useAuth'

export default function RepositoriesPage() {
  const queryClient = useQueryClient()
  const router = useRouter()
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
  const [editTarget, setEditTarget] = React.useState<Repository | null>(null)

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

  const editMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateRepositoryPayload }) =>
      repositoryService.updateRepository(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['repositories'] })
      toast({ title: 'Repository updated', variant: 'success' })
      setEditTarget(null)
    },
    onError: (err: any) => {
      const msg = err?.response?.data?.message ?? 'Could not update repository'
      toast({ title: 'Update failed', description: msg, variant: 'error' })
    },
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
      key: 'client',
      header: 'Client',
      cell: (row) => (
        <span className="text-sm text-slate-700">
          {row.clientName ?? '—'}
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
        <DropdownMenu.Root>
          <DropdownMenu.Trigger asChild>
            <button
              aria-label="Row actions"
              className="p-1.5 rounded-lg text-slate-500 hover:text-primary-700 hover:bg-slate-100 transition-colors focus:outline-none focus:ring-2 focus:ring-accent-500"
            >
              <MoreVertical className="w-4 h-4" />
            </button>
          </DropdownMenu.Trigger>
          <DropdownMenu.Portal>
            <DropdownMenu.Content
              align="end"
              sideOffset={4}
              className="bg-white rounded-xl border border-slate-200 shadow-lg py-1 z-50 min-w-[180px] animate-fade-in"
            >
              <DropdownMenu.Item
                onSelect={() => router.push(`/repositories/${row.id}`)}
                className="flex items-center gap-2 px-3 py-2 text-sm text-slate-700 cursor-pointer outline-none hover:bg-slate-50 focus:bg-slate-50"
              >
                <Eye className="w-4 h-4 text-slate-500" />
                View
              </DropdownMenu.Item>

              <RoleGuard module="Repositories" action="update">
                <DropdownMenu.Item
                  onSelect={() => setEditTarget(row)}
                  className="flex items-center gap-2 px-3 py-2 text-sm text-slate-700 cursor-pointer outline-none hover:bg-slate-50 focus:bg-slate-50"
                >
                  <Pencil className="w-4 h-4 text-primary-600" />
                  Edit
                </DropdownMenu.Item>
              </RoleGuard>

              <RoleGuard module="Repositories" action="execute">
                <DropdownMenu.Item
                  disabled={syncingId === row.id}
                  onSelect={() => handleSync(row.id)}
                  className="flex items-center gap-2 px-3 py-2 text-sm text-slate-700 cursor-pointer outline-none hover:bg-slate-50 focus:bg-slate-50 data-[disabled]:opacity-50 data-[disabled]:cursor-not-allowed"
                >
                  {syncingId === row.id ? (
                    <Loader2 className="w-4 h-4 animate-spin text-accent-600" />
                  ) : (
                    <RefreshCw className="w-4 h-4 text-accent-600" />
                  )}
                  Sync
                </DropdownMenu.Item>
              </RoleGuard>

              {row.isActive && (
                <RoleGuard module="Repositories" action="update">
                  <DropdownMenu.Item
                    onSelect={() => setConfirmDeactivate(row)}
                    className="flex items-center gap-2 px-3 py-2 text-sm text-warning-700 cursor-pointer outline-none hover:bg-warning-50 focus:bg-warning-50"
                  >
                    <PowerOff className="w-4 h-4" />
                    Deactivate
                  </DropdownMenu.Item>
                </RoleGuard>
              )}

              <RoleGuard module="Repositories" action="delete">
                <DropdownMenu.Separator className="my-1 h-px bg-slate-100" />
                <DropdownMenu.Item
                  onSelect={() => setConfirmDelete(row)}
                  className="flex items-center gap-2 px-3 py-2 text-sm text-danger-600 cursor-pointer outline-none hover:bg-danger-50 focus:bg-danger-50"
                >
                  <Trash2 className="w-4 h-4" />
                  Delete
                </DropdownMenu.Item>
              </RoleGuard>
            </DropdownMenu.Content>
          </DropdownMenu.Portal>
        </DropdownMenu.Root>
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

      {/* Edit Repository Form */}
      {editTarget && (
        <EditRepositoryForm
          open={Boolean(editTarget)}
          onOpenChange={(open) => !open && setEditTarget(null)}
          repository={editTarget}
          onSubmit={async (data) => { await editMutation.mutateAsync({ id: editTarget.id, data }) }}
          isLoading={editMutation.isPending}
        />
      )}
    </div>
  )
}
