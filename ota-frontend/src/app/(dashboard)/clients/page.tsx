'use client'

import * as React from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Search, RefreshCw, Pencil, Trash2, Plus, CheckCircle, XCircle } from 'lucide-react'
import { clientService } from '@/services/client.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { DataTable, Column } from '@/components/ui/DataTable'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
import { CreateClientForm } from '@/components/forms/CreateClientForm'
import { EditClientForm } from '@/components/forms/EditClientForm'
import { RoleGuard } from '@/components/role-access/RoleGuard'
import { useToast } from '@/components/ui/ToastProvider'
import { Client, UpdateClientRequest } from '@/types'
import { formatRelativeTime } from '@/utils/formatters'

export default function ClientsPage() {
  const queryClient = useQueryClient()
  const { toast }   = useToast()

  const [search, setSearch]             = React.useState('')
  const [page, setPage]                 = React.useState(1)
  const [pageSize, setPageSize]         = React.useState(25)
  const [createOpen, setCreateOpen]     = React.useState(false)
  const [editTarget, setEditTarget]     = React.useState<Client | null>(null)
  const [deleteTarget, setDeleteTarget] = React.useState<Client | null>(null)

  const { data, isLoading } = useQuery({
    queryKey: ['clients', { search, page, pageSize }],
    queryFn: () => clientService.getClients({ search, page, pageSize }),
  })

  const createMutation = useMutation({
    mutationFn: clientService.createClient,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['clients'] })
      toast({ title: 'Client created successfully', variant: 'success' })
      setCreateOpen(false)
    },
    onError: (error: any) => {
      const msg = error?.response?.data?.message || 'Failed to create client'
      toast({ title: msg, variant: 'error' })
    },
  })

  const editMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateClientRequest }) =>
      clientService.updateClient(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['clients'] })
      toast({ title: 'Client updated successfully', variant: 'success' })
      setEditTarget(null)
    },
    onError: (error: any) => {
      const msg = error?.response?.data?.message || 'Failed to update client'
      toast({ title: msg, variant: 'error' })
    },
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => clientService.deleteClient(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['clients'] })
      toast({ title: 'Client deleted', variant: 'success' })
      setDeleteTarget(null)
    },
    onError: () => toast({ title: 'Failed to delete client', variant: 'error' }),
  })

  const columns: Column<Client>[] = [
    {
      key: 'name',
      header: 'Client Name',
      cell: (row) => (
        <div>
          <p className="font-semibold text-primary-900 text-sm">{row.name}</p>
          <p className="text-xs text-slate-400 font-mono mt-0.5">{row.code}</p>
        </div>
      ),
    },
    {
      key: 'contactEmail',
      header: 'Contact',
      cell: (row) => (
        <div>
          {row.contactEmail ? (
            <a href={`mailto:${row.contactEmail}`} className="text-sm text-accent-600 hover:underline">
              {row.contactEmail}
            </a>
          ) : (
            <span className="text-sm text-slate-400">—</span>
          )}
          {row.contactPhone && (
            <p className="text-xs text-slate-500 mt-0.5">{row.contactPhone}</p>
          )}
        </div>
      ),
    },
    {
      key: 'isActive',
      header: 'Status',
      cell: (row) => (
        <span className={`inline-flex items-center gap-1.5 text-xs font-medium px-2.5 py-1 rounded-full ${
          row.isActive
            ? 'bg-success-50 text-success-700 border border-success-200'
            : 'bg-slate-100 text-slate-500 border border-slate-200'
        }`}>
          {row.isActive
            ? <CheckCircle className="w-3.5 h-3.5" />
            : <XCircle className="w-3.5 h-3.5" />}
          {row.isActive ? 'Active' : 'Inactive'}
        </span>
      ),
    },
    {
      key: 'createdAt',
      header: 'Created',
      cell: (row) => (
        <span className="text-xs text-slate-500">{formatRelativeTime(row.createdAt)}</span>
      ),
    },
    {
      key: 'actions',
      header: 'Actions',
      cell: (row) => (
        <div className="flex items-center gap-1">
          <RoleGuard module="Clients" action="update">
            <button
              onClick={() => setEditTarget(row)}
              className="p-1.5 rounded-lg text-slate-400 hover:text-primary-600 hover:bg-primary-50 transition-colors"
              title="Edit"
            >
              <Pencil className="w-4 h-4" />
            </button>
          </RoleGuard>
          <RoleGuard module="Clients" action="delete">
            <button
              onClick={() => setDeleteTarget(row)}
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
        title="Clients"
        subtitle="Manage customer organisations registered on the platform"
        breadcrumbs={[{ label: 'Dashboard', href: '/dashboard' }, { label: 'Clients' }]}
        actions={
          <RoleGuard module="Clients" action="create">
            <button onClick={() => setCreateOpen(true)} className="btn-primary">
              <Plus className="w-4 h-4" />
              New Client
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
            placeholder="Search by name, code or e-mail…"
            className="input pl-9"
          />
        </div>
        <button
          onClick={() => queryClient.invalidateQueries({ queryKey: ['clients'] })}
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
        emptyMessage="No clients found"
      />

      {/* Create Modal */}
      <CreateClientForm
        open={createOpen}
        onOpenChange={setCreateOpen}
        onSubmit={async (data) => { await createMutation.mutateAsync(data) }}
        isLoading={createMutation.isPending}
      />

      {/* Edit Modal */}
      {editTarget && (
        <EditClientForm
          open={Boolean(editTarget)}
          onOpenChange={(open) => !open && setEditTarget(null)}
          client={editTarget}
          onSubmit={async (data) => { await editMutation.mutateAsync({ id: editTarget.id, data }) }}
          isLoading={editMutation.isPending}
        />
      )}

      {/* Delete Confirm */}
      <ConfirmDialog
        open={Boolean(deleteTarget)}
        onOpenChange={(open) => !open && setDeleteTarget(null)}
        title="Delete Client"
        message={`Permanently delete client "${deleteTarget?.name}"? This cannot be undone.`}
        confirmLabel="Delete"
        variant="destructive"
        onConfirm={() => deleteTarget && deleteMutation.mutate(deleteTarget.id)}
        isLoading={deleteMutation.isPending}
      />
    </div>
  )
}
