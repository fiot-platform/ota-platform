'use client'

import * as React from 'react'
import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useQuery } from '@tanstack/react-query'
import * as Dialog from '@radix-ui/react-dialog'
import { X, Loader2, ChevronDown } from 'lucide-react'
import { projectService } from '@/services/project.service'
import { repositoryService } from '@/services/repository.service'
import { Project, ProjectClientRef, Repository } from '@/types'

// ─── Schema ───────────────────────────────────────────────────────────────────

const registerDeviceSchema = z.object({
  projectName: z.string().min(1, 'Please select a project'),
  customerCode: z.string().min(1, 'Please select a client').max(200),
  macImeiIp: z
    .string()
    .min(1, 'MAC / IMEI / IP is required')
    .max(100, 'Must be 100 characters or fewer'),
  model: z.string().min(1, 'Model is required').max(100),
  currentFirmwareVersion: z
    .string()
    .max(50)
    .optional()
    .or(z.literal('')),
  publishTopic: z
    .string()
    .max(200)
    .optional()
    .or(z.literal('')),
  repositoryId: z.string().optional(),
})

export type RegisterDeviceFormValues = z.infer<typeof registerDeviceSchema>

// ─── Props ────────────────────────────────────────────────────────────────────

interface RegisterDeviceFormProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSubmit: (data: RegisterDeviceFormValues) => Promise<void>
  isLoading?: boolean
}

// ─── Component ────────────────────────────────────────────────────────────────

export function RegisterDeviceForm({
  open,
  onOpenChange,
  onSubmit,
  isLoading = false,
}: RegisterDeviceFormProps) {
  const [selectedProjectId, setSelectedProjectId] = React.useState<string>('')
  const [projectClients, setProjectClients] = React.useState<ProjectClientRef[]>([])
  const [selectedClientCode, setSelectedClientCode] = React.useState<string>('')

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    watch,
    control,
    formState: { errors },
  } = useForm<RegisterDeviceFormValues>({
    resolver: zodResolver(registerDeviceSchema),
    defaultValues: {
      projectName: '',
      customerCode: '',
      macImeiIp: '',
      model: '',
      currentFirmwareVersion: '',
      publishTopic: '',
      repositoryId: '',
    },
  })

  // Auto-generate publish topic from MAC/IMEI/IP.
  // setValue is a stable ref from react-hook-form — omit it from deps to avoid loops.
  const macImeiIpValue = watch('macImeiIp')
  React.useEffect(() => {
    const serial = macImeiIpValue?.trim().toUpperCase()
    setValue('publishTopic', serial ? `OTA/${serial}/Status` : '')
  }, [macImeiIpValue]) // eslint-disable-line react-hooks/exhaustive-deps

  // Fetch all active projects for the dropdown
  const { data: projectsData, isLoading: projectsLoading } = useQuery({
    queryKey: ['projects-dropdown'],
    queryFn: () => projectService.getProjects({ isActive: true, pageSize: 200 }),
    enabled: open,
  })

  const projects: Project[] = projectsData?.items ?? []

  // Fetch repositories once both project and client are selected
  const { data: repositoriesData, isLoading: reposLoading } = useQuery({
    queryKey: ['repositories-dropdown', selectedProjectId, selectedClientCode],
    queryFn: () =>
      repositoryService.getRepositories({ projectId: selectedProjectId, isActive: true, pageSize: 200 }),
    enabled: open && !!selectedProjectId && !!selectedClientCode,
  })

  const repositories: Repository[] = repositoriesData?.items ?? []

  React.useEffect(() => {
    if (!open) {
      reset()
      setSelectedProjectId('')
      setProjectClients([])
      setSelectedClientCode('')
    }
  }, [open, reset])

  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50" />
        <Dialog.Content className="fixed left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 bg-white rounded-2xl shadow-2xl z-50 w-full max-w-md max-h-[90vh] overflow-y-auto">
          <div className="p-6">
            {/* Header */}
            <div className="flex items-center justify-between mb-6">
              <div>
                <Dialog.Title className="text-lg font-semibold text-primary-900">
                  Register Device
                </Dialog.Title>
                <Dialog.Description className="text-sm text-slate-500">
                  Add a new IoT device to the platform
                </Dialog.Description>
              </div>
              <Dialog.Close className="text-slate-400 hover:text-slate-600 transition-colors">
                <X className="w-5 h-5" />
              </Dialog.Close>
            </div>

            <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">

              {/* Project Name */}
              <div>
                <label className="label">
                  Project Name <span className="text-danger-500">*</span>
                </label>
                <div className="relative">
                  <Controller
                    name="projectName"
                    control={control}
                    render={({ field }) => (
                      <select
                        {...field}
                        disabled={projectsLoading}
                        className={`input appearance-none pr-9 ${errors.projectName ? 'border-danger-400' : ''}`}
                        onChange={(e) => {
                          field.onChange(e)
                          const matched = projects.find((p) => p.name === e.target.value)
                          setValue('customerCode', '', { shouldValidate: false })
                          setValue('repositoryId', '')
                          setProjectClients(matched?.clients ?? [])
                          setSelectedProjectId(matched?.id ?? '')
                          setSelectedClientCode('')
                        }}
                      >
                        <option value="" disabled>
                          {projectsLoading ? 'Loading projects…' : 'Select a project'}
                        </option>
                        {projects.map((p) => (
                          <option key={p.id} value={p.name}>
                            {p.name}
                          </option>
                        ))}
                      </select>
                    )}
                  />
                  <span className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-slate-400">
                    {projectsLoading
                      ? <Loader2 className="w-4 h-4 animate-spin" />
                      : <ChevronDown className="w-4 h-4" />
                    }
                  </span>
                </div>
                {errors.projectName && (
                  <p className="form-error">{errors.projectName.message}</p>
                )}
              </div>

              {/* Client — derived from selected project */}
              <div>
                <label className="label">
                  Client <span className="text-danger-500">*</span>
                </label>
                <div className="relative">
                  <Controller
                    name="customerCode"
                    control={control}
                    render={({ field }) => (
                      <select
                        {...field}
                        disabled={!selectedProjectId}
                        className={`input appearance-none pr-9 ${errors.customerCode ? 'border-danger-400' : ''}`}
                        onChange={(e) => {
                          field.onChange(e)
                          setValue('repositoryId', '')
                          setSelectedClientCode(e.target.value)
                        }}
                      >
                        <option value="">
                          {!selectedProjectId
                            ? 'Select a project first'
                            : projectClients.length === 0
                            ? 'No clients assigned to this project'
                            : 'Select a client'}
                        </option>
                        {projectClients.map((c) => (
                          <option key={c.code} value={c.code}>
                            {c.name} — {c.code}
                          </option>
                        ))}
                      </select>
                    )}
                  />
                  <span className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-slate-400">
                    <ChevronDown className="w-4 h-4" />
                  </span>
                </div>
                {errors.customerCode && (
                  <p className="form-error">{errors.customerCode.message}</p>
                )}
              </div>

              {/* Repository — loaded after both project and client are selected */}
              <div>
                <label className="label">Repository</label>
                <div className="relative">
                  <Controller
                    name="repositoryId"
                    control={control}
                    render={({ field }) => (
                      <select
                        {...field}
                        disabled={!selectedProjectId || !selectedClientCode || reposLoading}
                        className="input appearance-none pr-9"
                      >
                        <option value="">
                          {!selectedProjectId
                            ? 'Select a project first'
                            : !selectedClientCode
                            ? 'Select a client first'
                            : reposLoading
                            ? 'Loading repositories…'
                            : repositories.length === 0
                            ? 'No repositories found'
                            : 'Select a repository (optional)'}
                        </option>
                        {repositories.map((r) => (
                          <option key={r.id} value={r.id}>
                            {r.name}
                          </option>
                        ))}
                      </select>
                    )}
                  />
                  <span className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-slate-400">
                    {reposLoading && selectedProjectId && selectedClientCode
                      ? <Loader2 className="w-4 h-4 animate-spin" />
                      : <ChevronDown className="w-4 h-4" />
                    }
                  </span>
                </div>
              </div>

              {/* MAC / IMEI / IP */}
              <div>
                <label className="label">
                  MAC / IMEI / IP <span className="text-danger-500">*</span>
                </label>
                <input
                  type="text"
                  placeholder="e.g. AA:BB:CC:DD:EE:FF  or  192.168.1.10"
                  className={`input font-mono ${errors.macImeiIp ? 'border-danger-400' : ''}`}
                  {...register('macImeiIp')}
                />
                {errors.macImeiIp ? (
                  <p className="form-error">{errors.macImeiIp.message}</p>
                ) : (
                  <p className="text-xs text-slate-400 mt-1">
                    Used as the unique device identifier on this platform.
                  </p>
                )}
              </div>

              {/* Model */}
              <div>
                <label className="label">
                  Model <span className="text-danger-500">*</span>
                </label>
                <input
                  type="text"
                  placeholder="e.g. EDGE-GW-V2"
                  className={`input ${errors.model ? 'border-danger-400' : ''}`}
                  {...register('model')}
                />
                {errors.model && (
                  <p className="form-error">{errors.model.message}</p>
                )}
              </div>

              {/* Initial Firmware Version */}
              <div>
                <label className="label">Initial Firmware Version</label>
                <input
                  type="text"
                  placeholder="e.g. 1.0.0"
                  className="input font-mono"
                  {...register('currentFirmwareVersion')}
                />
                <p className="text-xs text-slate-400 mt-1">
                  Leave blank to default to 0.0.0.
                </p>
              </div>

              {/* Publish Topic */}
              <div>
                <label className="label">Publish Topic</label>
                <input
                  type="text"
                  placeholder="OTA/{MAC}/Status"
                  className="input font-mono text-sm"
                  {...register('publishTopic')}
                />
                <p className="text-xs text-slate-400 mt-1">
                  Auto-filled from MAC / IMEI / IP. You can override it.
                </p>
              </div>

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
                    'Register Device'
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
