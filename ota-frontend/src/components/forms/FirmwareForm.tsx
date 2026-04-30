'use client'

import * as React from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { X, Package, Loader2, Plus, Trash2, UploadCloud, CheckCircle2, AlertCircle } from 'lucide-react'
import * as Dialog from '@radix-ui/react-dialog'
import { useQuery } from '@tanstack/react-query'
import { repositoryService } from '@/services/repository.service'
import { projectService } from '@/services/project.service'
import { clientService } from '@/services/client.service'
import { firmwareService } from '@/services/firmware.service'
import { FirmwareChannel, FirmwareVersion } from '@/types'
import { formatFileSize } from '@/utils/formatters'

// ─── Schema ───────────────────────────────────────────────────────────────────

const firmwareSchema = z.object({
  repositoryId: z.string().min(1, 'Repository is required'),
  version: z.string().min(1, 'Version is required').max(50),
  giteaTagName: z.string().max(100).optional(),
  channel: z.nativeEnum(FirmwareChannel),
  releaseNotes: z.string().max(10000).optional(),
  fileName: z.string().max(255).optional(),
  storedFileName: z.string().max(512).optional(),
  fileSha256: z.string().max(64).optional(),
  fileSizeBytes: z.coerce.number().min(0).optional(),
  isMandate: z.boolean(),
  checkTrial: z.boolean(),
  minRequiredVersion: z.string().max(50).optional(),
  maxAllowedVersion: z.string().max(50).optional(),
  supportedModels: z.array(z.string()).optional(),
})

type FirmwareFormValues = z.infer<typeof firmwareSchema>

// For edit, only these fields are updatable
const editSchema = z.object({
  releaseNotes: z.string().max(10000).optional(),
  isMandate: z.boolean().optional(),
  minRequiredVersion: z.string().max(50).optional(),
  maxAllowedVersion: z.string().max(50).optional(),
  supportedModels: z.array(z.string()).optional(),
})

type EditFormValues = z.infer<typeof editSchema>

// ─── File upload zone ─────────────────────────────────────────────────────────

type UploadState =
  | { status: 'idle' }
  | { status: 'uploading'; progress: number }
  | { status: 'done'; fileName: string; storedFileName: string; fileSizeBytes: number; fileSha256: string; downloadUrl: string }
  | { status: 'error'; message: string }

function FileUploadZone({
  onUploaded,
}: {
  onUploaded: (result: { fileName: string; storedFileName: string; fileSizeBytes: number; fileSha256: string; downloadUrl: string }) => void
}) {
  const [state, setState] = React.useState<UploadState>({ status: 'idle' })
  const inputRef = React.useRef<HTMLInputElement>(null)
  const [dragging, setDragging] = React.useState(false)

  const handleFile = async (file: File) => {
    setState({ status: 'uploading', progress: 0 })
    try {
      const result = await firmwareService.uploadFirmwareFile(file)
      setState({
        status: 'done',
        fileName: result.fileName,
        storedFileName: result.storedFileName,
        fileSizeBytes: result.fileSizeBytes,
        fileSha256: result.fileSha256,
        downloadUrl: result.downloadUrl,
      })
      onUploaded(result)
    } catch (err: any) {
      setState({ status: 'error', message: err?.response?.data?.message ?? 'Upload failed' })
    }
  }

  const onDrop = (e: React.DragEvent) => {
    e.preventDefault()
    setDragging(false)
    const file = e.dataTransfer.files[0]
    if (file) handleFile(file)
  }

  const onInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (file) handleFile(file)
  }

  return (
    <div>
      <input
        ref={inputRef}
        type="file"
        className="hidden"
        accept=".bin,.hex,.img,.elf,.fw,.zip,.gz,.tar,.tar.gz"
        onChange={onInputChange}
      />

      {state.status === 'idle' || state.status === 'error' ? (
        <div
          onClick={() => inputRef.current?.click()}
          onDragOver={(e) => { e.preventDefault(); setDragging(true) }}
          onDragLeave={() => setDragging(false)}
          onDrop={onDrop}
          className={`flex flex-col items-center justify-center gap-2 border-2 border-dashed rounded-xl p-6 cursor-pointer transition-colors ${
            dragging
              ? 'border-accent-400 bg-accent-50'
              : 'border-slate-200 hover:border-accent-300 hover:bg-slate-50'
          }`}
        >
          <UploadCloud className={`w-8 h-8 ${dragging ? 'text-accent-500' : 'text-slate-300'}`} />
          <div className="text-center">
            <p className="text-sm font-medium text-primary-700">
              Drop firmware binary here or <span className="text-accent-600">browse</span>
            </p>
            <p className="text-xs text-slate-400 mt-0.5">.bin .hex .img .elf .fw .zip .gz — up to 512 MB</p>
          </div>
          {state.status === 'error' && (
            <div className="flex items-center gap-1.5 text-xs text-danger-600 mt-1">
              <AlertCircle className="w-3.5 h-3.5" />
              {state.message}
            </div>
          )}
        </div>
      ) : state.status === 'uploading' ? (
        <div className="flex flex-col items-center gap-3 border-2 border-dashed border-accent-200 rounded-xl p-6 bg-accent-50">
          <Loader2 className="w-7 h-7 text-accent-500 animate-spin" />
          <p className="text-sm text-accent-700 font-medium">Uploading and computing checksum…</p>
        </div>
      ) : (
        /* done */
        <div className="flex items-start gap-3 border-2 border-success-200 bg-success-50 rounded-xl p-4">
          <CheckCircle2 className="w-5 h-5 text-success-500 flex-shrink-0 mt-0.5" />
          <div className="flex-1 min-w-0 text-sm">
            <p className="font-medium text-success-800 truncate">{state.fileName}</p>
            <p className="text-xs text-success-600 mt-0.5">
              {formatFileSize(state.fileSizeBytes)} &nbsp;·&nbsp;
              <code className="font-mono">{state.fileSha256.slice(0, 16)}…</code>
            </p>
          </div>
          <button
            type="button"
            onClick={() => { setState({ status: 'idle' }); if (inputRef.current) inputRef.current.value = '' }}
            className="text-success-400 hover:text-success-700 transition-colors flex-shrink-0"
            title="Remove file"
          >
            <X className="w-4 h-4" />
          </button>
        </div>
      )}
    </div>
  )
}

// ─── Tag input helper ─────────────────────────────────────────────────────────

function TagInput({
  value,
  onChange,
  placeholder,
}: {
  value: string[]
  onChange: (v: string[]) => void
  placeholder?: string
}) {
  const [input, setInput] = React.useState('')

  const add = () => {
    const trimmed = input.trim()
    if (trimmed && !value.includes(trimmed)) {
      onChange([...value, trimmed])
    }
    setInput('')
  }

  return (
    <div>
      <div className="flex gap-2 mb-2">
        <input
          type="text"
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); add() } }}
          placeholder={placeholder}
          className="input flex-1 text-sm"
        />
        <button type="button" onClick={add} className="btn-secondary px-3">
          <Plus className="w-3.5 h-3.5" />
        </button>
      </div>
      {value.length > 0 && (
        <div className="flex flex-wrap gap-1.5">
          {value.map((tag) => (
            <span
              key={tag}
              className="inline-flex items-center gap-1 px-2 py-0.5 bg-accent-50 text-accent-700 rounded-full text-xs font-mono border border-accent-200"
            >
              {tag}
              <button type="button" onClick={() => onChange(value.filter((t) => t !== tag))}>
                <Trash2 className="w-3 h-3" />
              </button>
            </span>
          ))}
        </div>
      )}
    </div>
  )
}

// ─── Create Firmware Form ─────────────────────────────────────────────────────

interface CreateFirmwareFormProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  preselectedRepositoryId?: string
  onSubmit: (data: FirmwareFormValues) => Promise<void>
  isLoading?: boolean
}

export function CreateFirmwareForm({
  open,
  onOpenChange,
  preselectedRepositoryId,
  onSubmit,
  isLoading = false,
}: CreateFirmwareFormProps) {
  const {
    register,
    handleSubmit,
    reset,
    setValue,
    watch,
    formState: { errors },
  } = useForm<FirmwareFormValues>({
    resolver: zodResolver(firmwareSchema),
    defaultValues: {
      repositoryId: preselectedRepositoryId ?? '',
      version: '',
      giteaTagName: '',
      channel: FirmwareChannel.Alpha,
      releaseNotes: '',
      fileName: '',
      storedFileName: '',
      fileSha256: '',
      fileSizeBytes: 0,
      isMandate: false,
      checkTrial: false,
      minRequiredVersion: '',
      maxAllowedVersion: '',
      supportedModels: [],
    },
  })

  const supportedModels = watch('supportedModels') ?? []

  const [selectedClientCode, setSelectedClientCode] = React.useState('')
  const [selectedProjectId, setSelectedProjectId] = React.useState('')

  const { data: clients } = useQuery({
    queryKey: ['clients-all'],
    queryFn: () => clientService.getClients({ pageSize: 200 }),
    enabled: !preselectedRepositoryId,
  })

  const { data: projects } = useQuery({
    queryKey: ['projects-all'],
    queryFn: () => projectService.getProjects({ pageSize: 200 }),
    enabled: !preselectedRepositoryId,
  })

  const selectedProject = React.useMemo(
    () => (projects?.items ?? []).find((p) => p.id === selectedProjectId) ?? null,
    [projects, selectedProjectId]
  )

  const filteredClients = React.useMemo(() => {
    const all = clients?.items ?? []
    if (!selectedProject) return []
    const codes = new Set<string>()
    if (selectedProject.customerId) codes.add(selectedProject.customerId)
    for (const c of selectedProject.clients ?? []) codes.add(c.code)
    return all.filter((c) => codes.has(c.code))
  }, [clients, selectedProject])

  const { data: repos } = useQuery({
    queryKey: ['repositories-by-project', selectedProjectId],
    queryFn: () => repositoryService.getRepositories({ projectId: selectedProjectId || undefined, pageSize: 200 }),
    enabled: !preselectedRepositoryId && !!selectedProjectId && !!selectedClientCode,
  })

  const selectedClientName = React.useMemo(
    () => (clients?.items ?? []).find((c) => c.code === selectedClientCode)?.name ?? null,
    [clients, selectedClientCode]
  )

  // Match the repo's per-repo client tag first (clientCode); fall back to clientName for repos
  // registered before per-repo client tagging existed.
  const filteredRepos = React.useMemo(() => {
    const all = repos?.items ?? []
    if (!selectedClientCode) return all
    return all.filter((r) => {
      if (r.clientCode) return r.clientCode === selectedClientCode
      if (r.clientName && selectedClientName) return r.clientName === selectedClientName
      return false
    })
  }, [repos, selectedClientCode, selectedClientName])

  React.useEffect(() => {
    if (open) {
      setSelectedClientCode('')
      setSelectedProjectId('')
      reset({
        repositoryId: preselectedRepositoryId ?? '',
        version: '',
        giteaTagName: '',
        channel: FirmwareChannel.Alpha,
        releaseNotes: '',
        fileName: '',
        storedFileName: '',
        fileSha256: '',
        fileSizeBytes: 0,
        isMandate: false,
        checkTrial: false,
        minRequiredVersion: '',
        maxAllowedVersion: '',
        supportedModels: [],
      })
    }
  }, [open, preselectedRepositoryId, reset])

  const handleFormSubmit = async (values: FirmwareFormValues) => {
    await onSubmit(values)
  }

  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50" />
        <Dialog.Content className="fixed left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 bg-white rounded-2xl shadow-2xl z-50 w-full max-w-2xl max-h-[90vh] overflow-y-auto">
          <div className="p-6">
            {/* Header */}
            <div className="flex items-center justify-between mb-6">
              <div>
                <Dialog.Title className="text-lg font-semibold text-primary-900 flex items-center gap-2">
                  <Package className="w-5 h-5 text-accent-600" />
                  Add Firmware Version
                </Dialog.Title>
                <Dialog.Description className="text-sm text-slate-500 mt-0.5">
                  Register a new firmware version in Draft status
                </Dialog.Description>
              </div>
              <Dialog.Close className="text-slate-400 hover:text-slate-600 transition-colors">
                <X className="w-5 h-5" />
              </Dialog.Close>
            </div>

            <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-4">
              {/* Project + Client + Repository */}
              {!preselectedRepositoryId && (
                <div>
                  <label className="label">
                    Project <span className="text-danger-500">*</span>
                  </label>
                  <select
                    className="input"
                    value={selectedProjectId}
                    onChange={(e) => {
                      setSelectedProjectId(e.target.value)
                      setSelectedClientCode('')
                      setValue('repositoryId', '')
                    }}
                  >
                    <option value="">Select a project...</option>
                    {(projects?.items ?? []).map((p) => (
                      <option key={p.id} value={p.id}>{p.name}</option>
                    ))}
                  </select>
                </div>
              )}

              {!preselectedRepositoryId && (
                <div>
                  <label className="label">
                    Client <span className="text-danger-500">*</span>
                  </label>
                  <select
                    className={`input ${!selectedProjectId ? 'bg-slate-50 text-slate-400 cursor-not-allowed' : ''}`}
                    disabled={!selectedProjectId}
                    value={selectedClientCode}
                    onChange={(e) => {
                      setSelectedClientCode(e.target.value)
                      setValue('repositoryId', '')
                    }}
                  >
                    <option value="">
                      {selectedProjectId ? 'Select a client...' : 'Select a project first...'}
                    </option>
                    {filteredClients.map((c) => (
                      <option key={c.id} value={c.code}>{c.name}</option>
                    ))}
                  </select>
                </div>
              )}

              <div>
                <label className="label">
                  Repository <span className="text-danger-500">*</span>
                </label>
                {preselectedRepositoryId ? (
                  <input
                    type="text"
                    value={preselectedRepositoryId}
                    disabled
                    className="input bg-slate-50 text-slate-500 cursor-not-allowed"
                  />
                ) : (
                  <select
                    className={`input ${errors.repositoryId ? 'border-danger-400' : ''} ${!selectedProjectId || !selectedClientCode ? 'bg-slate-50 text-slate-400 cursor-not-allowed' : ''}`}
                    disabled={!selectedProjectId || !selectedClientCode}
                    {...register('repositoryId')}
                  >
                    <option value="">
                      {!selectedProjectId
                        ? 'Select a project first...'
                        : !selectedClientCode
                          ? 'Select a client first...'
                          : 'Select a repository...'}
                    </option>
                    {filteredRepos.map((r) => (
                      <option key={r.id} value={r.id}>
                        {r.giteaOwner}/{r.giteaRepo ?? r.name}
                      </option>
                    ))}
                  </select>
                )}
                {errors.repositoryId && <p className="form-error">{errors.repositoryId.message}</p>}
              </div>

              {/* Version + Tag */}
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="label">
                    Version <span className="text-danger-500">*</span>
                  </label>
                  <input
                    type="text"
                    placeholder="2.4.1"
                    className={`input font-mono ${errors.version ? 'border-danger-400' : ''}`}
                    {...register('version')}
                  />
                  {errors.version && <p className="form-error">{errors.version.message}</p>}
                </div>
                <div>
                  <label className="label">Gitea Tag</label>
                  <input
                    type="text"
                    placeholder="v2.4.1"
                    className="input font-mono"
                    {...register('giteaTagName')}
                  />
                </div>
              </div>

              {/* Channel + flags */}
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="label">Channel</label>
                  <select className="input" {...register('channel')}>
                    {Object.values(FirmwareChannel).map((c) => (
                      <option key={c} value={c}>{c}</option>
                    ))}
                  </select>
                </div>
                <div className="flex flex-col gap-3 justify-end pb-1">
                  <label className="flex items-center gap-2 cursor-pointer select-none">
                    <input
                      type="checkbox"
                      className="w-4 h-4 accent-accent-600"
                      {...register('isMandate')}
                    />
                    <span className="text-sm font-medium text-primary-800">Mandatory update</span>
                  </label>
                </div>
              </div>

              {/* File Upload */}
              <div>
                <label className="label">Firmware Binary</label>
                <FileUploadZone
                  onUploaded={(result) => {
                    setValue('fileName', result.fileName)
                    setValue('storedFileName', result.storedFileName)
                    setValue('fileSha256', result.fileSha256)
                    setValue('fileSizeBytes', result.fileSizeBytes)
                  }}
                />
              </div>

              {/* Version constraints */}
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="label">Min Required Version</label>
                  <input
                    type="text"
                    placeholder="e.g. 2.0.0"
                    className="input font-mono"
                    {...register('minRequiredVersion')}
                  />
                </div>
                <div>
                  <label className="label">Max Allowed Version</label>
                  <input
                    type="text"
                    placeholder="e.g. 2.3.9"
                    className="input font-mono"
                    {...register('maxAllowedVersion')}
                  />
                </div>
              </div>

              {/* Supported Models */}
              <div>
                <label className="label">Supported Models</label>
                <TagInput
                  value={supportedModels}
                  onChange={(v) => setValue('supportedModels', v)}
                  placeholder="e.g. EDGE-GW-V1 (press Enter to add)"
                />
              </div>

              {/* Release Notes */}
              <div>
                <label className="label">Release Notes</label>
                <textarea
                  placeholder="Describe the changes in this version..."
                  rows={4}
                  className="input resize-none font-mono text-sm"
                  {...register('releaseNotes')}
                />
              </div>

              {/* Actions */}
              <div className="flex items-center justify-end gap-3 pt-4 border-t border-slate-100">
                <Dialog.Close className="btn-secondary">Cancel</Dialog.Close>
                <button type="submit" disabled={isLoading} className="btn-primary">
                  {isLoading ? (
                    <><Loader2 className="w-4 h-4 animate-spin" /> Adding...</>
                  ) : (
                    <><Package className="w-4 h-4" /> Add Firmware</>
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

// ─── Edit Firmware Form ───────────────────────────────────────────────────────

interface EditFirmwareFormProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  firmware: FirmwareVersion
  onSubmit: (data: EditFormValues) => Promise<void>
  isLoading?: boolean
}

export function EditFirmwareForm({
  open,
  onOpenChange,
  firmware,
  onSubmit,
  isLoading = false,
}: EditFirmwareFormProps) {
  const {
    register,
    handleSubmit,
    reset,
    setValue,
    watch,
    formState: { errors },
  } = useForm<EditFormValues>({
    resolver: zodResolver(editSchema),
    defaultValues: {
      releaseNotes: firmware.releaseNotes ?? '',
      isMandate: firmware.isMandate ?? false,
      minRequiredVersion: firmware.minRequiredVersion ?? '',
      maxAllowedVersion: firmware.maxAllowedVersion ?? '',
      supportedModels: firmware.supportedModels ?? [],
    },
  })

  const supportedModels = watch('supportedModels') ?? []

  React.useEffect(() => {
    if (open) {
      reset({
        releaseNotes: firmware.releaseNotes ?? '',
        isMandate: firmware.isMandate ?? false,
        minRequiredVersion: firmware.minRequiredVersion ?? '',
        maxAllowedVersion: firmware.maxAllowedVersion ?? '',
        supportedModels: firmware.supportedModels ?? [],
      })
    }
  }, [open, firmware, reset])

  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50" />
        <Dialog.Content className="fixed left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 bg-white rounded-2xl shadow-2xl z-50 w-full max-w-lg max-h-[90vh] overflow-y-auto">
          <div className="p-6">
            <div className="flex items-center justify-between mb-6">
              <div>
                <Dialog.Title className="text-lg font-semibold text-primary-900 flex items-center gap-2">
                  <Package className="w-5 h-5 text-accent-600" />
                  Edit Firmware
                </Dialog.Title>
                <Dialog.Description className="text-sm text-slate-500 mt-0.5">
                  Editing <span className="font-mono font-semibold text-accent-600">{firmware.version}</span>
                </Dialog.Description>
              </div>
              <Dialog.Close className="text-slate-400 hover:text-slate-600 transition-colors">
                <X className="w-5 h-5" />
              </Dialog.Close>
            </div>

            <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
              {/* Mandate */}
              <div>
                <label className="flex items-center gap-2 cursor-pointer select-none">
                  <input
                    type="checkbox"
                    className="w-4 h-4 accent-accent-600"
                    {...register('isMandate')}
                  />
                  <span className="text-sm font-medium text-primary-800">Mandatory update</span>
                </label>
              </div>

              {/* Version constraints */}
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="label">Min Required Version</label>
                  <input
                    type="text"
                    placeholder="e.g. 2.0.0"
                    className="input font-mono"
                    {...register('minRequiredVersion')}
                  />
                </div>
                <div>
                  <label className="label">Max Allowed Version</label>
                  <input
                    type="text"
                    placeholder="e.g. 2.3.9"
                    className="input font-mono"
                    {...register('maxAllowedVersion')}
                  />
                </div>
              </div>

              {/* Supported Models */}
              <div>
                <label className="label">Supported Models</label>
                <TagInput
                  value={supportedModels}
                  onChange={(v) => setValue('supportedModels', v)}
                  placeholder="e.g. EDGE-GW-V1 (press Enter to add)"
                />
              </div>

              {/* Release Notes */}
              <div>
                <label className="label">Release Notes</label>
                <textarea
                  placeholder="Describe the changes in this version..."
                  rows={5}
                  className="input resize-none font-mono text-sm"
                  {...register('releaseNotes')}
                />
              </div>

              <div className="flex items-center justify-end gap-3 pt-4 border-t border-slate-100">
                <Dialog.Close className="btn-secondary">Cancel</Dialog.Close>
                <button type="submit" disabled={isLoading} className="btn-primary">
                  {isLoading ? (
                    <><Loader2 className="w-4 h-4 animate-spin" /> Saving...</>
                  ) : (
                    'Save Changes'
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
