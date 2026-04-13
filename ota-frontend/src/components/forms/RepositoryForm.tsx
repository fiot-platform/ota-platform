'use client'

import * as React from 'react'
import { useForm, useWatch, useController } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { X, GitBranch, Loader2, Lock, Globe } from 'lucide-react'
import * as Dialog from '@radix-ui/react-dialog'
import { useQuery } from '@tanstack/react-query'
import { projectService } from '@/services/project.service'

// ─── Schema ───────────────────────────────────────────────────────────────────

const repositorySchema = z.object({
  giteaOwner: z.string().min(1, 'Gitea owner is required').max(100),
  giteaRepoName: z.string().min(1, 'Repository name is required').max(200),
  projectId: z.string().min(1, 'Project is required'),
  description: z.string().max(1000).optional(),
  defaultBranch: z.string().min(1).max(100),
  isPrivate: z.boolean(),
})

type RepositoryFormValues = z.infer<typeof repositorySchema>

export interface RegisterRepositoryPayload {
  giteaOwner: string
  giteaRepoName: string
  projectId: string
  description?: string
  defaultBranch: string
  isPrivate: boolean
}

interface RepositoryFormProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  preselectedProjectId?: string
  preselectedProjectName?: string
  onSubmit: (data: RegisterRepositoryPayload) => Promise<void>
  isLoading?: boolean
}

// ─── Visibility Toggle ────────────────────────────────────────────────────────

import { Control } from 'react-hook-form'

function VisibilityToggle({ control }: { control: Control<RepositoryFormValues> }) {
  const { field } = useController({ control, name: 'isPrivate' })
  return (
    <div>
      <label className="label">Visibility</label>
      <div className="flex gap-3">
        {([
          { value: false, icon: <Globe className="w-4 h-4" />, label: 'Public',  sub: 'Anyone can view' },
          { value: true,  icon: <Lock  className="w-4 h-4" />, label: 'Private', sub: 'Only members can view' },
        ] as const).map(({ value, icon, label, sub }) => (
          <button
            key={String(value)}
            type="button"
            onClick={() => field.onChange(value)}
            className={`flex-1 flex items-center gap-3 p-3 rounded-lg border-2 transition-all text-left ${
              field.value === value
                ? 'border-accent-500 bg-accent-50'
                : 'border-slate-200 hover:border-accent-300'
            }`}
          >
            <span className={field.value === value ? 'text-accent-600' : 'text-slate-400'}>{icon}</span>
            <div>
              <p className="text-sm font-medium text-primary-900">{label}</p>
              <p className="text-xs text-slate-400">{sub}</p>
            </div>
          </button>
        ))}
      </div>
    </div>
  )
}

// ─── Repository Form ──────────────────────────────────────────────────────────

export function RepositoryForm({
  open,
  onOpenChange,
  preselectedProjectId,
  preselectedProjectName,
  onSubmit,
  isLoading = false,
}: RepositoryFormProps) {
  const {
    register,
    handleSubmit,
    control,
    reset,
    formState: { errors },
  } = useForm<RepositoryFormValues>({
    resolver: zodResolver(repositorySchema),
    defaultValues: {
      giteaOwner: '',
      giteaRepoName: '',
      projectId: preselectedProjectId ?? '',
      description: '',
      defaultBranch: 'main',
      isPrivate: false,
    },
  })

  const giteaOwner = useWatch({ control, name: 'giteaOwner' })
  const giteaRepoName = useWatch({ control, name: 'giteaRepoName' })

  const { data: projects } = useQuery({
    queryKey: ['projects-all'],
    queryFn: () => projectService.getProjects({ pageSize: 200 }),
    enabled: !preselectedProjectId,
  })

  React.useEffect(() => {
    if (open) {
      reset({
        giteaOwner: '',
        giteaRepoName: '',
        projectId: preselectedProjectId ?? '',
        description: '',
        defaultBranch: 'main',
        isPrivate: false,
      })
    }
  }, [open, preselectedProjectId, reset])

  const handleFormSubmit = async (values: RepositoryFormValues) => {
    await onSubmit({
      giteaOwner: values.giteaOwner.trim(),
      giteaRepoName: values.giteaRepoName.trim(),
      projectId: values.projectId,
      description: values.description?.trim() || undefined,
      defaultBranch: values.defaultBranch.trim() || 'main',
      isPrivate: values.isPrivate,
    })
  }

  const previewOwner = giteaOwner?.trim() || 'owner'
  const previewRepo = giteaRepoName?.trim() || 'repo-name'

  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50" />
        <Dialog.Content className="fixed left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 bg-white rounded-2xl shadow-2xl z-50 w-full max-w-lg max-h-[90vh] overflow-y-auto">
          <div className="p-6">
            {/* Header */}
            <div className="flex items-center justify-between mb-6">
              <div>
                <Dialog.Title className="text-lg font-semibold text-primary-900 flex items-center gap-2">
                  <GitBranch className="w-5 h-5 text-accent-600" />
                  Register Repository
                </Dialog.Title>
                <Dialog.Description className="text-sm text-slate-500 mt-0.5">
                  Creates the repository on Gitea if it doesn&apos;t exist, then links it
                  {preselectedProjectName && (
                    <> to <span className="font-medium text-accent-600">{preselectedProjectName}</span></>
                  )}
                </Dialog.Description>
              </div>
              <Dialog.Close className="text-slate-400 hover:text-slate-600 transition-colors">
                <X className="w-5 h-5" />
              </Dialog.Close>
            </div>

            {/* Form */}
            <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-4">

              {/* Project */}
              <div>
                <label className="label">
                  Project <span className="text-danger-500">*</span>
                </label>
                {preselectedProjectId ? (
                  <input
                    type="text"
                    value={preselectedProjectName ?? preselectedProjectId}
                    disabled
                    className="input bg-slate-50 text-slate-500 cursor-not-allowed"
                  />
                ) : (
                  <select
                    className={`input ${errors.projectId ? 'border-danger-400' : ''}`}
                    {...register('projectId')}
                  >
                    <option value="">Select a project...</option>
                    {(projects?.items ?? []).map((p) => (
                      <option key={p.id} value={p.id}>
                        {p.name}
                      </option>
                    ))}
                  </select>
                )}
                {errors.projectId && <p className="form-error">{errors.projectId.message}</p>}
              </div>

              {/* Gitea Owner + Repo */}
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="label">
                    Gitea Owner <span className="text-danger-500">*</span>
                  </label>
                  <input
                    type="text"
                    placeholder="org-name"
                    className={`input font-mono ${errors.giteaOwner ? 'border-danger-400' : ''}`}
                    {...register('giteaOwner')}
                  />
                  {errors.giteaOwner && <p className="form-error">{errors.giteaOwner.message}</p>}
                </div>
                <div>
                  <label className="label">
                    Repository Name <span className="text-danger-500">*</span>
                  </label>
                  <input
                    type="text"
                    placeholder="firmware-gateway"
                    className={`input font-mono ${errors.giteaRepoName ? 'border-danger-400' : ''}`}
                    {...register('giteaRepoName')}
                  />
                  {errors.giteaRepoName && (
                    <p className="form-error">{errors.giteaRepoName.message}</p>
                  )}
                </div>
              </div>

              {/* Live path preview */}
              <div className="flex items-center gap-2 bg-slate-50 border border-slate-200 rounded-lg px-4 py-2.5">
                <GitBranch className="w-4 h-4 text-slate-400 flex-shrink-0" />
                <span className="text-sm font-mono text-slate-500">
                  <span className="text-accent-600 font-semibold">{previewOwner}</span>
                  <span className="text-slate-400"> / </span>
                  <span className="text-accent-600 font-semibold">{previewRepo}</span>
                </span>
              </div>

              {/* Default Branch */}
              <div>
                <label className="label">Default Branch</label>
                <input
                  type="text"
                  placeholder="main"
                  className={`input font-mono ${errors.defaultBranch ? 'border-danger-400' : ''}`}
                  {...register('defaultBranch')}
                />
                {errors.defaultBranch && (
                  <p className="form-error">{errors.defaultBranch.message}</p>
                )}
              </div>

              {/* Description */}
              <div>
                <label className="label">Description</label>
                <textarea
                  placeholder="Brief description of this repository..."
                  rows={2}
                  className="input resize-none"
                  {...register('description')}
                />
              </div>

              {/* Visibility */}
              <VisibilityToggle control={control} />

              {/* Actions */}
              <div className="flex items-center justify-end gap-3 pt-4 border-t border-slate-100">
                <Dialog.Close className="btn-secondary">Cancel</Dialog.Close>
                <button type="submit" disabled={isLoading} className="btn-primary">
                  {isLoading ? (
                    <>
                      <Loader2 className="w-4 h-4 animate-spin" />
                      Registering...
                    </>
                  ) : (
                    <>
                      <GitBranch className="w-4 h-4" />
                      Register Repository
                    </>
                  )}
                </button>
              </div>
            </form>
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  )
}
