'use client'

import * as React from 'react'
import { useQuery } from '@tanstack/react-query'
import { useParams, useRouter } from 'next/navigation'
import {
  ArrowLeft, Mail, Shield, Calendar, Clock, Building2,
  FolderKanban, Hash, CheckCircle, XCircle, User,
} from 'lucide-react'
import { userService } from '@/services/user.service'
import { projectService } from '@/services/project.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { StatusBadge, RoleBadge } from '@/components/ui/Badge'
import { formatDate, formatRelativeTime } from '@/utils/formatters'

function InfoRow({
  icon,
  label,
  value,
}: {
  icon: React.ReactNode
  label: string
  value: React.ReactNode
}) {
  return (
    <div className="flex items-start gap-3 py-3 border-b border-slate-50 last:border-0">
      <span className="text-slate-400 mt-0.5 flex-shrink-0">{icon}</span>
      <div className="min-w-0 flex-1">
        <p className="text-xs font-semibold text-slate-400 uppercase tracking-wide mb-0.5">{label}</p>
        <div className="text-sm font-medium text-primary-800">{value}</div>
      </div>
    </div>
  )
}

export default function UserDetailPage() {
  const { id } = useParams<{ id: string }>()
  const router = useRouter()

  const { data: user, isLoading } = useQuery({
    queryKey: ['user', id],
    queryFn: () => userService.getUserById(id),
    enabled: Boolean(id),
  })

  const { data: projectsData } = useQuery({
    queryKey: ['projects-all'],
    queryFn: () => projectService.getProjects({ pageSize: 200 }),
    enabled: Boolean(user?.projectScope?.length),
  })

  const assignedProjects = React.useMemo(() => {
    if (!user?.projectScope?.length || !projectsData?.items) return []
    return projectsData.items.filter((p) => user.projectScope!.includes(p.id))
  }, [user, projectsData])

  if (isLoading) {
    return (
      <div className="space-y-6 animate-pulse">
        <div className="h-10 bg-slate-200 rounded-lg w-64" />
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="card p-6 h-64" />
          <div className="lg:col-span-2 card p-6 h-64" />
        </div>
      </div>
    )
  }

  if (!user) {
    return (
      <div className="text-center py-20">
        <p className="text-slate-500 text-lg">User not found</p>
        <button onClick={() => router.back()} className="btn-secondary mt-4">
          <ArrowLeft className="w-4 h-4" /> Go Back
        </button>
      </div>
    )
  }

  const initials = user.name
    ?.split(' ')
    .map((w) => w[0])
    .slice(0, 2)
    .join('')
    .toUpperCase() ?? 'U'

  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        title={user.name}
        subtitle={user.email}
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Users', href: '/users' },
          { label: user.name },
        ]}
        actions={
          <button onClick={() => router.back()} className="btn-secondary">
            <ArrowLeft className="w-4 h-4" />
            Back
          </button>
        }
      />

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">

        {/* ── Left column: Profile ── */}
        <div className="space-y-4">

          {/* Avatar + identity card */}
          <div className="card p-6">
            <div className="flex flex-col items-center text-center gap-3 pb-5 border-b border-slate-100">
              <div className="w-16 h-16 bg-gradient-to-br from-accent-400 to-accent-600 rounded-full flex items-center justify-center shadow-md">
                <span className="text-white text-xl font-bold tracking-wide">{initials}</span>
              </div>
              <div>
                <h2 className="text-lg font-bold text-primary-900">{user.name}</h2>
                <p className="text-sm text-slate-500 mt-0.5">{user.email}</p>
              </div>
              <div className="flex items-center gap-2 flex-wrap justify-center">
                <RoleBadge role={user.role} />
                <StatusBadge status={user.isActive ? 'Active' : 'Inactive'} dot />
              </div>
            </div>

            <div className="pt-4 space-y-0">
              <InfoRow
                icon={<Hash className="w-4 h-4" />}
                label="User ID"
                value={
                  <span className="font-mono text-xs text-slate-600 break-all">{user.id}</span>
                }
              />
              <InfoRow
                icon={<Building2 className="w-4 h-4" />}
                label="Customer"
                value={user.customerName ?? user.customerId ?? '—'}
              />
            </div>
          </div>

          {/* Account status card */}
          <div className="card p-6 space-y-3">
            <h3 className="section-title">Account Status</h3>

            <div className={`flex items-center gap-3 p-3 rounded-lg ${
              user.isActive
                ? 'bg-success-50 border border-success-200'
                : 'bg-danger-50 border border-danger-200'
            }`}>
              {user.isActive ? (
                <CheckCircle className="w-5 h-5 text-success-600 flex-shrink-0" />
              ) : (
                <XCircle className="w-5 h-5 text-danger-500 flex-shrink-0" />
              )}
              <div>
                <p className={`text-sm font-semibold ${user.isActive ? 'text-success-700' : 'text-danger-600'}`}>
                  {user.isActive ? 'Active' : 'Inactive'}
                </p>
                <p className="text-xs text-slate-500">
                  {user.isActive ? 'Can log in and access the platform' : 'Login is disabled'}
                </p>
              </div>
            </div>

            <InfoRow
              icon={<Clock className="w-4 h-4" />}
              label="Last Login"
              value={user.lastLoginAt ? formatRelativeTime(user.lastLoginAt) : 'Never'}
            />
            <InfoRow
              icon={<Calendar className="w-4 h-4" />}
              label="Created"
              value={formatDate(user.createdAt)}
            />
            <InfoRow
              icon={<Calendar className="w-4 h-4" />}
              label="Last Updated"
              value={formatDate(user.updatedAt)}
            />
          </div>
        </div>

        {/* ── Right column: Details + Project Scope ── */}
        <div className="lg:col-span-2 space-y-4">

          {/* User details card */}
          <div className="card p-6">
            <h3 className="section-title mb-2">User Details</h3>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-x-8">
              <InfoRow
                icon={<User className="w-4 h-4" />}
                label="Full Name"
                value={user.name}
              />
              <InfoRow
                icon={<Mail className="w-4 h-4" />}
                label="Email Address"
                value={user.email}
              />
              <InfoRow
                icon={<Shield className="w-4 h-4" />}
                label="Platform Role"
                value={<RoleBadge role={user.role} />}
              />
              <InfoRow
                icon={<Building2 className="w-4 h-4" />}
                label="Customer"
                value={user.customerName ?? user.customerId ?? '—'}
              />
            </div>
          </div>

          {/* Project scope card */}
          <div className="card p-6">
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center gap-2">
                <FolderKanban className="w-4 h-4 text-accent-600" />
                <h3 className="section-title">Project Scope</h3>
              </div>
              {user.projectScope?.length ? (
                <span className="text-xs font-semibold bg-accent-100 text-accent-700 px-2.5 py-0.5 rounded-full">
                  {user.projectScope.length} project{user.projectScope.length !== 1 ? 's' : ''}
                </span>
              ) : (
                <span className="text-xs font-semibold bg-success-100 text-success-700 px-2.5 py-0.5 rounded-full">
                  All projects
                </span>
              )}
            </div>

            {!user.projectScope?.length ? (
              <div className="flex flex-col items-center justify-center py-8 bg-slate-50 rounded-xl border border-dashed border-slate-200">
                <FolderKanban className="w-8 h-8 text-slate-300 mb-2" />
                <p className="text-sm font-medium text-danger-600">No projects assigned</p>
                <p className="text-xs text-slate-400 mt-0.5">This user cannot see any projects, devices or firmware until projects are assigned.</p>
              </div>
            ) : (
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                {assignedProjects.length > 0
                  ? assignedProjects.map((project) => (
                      <div
                        key={project.id}
                        className="flex items-start justify-between p-3 rounded-xl bg-slate-50 border border-slate-100 hover:border-accent-200 hover:bg-accent-50 transition-colors"
                      >
                        <div className="min-w-0">
                          <p className="text-sm font-semibold text-primary-800 truncate">{project.name}</p>
                          {project.description && (
                            <p className="text-xs text-slate-500 truncate mt-0.5">{project.description}</p>
                          )}
                          <p className="text-xs text-slate-400 font-mono mt-1 truncate">{project.id}</p>
                        </div>
                        <StatusBadge status={project.isActive ? 'Active' : 'Inactive'} />
                      </div>
                    ))
                  : user.projectScope.map((pid) => (
                      <div key={pid} className="p-3 rounded-xl bg-slate-50 border border-slate-100">
                        <p className="text-xs font-mono text-slate-500 break-all">{pid}</p>
                      </div>
                    ))}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}
