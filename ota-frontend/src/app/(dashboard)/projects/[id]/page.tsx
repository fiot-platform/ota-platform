'use client'

import * as React from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useParams, useRouter } from 'next/navigation'
import {
  ArrowLeft,
  GitBranch,
  Cpu,
  Calendar,
  Building,
  Hash,
  Globe,
  Tag,
  Plus,
} from 'lucide-react'
import { projectService } from '@/services/project.service'
import { repositoryService } from '@/services/repository.service'
import { firmwareService } from '@/services/firmware.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { DataTable, Column } from '@/components/ui/DataTable'
import { StatusBadge } from '@/components/ui/Badge'
import { RepositoryForm, RegisterRepositoryPayload } from '@/components/forms/RepositoryForm'
import { RoleGuard } from '@/components/role-access/RoleGuard'
import { useToast } from '@/components/ui/ToastProvider'
import { Repository } from '@/types'
import { formatDate, formatRelativeTime } from '@/utils/formatters'

function InfoRow({ icon, label, value }: { icon: React.ReactNode; label: string; value: React.ReactNode }) {
  return (
    <div className="flex items-start gap-3 py-3 border-b border-slate-50 last:border-0">
      <span className="text-slate-400 mt-0.5 flex-shrink-0">{icon}</span>
      <div className="flex-1 min-w-0">
        <p className="text-xs font-semibold text-slate-400 uppercase tracking-wide mb-0.5">{label}</p>
        <div className="text-sm text-primary-800 font-medium">{value}</div>
      </div>
    </div>
  )
}

export default function ProjectDetailPage() {
  const { id } = useParams<{ id: string }>()
  const router = useRouter()
  const queryClient = useQueryClient()
  const { toast } = useToast()
  const [repoFormOpen, setRepoFormOpen] = React.useState(false)

  const { data: project, isLoading: projectLoading } = useQuery({
    queryKey: ['project', id],
    queryFn: () => projectService.getProjectById(id),
  })

  const { data: repositories, isLoading: reposLoading } = useQuery({
    queryKey: ['repositories', { projectId: id }],
    queryFn: () => repositoryService.getRepositories({ projectId: id }),
    staleTime: 0,
  })

  const { data: firmwareList, isLoading: firmwareLoading } = useQuery({
    queryKey: ['firmware', { projectId: id, pageSize: 5 }],
    queryFn: () => firmwareService.getFirmwareList({ projectId: id, pageSize: 5 }),
    staleTime: 0,
  })

  const registerRepoMutation = useMutation({
    mutationFn: (data: RegisterRepositoryPayload) => repositoryService.registerRepository(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['repositories', { projectId: id }] })
      toast({ title: 'Repository registered', variant: 'success' })
      setRepoFormOpen(false)
    },
    onError: (err: any) => {
      const msg = err?.response?.data?.message ?? 'Could not register repository'
      toast({ title: 'Registration failed', description: msg, variant: 'error' })
    },
  })

  const repoColumns: Column<Repository>[] = [
    {
      key: 'name',
      header: 'Repository',
      cell: (row) => (
        <div>
          <p className="font-medium text-accent-600">{row.name}</p>
          <p className="text-xs text-slate-500">{row.giteaOwner}/{row.giteaRepo}</p>
        </div>
      ),
    },
    {
      key: 'defaultBranch',
      header: 'Default Branch',
      cell: (row) => (
        <span className="text-sm font-mono text-slate-600">{row.defaultBranch}</span>
      ),
    },
    {
      key: 'status',
      header: 'Status',
      cell: (row) => <StatusBadge status={row.isActive ? 'Active' : 'Inactive'} />,
    },
    {
      key: 'webhookConfigured',
      header: 'Webhook',
      cell: (row) => (
        <StatusBadge status={row.webhookConfigured ? 'Active' : 'Inactive'} />
      ),
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
  ]

  if (projectLoading) {
    return (
      <div className="space-y-6 animate-pulse">
        <div className="h-10 bg-slate-200 rounded-lg w-64" />
        <div className="card p-6 space-y-4">
          {Array.from({ length: 6 }).map((_, i) => (
            <div key={i} className="h-8 bg-slate-100 rounded" />
          ))}
        </div>
      </div>
    )
  }

  if (!project) {
    return (
      <div className="text-center py-20">
        <p className="text-slate-500 text-lg">Project not found</p>
        <button onClick={() => router.back()} className="btn-secondary mt-4">
          <ArrowLeft className="w-4 h-4" /> Go Back
        </button>
      </div>
    )
  }

  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        title={project.name}
        subtitle={`Project details and associated resources`}
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Projects', href: '/projects' },
          { label: project.name },
        ]}
        actions={
          <button onClick={() => router.back()} className="btn-secondary">
            <ArrowLeft className="w-4 h-4" />
            Back
          </button>
        }
      />

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Info Card */}
        <div className="lg:col-span-1">
          <div className="card p-6">
            <div className="flex items-center justify-between mb-4">
              <h3 className="section-title">Project Details</h3>
              <StatusBadge status={project.isActive ? 'Active' : 'Inactive'} />
            </div>

            <InfoRow
              icon={<Building className="w-4 h-4" />}
              label="Customer"
              value={
                <div>
                  <p>{project.customerName}</p>
                  <p className="text-xs text-slate-400 font-mono">{project.customerId}</p>
                </div>
              }
            />

            {project.businessUnit && (
              <InfoRow
                icon={<Hash className="w-4 h-4" />}
                label="Business Unit"
                value={project.businessUnit}
              />
            )}

            {project.giteaOrgName && (
              <InfoRow
                icon={<Globe className="w-4 h-4" />}
                label="Gitea Organization"
                value={
                  <span className="font-mono text-accent-600">{project.giteaOrgName}</span>
                }
              />
            )}

            <InfoRow
              icon={<GitBranch className="w-4 h-4" />}
              label="Repositories"
              value={repositories?.pagination?.totalCount ?? 0}
            />

            <InfoRow
              icon={<Cpu className="w-4 h-4" />}
              label="Firmware Versions"
              value={firmwareList?.pagination?.totalCount ?? 0}
            />

            <InfoRow
              icon={<Calendar className="w-4 h-4" />}
              label="Created"
              value={formatDate(project.createdAt)}
            />

            <InfoRow
              icon={<Calendar className="w-4 h-4" />}
              label="Last Updated"
              value={formatDate(project.updatedAt)}
            />

            {project.description && (
              <div className="mt-4 pt-4 border-t border-slate-100">
                <p className="text-xs font-semibold text-slate-400 uppercase tracking-wide mb-1">Description</p>
                <p className="text-sm text-slate-600 leading-relaxed">{project.description}</p>
              </div>
            )}

            {(project.tags ?? []).length > 0 && (
              <div className="mt-4">
                <div className="flex items-center gap-1.5 text-xs font-semibold text-slate-400 uppercase tracking-wide mb-2">
                  <Tag className="w-3.5 h-3.5" />
                  Tags
                </div>
                <div className="flex flex-wrap gap-1.5">
                  {project.tags!.map((tag) => (
                    <span key={tag} className="px-2 py-0.5 bg-accent-50 text-accent-700 border border-accent-200 rounded-full text-xs font-medium">
                      {tag}
                    </span>
                  ))}
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Repositories Table */}
        <div className="lg:col-span-2 space-y-6">
          <div>
            <div className="flex items-center justify-between mb-3">
              <h3 className="section-title flex items-center gap-2">
                <GitBranch className="w-4 h-4 text-accent-600" />
                Repositories ({repositories?.pagination?.totalCount ?? 0})
              </h3>
              <RoleGuard module="Repositories" action="create">
                <button
                  onClick={() => setRepoFormOpen(true)}
                  className="btn-primary text-xs px-3 py-1.5"
                >
                  <Plus className="w-3.5 h-3.5" />
                  Add Repository
                </button>
              </RoleGuard>
            </div>
            <DataTable
              columns={repoColumns}
              data={repositories?.items ?? []}
              isLoading={reposLoading}
              keyExtractor={(r) => r.id}
              emptyMessage="No repositories associated with this project"
            />
          </div>

          {/* Quick Firmware Stats */}
          <div className="card p-6">
            <h3 className="section-title flex items-center gap-2 mb-4">
              <Cpu className="w-4 h-4 text-accent-600" />
              Recent Firmware ({firmwareList?.pagination?.totalCount ?? 0} total)
            </h3>
            {firmwareLoading ? (
              <div className="space-y-3">
                {Array.from({ length: 3 }).map((_, i) => (
                  <div key={i} className="h-12 bg-slate-100 rounded-lg animate-pulse" />
                ))}
              </div>
            ) : (firmwareList?.items ?? []).length === 0 ? (
              <p className="text-slate-400 text-sm text-center py-6">No firmware versions yet</p>
            ) : (
              <div className="space-y-2">
                {firmwareList!.items.map((fw) => (
                  <div
                    key={fw.id}
                    className="flex items-center justify-between p-3 bg-slate-50 rounded-lg"
                  >
                    <div>
                      <span className="font-mono font-semibold text-sm text-accent-600">{fw.version}</span>
                      <span className="text-xs text-slate-400 ml-2">{fw.repositoryName}</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <StatusBadge status={fw.channel} />
                      <StatusBadge status={fw.status} />
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Register Repository — pre-selected to this project */}
      <RepositoryForm
        open={repoFormOpen}
        onOpenChange={setRepoFormOpen}
        preselectedProjectId={id}
        preselectedProjectName={project?.name}
        onSubmit={async (data) => { await registerRepoMutation.mutateAsync(data) }}
        isLoading={registerRepoMutation.isPending}
      />
    </div>
  )
}
