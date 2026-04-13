'use client'

import * as React from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { X, Plus, Loader2 } from 'lucide-react'
import * as Dialog from '@radix-ui/react-dialog'
import { Project, CreateProjectRequest, UpdateProjectRequest } from '@/types'

// ─── Schema ───────────────────────────────────────────────────────────────────

const projectSchema = z.object({
  name: z.string().min(2, 'Project name must be at least 2 characters').max(100),
  description: z.string().max(500).optional(),
  customerId: z.string().min(1, 'Customer ID is required'),
  customerName: z.string().min(1, 'Customer name is required'),
  businessUnit: z.string().max(100).optional(),
  giteaOrgName: z.string().max(100).optional(),
  tags: z.array(z.string()).optional(),
})

type ProjectFormValues = z.infer<typeof projectSchema>

interface ProjectFormProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  project?: Project
  onSubmit: (data: CreateProjectRequest | UpdateProjectRequest) => Promise<void>
  isLoading?: boolean
}

// ─── Project Form ─────────────────────────────────────────────────────────────

export function ProjectForm({
  open,
  onOpenChange,
  project,
  onSubmit,
  isLoading = false,
}: ProjectFormProps) {
  const [tagInput, setTagInput] = React.useState('')
  const isEditing = Boolean(project)

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    reset,
    formState: { errors },
  } = useForm<ProjectFormValues>({
    resolver: zodResolver(projectSchema),
    defaultValues: {
      name: project?.name ?? '',
      description: project?.description ?? '',
      customerId: project?.customerId ?? '',
      customerName: project?.customerName ?? '',
      businessUnit: project?.businessUnit ?? '',
      giteaOrgName: project?.giteaOrgName ?? '',
      tags: project?.tags ?? [],
    },
  })

  const tags = watch('tags') ?? []

  React.useEffect(() => {
    if (open) {
      reset({
        name: project?.name ?? '',
        description: project?.description ?? '',
        customerId: project?.customerId ?? '',
        customerName: project?.customerName ?? '',
        businessUnit: project?.businessUnit ?? '',
        giteaOrgName: project?.giteaOrgName ?? '',
        tags: project?.tags ?? [],
      })
      setTagInput('')
    }
  }, [open, project, reset])

  const addTag = () => {
    const tag = tagInput.trim()
    if (tag && !tags.includes(tag)) {
      setValue('tags', [...tags, tag])
      setTagInput('')
    }
  }

  const removeTag = (tag: string) => {
    setValue('tags', tags.filter((t) => t !== tag))
  }

  const handleFormSubmit = async (values: ProjectFormValues) => {
    await onSubmit(values)
    onOpenChange(false)
  }

  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50" />
        <Dialog.Content className="fixed left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 bg-white rounded-2xl shadow-2xl z-50 w-full max-w-xl max-h-[90vh] overflow-y-auto">
          <div className="p-6">
            {/* Header */}
            <div className="flex items-center justify-between mb-6">
              <div>
                <Dialog.Title className="text-lg font-semibold text-primary-900">
                  {isEditing ? 'Edit Project' : 'Create New Project'}
                </Dialog.Title>
                <Dialog.Description className="text-sm text-slate-500 mt-0.5">
                  {isEditing ? 'Update project details' : 'Add a new project to the platform'}
                </Dialog.Description>
              </div>
              <Dialog.Close className="text-slate-400 hover:text-slate-600 transition-colors">
                <X className="w-5 h-5" />
              </Dialog.Close>
            </div>

            {/* Form */}
            <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-4">
              {/* Name */}
              <div>
                <label className="label">Project Name <span className="text-danger-500">*</span></label>
                <input
                  type="text"
                  placeholder="e.g., Smart Gateway Firmware"
                  className={`input ${errors.name ? 'border-danger-400' : ''}`}
                  {...register('name')}
                />
                {errors.name && <p className="form-error">{errors.name.message}</p>}
              </div>

              {/* Description */}
              <div>
                <label className="label">Description</label>
                <textarea
                  placeholder="Brief description of this project..."
                  rows={3}
                  className={`input resize-none ${errors.description ? 'border-danger-400' : ''}`}
                  {...register('description')}
                />
                {errors.description && <p className="form-error">{errors.description.message}</p>}
              </div>

              {/* Customer ID + Name */}
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="label">Customer ID <span className="text-danger-500">*</span></label>
                  <input
                    type="text"
                    placeholder="CUST-001"
                    className={`input ${errors.customerId ? 'border-danger-400' : ''}`}
                    {...register('customerId')}
                    disabled={isEditing}
                  />
                  {errors.customerId && <p className="form-error">{errors.customerId.message}</p>}
                </div>
                <div>
                  <label className="label">Customer Name <span className="text-danger-500">*</span></label>
                  <input
                    type="text"
                    placeholder="Acme Corporation"
                    className={`input ${errors.customerName ? 'border-danger-400' : ''}`}
                    {...register('customerName')}
                  />
                  {errors.customerName && <p className="form-error">{errors.customerName.message}</p>}
                </div>
              </div>

              {/* Business Unit + Gitea Org */}
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="label">Business Unit</label>
                  <input
                    type="text"
                    placeholder="IoT Division"
                    className="input"
                    {...register('businessUnit')}
                  />
                </div>
                <div>
                  <label className="label">Gitea Org Name</label>
                  <input
                    type="text"
                    placeholder="acme-iot"
                    className="input"
                    {...register('giteaOrgName')}
                  />
                </div>
              </div>

              {/* Tags */}
              <div>
                <label className="label">Tags</label>
                <div className="flex gap-2 mb-2">
                  <input
                    type="text"
                    value={tagInput}
                    onChange={(e) => setTagInput(e.target.value)}
                    onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); addTag() } }}
                    placeholder="Add tag and press Enter"
                    className="input flex-1"
                  />
                  <button type="button" onClick={addTag} className="btn-secondary px-3">
                    <Plus className="w-4 h-4" />
                  </button>
                </div>
                {tags.length > 0 && (
                  <div className="flex flex-wrap gap-2">
                    {tags.map((tag) => (
                      <span
                        key={tag}
                        className="inline-flex items-center gap-1.5 px-2.5 py-1 bg-accent-100 text-accent-800 rounded-full text-xs font-medium"
                      >
                        {tag}
                        <button type="button" onClick={() => removeTag(tag)} className="text-accent-500 hover:text-accent-700">
                          <X className="w-3 h-3" />
                        </button>
                      </span>
                    ))}
                  </div>
                )}
              </div>

              {/* Actions */}
              <div className="flex items-center justify-end gap-3 pt-4 border-t border-slate-100">
                <Dialog.Close className="btn-secondary">Cancel</Dialog.Close>
                <button type="submit" disabled={isLoading} className="btn-primary">
                  {isLoading ? (
                    <><Loader2 className="w-4 h-4 animate-spin" /> {isEditing ? 'Updating...' : 'Creating...'}</>
                  ) : (
                    isEditing ? 'Update Project' : 'Create Project'
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
