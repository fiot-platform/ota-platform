'use client'

import * as React from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import * as Dialog from '@radix-ui/react-dialog'
import { X, Loader2, GitBranch } from 'lucide-react'
import { useQuery } from '@tanstack/react-query'
import { projectService } from '@/services/project.service'
import { Repository, ProjectClientRef } from '@/types'

// ─── Schema ───────────────────────────────────────────────────────────────────

const schema = z.object({
  name:          z.string().min(1, 'Name is required').max(200),
  description:   z.string().max(1000).optional().or(z.literal('')),
  defaultBranch: z.string().min(1, 'Default branch is required').max(100),
  isActive:      z.boolean(),
  projectId:     z.string().min(1, 'Project is required'),
  clientCode:    z.string().optional(),
})

type FormValues = z.infer<typeof schema>

export interface UpdateRepositoryPayload {
  name?:          string
  description?:   string
  defaultBranch?: string
  isActive?:      boolean
  projectId?:     string
  clientCode?:    string
}

// ─── Props ────────────────────────────────────────────────────────────────────

interface Props {
  open:         boolean
  onOpenChange: (open: boolean) => void
  repository:   Repository
  onSubmit:     (data: UpdateRepositoryPayload) => Promise<void>
  isLoading?:   boolean
}

// ─── Component ────────────────────────────────────────────────────────────────

export function EditRepositoryForm({ open, onOpenChange, repository, onSubmit, isLoading = false }: Props) {
  const { register, handleSubmit, reset, watch, setValue, formState: { errors } } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      name:          repository.name,
      description:   repository.description ?? '',
      defaultBranch: repository.defaultBranch,
      isActive:      repository.isActive,
      projectId:     repository.projectId,
      clientCode:    repository.clientCode ?? '',
    },
  })

  const selectedProjectId = watch('projectId')
  const initialProjectIdRef = React.useRef(repository.projectId)

  // All projects
  const { data: projectsData, isLoading: projectsLoading } = useQuery({
    queryKey: ['projects-dropdown'],
    queryFn: () => projectService.getProjects({ pageSize: 200 }),
    enabled: open,
  })
  const allProjects = projectsData?.items ?? []

  // Derive clients from the selected project
  const selectedProject  = allProjects.find((p) => p.id === selectedProjectId)
  const projectClients: ProjectClientRef[] = selectedProject?.clients ?? []

  React.useEffect(() => {
    if (open) {
      initialProjectIdRef.current = repository.projectId
      reset({
        name:          repository.name,
        description:   repository.description ?? '',
        defaultBranch: repository.defaultBranch,
        isActive:      repository.isActive,
        projectId:     repository.projectId,
        clientCode:    repository.clientCode ?? '',
      })
    }
  }, [open, repository, reset])

  // When the user picks a different project, reset clientCode (don't carry the old project's client over).
  // For the original project, keep whatever was stored.
  React.useEffect(() => {
    if (selectedProjectId === initialProjectIdRef.current) return
    if (projectClients.length === 1) {
      setValue('clientCode', projectClients[0].code, { shouldValidate: false })
    } else {
      setValue('clientCode', '', { shouldValidate: false })
    }
  }, [selectedProjectId, projectClients, setValue])

  const handleFormSubmit = async (values: FormValues) => {
    await onSubmit({
      name:          values.name,
      description:   values.description || undefined,
      defaultBranch: values.defaultBranch,
      isActive:      values.isActive,
      projectId:     values.projectId,
      clientCode:    values.clientCode ?? '',
    })
  }

  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50" />
        <Dialog.Content className="fixed left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 bg-white rounded-2xl shadow-2xl z-50 w-full max-w-md max-h-[90vh] overflow-y-auto">
          <div className="p-6">
            <div className="flex items-center justify-between mb-6">
              <div>
                <Dialog.Title className="text-lg font-semibold text-primary-900 flex items-center gap-2">
                  <GitBranch className="w-5 h-5 text-accent-600" />
                  Edit Repository
                </Dialog.Title>
                <Dialog.Description className="text-sm text-slate-500 mt-0.5">
                  Update details for <span className="font-medium text-accent-600">{repository.giteaOwner}/{repository.giteaRepo}</span>
                </Dialog.Description>
              </div>
              <Dialog.Close className="text-slate-400 hover:text-slate-600 transition-colors">
                <X className="w-5 h-5" />
              </Dialog.Close>
            </div>

            <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-4">

              {/* Display Name */}
              <div>
                <label className="label">Display Name <span className="text-danger-500">*</span></label>
                <input
                  {...register('name')}
                  className={`input ${errors.name ? 'border-danger-400' : ''}`}
                />
                {errors.name && <p className="form-error">{errors.name.message}</p>}
              </div>

              {/* Project */}
              <div>
                <label className="label">Project <span className="text-danger-500">*</span></label>
                <select
                  {...register('projectId')}
                  disabled={projectsLoading}
                  className={`input ${errors.projectId ? 'border-danger-400' : ''}`}
                >
                  <option value="">
                    {projectsLoading ? 'Loading projects…' : 'Select a project'}
                  </option>
                  {allProjects.map((p) => (
                    <option key={p.id} value={p.id}>{p.name}</option>
                  ))}
                </select>
                {errors.projectId && <p className="form-error">{errors.projectId.message}</p>}
              </div>

              {/* Client — derived from selected project */}
              <div>
                <label className="label">Client <span className="text-danger-500">*</span></label>
                {!selectedProjectId ? (
                  <input
                    type="text"
                    readOnly
                    placeholder="Select a project first"
                    className="input bg-slate-50 text-slate-400 cursor-not-allowed"
                  />
                ) : projectClients.length === 0 ? (
                  <input
                    type="text"
                    value="No client assigned to project"
                    readOnly
                    className="input bg-slate-50 text-slate-400 cursor-not-allowed"
                  />
                ) : (
                  <select className="input" {...register('clientCode')}>
                    <option value="">Select a client…</option>
                    {projectClients.map((c) => (
                      <option key={c.code} value={c.code}>
                        {c.name} — {c.code}
                      </option>
                    ))}
                  </select>
                )}
              </div>

              {/* Default Branch */}
              <div>
                <label className="label">Default Branch <span className="text-danger-500">*</span></label>
                <input
                  {...register('defaultBranch')}
                  className={`input font-mono ${errors.defaultBranch ? 'border-danger-400' : ''}`}
                  placeholder="main"
                />
                {errors.defaultBranch && <p className="form-error">{errors.defaultBranch.message}</p>}
              </div>

              {/* Description */}
              <div>
                <label className="label">Description</label>
                <textarea
                  {...register('description')}
                  rows={3}
                  className="input resize-none"
                  placeholder="Brief description of this repository..."
                />
              </div>

              {/* Active Toggle */}
              <div className="flex items-center gap-3 p-3 bg-slate-50 rounded-lg">
                <input
                  {...register('isActive')}
                  id="repo-isActive"
                  type="checkbox"
                  className="w-4 h-4 accent-accent-600 cursor-pointer"
                />
                <label htmlFor="repo-isActive" className="text-sm font-medium text-primary-800 cursor-pointer select-none">
                  Active — repository will process webhook events
                </label>
              </div>

              {/* Actions */}
              <div className="flex justify-end gap-3 pt-2">
                <Dialog.Close asChild>
                  <button type="button" className="btn-secondary" disabled={isLoading}>Cancel</button>
                </Dialog.Close>
                <button type="submit" className="btn-primary" disabled={isLoading}>
                  {isLoading
                    ? <><Loader2 className="w-4 h-4 animate-spin" /> Saving…</>
                    : 'Save Changes'}
                </button>
              </div>
            </form>
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  )
}
