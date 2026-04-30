'use client'

import * as React from 'react'
import { useQuery, useMutation } from '@tanstack/react-query'
import { X, Loader2, ChevronDown, Building2, GitBranch, CheckCircle2, AlertCircle } from 'lucide-react'
import { clientService } from '@/services/client.service'
import { repositoryService } from '@/services/repository.service'
import { projectService } from '@/services/project.service'
import { firmwareService } from '@/services/firmware.service'
import { useToast } from '@/components/ui/ToastProvider'
import { Client, FirmwareVersion, Repository } from '@/types'

// ─── Stages ──────────────────────────────────────────────────────────────────
type Stage = 'select' | 'confirm'

interface Props {
  open: boolean
  onClose: () => void
  firmware: FirmwareVersion
  /** Called with the result of the copy operation when it completes. */
  onCopied?: () => void
}

export function CopyFirmwareToClientsDialog({ open, onClose, firmware, onCopied }: Props) {
  const { toast } = useToast()
  const [stage, setStage] = React.useState<Stage>('select')

  // Modal-1 state
  const [selectedClientCodes, setSelectedClientCodes] = React.useState<string[]>([])
  const [activeClientCode, setActiveClientCode] = React.useState<string | null>(null)
  // Map: clientCode -> Set of repoIds chosen inside that client's tab
  const [repoSelection, setRepoSelection] = React.useState<Record<string, Set<string>>>({})
  const [clientPickerOpen, setClientPickerOpen] = React.useState(false)

  // Modal-2 state — checkbox per (clientCode, repoId) pair, defaults all true
  const [confirmCheckboxes, setConfirmCheckboxes] = React.useState<Record<string, boolean>>({})

  // Reset on open
  React.useEffect(() => {
    if (!open) return
    setStage('select')
    setSelectedClientCodes([])
    setActiveClientCode(null)
    setRepoSelection({})
    setConfirmCheckboxes({})
    setClientPickerOpen(false)
  }, [open])

  // ── Data ──────────────────────────────────────────────────────────────────
  // Parent project — gives us the list of clients linked to this project so
  // the picker only shows clients that actually belong here.
  const { data: project, isLoading: projectLoading } = useQuery({
    queryKey: ['copy-fw-project', firmware.projectId],
    queryFn: () => projectService.getProjectById(firmware.projectId),
    enabled: open && !!firmware.projectId,
  })

  const { data: clientsData, isLoading: clientsLoading } = useQuery({
    queryKey: ['copy-fw-clients'],
    queryFn: () => clientService.getClients({ pageSize: 200 }),
    enabled: open,
  })
  // Only the firmware's parent project's repositories are eligible targets.
  const { data: reposData, isLoading: reposLoading } = useQuery({
    queryKey: ['copy-fw-repos', firmware.projectId],
    queryFn: () => repositoryService.getRepositories({
      projectId: firmware.projectId,
      isActive: true,
      pageSize: 500,
    }),
    enabled: open && !!firmware.projectId,
  })

  const allClients: Client[] = clientsData?.items ?? []
  const repos: Repository[] = reposData?.items ?? []

  // Restrict the picker to clients that belong to this project.
  const projectClientCodes = React.useMemo(() => {
    const set = new Set<string>()
    for (const c of project?.clients ?? []) {
      if (c.code) set.add(c.code)
    }
    return set
  }, [project])

  // The source client owns the firmware's parent repo — exclude it from the
  // picker so users don't accidentally pick "copy to myself".
  // The repository entity carries its owning client via `clientCode` (e.g.
  // "CUSTOM_00001"). `giteaOwner` is just the Gitea org and is unrelated to
  // the platform's client identity.
  const sourceClientCode = React.useMemo(() => {
    const norm = (s?: string | null) => (s ?? '').trim().toUpperCase()

    // 1) Match parent repo from loaded list by id or name → use its clientCode.
    const parentRepo = repos.find((r) =>
      norm(r.id) === norm(firmware.repositoryId)
      || norm(r.name) === norm(firmware.repositoryName)
    )
    if (parentRepo?.clientCode) return norm(parentRepo.clientCode)

    // 2) Fall back to firmware.clientName matching against the loaded clients.
    if (firmware.clientName) {
      const byName = allClients.find((c) => norm(c.name) === norm(firmware.clientName))
      if (byName?.code) return norm(byName.code)
    }

    return ''
  }, [repos, firmware.repositoryId, firmware.repositoryName, firmware.clientName, allClients])

  const clients = React.useMemo(() => {
    const norm = (s?: string | null) => (s ?? '').trim().toUpperCase()
    return allClients.filter((c) =>
      projectClientCodes.has(c.code) &&
      norm(c.code) !== sourceClientCode,
    )
  }, [allClients, projectClientCodes, sourceClientCode])

  // Index repos by their owning client's code (= repo.clientCode). We exclude
  // any repo that's the firmware's own parent repo — copying to itself doesn't
  // make sense.
  const reposByClientCode = React.useMemo(() => {
    const m = new Map<string, Repository[]>()
    for (const r of repos) {
      if (r.id === firmware.repositoryId) continue
      const key = (r.clientCode ?? '').trim()
      if (!key) continue
      // Defensive: only keep repos owned by a client of this project.
      if (!projectClientCodes.has(key)) continue
      if (!m.has(key)) m.set(key, [])
      m.get(key)!.push(r)
    }
    return m
  }, [repos, firmware.repositoryId, projectClientCodes])

  const clientByCode = React.useMemo(() => {
    const m = new Map<string, Client>()
    for (const c of clients) m.set(c.code, c)
    return m
  }, [clients])

  // Show all clients linked to this project, even if a particular client doesn't
  // have any repositories yet — the user can still see them in the picker. The
  // per-client tab body will explain when there's nothing to select.
  const eligibleClients = clients

  // ── Modal 1 helpers ───────────────────────────────────────────────────────
  const toggleClient = (code: string) => {
    setSelectedClientCodes((prev) => {
      if (prev.includes(code)) {
        // Removing a client — drop its repo selection too
        setRepoSelection((rs) => {
          const next = { ...rs }
          delete next[code]
          return next
        })
        const next = prev.filter((c) => c !== code)
        if (activeClientCode === code) setActiveClientCode(next[0] ?? null)
        return next
      }
      if (!activeClientCode) setActiveClientCode(code)
      return [...prev, code]
    })
  }

  const toggleRepoForClient = (clientCode: string, repoId: string) => {
    setRepoSelection((prev) => {
      const cur = new Set(prev[clientCode] ?? [])
      if (cur.has(repoId)) cur.delete(repoId)
      else cur.add(repoId)
      return { ...prev, [clientCode]: cur }
    })
  }

  const totalSelectedRepoCount = React.useMemo(() => {
    let n = 0
    for (const code of selectedClientCodes) n += repoSelection[code]?.size ?? 0
    return n
  }, [selectedClientCodes, repoSelection])

  const proceedToConfirm = () => {
    // Initialise checkboxes — default all checked
    const initial: Record<string, boolean> = {}
    for (const code of selectedClientCodes) {
      const set = repoSelection[code]
      if (!set) continue
      for (const repoId of set) initial[`${code}::${repoId}`] = true
    }
    setConfirmCheckboxes(initial)
    setStage('confirm')
  }

  // ── Mutation ──────────────────────────────────────────────────────────────
  const mutation = useMutation({
    mutationFn: () => {
      const targetIds = Object.entries(confirmCheckboxes)
        .filter(([_, v]) => v)
        .map(([k]) => k.split('::')[1])
      return firmwareService.copyToRepositories(firmware.id, targetIds)
    },
    onSuccess: (res) => {
      const parts: string[] = []
      if (res.createdCount) parts.push(`${res.createdCount} created`)
      if (res.skippedCount) parts.push(`${res.skippedCount} skipped`)
      if (res.failedCount)  parts.push(`${res.failedCount} failed`)
      const summary = parts.join(', ') || 'No targets selected'

      // Build a description listing the skipped/failed reasons (compact)
      const issues = res.results.filter((r) => r.status !== 'created').slice(0, 3)
      const extra = issues.map((r) => `${r.repositoryName}: ${r.reason ?? r.status}`).join(' · ')

      toast({
        title: `Firmware copy: ${summary}`,
        description: extra || undefined,
        variant: res.failedCount > 0 ? 'warning' : 'success',
      })
      onCopied?.()
      onClose()
    },
    onError: (e: any) => {
      toast({
        title: 'Copy failed',
        description: e?.response?.data?.message ?? e?.message ?? 'Unknown error',
        variant: 'error',
      })
    },
  })

  // ── Modal 2 helpers ───────────────────────────────────────────────────────
  const confirmRows = React.useMemo(() => {
    const rows: { key: string; clientName: string; clientCode: string; repo: Repository }[] = []
    for (const code of selectedClientCodes) {
      const client = clientByCode.get(code)
      const set = repoSelection[code]
      if (!set || !client) continue
      for (const repoId of set) {
        const repo = repos.find((r) => r.id === repoId)
        if (!repo) continue
        rows.push({
          key: `${code}::${repoId}`,
          clientName: client.name,
          clientCode: client.code,
          repo,
        })
      }
    }
    return rows
  }, [selectedClientCodes, repoSelection, clientByCode, repos])

  const checkedCount = React.useMemo(
    () => Object.values(confirmCheckboxes).filter(Boolean).length,
    [confirmCheckboxes],
  )

  const toggleConfirm = (key: string) =>
    setConfirmCheckboxes((p) => ({ ...p, [key]: !p[key] }))

  if (!open) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4 animate-fade-in">
      <div className="w-full max-w-3xl bg-white rounded-2xl shadow-xl border border-slate-200 h-[80vh] max-h-[720px] min-h-[520px] flex flex-col">
        {/* Header */}
        <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between flex-shrink-0">
          <div>
            <h3 className="font-semibold text-primary-900">
              {stage === 'select' ? 'Copy firmware to other clients' : 'Confirm copy targets'}
            </h3>
            <p className="text-xs text-slate-500 mt-0.5">
              {stage === 'select'
                ? <>Source firmware: <strong className="text-primary-800">v{firmware.version}</strong></>
                : <>Review the {checkedCount} target{checkedCount === 1 ? '' : 's'} you're about to write to</>}
            </p>
          </div>
          <button
            onClick={onClose}
            disabled={mutation.isPending}
            className="text-slate-400 hover:text-slate-600 disabled:opacity-40 transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Body */}
        <div className="flex-1 overflow-y-auto">
          {stage === 'select' ? (
            <SelectStage
              clients={eligibleClients}
              clientsLoading={clientsLoading || reposLoading || projectLoading}
              reposByClientCode={reposByClientCode}
              selectedClientCodes={selectedClientCodes}
              activeClientCode={activeClientCode}
              setActiveClientCode={setActiveClientCode}
              toggleClient={toggleClient}
              repoSelection={repoSelection}
              toggleRepoForClient={toggleRepoForClient}
              clientByCode={clientByCode}
              clientPickerOpen={clientPickerOpen}
              setClientPickerOpen={setClientPickerOpen}
            />
          ) : (
            <ConfirmStage
              confirmRows={confirmRows}
              confirmCheckboxes={confirmCheckboxes}
              toggleConfirm={toggleConfirm}
            />
          )}
        </div>

        {/* Footer */}
        <div className="px-6 py-4 border-t border-slate-100 flex items-center justify-between flex-shrink-0">
          {stage === 'select' ? (
            <>
              <span className="text-xs text-slate-500">
                {selectedClientCodes.length} client{selectedClientCodes.length === 1 ? '' : 's'},{' '}
                {totalSelectedRepoCount} repository{totalSelectedRepoCount === 1 ? '' : 'ies'} selected
              </span>
              <div className="flex items-center gap-2">
                <button onClick={onClose} className="btn-secondary">Cancel</button>
                <button
                  onClick={proceedToConfirm}
                  disabled={totalSelectedRepoCount === 0}
                  className="btn-primary"
                >
                  Next
                </button>
              </div>
            </>
          ) : (
            <>
              <span className="text-xs text-slate-500">
                {checkedCount} of {confirmRows.length} target{confirmRows.length === 1 ? '' : 's'} selected
              </span>
              <div className="flex items-center gap-2">
                <button
                  onClick={() => setStage('select')}
                  disabled={mutation.isPending}
                  className="btn-secondary"
                >
                  Back
                </button>
                <button
                  onClick={() => mutation.mutate()}
                  disabled={mutation.isPending || checkedCount === 0}
                  className="btn-primary"
                >
                  {mutation.isPending
                    ? <><Loader2 className="w-4 h-4 animate-spin" /> Copying…</>
                    : <>Proceed</>}
                </button>
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  )
}

// ─── Stage 1 ─────────────────────────────────────────────────────────────────
function SelectStage(props: {
  clients: Client[]
  clientsLoading: boolean
  reposByClientCode: Map<string, Repository[]>
  selectedClientCodes: string[]
  activeClientCode: string | null
  setActiveClientCode: (c: string | null) => void
  toggleClient: (code: string) => void
  repoSelection: Record<string, Set<string>>
  toggleRepoForClient: (clientCode: string, repoId: string) => void
  clientByCode: Map<string, Client>
  clientPickerOpen: boolean
  setClientPickerOpen: (b: boolean) => void
}) {
  const {
    clients, clientsLoading, reposByClientCode, selectedClientCodes, activeClientCode,
    setActiveClientCode, toggleClient, repoSelection, toggleRepoForClient,
    clientByCode, clientPickerOpen, setClientPickerOpen,
  } = props

  if (clientsLoading) {
    return (
      <div className="p-6 space-y-2">
        {Array.from({ length: 3 }).map((_, i) => (
          <div key={i} className="h-10 bg-slate-100 rounded animate-pulse" />
        ))}
      </div>
    )
  }

  if (!clients.length) {
    return (
      <div className="p-10 text-center">
        <Building2 className="w-9 h-9 text-slate-300 mx-auto mb-2" />
        <p className="text-sm font-medium text-slate-700">No other clients with active repositories in this project</p>
        <p className="text-xs text-slate-400 mt-1">
          Add another client to this project (or register a repository for them) to enable copying.
        </p>
      </div>
    )
  }

  const activeRepos = activeClientCode ? reposByClientCode.get(activeClientCode) ?? [] : []
  const activeClient = activeClientCode ? clientByCode.get(activeClientCode) : null

  return (
    <div className="p-6 space-y-5">
      {/* Multi-client picker */}
      <div>
        <label className="block text-xs font-semibold text-slate-600 uppercase tracking-wide mb-2">
          Select clients
        </label>
        <div className="relative">
          <button
            type="button"
            onClick={() => setClientPickerOpen(!clientPickerOpen)}
            className="w-full flex items-center justify-between px-3 py-2 text-sm border border-slate-300 rounded-lg bg-white hover:bg-slate-50 transition-colors"
          >
            <span className={selectedClientCodes.length ? 'text-primary-800' : 'text-slate-400'}>
              {selectedClientCodes.length === 0
                ? 'Choose one or more clients'
                : `${selectedClientCodes.length} selected`}
            </span>
            <ChevronDown className={`w-4 h-4 text-slate-400 transition-transform ${clientPickerOpen ? 'rotate-180' : ''}`} />
          </button>
          {clientPickerOpen && (
            <div className="absolute left-0 right-0 top-full mt-1 z-10 bg-white border border-slate-200 rounded-lg shadow-lg max-h-64 overflow-y-auto">
              {clients.map((c) => {
                const checked = selectedClientCodes.includes(c.code)
                return (
                  <label
                    key={c.code}
                    className="flex items-center gap-3 px-3 py-2 cursor-pointer hover:bg-slate-50"
                  >
                    <input
                      type="checkbox"
                      checked={checked}
                      onChange={() => toggleClient(c.code)}
                      className="w-4 h-4 accent-accent-600"
                    />
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium text-primary-800 truncate">{c.name}</p>
                      <p className="text-xs text-slate-400 truncate">
                        {c.code} · {reposByClientCode.get(c.code)?.length ?? 0} repo{(reposByClientCode.get(c.code)?.length ?? 0) === 1 ? '' : 's'}
                      </p>
                    </div>
                  </label>
                )
              })}
            </div>
          )}
        </div>
        {selectedClientCodes.length > 0 && (
          <div className="flex flex-wrap gap-1.5 mt-2">
            {selectedClientCodes.map((code) => {
              const c = clientByCode.get(code)
              if (!c) return null
              return (
                <span
                  key={code}
                  className="inline-flex items-center gap-1.5 bg-accent-50 text-accent-700 border border-accent-200 rounded-full px-2.5 py-0.5 text-xs"
                >
                  {c.name}
                  <button
                    onClick={() => toggleClient(code)}
                    className="hover:text-red-600 transition-colors"
                  >
                    <X className="w-3 h-3" />
                  </button>
                </span>
              )
            })}
          </div>
        )}
      </div>

      {/* Per-client tabs */}
      {selectedClientCodes.length > 0 && (
        <div>
          <label className="block text-xs font-semibold text-slate-600 uppercase tracking-wide mb-2">
            Choose repositories per client
          </label>
          <div className="border border-slate-200 rounded-lg overflow-hidden">
            <div className="flex border-b border-slate-200 bg-slate-50 overflow-x-auto">
              {selectedClientCodes.map((code) => {
                const c = clientByCode.get(code)
                if (!c) return null
                const selectedCount = repoSelection[code]?.size ?? 0
                return (
                  <button
                    key={code}
                    onClick={() => setActiveClientCode(code)}
                    className={`flex items-center gap-2 px-4 py-2.5 text-sm font-medium border-b-2 transition-colors whitespace-nowrap ${
                      activeClientCode === code
                        ? 'border-accent-500 text-accent-700 bg-white'
                        : 'border-transparent text-slate-500 hover:text-slate-700'
                    }`}
                  >
                    <Building2 className="w-3.5 h-3.5" />
                    {c.name}
                    {selectedCount > 0 && (
                      <span className="text-xs font-bold bg-accent-100 text-accent-700 px-1.5 py-0.5 rounded-full">
                        {selectedCount}
                      </span>
                    )}
                  </button>
                )
              })}
            </div>

            <div className="p-3 max-h-72 overflow-y-auto">
              {!activeClient ? (
                <p className="text-sm text-slate-400 text-center py-6">Pick a client tab above</p>
              ) : activeRepos.length === 0 ? (
                <p className="text-sm text-slate-400 text-center py-6">
                  {activeClient.name} has no active repositories.
                </p>
              ) : (
                <div className="space-y-1">
                  {activeRepos.map((r) => {
                    const checked = repoSelection[activeClient.code]?.has(r.id) ?? false
                    return (
                      <label
                        key={r.id}
                        className="flex items-center gap-3 px-3 py-2 cursor-pointer rounded-md hover:bg-slate-50 transition-colors"
                      >
                        <input
                          type="checkbox"
                          checked={checked}
                          onChange={() => toggleRepoForClient(activeClient.code, r.id)}
                          className="w-4 h-4 accent-accent-600"
                        />
                        <GitBranch className="w-4 h-4 text-slate-400 flex-shrink-0" />
                        <div className="flex-1 min-w-0">
                          <p className="text-sm font-medium text-primary-800 truncate">{r.name}</p>
                          {r.projectName && (
                            <p className="text-xs text-slate-400 truncate">{r.projectName}</p>
                          )}
                        </div>
                      </label>
                    )
                  })}
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

// ─── Stage 2 ─────────────────────────────────────────────────────────────────
function ConfirmStage({
  confirmRows,
  confirmCheckboxes,
  toggleConfirm,
}: {
  confirmRows: { key: string; clientName: string; clientCode: string; repo: Repository }[]
  confirmCheckboxes: Record<string, boolean>
  toggleConfirm: (key: string) => void
}) {
  if (!confirmRows.length) {
    return (
      <div className="p-10 text-center">
        <AlertCircle className="w-9 h-9 text-slate-300 mx-auto mb-2" />
        <p className="text-sm text-slate-500">Nothing selected. Go back and pick at least one repository.</p>
      </div>
    )
  }

  // Group by client for display
  const grouped = new Map<string, typeof confirmRows>()
  for (const row of confirmRows) {
    if (!grouped.has(row.clientCode)) grouped.set(row.clientCode, [])
    grouped.get(row.clientCode)!.push(row)
  }

  return (
    <div className="p-6 space-y-4">
      <div className="flex items-start gap-3 p-3 bg-accent-50 border border-accent-200 rounded-lg">
        <CheckCircle2 className="w-4 h-4 text-accent-600 flex-shrink-0 mt-0.5" />
        <p className="text-xs text-accent-800">
          The firmware binary will be re-uploaded to each selected repository's Gitea, and a new
          firmware record (Approved) will be created. Targets that already have this version will
          be silently skipped.
        </p>
      </div>

      <div className="space-y-3">
        {Array.from(grouped.entries()).map(([clientCode, rows]) => (
          <div key={clientCode} className="border border-slate-200 rounded-lg overflow-hidden">
            <div className="px-4 py-2 bg-slate-50 border-b border-slate-200 flex items-center gap-2">
              <Building2 className="w-3.5 h-3.5 text-slate-500" />
              <span className="text-sm font-semibold text-primary-800">{rows[0].clientName}</span>
              <span className="text-xs text-slate-400">{clientCode}</span>
            </div>
            <div className="divide-y divide-slate-100">
              {rows.map((row) => (
                <label
                  key={row.key}
                  className="flex items-center gap-3 px-4 py-2.5 cursor-pointer hover:bg-slate-50 transition-colors"
                >
                  <input
                    type="checkbox"
                    checked={confirmCheckboxes[row.key] ?? false}
                    onChange={() => toggleConfirm(row.key)}
                    className="w-4 h-4 accent-accent-600"
                  />
                  <GitBranch className="w-4 h-4 text-slate-400 flex-shrink-0" />
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium text-primary-800 truncate">{row.repo.name}</p>
                    {row.repo.projectName && (
                      <p className="text-xs text-slate-400 truncate">{row.repo.projectName}</p>
                    )}
                  </div>
                </label>
              ))}
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
