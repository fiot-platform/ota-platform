'use client'

import * as React from 'react'
import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import * as Dialog from '@radix-ui/react-dialog'
import { X, ChevronRight, ChevronLeft, Loader2, CheckCircle } from 'lucide-react'
import { otaService } from '@/services/ota.service'
import { firmwareService } from '@/services/firmware.service'
import { projectService } from '@/services/project.service'
import { useToast } from '@/components/ui/ToastProvider'
import { CreateRolloutRequest, RolloutTargetType, FirmwareStatus } from '@/types'

// ─── Schema ───────────────────────────────────────────────────────────────────

const rolloutSchema = z.object({
  name: z.string().min(2, 'Name must be at least 2 characters').max(100),
  description: z.string().max(500).optional(),
  projectId: z.string().min(1, 'Project is required'),
  firmwareVersionId: z.string().min(1, 'Firmware version is required'),
  targetType: z.nativeEnum(RolloutTargetType),
  targetIds: z.array(z.string()).optional(),
  policyId: z.string().optional(),
  scheduledAt: z.string().optional(),
})

type RolloutFormValues = z.infer<typeof rolloutSchema>

interface CreateRolloutFormProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSuccess?: () => void
}

const STEPS = ['Basic Info', 'Target', 'Policy & Schedule']

// ─── Step 1: Basic Info ───────────────────────────────────────────────────────

function Step1({
  register,
  errors,
  control,
  watch,
}: {
  register: ReturnType<typeof useForm<RolloutFormValues>>['register']
  errors: ReturnType<typeof useForm<RolloutFormValues>>['formState']['errors']
  control: ReturnType<typeof useForm<RolloutFormValues>>['control']
  watch: ReturnType<typeof useForm<RolloutFormValues>>['watch']
}) {
  const projectId = watch('projectId')

  const { data: projects } = useQuery({
    queryKey: ['projects-all'],
    queryFn: () => projectService.getProjects({ pageSize: 200 }),
  })

  const { data: firmwareList } = useQuery({
    queryKey: ['firmware-approved', projectId],
    queryFn: () => firmwareService.getFirmwareList({
      projectId: projectId || undefined,
      status: FirmwareStatus.Approved,
      pageSize: 100,
    }),
    enabled: Boolean(projectId),
  })

  return (
    <div className="space-y-4">
      <div>
        <label className="label">Rollout Name <span className="text-danger-500">*</span></label>
        <input type="text" placeholder="e.g., Production Firmware v2.1.0" className={`input ${errors.name ? 'border-danger-400' : ''}`} {...register('name')} />
        {errors.name && <p className="form-error">{errors.name.message}</p>}
      </div>

      <div>
        <label className="label">Description</label>
        <textarea placeholder="What does this rollout include?" rows={3} className="input resize-none" {...register('description')} />
      </div>

      <div>
        <label className="label">Project <span className="text-danger-500">*</span></label>
        <Controller
          name="projectId"
          control={control}
          render={({ field }) => (
            <select {...field} className={`input ${errors.projectId ? 'border-danger-400' : ''}`}>
              <option value="">Select a project...</option>
              {(projects?.items ?? []).map((p) => (
                <option key={p.id} value={p.id}>{p.name}</option>
              ))}
            </select>
          )}
        />
        {errors.projectId && <p className="form-error">{errors.projectId.message}</p>}
      </div>

      <div>
        <label className="label">Firmware Version <span className="text-danger-500">*</span></label>
        <Controller
          name="firmwareVersionId"
          control={control}
          render={({ field }) => (
            <select {...field} className={`input ${errors.firmwareVersionId ? 'border-danger-400' : ''}`} disabled={!projectId}>
              <option value="">{projectId ? 'Select approved firmware...' : 'Select a project first'}</option>
              {(firmwareList?.items ?? []).map((fw) => (
                <option key={fw.id} value={fw.id}>{fw.version} — {fw.channel}</option>
              ))}
            </select>
          )}
        />
        {errors.firmwareVersionId && <p className="form-error">{errors.firmwareVersionId.message}</p>}
        {projectId && (firmwareList?.items ?? []).length === 0 && (
          <p className="text-xs text-warning-600 mt-1">No approved firmware versions found for this project.</p>
        )}
      </div>
    </div>
  )
}

// ─── Step 2: Target ───────────────────────────────────────────────────────────

function Step2({
  register,
  control,
  watch,
}: {
  register: ReturnType<typeof useForm<RolloutFormValues>>['register']
  control: ReturnType<typeof useForm<RolloutFormValues>>['control']
  watch: ReturnType<typeof useForm<RolloutFormValues>>['watch']
}) {
  const targetType = watch('targetType')
  const [targetInput, setTargetInput] = React.useState('')
  const [targetList, setTargetList] = React.useState<string[]>([])

  const targetOptions = [
    { value: RolloutTargetType.AllDevices, label: 'All Devices', description: 'Deploy to all registered devices in the project' },
    { value: RolloutTargetType.DeviceGroup, label: 'Device Group', description: 'Deploy to a specific group of devices' },
    { value: RolloutTargetType.Site, label: 'Site', description: 'Deploy to all devices at specific sites' },
    { value: RolloutTargetType.Channel, label: 'Channel', description: 'Deploy to devices subscribed to a channel' },
    { value: RolloutTargetType.SpecificDevices, label: 'Specific Devices', description: 'Deploy to manually specified device IDs' },
  ]

  const showTargetIds = targetType !== RolloutTargetType.AllDevices

  return (
    <div className="space-y-4">
      <div>
        <label className="label">Target Type <span className="text-danger-500">*</span></label>
        <Controller
          name="targetType"
          control={control}
          render={({ field }) => (
            <div className="space-y-2">
              {targetOptions.map((opt) => (
                <label
                  key={opt.value}
                  className={`flex items-start gap-3 p-3 rounded-lg border-2 cursor-pointer transition-all ${
                    field.value === opt.value
                      ? 'border-accent-500 bg-accent-50'
                      : 'border-slate-200 hover:border-slate-300'
                  }`}
                >
                  <input
                    type="radio"
                    value={opt.value}
                    checked={field.value === opt.value}
                    onChange={() => field.onChange(opt.value)}
                    className="mt-0.5 accent-accent-600"
                  />
                  <div>
                    <p className={`text-sm font-semibold ${field.value === opt.value ? 'text-accent-700' : 'text-primary-800'}`}>
                      {opt.label}
                    </p>
                    <p className="text-xs text-slate-500 mt-0.5">{opt.description}</p>
                  </div>
                </label>
              ))}
            </div>
          )}
        />
      </div>

      {showTargetIds && (
        <div>
          <label className="label">Target IDs</label>
          <div className="flex gap-2 mb-2">
            <input
              type="text"
              value={targetInput}
              onChange={(e) => setTargetInput(e.target.value)}
              placeholder="Enter ID and press Enter"
              className="input flex-1"
              onKeyDown={(e) => {
                if (e.key === 'Enter') {
                  e.preventDefault()
                  const id = targetInput.trim()
                  if (id && !targetList.includes(id)) {
                    setTargetList([...targetList, id])
                    setTargetInput('')
                  }
                }
              }}
            />
          </div>
          {targetList.length > 0 && (
            <div className="flex flex-wrap gap-2 p-3 bg-slate-50 rounded-lg">
              {targetList.map((tid) => (
                <span
                  key={tid}
                  className="inline-flex items-center gap-1 px-2.5 py-0.5 bg-accent-100 text-accent-800 rounded-full text-xs font-mono"
                >
                  {tid}
                  <button
                    type="button"
                    onClick={() => setTargetList(targetList.filter((t) => t !== tid))}
                    className="text-accent-500 hover:text-accent-700"
                  >
                    <X className="w-3 h-3" />
                  </button>
                </span>
              ))}
            </div>
          )}
          <p className="text-xs text-slate-400 mt-1">
            {targetType === RolloutTargetType.SpecificDevices ? 'Enter device IDs' :
              targetType === RolloutTargetType.Site ? 'Enter site IDs' :
                targetType === RolloutTargetType.Channel ? 'Enter channel names' : 'Enter group IDs'}
          </p>
        </div>
      )}
    </div>
  )
}

// ─── Step 3: Policy & Schedule ────────────────────────────────────────────────

function Step3({
  register,
  control,
}: {
  register: ReturnType<typeof useForm<RolloutFormValues>>['register']
  control: ReturnType<typeof useForm<RolloutFormValues>>['control']
}) {
  const { data: policies } = useQuery({
    queryKey: ['rollout-policies'],
    queryFn: () => otaService.getPolicies({ pageSize: 50 }),
  })

  return (
    <div className="space-y-4">
      <div>
        <label className="label">Rollout Policy</label>
        <Controller
          name="policyId"
          control={control}
          render={({ field }) => (
            <select {...field} className="input">
              <option value="">Default policy</option>
              {(policies?.items ?? []).map((p) => (
                <option key={p.id} value={p.id}>{p.name}{p.description ? ` — ${p.description}` : ''}</option>
              ))}
            </select>
          )}
        />
        <p className="text-xs text-slate-400 mt-1">Controls retry behavior, parallelism, and failure thresholds.</p>
      </div>

      <div>
        <label className="label">Scheduled Start (Optional)</label>
        <input
          type="datetime-local"
          className="input"
          {...register('scheduledAt')}
        />
        <p className="text-xs text-slate-400 mt-1">Leave empty to start immediately when manually triggered.</p>
      </div>

      <div className="p-4 bg-accent-50 border border-accent-200 rounded-lg text-sm">
        <p className="font-semibold text-accent-800 mb-1">Ready to create rollout</p>
        <p className="text-accent-600">
          Review your settings above. The rollout will be created in <strong>Draft</strong> status and must be manually started.
        </p>
      </div>
    </div>
  )
}

// ─── Main Form Component ──────────────────────────────────────────────────────

export function CreateRolloutForm({ open, onOpenChange, onSuccess }: CreateRolloutFormProps) {
  const [step, setStep] = React.useState(0)
  const { toast } = useToast()
  const queryClient = useQueryClient()

  const {
    register,
    handleSubmit,
    watch,
    control,
    reset,
    formState: { errors },
  } = useForm<RolloutFormValues>({
    resolver: zodResolver(rolloutSchema),
    defaultValues: {
      name: '',
      description: '',
      projectId: '',
      firmwareVersionId: '',
      targetType: RolloutTargetType.AllDevices,
      targetIds: [],
      policyId: '',
      scheduledAt: '',
    },
  })

  const mutation = useMutation({
    mutationFn: (data: CreateRolloutRequest) => otaService.createRollout(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rollouts'] })
      toast({ title: 'Rollout created', description: 'The rollout has been created in Draft status.', variant: 'success' })
      onSuccess?.()
      onOpenChange(false)
      reset()
      setStep(0)
    },
    onError: () => toast({ title: 'Failed to create rollout', variant: 'error' }),
  })

  React.useEffect(() => {
    if (!open) { reset(); setStep(0) }
  }, [open, reset])

  const onSubmit = (values: RolloutFormValues) => {
    mutation.mutate({
      name: values.name,
      description: values.description,
      projectId: values.projectId,
      firmwareVersionId: values.firmwareVersionId,
      targetType: values.targetType,
      targetIds: values.targetIds,
      policyId: values.policyId || undefined,
      scheduledAt: values.scheduledAt || undefined,
    })
  }

  const canProceed = () => {
    const values = watch()
    if (step === 0) return values.name.length >= 2 && values.projectId && values.firmwareVersionId
    return true
  }

  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50" />
        <Dialog.Content className="fixed left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 bg-white rounded-2xl shadow-2xl z-50 w-full max-w-lg max-h-[90vh] overflow-y-auto">
          <div className="p-6">
            {/* Header */}
            <div className="flex items-center justify-between mb-6">
              <div>
                <Dialog.Title className="text-lg font-semibold text-primary-900">Create OTA Rollout</Dialog.Title>
                <Dialog.Description className="text-sm text-slate-500">Step {step + 1} of {STEPS.length}: {STEPS[step]}</Dialog.Description>
              </div>
              <Dialog.Close className="text-slate-400 hover:text-slate-600">
                <X className="w-5 h-5" />
              </Dialog.Close>
            </div>

            {/* Step Progress */}
            <div className="flex items-center gap-2 mb-6">
              {STEPS.map((s, i) => (
                <React.Fragment key={s}>
                  <div className="flex items-center gap-2">
                    <div className={`w-7 h-7 rounded-full flex items-center justify-center text-xs font-bold transition-colors ${
                      i < step ? 'bg-success-500 text-white' :
                        i === step ? 'bg-accent-600 text-white' : 'bg-slate-200 text-slate-500'
                    }`}>
                      {i < step ? <CheckCircle className="w-4 h-4" /> : i + 1}
                    </div>
                    <span className={`text-xs font-medium hidden sm:block ${i === step ? 'text-accent-700' : 'text-slate-400'}`}>{s}</span>
                  </div>
                  {i < STEPS.length - 1 && <div className={`flex-1 h-0.5 ${i < step ? 'bg-success-300' : 'bg-slate-200'}`} />}
                </React.Fragment>
              ))}
            </div>

            {/* Step Content */}
            <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
              {step === 0 && <Step1 register={register} errors={errors} control={control} watch={watch} />}
              {step === 1 && <Step2 register={register} control={control} watch={watch} />}
              {step === 2 && <Step3 register={register} control={control} />}

              {/* Actions */}
              <div className="flex items-center justify-between pt-4 border-t border-slate-100">
                <button
                  type="button"
                  onClick={() => step > 0 ? setStep(step - 1) : onOpenChange(false)}
                  className="btn-secondary"
                >
                  {step === 0 ? <><X className="w-4 h-4" /> Cancel</> : <><ChevronLeft className="w-4 h-4" /> Back</>}
                </button>

                {step < STEPS.length - 1 ? (
                  <button
                    type="button"
                    onClick={() => setStep(step + 1)}
                    disabled={!canProceed()}
                    className="btn-primary disabled:opacity-50"
                  >
                    Next <ChevronRight className="w-4 h-4" />
                  </button>
                ) : (
                  <button type="submit" disabled={mutation.isPending} className="btn-primary">
                    {mutation.isPending ? (
                      <><Loader2 className="w-4 h-4 animate-spin" /> Creating...</>
                    ) : (
                      <><CheckCircle className="w-4 h-4" /> Create Rollout</>
                    )}
                  </button>
                )}
              </div>
            </form>
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  )
}
