'use client'

import * as React from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { X, Plus, Loader2, ChevronDown, Check } from 'lucide-react'
import * as Dialog from '@radix-ui/react-dialog'
import { useQuery } from '@tanstack/react-query'
import { clientService } from '@/services/client.service'
import { Project, CreateProjectRequest, UpdateProjectRequest, Client } from '@/types'

// ─── Schema ───────────────────────────────────────────────────────────────────

const projectSchema = z.object({
  name:         z.string().min(2, 'Project name must be at least 2 characters').max(100),
  description:  z.string().max(500).optional(),
  clientCodes:  z.array(z.string()).min(1, 'Select at least one client'),
  businessUnit: z.string().max(100).optional(),
  giteaOrgName: z.string().max(100).optional(),
  tags:         z.array(z.string()).optional(),
})

type ProjectFormValues = z.infer<typeof projectSchema>

interface ProjectFormProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  project?: Project
  onSubmit: (data: CreateProjectRequest | UpdateProjectRequest) => Promise<void>
  isLoading?: boolean
}

// ─── Multi-select dropdown ────────────────────────────────────────────────────

interface ClientMultiSelectProps {
  clients:  Client[]
  selected: string[]
  onChange: (codes: string[]) => void
  loading:  boolean
  error?:   string
}

function ClientMultiSelect({ clients, selected, onChange, loading, error }: ClientMultiSelectProps) {
  const [open, setOpen] = React.useState(false)
  const ref             = React.useRef<HTMLDivElement>(null)

  React.useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false)
    }
    document.addEventListener('mousedown', handler)
    return () => document.removeEventListener('mousedown', handler)
  }, [])

  const toggle = (code: string) => {
    onChange(selected.includes(code) ? selected.filter((c) => c !== code) : [...selected, code])
  }

  const label = () => {
    if (loading) return 'Loading clients…'
    if (selected.length === 0) return 'Select clients…'
    if (selected.length === 1) {
      const match = clients.find((c) => c.code === selected[0])
      return match ? `${match.name} — ${match.code}` : selected[0]
    }
    return `${selected.length} clients selected`
  }

  return (
    <div ref={ref} className="relative">
      <button
        type="button"
        onClick={() => !loading && setOpen((v) => !v)}
        className={`input w-full flex items-center justify-between text-left ${
          error ? 'border-danger-400' : ''
        } ${loading ? 'cursor-wait opacity-60' : ''}`}
      >
        <span className={selected.length === 0 ? 'text-slate-400' : 'text-primary-900'}>
          {label()}
        </span>
        {loading
          ? <Loader2 className="w-4 h-4 animate-spin text-slate-400 flex-shrink-0" />
          : <ChevronDown className={`w-4 h-4 text-slate-400 flex-shrink-0 transition-transform ${open ? 'rotate-180' : ''}`} />}
      </button>

      {open && (
        <div className="absolute z-50 mt-1 w-full bg-white border border-slate-200 rounded-xl shadow-xl max-h-56 overflow-y-auto">
          {clients.length === 0
            ? <p className="px-4 py-3 text-sm text-slate-400">No active clients found.</p>
            : clients.map((c) => {
                const checked = selected.includes(c.code)
                return (
                  <button
                    key={c.id}
                    type="button"
                    onClick={() => toggle(c.code)}
                    className={`w-full flex items-center gap-3 px-4 py-2.5 text-left hover:bg-slate-50 transition-colors ${
                      checked ? 'bg-accent-50' : ''
                    }`}
                  >
                    <span className={`w-4 h-4 flex-shrink-0 rounded border flex items-center justify-center ${
                      checked ? 'bg-accent-600 border-accent-600' : 'border-slate-300'
                    }`}>
                      {checked && <Check className="w-3 h-3 text-white" />}
                    </span>
                    <span className="flex-1 min-w-0">
                      <span className="block text-sm font-medium text-primary-900 truncate">{c.name}</span>
                      <span className="block text-xs text-slate-400 font-mono">{c.code}</span>
                    </span>
                  </button>
                )
              })}
        </div>
      )}

      {/* Selected pills below the dropdown */}
      {selected.length > 0 && (
        <div className="flex flex-wrap gap-1.5 mt-2">
          {selected.map((code) => {
            const match = clients.find((c) => c.code === code)
            return (
              <span key={code} className="inline-flex items-center gap-1 px-2.5 py-0.5 bg-accent-100 text-accent-800 text-xs font-medium rounded-full border border-accent-200">
                {match?.name ?? code}
                <button type="button" onClick={() => toggle(code)} className="text-accent-500 hover:text-accent-700 ml-0.5">
                  <X className="w-3 h-3" />
                </button>
              </span>
            )
          })}
        </div>
      )}
    </div>
  )
}

// ─── Main form ────────────────────────────────────────────────────────────────

export function ProjectForm({
  open, onOpenChange, project, onSubmit, isLoading = false,
}: ProjectFormProps) {
  const [tagInput, setTagInput] = React.useState('')
  const isEditing = Boolean(project)

  const {
    register, handleSubmit, watch, setValue, reset,
    formState: { errors },
  } = useForm<ProjectFormValues>({
    resolver: zodResolver(projectSchema),
    defaultValues: {
      name:         project?.name         ?? '',
      description:  project?.description  ?? '',
      clientCodes:  project?.clients?.map((c) => c.code) ?? [],
      businessUnit: project?.businessUnit ?? '',
      giteaOrgName: project?.giteaOrgName ?? '',
      tags:         project?.tags         ?? [],
    },
  })

  const tags        = watch('tags')        ?? []
  const clientCodes = watch('clientCodes') ?? []

  const { data: clientsData, isLoading: clientsLoading } = useQuery({
    queryKey: ['clients-dropdown'],
    queryFn: () => clientService.getClients({ pageSize: 200 }),
    enabled: open,
  })
  const clients: Client[] = clientsData?.items ?? []

  React.useEffect(() => {
    if (open) {
      reset({
        name:         project?.name         ?? '',
        description:  project?.description  ?? '',
        clientCodes:  project?.clients?.map((c) => c.code) ?? [],
        businessUnit: project?.businessUnit ?? '',
        giteaOrgName: project?.giteaOrgName ?? '',
        tags:         project?.tags         ?? [],
      })
      setTagInput('')
    }
  }, [open, project, reset])

  const addTag = () => {
    const tag = tagInput.trim()
    if (tag && !tags.includes(tag)) { setValue('tags', [...tags, tag]); setTagInput('') }
  }

  const removeTag = (tag: string) => setValue('tags', tags.filter((t) => t !== tag))

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
              </div>

              {/* Client multi-select */}
              <div>
                <label className="label">
                  Clients <span className="text-danger-500">*</span>
                </label>
                <ClientMultiSelect
                  clients={clients}
                  selected={clientCodes}
                  onChange={(codes) => setValue('clientCodes', codes, { shouldValidate: true })}
                  loading={clientsLoading}
                  error={errors.clientCodes?.message}
                />
                {errors.clientCodes && (
                  <p className="form-error">{errors.clientCodes.message}</p>
                )}
              </div>

              {/* Business Unit + Gitea Org */}
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="label">Business Unit</label>
                  <input type="text" placeholder="IoT Division" className="input" {...register('businessUnit')} />
                </div>
                <div>
                  <label className="label">Gitea Org Name</label>
                  <input type="text" placeholder="acme-iot" className="input" {...register('giteaOrgName')} />
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
                      <span key={tag} className="inline-flex items-center gap-1.5 px-2.5 py-1 bg-accent-100 text-accent-800 rounded-full text-xs font-medium">
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
                  {isLoading
                    ? <><Loader2 className="w-4 h-4 animate-spin" /> {isEditing ? 'Updating...' : 'Creating...'}</>
                    : isEditing ? 'Update Project' : 'Create Project'}
                </button>
              </div>
            </form>
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  )
}
