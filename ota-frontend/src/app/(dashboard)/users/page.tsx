'use client'

import * as React from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, Search, RefreshCw, Power, PowerOff, Shield } from 'lucide-react'
import { userService } from '@/services/user.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { DataTable, Column } from '@/components/ui/DataTable'
import { StatusBadge, RoleBadge } from '@/components/ui/Badge'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
import { CreateUserForm } from '@/components/forms/CreateUserForm'
import { RoleGuard } from '@/components/role-access/RoleGuard'
import { useToast } from '@/components/ui/ToastProvider'
import { User, UserRole, CreateUserRequest } from '@/types'
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
    onError: () => toast({ title: 'Failed to create user', variant: 'error' }),
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
          <RoleGuard module="Users" action="update">
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
    </div>
  )
}
