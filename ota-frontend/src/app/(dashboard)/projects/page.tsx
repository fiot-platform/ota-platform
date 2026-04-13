'use client'

import * as React from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, Search, RefreshCw, Pencil, Power, PowerOff, Eye } from 'lucide-react'
import Link from 'next/link'
import { projectService } from '@/services/project.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { DataTable, Column } from '@/components/ui/DataTable'
import { StatusBadge } from '@/components/ui/Badge'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
import { ProjectForm } from '@/components/forms/ProjectForm'
import { RoleGuard } from '@/components/role-access/RoleGuard'
import { useToast } from '@/components/ui/ToastProvider'
import { Project, CreateProjectRequest, UpdateProjectRequest } from '@/types'
import { formatDate } from '@/utils/formatters'

export default function ProjectsPage() {
  const queryClient = useQueryClient()
  const { toast } = useToast()

  const [search, setSearch] = React.useState('')
  const [page, setPage] = React.useState(1)
  const [pageSize, setPageSize] = React.useState(25)
  const [formOpen, setFormOpen] = React.useState(false)
  const [editProject, setEditProject] = React.useState<Project | undefined>()
  const [confirmDeactivate, setConfirmDeactivate] = React.useState<Project | null>(null)
  const [confirmActivate, setConfirmActivate] = React.useState<Project | null>(null)

  const { data, isLoading } = useQuery({
    queryKey: ['projects', { search, page, pageSize }],
    queryFn: () => projectService.getProjects({ search, page, pageSize }),
  })

  const createMutation = useMutation({
    mutationFn: (d: CreateProjectRequest) => projectService.createProject(d),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects'] })
      toast({ title: 'Project created', variant: 'success' })
      setFormOpen(false)
    },
    onError: () => toast({ title: 'Failed to create project', variant: 'error' }),
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateProjectRequest }) =>
      projectService.updateProject(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects'] })
      toast({ title: 'Project updated', variant: 'success' })
      setEditProject(undefined)
      setFormOpen(false)
    },
    onError: () => toast({ title: 'Failed to update project', variant: 'error' }),
  })

  const deactivateMutation = useMutation({
    mutationFn: (id: string) => projectService.deactivateProject(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects'] })
      toast({ title: 'Project deactivated', variant: 'success' })
      setConfirmDeactivate(null)
    },
    onError: () => toast({ title: 'Failed to deactivate project', variant: 'error' }),
  })

  const activateMutation = useMutation({
    mutationFn: (id: string) => projectService.activateProject(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects'] })
      toast({ title: 'Project activated', variant: 'success' })
      setConfirmActivate(null)
    },
    onError: () => toast({ title: 'Failed to activate project', variant: 'error' }),
  })

  const columns: Column<Project>[] = [
    {
      key: 'name',
      header: 'Project Name',
      cell: (row) => (
        <div>
          <Link
            href={`/projects/${row.id}`}
            className="font-semibold text-accent-600 hover:text-accent-700 transition-colors"
          >
            {row.name}
          </Link>
          {row.description && (
            <p className="text-xs text-slate-500 mt-0.5 truncate max-w-[200px]">{row.description}</p>
          )}
        </div>
      ),
    },
    {
      key: 'customerName',
      header: 'Customer',
      cell: (row) => (
        <div>
          <p className="font-medium text-sm">{row.customerName}</p>
          <p className="text-xs text-slate-400">{row.customerId}</p>
        </div>
      ),
    },
    {
      key: 'businessUnit',
      header: 'Business Unit',
      cell: (row) => <span className="text-sm text-slate-600">{row.businessUnit ?? '—'}</span>,
    },
    {
      key: 'status',
      header: 'Status',
      cell: (row) => <StatusBadge status={row.isActive ? 'Active' : 'Inactive'} />,
    },
    {
      key: 'tags',
      header: 'Tags',
      cell: (row) => (
        <div className="flex flex-wrap gap-1">
          {(row.tags ?? []).slice(0, 3).map((tag) => (
            <span key={tag} className="px-2 py-0.5 bg-slate-100 text-slate-600 rounded text-xs">
              {tag}
            </span>
          ))}
          {(row.tags?.length ?? 0) > 3 && (
            <span className="text-xs text-slate-400">+{(row.tags?.length ?? 0) - 3}</span>
          )}
        </div>
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
            href={`/projects/${row.id}`}
            className="p-1.5 rounded-lg text-slate-400 hover:text-accent-600 hover:bg-accent-50 transition-colors"
            title="View"
          >
            <Eye className="w-4 h-4" />
          </Link>
          <RoleGuard module="Projects" action="update">
            <button
              onClick={() => { setEditProject(row); setFormOpen(true) }}
              className="p-1.5 rounded-lg text-slate-400 hover:text-accent-600 hover:bg-accent-50 transition-colors"
              title="Edit"
            >
              <Pencil className="w-4 h-4" />
            </button>
          </RoleGuard>
          <RoleGuard module="Projects" action="update">
            {row.isActive ? (
              <button
                onClick={() => setConfirmDeactivate(row)}
                className="p-1.5 rounded-lg text-slate-400 hover:text-warning-600 hover:bg-warning-50 transition-colors"
                title="Deactivate"
              >
                <PowerOff className="w-4 h-4" />
              </button>
            ) : (
              <button
                onClick={() => setConfirmActivate(row)}
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
        title="Projects"
        subtitle="Manage your firmware projects and customer configurations"
        breadcrumbs={[{ label: 'Dashboard', href: '/dashboard' }, { label: 'Projects' }]}
        actions={
          <RoleGuard module="Projects" action="create">
            <button
              onClick={() => { setEditProject(undefined); setFormOpen(true) }}
              className="btn-primary"
            >
              <Plus className="w-4 h-4" />
              New Project
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
            placeholder="Search projects..."
            className="input pl-9"
          />
        </div>
        <button
          onClick={() => queryClient.invalidateQueries({ queryKey: ['projects'] })}
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
        keyExtractor={(row) => row.id}
        emptyMessage="No projects found. Create your first project to get started."
      />

      {/* Create / Edit Form */}
      <ProjectForm
        open={formOpen}
        onOpenChange={setFormOpen}
        project={editProject}
        onSubmit={async (data) => {
          if (editProject) {
            await updateMutation.mutateAsync({ id: editProject.id, data: data as UpdateProjectRequest })
          } else {
            await createMutation.mutateAsync(data as CreateProjectRequest)
          }
        }}
        isLoading={createMutation.isPending || updateMutation.isPending}
      />

      {/* Deactivate Confirm */}
      <ConfirmDialog
        open={Boolean(confirmDeactivate)}
        onOpenChange={(open) => !open && setConfirmDeactivate(null)}
        title="Deactivate Project"
        message={`Are you sure you want to deactivate "${confirmDeactivate?.name}"? This will prevent new firmware deployments for this project.`}
        confirmLabel="Deactivate"
        variant="warning"
        onConfirm={() => confirmDeactivate && deactivateMutation.mutate(confirmDeactivate.id)}
        isLoading={deactivateMutation.isPending}
      />

      {/* Activate Confirm */}
      <ConfirmDialog
        open={Boolean(confirmActivate)}
        onOpenChange={(open) => !open && setConfirmActivate(null)}
        title="Activate Project"
        message={`Activate "${confirmActivate?.name}"? This will allow firmware deployments for this project.`}
        confirmLabel="Activate"
        variant="default"
        onConfirm={() => confirmActivate && activateMutation.mutate(confirmActivate.id)}
        isLoading={activateMutation.isPending}
      />
    </div>
  )
}
