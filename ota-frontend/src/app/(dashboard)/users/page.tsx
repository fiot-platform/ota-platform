'use client'

import * as React from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, Search, RefreshCw, Power, PowerOff, FolderKanban, Trash2, Eye, CheckCircle2, Circle } from 'lucide-react'
import { userService } from '@/services/user.service'
import { projectService } from '@/services/project.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { DataTable, Column } from '@/components/ui/DataTable'
import { StatusBadge, RoleBadge } from '@/components/ui/Badge'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
import { CreateUserForm } from '@/components/forms/CreateUserForm'
import { RoleGuard } from '@/components/role-access/RoleGuard'
import { useToast } from '@/components/ui/ToastProvider'
import Link from 'next/link'
import { User, UserRole, CreateUserRequest, Project } from '@/types'
import { formatDate, formatRelativeTime } from '@/utils/formatters'

export default function UsersPage() {
  const queryClient = useQueryClient()
  const { toast } = useToast()

  const [search, setSearch] = React.useState('')
  const [roleFilter, setRoleFilter] = React.useState('')
  const [statusFilter, setStatusFilter] = React.useState('')
  const [page, setPage] = React.useState(1)
  const [pageSize, setPageSize] = React.useState(25)
  const [createOpen, setCreateOpen] = React.useState(false)
  const [deactivateTarget, setDeactivateTarget] = React.useState<User | null>(null)
  const [activateTarget, setActivateTarget] = React.useState<User | null>(null)
  const [assignProjectsTarget, setAssignProjectsTarget] = React.useState<User | null>(null)
  const [selectedProjectIds, setSelectedProjectIds] = React.useState<string[]>([])
  const [confirmDelete, setConfirmDelete] = React.useState<User | null>(null)

  const { data, isLoading } = useQuery({
    queryKey: ['users', { search, role: roleFilter, isActive: statusFilter, page, pageSize }],
    queryFn: () => userService.getUsers({
      search,
      role: (roleFilter as UserRole) || undefined,
      isActive: statusFilter === '' ? undefined : statusFilter === 'active',
      page,
      pageSize,
    }),
  })

  const createMutation = useMutation({
    mutationFn: (d: CreateUserRequest) => userService.createUser(d),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] })
      toast({ title: 'User created successfully', variant: 'success' })
      setCreateOpen(false)
    },
    onError: (e: any) => toast({ title: 'Failed to create user', description: e?.response?.data?.message, variant: 'error' }),
  })

  const deactivateMutation = useMutation({
    mutationFn: (id: string) => userService.deactivateUser(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] })
      toast({ title: 'User deactivated', variant: 'warning' })
      setDeactivateTarget(null)
    },
    onError: () => toast({ title: 'Failed to deactivate user', variant: 'error' }),
  })

  const activateMutation = useMutation({
    mutationFn: (id: string) => userService.activateUser(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] })
      toast({ title: 'User activated', variant: 'success' })
      setActivateTarget(null)
    },
    onError: () => toast({ title: 'Failed to activate user', variant: 'error' }),
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => userService.deleteUser(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] })
      toast({ title: 'User deleted', variant: 'success' })
      setConfirmDelete(null)
    },
    onError: (e: any) => toast({ title: 'Failed to delete user', description: e?.response?.data?.message, variant: 'error' }),
  })

  const assignProjectsMutation = useMutation({
    mutationFn: ({ id, projectIds }: { id: string; projectIds: string[] }) =>
      userService.assignProjects(id, projectIds),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] })
      toast({ title: 'Projects assigned successfully', variant: 'success' })
      setAssignProjectsTarget(null)
    },
    onError: (e: any) =>
      toast({ title: 'Failed to assign projects', description: e?.response?.data?.message, variant: 'error' }),
  })

  const { data: projectsData } = useQuery({
    queryKey: ['projects-all'],
    queryFn: () => projectService.getProjects({ pageSize: 200 }),
    enabled: assignProjectsTarget !== null,
  })

  const columns: Column<User>[] = [
    {
      key: 'name',
      header: 'User',
      cell: (row) => (
        <div className="flex items-center gap-3">
          <div className="w-8 h-8 bg-accent-100 rounded-full flex items-center justify-center flex-shrink-0">
            <span className="text-accent-700 text-sm font-semibold">
              {row.name?.charAt(0).toUpperCase() ?? 'U'}
            </span>
          </div>
          <div>
            <p className="font-semibold text-primary-900 text-sm">{row.name}</p>
            <p className="text-xs text-slate-500">{row.email}</p>
          </div>
        </div>
      ),
    },
    {
      key: 'role',
      header: 'Role',
      cell: (row) => <RoleBadge role={row.role} />,
    },
    {
      key: 'customer',
      header: 'Customer',
      cell: (row) => (
        <span className="text-sm text-slate-600">{row.customerName ?? row.customerId ?? '—'}</span>
      ),
    },
    {
      key: 'status',
      header: 'Status',
      cell: (row) => <StatusBadge status={row.isActive ? 'Active' : 'Inactive'} dot />,
    },
    {
      key: 'lastLogin',
      header: 'Last Login',
      cell: (row) => (
        <span className="text-sm text-slate-500">
          {row.lastLoginAt ? formatRelativeTime(row.lastLoginAt) : 'Never'}
        </span>
      ),
    },
    {
      key: 'createdAt',
      header: 'Created',
      cell: (row) => <span className="text-sm text-slate-500">{formatDate(row.createdAt)}</span>,
    },
    {
      key: 'actions',
      header: 'Actions',
      cell: (row) => (
        <div className="flex items-center gap-1">
          <Link
            href={`/users/${row.id}`}
            className="p-1.5 rounded-lg text-slate-400 hover:text-accent-600 hover:bg-accent-50 transition-colors"
            title="View"
          >
            <Eye className="w-4 h-4" />
          </Link>
          <RoleGuard module="Users" action="update">
            <button
              onClick={() => {
                setAssignProjectsTarget(row)
                setSelectedProjectIds(row.projectScope ?? [])
              }}
              className="p-1.5 rounded-lg text-slate-400 hover:text-accent-600 hover:bg-accent-50 transition-colors"
              title="Assign Projects"
            >
              <FolderKanban className="w-4 h-4" />
            </button>
            {row.isActive ? (
              <button
                onClick={() => setDeactivateTarget(row)}
                className="p-1.5 rounded-lg text-slate-400 hover:text-warning-600 hover:bg-warning-50 transition-colors"
                title="Deactivate"
              >
                <PowerOff className="w-4 h-4" />
              </button>
            ) : (
              <button
                onClick={() => setActivateTarget(row)}
                className="p-1.5 rounded-lg text-slate-400 hover:text-success-600 hover:bg-success-50 transition-colors"
                title="Activate"
              >
                <Power className="w-4 h-4" />
              </button>
            )}
          </RoleGuard>
          <RoleGuard module="Users" action="delete">
            <button
              onClick={() => setConfirmDelete(row)}
              className="p-1.5 rounded-lg text-slate-400 hover:text-danger-600 hover:bg-danger-50 transition-colors"
              title="Delete"
            >
              <Trash2 className="w-4 h-4" />
            </button>
          </RoleGuard>
        </div>
      ),
    },
  ]

  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        title="User Management"
        subtitle="Manage platform users, roles, and access control"
        breadcrumbs={[{ label: 'Dashboard', href: '/dashboard' }, { label: 'Users' }]}
        actions={
          <RoleGuard module="Users" action="create">
            <button onClick={() => setCreateOpen(true)} className="btn-primary">
              <Plus className="w-4 h-4" />
              Invite User
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
            placeholder="Search by name or email..."
            className="input pl-9"
          />
        </div>

        <select
          value={roleFilter}
          onChange={(e) => { setRoleFilter(e.target.value); setPage(1) }}
          className="input w-auto"
        >
          <option value="">All Roles</option>
          {Object.values(UserRole).filter((r) => r !== UserRole.Device).map((r) => (
            <option key={r} value={r}>{r.replace(/([A-Z])/g, ' $1').trim()}</option>
          ))}
        </select>

        <select
          value={statusFilter}
          onChange={(e) => { setStatusFilter(e.target.value); setPage(1) }}
          className="input w-auto"
        >
          <option value="">All Statuses</option>
          <option value="active">Active</option>
          <option value="inactive">Inactive</option>
        </select>

        <button
          onClick={() => queryClient.invalidateQueries({ queryKey: ['users'] })}
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
        emptyMessage="No users found"
      />

      {/* Create User */}
      <CreateUserForm
        open={createOpen}
        onOpenChange={setCreateOpen}
        onSubmit={async (d) => {
          await createMutation.mutateAsync(d as CreateUserRequest)
        }}
        isLoading={createMutation.isPending}
      />

      {/* Deactivate Confirm */}
      <ConfirmDialog
        open={Boolean(deactivateTarget)}
        onOpenChange={(open) => !open && setDeactivateTarget(null)}
        title="Deactivate User"
        message={`Deactivate "${deactivateTarget?.name}" (${deactivateTarget?.email})? They will not be able to log in.`}
        confirmLabel="Deactivate"
        variant="warning"
        onConfirm={() => deactivateTarget && deactivateMutation.mutate(deactivateTarget.id)}
        isLoading={deactivateMutation.isPending}
      />

      {/* Activate Confirm */}
      <ConfirmDialog
        open={Boolean(activateTarget)}
        onOpenChange={(open) => !open && setActivateTarget(null)}
        title="Activate User"
        message={`Activate "${activateTarget?.name}"? They will be able to log in and access the platform.`}
        confirmLabel="Activate"
        variant="default"
        onConfirm={() => activateTarget && activateMutation.mutate(activateTarget.id)}
        isLoading={activateMutation.isPending}
      />

      {/* Delete Confirm */}
      <ConfirmDialog
        open={Boolean(confirmDelete)}
        onOpenChange={(open) => !open && setConfirmDelete(null)}
        title="Delete User"
        message={`Permanently delete "${confirmDelete?.name}" (${confirmDelete?.email})? This cannot be undone.`}
        confirmLabel="Delete"
        variant="destructive"
        onConfirm={() => confirmDelete && deleteMutation.mutate(confirmDelete.id)}
        isLoading={deleteMutation.isPending}
      />

      {/* Assign Projects Dialog */}
      {assignProjectsTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm p-4">
          <div className="bg-white rounded-2xl shadow-2xl w-full max-w-lg overflow-hidden">

            {/* Header */}
            <div className="bg-white border-b border-slate-100 px-6 py-5">
              <div className="flex items-center gap-3">
                <div className="w-9 h-9 bg-accent-50 border border-accent-100 rounded-xl flex items-center justify-center flex-shrink-0">
                  <FolderKanban className="w-5 h-5 text-accent-600" />
                </div>
                <div>
                  <h2 className="text-base font-bold text-primary-900">Assign Projects</h2>
                  <p className="text-xs text-slate-500 mt-0.5">
                    Scoping access for <span className="font-semibold text-primary-800">{assignProjectsTarget.name}</span>
                  </p>
                </div>
              </div>
            </div>

            <div className="p-6 space-y-4">
              {/* Info banner */}
              <div className="flex items-start gap-2.5 bg-accent-50 border border-accent-100 rounded-xl px-4 py-3">
                <FolderKanban className="w-4 h-4 text-accent-500 mt-0.5 flex-shrink-0" />
                <p className="text-xs text-accent-700 leading-relaxed">
                  Select projects this user can access. <span className="font-semibold text-danger-600">No projects selected = no access to projects, devices or firmware</span> (SuperAdmin / PlatformAdmin are unrestricted).
                </p>
              </div>

              {/* Project list */}
              <div className="space-y-2 max-h-64 overflow-y-auto pr-1">
                {(projectsData?.items ?? []).length === 0 ? (
                  <div className="flex flex-col items-center justify-center py-10 text-center">
                    <FolderKanban className="w-8 h-8 text-slate-300 mb-2" />
                    <p className="text-sm text-slate-400">No projects available</p>
                  </div>
                ) : (
                  (projectsData?.items ?? []).map((project: Project) => {
                    const isChecked = selectedProjectIds.includes(project.id)
                    return (
                      <label
                        key={project.id}
                        className={`flex items-center gap-3 p-3.5 rounded-xl border cursor-pointer transition-all select-none ${
                          isChecked
                            ? 'bg-accent-50 border-accent-400 shadow-sm'
                            : 'bg-white border-slate-200 hover:border-accent-200 hover:bg-accent-50/30'
                        }`}
                      >
                        {/* Hidden native checkbox for accessibility */}
                        <input
                          type="checkbox"
                          checked={isChecked}
                          onChange={(e) => {
                            setSelectedProjectIds((prev) =>
                              e.target.checked
                                ? [...prev, project.id]
                                : prev.filter((pid) => pid !== project.id)
                            )
                          }}
                          className="sr-only"
                        />

                        {/* Custom checkbox icon */}
                        <span className="flex-shrink-0">
                          {isChecked
                            ? <CheckCircle2 className="w-5 h-5 text-accent-600" />
                            : <Circle className="w-5 h-5 text-slate-300" />
                          }
                        </span>

                        <div className="min-w-0 flex-1">
                          <p className={`text-sm font-semibold truncate ${isChecked ? 'text-accent-800' : 'text-primary-800'}`}>
                            {project.name}
                          </p>
                          {project.description && (
                            <p className="text-xs text-slate-500 truncate mt-0.5">{project.description}</p>
                          )}
                        </div>

                        {isChecked && (
                          <span className="text-xs font-semibold bg-accent-500 text-white px-2.5 py-0.5 rounded-full flex-shrink-0">
                            Selected
                          </span>
                        )}
                      </label>
                    )
                  })
                )}
              </div>

              {/* Footer */}
              <div className="flex items-center justify-between pt-1 border-t border-slate-100">
                <span className={`text-xs font-medium px-2.5 py-1 rounded-full ${
                  selectedProjectIds.length > 0
                    ? 'bg-accent-100 text-accent-700'
                    : 'bg-slate-100 text-slate-500'
                }`}>
                  {selectedProjectIds.length === 0
                    ? 'No projects assigned (no access)'
                    : `${selectedProjectIds.length} project${selectedProjectIds.length !== 1 ? 's' : ''} selected`}
                </span>
                <div className="flex gap-2">
                  <button
                    onClick={() => setAssignProjectsTarget(null)}
                    className="btn-secondary"
                    disabled={assignProjectsMutation.isPending}
                  >
                    Cancel
                  </button>
                  <button
                    onClick={() =>
                      assignProjectsMutation.mutate({
                        id: assignProjectsTarget.id,
                        projectIds: selectedProjectIds,
                      })
                    }
                    className="btn-primary"
                    disabled={assignProjectsMutation.isPending}
                  >
                    {assignProjectsMutation.isPending ? 'Saving…' : 'Save Changes'}
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
