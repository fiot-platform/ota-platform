'use client'

import * as React from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  FlaskConical, Plus, Upload, Trash2, CheckCircle2, XCircle, AlertTriangle,
  Bug, FileText, Clock, ChevronDown, Loader2, Download, RefreshCw,
} from 'lucide-react'
import { qaSessionService } from '@/services/qaSession.service'
import { useToast } from '@/components/ui/ToastProvider'
import { StatusBadge } from '@/components/ui/Badge'
import { formatDate, formatFileSize, formatRelativeTime } from '@/utils/formatters'
import {
  QASession, QASessionStatus, BugSeverity, BugStatus,
  AddBugRequest, UpdateBugRequest, UpdateQAStatusRequest, CompleteQARequest,
  QABugItem, QADocumentItem,
} from '@/types'

// ── Helpers ───────────────────────────────────────────────────────────────────

const QA_STATUSES: QASessionStatus[] = [
  QASessionStatus.NotStarted,
  QASessionStatus.InProgress,
  QASessionStatus.BugListRaised,
  QASessionStatus.Complete,
  QASessionStatus.Fail,
]

const STATUS_COLOR: Record<QASessionStatus, string> = {
  [QASessionStatus.NotStarted]:   'bg-slate-100 text-slate-600',
  [QASessionStatus.InProgress]:   'bg-accent-100 text-accent-700',
  [QASessionStatus.BugListRaised]:'bg-warning-100 text-warning-700',
  [QASessionStatus.Complete]:     'bg-success-100 text-success-700',
  [QASessionStatus.Fail]:         'bg-danger-100 text-danger-700',
}

const SEVERITY_COLOR: Record<string, string> = {
  Low:      'bg-slate-100 text-slate-600',
  Medium:   'bg-warning-100 text-warning-700',
  High:     'bg-orange-100 text-orange-700',
  Critical: 'bg-danger-100 text-danger-700',
}

const BUG_STATUS_COLOR: Record<string, string> = {
  Open:       'bg-danger-100 text-danger-700',
  InProgress: 'bg-accent-100 text-accent-700',
  Resolved:   'bg-success-100 text-success-700',
  WontFix:    'bg-slate-100 text-slate-500',
}

function StatusPill({ label, color }: { label: string; color: string }) {
  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold ${color}`}>
      {label.replace(/([A-Z])/g, ' $1').trim()}
    </span>
  )
}

// ── File upload dropzone (inline, no dialog) ──────────────────────────────────

function DocUploadZone({
  onFile,
  isLoading,
}: {
  onFile: (file: File) => void
  isLoading?: boolean
}) {
  const inputRef = React.useRef<HTMLInputElement>(null)
  const [drag, setDrag] = React.useState(false)

  const handleFile = (file: File) => onFile(file)

  return (
    <div
      onClick={() => !isLoading && inputRef.current?.click()}
      onDragOver={(e) => { e.preventDefault(); setDrag(true) }}
      onDragLeave={() => setDrag(false)}
      onDrop={(e) => { e.preventDefault(); setDrag(false); const f = e.dataTransfer.files[0]; if (f) handleFile(f) }}
      className={`flex items-center gap-3 border-2 border-dashed rounded-xl px-4 py-3 cursor-pointer transition-colors text-sm ${
        isLoading ? 'opacity-60 cursor-not-allowed' :
        drag ? 'border-accent-400 bg-accent-50' : 'border-slate-200 hover:border-accent-300 hover:bg-slate-50'
      }`}
    >
      <input ref={inputRef} type="file" className="hidden" onChange={(e) => { const f = e.target.files?.[0]; if (f) handleFile(f) }} />
      {isLoading
        ? <><Loader2 className="w-4 h-4 animate-spin text-accent-500" /><span className="text-accent-600">Uploading…</span></>
        : <><Upload className="w-4 h-4 text-slate-400" /><span className="text-slate-500">Drop file or click to browse</span></>
      }
    </div>
  )
}

// ── Document list ─────────────────────────────────────────────────────────────

function DocList({
  docs,
  onRemove,
  isRemoving,
}: {
  docs: QADocumentItem[]
  onRemove: (documentId: string) => void
  isRemoving?: string | null
}) {
  if (docs.length === 0)
    return <p className="text-xs text-slate-400 italic py-2">No documents uploaded yet.</p>

  return (
    <div className="space-y-1.5">
      {docs.map((doc) => (
        <div key={doc.documentId} className="flex items-center justify-between p-2.5 bg-slate-50 rounded-lg group">
          <div className="flex items-center gap-2.5 min-w-0">
            <FileText className="w-4 h-4 text-accent-500 flex-shrink-0" />
            <div className="min-w-0">
              <p className="text-sm font-medium text-primary-800 truncate">{doc.name}</p>
              <p className="text-xs text-slate-400">{formatFileSize(doc.fileSizeBytes)} · {formatDate(doc.uploadedAt)}</p>
            </div>
          </div>
          <div className="flex items-center gap-1 flex-shrink-0">
            <a
              href={doc.downloadUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="p-1.5 rounded text-slate-400 hover:text-accent-600 hover:bg-accent-50 transition-colors"
              title="Download"
            >
              <Download className="w-3.5 h-3.5" />
            </a>
            <button
              onClick={() => onRemove(doc.documentId)}
              disabled={isRemoving === doc.documentId}
              className="p-1.5 rounded text-slate-400 hover:text-danger-600 hover:bg-danger-50 transition-colors disabled:opacity-40"
              title="Remove"
            >
              {isRemoving === doc.documentId
                ? <Loader2 className="w-3.5 h-3.5 animate-spin" />
                : <Trash2 className="w-3.5 h-3.5" />}
            </button>
          </div>
        </div>
      ))}
    </div>
  )
}

// ── Add Bug form (inline collapsible) ────────────────────────────────────────

function AddBugForm({ onSubmit, isLoading }: { onSubmit: (d: AddBugRequest) => void; isLoading?: boolean }) {
  const [open, setOpen] = React.useState(false)
  const [title, setTitle] = React.useState('')
  const [description, setDescription] = React.useState('')
  const [severity, setSeverity] = React.useState<BugSeverity>(BugSeverity.Medium)

  const submit = (e: React.FormEvent) => {
    e.preventDefault()
    if (!title.trim()) return
    onSubmit({ title: title.trim(), description: description.trim() || undefined, severity })
    setTitle(''); setDescription(''); setSeverity(BugSeverity.Medium); setOpen(false)
  }

  return (
    <div>
      <button
        type="button"
        onClick={() => setOpen(!open)}
        className="flex items-center gap-1.5 text-xs font-medium text-accent-600 hover:text-accent-700 transition-colors"
      >
        <Plus className="w-3.5 h-3.5" />
        {open ? 'Cancel' : 'Add Bug'}
      </button>
      {open && (
        <form onSubmit={submit} className="mt-3 space-y-3 p-3 bg-slate-50 rounded-xl border border-slate-200">
          <div>
            <label className="label text-xs">Title *</label>
            <input
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="Brief description of the bug"
              className="input text-sm"
              required
            />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="label text-xs">Severity</label>
              <select
                value={severity}
                onChange={(e) => setSeverity(e.target.value as BugSeverity)}
                className="input text-sm"
              >
                {Object.values(BugSeverity).map((s) => <option key={s} value={s}>{s}</option>)}
              </select>
            </div>
          </div>
          <div>
            <label className="label text-xs">Description</label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Steps to reproduce, expected vs actual behavior…"
              rows={3}
              className="input text-sm resize-none"
            />
          </div>
          <div className="flex justify-end gap-2">
            <button type="button" onClick={() => setOpen(false)} className="btn-secondary text-xs py-1.5 px-3">Cancel</button>
            <button type="submit" disabled={isLoading || !title.trim()} className="btn-primary text-xs py-1.5 px-3">
              {isLoading ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : 'Raise Bug'}
            </button>
          </div>
        </form>
      )}
    </div>
  )
}

// ── Bug list row ──────────────────────────────────────────────────────────────

function BugRow({ bug, onUpdate }: { bug: QABugItem; onUpdate: (bugId: string, d: UpdateBugRequest) => void }) {
  const [expanded, setExpanded] = React.useState(false)
  const [editing, setEditing] = React.useState(false)
  const [resolution, setResolution] = React.useState(bug.resolution ?? '')

  const nextStatus: Record<BugStatus, BugStatus | null> = {
    [BugStatus.Open]:       BugStatus.InProgress,
    [BugStatus.InProgress]: BugStatus.Resolved,
    [BugStatus.Resolved]:   null,
    [BugStatus.WontFix]:    null,
  }

  const advance = () => {
    const next = nextStatus[bug.bugStatus as BugStatus]
    if (!next) return
    const data: UpdateBugRequest = { bugStatus: next }
    if (next === BugStatus.Resolved) data.resolution = resolution || undefined
    onUpdate(bug.bugId, data)
  }

  const markWontFix = () => onUpdate(bug.bugId, { bugStatus: BugStatus.WontFix, resolution: "Won't fix" })

  return (
    <div className="border border-slate-200 rounded-xl overflow-hidden">
      <div
        className="flex items-center gap-3 p-3 cursor-pointer hover:bg-slate-50 transition-colors"
        onClick={() => setExpanded(!expanded)}
      >
        <Bug className="w-4 h-4 text-slate-400 flex-shrink-0" />
        <div className="flex-1 min-w-0">
          <p className="text-sm font-medium text-primary-800 truncate">{bug.title}</p>
          <p className="text-xs text-slate-400">{formatDate(bug.reportedAt)}</p>
        </div>
        <StatusPill label={bug.severity} color={SEVERITY_COLOR[bug.severity] ?? 'bg-slate-100 text-slate-600'} />
        <StatusPill label={bug.bugStatus} color={BUG_STATUS_COLOR[bug.bugStatus] ?? 'bg-slate-100 text-slate-600'} />
        <ChevronDown className={`w-4 h-4 text-slate-400 transition-transform ${expanded ? 'rotate-180' : ''}`} />
      </div>

      {expanded && (
        <div className="border-t border-slate-100 p-3 bg-slate-50 space-y-3 text-sm">
          {bug.description && (
            <p className="text-slate-600 whitespace-pre-wrap">{bug.description}</p>
          )}

          {bug.resolution && (
            <div className="p-2 bg-success-50 rounded-lg border border-success-200">
              <p className="text-xs font-semibold text-success-700 mb-1">Resolution</p>
              <p className="text-xs text-success-600">{bug.resolution}</p>
            </div>
          )}

          {bug.bugStatus !== BugStatus.Resolved && bug.bugStatus !== BugStatus.WontFix && (
            <div className="space-y-2">
              {(nextStatus[bug.bugStatus as BugStatus] === BugStatus.Resolved) && (
                <div>
                  <label className="label text-xs">Resolution notes</label>
                  <input
                    type="text"
                    value={resolution}
                    onChange={(e) => setResolution(e.target.value)}
                    placeholder="Describe how the bug was fixed…"
                    className="input text-xs"
                  />
                </div>
              )}
              <div className="flex gap-2">
                {nextStatus[bug.bugStatus as BugStatus] && (
                  <button onClick={advance} className="btn-primary text-xs py-1.5 px-3">
                    Move to {nextStatus[bug.bugStatus as BugStatus]}
                  </button>
                )}
                <button onClick={markWontFix} className="btn-secondary text-xs py-1.5 px-3">
                  Won&apos;t Fix
                </button>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  )
}

// ── Event log tab ─────────────────────────────────────────────────────────────

function EventLogTab({ firmwareId }: { firmwareId: string }) {
  const { data: log = [], isLoading } = useQuery({
    queryKey: ['qa-event-log', firmwareId],
    queryFn: () => qaSessionService.getEventLog(firmwareId),
  })

  const EVENT_ICON: Record<string, React.ReactNode> = {
    SessionStarted:   <FlaskConical className="w-3.5 h-3.5 text-accent-500" />,
    StatusChanged:    <RefreshCw className="w-3.5 h-3.5 text-primary-500" />,
    DocumentUploaded: <FileText className="w-3.5 h-3.5 text-success-500" />,
    DocumentRemoved:  <Trash2 className="w-3.5 h-3.5 text-slate-400" />,
    BugRaised:        <Bug className="w-3.5 h-3.5 text-warning-500" />,
    BugUpdated:       <Bug className="w-3.5 h-3.5 text-accent-500" />,
    SessionCompleted: <CheckCircle2 className="w-3.5 h-3.5 text-success-500" />,
  }

  if (isLoading) return <div className="animate-pulse space-y-2">{[1,2,3].map((i) => <div key={i} className="h-10 bg-slate-100 rounded-lg" />)}</div>

  if (log.length === 0)
    return <p className="text-xs text-slate-400 italic py-4 text-center">No events recorded yet.</p>

  return (
    <div className="space-y-0">
      {log.map((entry, i) => (
        <div key={entry.eventId} className="flex gap-3 pb-4 relative">
          {/* Timeline connector */}
          {i < log.length - 1 && (
            <div className="absolute left-3.5 top-7 w-0.5 bottom-0 bg-slate-100" />
          )}
          <div className="w-7 h-7 rounded-full bg-white border-2 border-slate-200 flex items-center justify-center flex-shrink-0 z-10">
            {EVENT_ICON[entry.eventType] ?? <Clock className="w-3.5 h-3.5 text-slate-400" />}
          </div>
          <div className="flex-1 min-w-0 pt-0.5">
            <p className="text-sm text-primary-800">{entry.description}</p>
            <p className="text-xs text-slate-400 mt-0.5">
              {formatRelativeTime(entry.timestamp)} · <code className="font-mono text-slate-500">{entry.eventType}</code>
            </p>
            {entry.metadata && Object.keys(entry.metadata).length > 0 && (
              <div className="mt-1.5 flex flex-wrap gap-1">
                {Object.entries(entry.metadata).filter(([, v]) => v).map(([k, v]) => (
                  <span key={k} className="inline-flex items-center gap-1 px-2 py-0.5 bg-slate-100 rounded text-xs text-slate-500 font-mono">
                    {k}: {v}
                  </span>
                ))}
              </div>
            )}
          </div>
        </div>
      ))}
    </div>
  )
}

// ── Main panel ────────────────────────────────────────────────────────────────

interface QASessionPanelProps {
  firmwareId: string
  firmwareVersion: string
}

type TabId = 'overview' | 'testCases' | 'testResults' | 'bugs' | 'log'

export function QASessionPanel({ firmwareId, firmwareVersion }: QASessionPanelProps) {
  const queryClient = useQueryClient()
  const { toast } = useToast()
  const [tab, setTab] = React.useState<TabId>('overview')
  const [removingDocId, setRemovingDocId] = React.useState<string | null>(null)

  const { data: session, isLoading } = useQuery({
    queryKey: ['qa-session', firmwareId],
    queryFn: () => qaSessionService.getSession(firmwareId),
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['qa-session', firmwareId] })

  const startMutation = useMutation({
    mutationFn: () => qaSessionService.startSession(firmwareId),
    onSuccess: () => { invalidate(); toast({ title: 'QA session started', variant: 'success' }) },
    onError: (e: any) => toast({ title: 'Failed to start session', description: e?.response?.data?.message, variant: 'error' }),
  })

  const statusMutation = useMutation({
    mutationFn: (data: UpdateQAStatusRequest) => qaSessionService.updateStatus(firmwareId, data),
    onSuccess: () => { invalidate(); toast({ title: 'QA status updated', variant: 'success' }) },
    onError: (e: any) => toast({ title: 'Failed to update status', description: e?.response?.data?.message, variant: 'error' }),
  })

  const uploadTestCaseMutation = useMutation({
    mutationFn: (file: File) => qaSessionService.uploadDocument(firmwareId, file, 'testCase'),
    onSuccess: () => { invalidate(); toast({ title: 'Test case document uploaded', variant: 'success' }) },
    onError: (e: any) => toast({ title: 'Upload failed', description: e?.response?.data?.message, variant: 'error' }),
  })

  const uploadTestResultMutation = useMutation({
    mutationFn: (file: File) => qaSessionService.uploadDocument(firmwareId, file, 'testResult'),
    onSuccess: () => { invalidate(); toast({ title: 'Test result document uploaded', variant: 'success' }) },
    onError: (e: any) => toast({ title: 'Upload failed', description: e?.response?.data?.message, variant: 'error' }),
  })

  const removeDocMutation = useMutation({
    mutationFn: (documentId: string) => qaSessionService.removeDocument(firmwareId, documentId),
    onSuccess: () => { invalidate(); setRemovingDocId(null); toast({ title: 'Document removed', variant: 'success' }) },
    onError: (e: any) => { setRemovingDocId(null); toast({ title: 'Remove failed', description: e?.response?.data?.message, variant: 'error' }) },
  })

  const addBugMutation = useMutation({
    mutationFn: (data: AddBugRequest) => qaSessionService.addBug(firmwareId, data),
    onSuccess: () => { invalidate(); toast({ title: 'Bug raised', variant: 'success' }) },
    onError: (e: any) => toast({ title: 'Failed to add bug', description: e?.response?.data?.message, variant: 'error' }),
  })

  const updateBugMutation = useMutation({
    mutationFn: ({ bugId, data }: { bugId: string; data: UpdateBugRequest }) =>
      qaSessionService.updateBug(firmwareId, bugId, data),
    onSuccess: () => { invalidate(); toast({ title: 'Bug updated', variant: 'success' }) },
    onError: (e: any) => toast({ title: 'Update failed', description: e?.response?.data?.message, variant: 'error' }),
  })

  const completeMutation = useMutation({
    mutationFn: (data: CompleteQARequest) => qaSessionService.completeSession(firmwareId, data),
    onSuccess: (s) => { invalidate(); toast({ title: `QA ${s.status}`, variant: s.status === 'Complete' ? 'success' : 'error' }) },
    onError: (e: any) => toast({ title: 'Failed to complete session', description: e?.response?.data?.message, variant: 'error' }),
  })

  if (isLoading) {
    return (
      <div className="card p-6 animate-pulse space-y-4">
        <div className="h-5 bg-slate-200 rounded w-40" />
        <div className="h-24 bg-slate-100 rounded-xl" />
      </div>
    )
  }

  // ── No session yet ──
  if (!session) {
    return (
      <div className="card p-6">
        <div className="flex items-center gap-2 mb-4">
          <FlaskConical className="w-5 h-5 text-accent-600" />
          <h3 className="section-title">QA Testing</h3>
        </div>
        <div className="flex flex-col items-center py-8 gap-4 text-center">
          <div className="w-14 h-14 rounded-2xl bg-accent-50 flex items-center justify-center">
            <FlaskConical className="w-7 h-7 text-accent-400" />
          </div>
          <div>
            <p className="text-sm font-medium text-primary-800">No QA session started</p>
            <p className="text-xs text-slate-400 mt-1">Start a QA session to track test cases, results, and bugs for {firmwareVersion}.</p>
          </div>
          <button
            onClick={() => startMutation.mutate()}
            disabled={startMutation.isPending}
            className="btn-primary"
          >
            {startMutation.isPending
              ? <><Loader2 className="w-4 h-4 animate-spin" /> Starting…</>
              : <><FlaskConical className="w-4 h-4" /> Start QA Session</>}
          </button>
        </div>
      </div>
    )
  }

  const tabs: { id: TabId; label: string; count?: number }[] = [
    { id: 'overview', label: 'Overview' },
    { id: 'testCases', label: 'Test Cases', count: session.totalTestCaseDocs },
    { id: 'testResults', label: 'Test Results', count: session.totalTestResultDocs },
    { id: 'bugs', label: 'Bugs', count: session.totalBugs },
    { id: 'log', label: 'Event Log' },
  ]

  const isDone = session.status === QASessionStatus.Complete || session.status === QASessionStatus.Fail

  return (
    <div className="card overflow-hidden">
      {/* Header */}
      <div className="flex items-center justify-between p-5 border-b border-slate-100">
        <div className="flex items-center gap-3">
          <FlaskConical className="w-5 h-5 text-accent-600" />
          <div>
            <h3 className="font-semibold text-primary-900">QA Testing Session</h3>
            <p className="text-xs text-slate-400 mt-0.5">Firmware {firmwareVersion}</p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <StatusPill
            label={session.status}
            color={STATUS_COLOR[session.status as QASessionStatus] ?? 'bg-slate-100 text-slate-600'}
          />
          {!isDone && (
            <select
              className="input text-xs py-1 px-2 w-auto"
              value=""
              onChange={(e) => { if (e.target.value) statusMutation.mutate({ status: e.target.value as QASessionStatus }) }}
            >
              <option value="">Change status…</option>
              {QA_STATUSES.filter(s => s !== session.status && s !== QASessionStatus.NotStarted).map(s => (
                <option key={s} value={s}>{s.replace(/([A-Z])/g, ' $1').trim()}</option>
              ))}
            </select>
          )}
        </div>
      </div>

      {/* Tabs */}
      <div className="flex border-b border-slate-100 overflow-x-auto">
        {tabs.map(t => (
          <button
            key={t.id}
            onClick={() => setTab(t.id)}
            className={`flex items-center gap-1.5 px-4 py-2.5 text-sm font-medium whitespace-nowrap transition-colors border-b-2 ${
              tab === t.id
                ? 'border-accent-500 text-accent-600'
                : 'border-transparent text-slate-500 hover:text-primary-700'
            }`}
          >
            {t.label}
            {t.count != null && t.count > 0 && (
              <span className={`inline-flex items-center justify-center w-4 h-4 rounded-full text-[10px] font-bold ${
                tab === t.id ? 'bg-accent-100 text-accent-700' : 'bg-slate-100 text-slate-600'
              }`}>
                {t.count}
              </span>
            )}
          </button>
        ))}
      </div>

      {/* Tab content */}
      <div className="p-5">
        {/* ── Overview ── */}
        {tab === 'overview' && (
          <div className="space-y-5">
            {/* Stats row */}
            <div className="grid grid-cols-3 gap-4">
              {[
                { label: 'Test Case Docs', value: session.totalTestCaseDocs, icon: <FileText className="w-4 h-4 text-accent-500" /> },
                { label: 'Test Result Docs', value: session.totalTestResultDocs, icon: <FileText className="w-4 h-4 text-success-500" /> },
                { label: 'Open Bugs', value: session.openBugs, icon: <Bug className="w-4 h-4 text-warning-500" /> },
              ].map(stat => (
                <div key={stat.label} className="flex items-center gap-3 p-3 bg-slate-50 rounded-xl">
                  <div className="w-8 h-8 rounded-lg bg-white flex items-center justify-center shadow-sm">{stat.icon}</div>
                  <div>
                    <p className="text-xl font-bold text-primary-900">{stat.value}</p>
                    <p className="text-xs text-slate-500">{stat.label}</p>
                  </div>
                </div>
              ))}
            </div>

            {/* Status stepper */}
            <div>
              <p className="label mb-3">QA Progress</p>
              <div className="flex items-center gap-1">
                {[QASessionStatus.NotStarted, QASessionStatus.InProgress, QASessionStatus.BugListRaised, QASessionStatus.Complete].map((s, i, arr) => {
                  const currentIdx = arr.indexOf(session.status as QASessionStatus)
                  const stepIdx = i
                  const done = stepIdx < currentIdx || (session.status === QASessionStatus.Complete && s === QASessionStatus.Complete)
                  const active = s === session.status
                  return (
                    <React.Fragment key={s}>
                      <div className="flex flex-col items-center">
                        <div className={`w-7 h-7 rounded-full flex items-center justify-center flex-shrink-0 text-xs font-bold ${
                          done ? 'bg-success-500 text-white' : active ? 'bg-accent-500 text-white' : 'bg-slate-200 text-slate-400'
                        }`}>
                          {done ? <CheckCircle2 className="w-4 h-4" /> : i + 1}
                        </div>
                        <p className={`text-[10px] mt-1 font-medium ${active ? 'text-accent-600' : done ? 'text-success-600' : 'text-slate-400'}`}>
                          {s.replace(/([A-Z])/g, ' $1').trim()}
                        </p>
                      </div>
                      {i < arr.length - 1 && (
                        <div className={`flex-1 h-0.5 mb-4 ${stepIdx < currentIdx ? 'bg-success-400' : 'bg-slate-200'}`} />
                      )}
                    </React.Fragment>
                  )
                })}
              </div>
              {session.status === QASessionStatus.Fail && (
                <div className="mt-3 flex items-center gap-2 p-3 bg-danger-50 border border-danger-200 rounded-lg text-sm text-danger-700">
                  <XCircle className="w-4 h-4 flex-shrink-0" />
                  QA session has been marked as Failed.
                  {session.remarks && <span className="italic ml-1">{session.remarks}</span>}
                </div>
              )}
            </div>

            {/* Finalize buttons */}
            {!isDone && (
              <div className="flex items-center gap-3 pt-2 border-t border-slate-100">
                <button
                  onClick={() => completeMutation.mutate({ finalStatus: QASessionStatus.Complete, remarks: 'All tests passed.' })}
                  disabled={completeMutation.isPending || session.openBugs > 0}
                  className="btn-primary bg-success-600 hover:bg-success-700 disabled:opacity-50"
                  title={session.openBugs > 0 ? 'Resolve all open bugs before completing' : undefined}
                >
                  <CheckCircle2 className="w-4 h-4" /> Mark Complete
                </button>
                <button
                  onClick={() => completeMutation.mutate({ finalStatus: QASessionStatus.Fail, remarks: 'QA failed.' })}
                  disabled={completeMutation.isPending}
                  className="btn-danger"
                >
                  <XCircle className="w-4 h-4" /> Mark Failed
                </button>
                {session.openBugs > 0 && (
                  <p className="text-xs text-warning-600 flex items-center gap-1">
                    <AlertTriangle className="w-3.5 h-3.5" />
                    {session.openBugs} open bug{session.openBugs > 1 ? 's' : ''} must be resolved first
                  </p>
                )}
              </div>
            )}

            {session.remarks && (
              <div className="p-3 bg-slate-50 rounded-xl text-sm text-slate-600 italic border border-slate-200">
                {session.remarks}
              </div>
            )}
          </div>
        )}

        {/* ── Test Cases ── */}
        {tab === 'testCases' && (
          <div className="space-y-4">
            {!isDone && (
              <DocUploadZone
                onFile={(f) => uploadTestCaseMutation.mutate(f)}
                isLoading={uploadTestCaseMutation.isPending}
              />
            )}
            <DocList
              docs={session.testCaseDocuments}
              onRemove={(id) => { setRemovingDocId(id); removeDocMutation.mutate(id) }}
              isRemoving={removingDocId}
            />
          </div>
        )}

        {/* ── Test Results ── */}
        {tab === 'testResults' && (
          <div className="space-y-4">
            {!isDone && (
              <DocUploadZone
                onFile={(f) => uploadTestResultMutation.mutate(f)}
                isLoading={uploadTestResultMutation.isPending}
              />
            )}
            <DocList
              docs={session.testResultDocuments}
              onRemove={(id) => { setRemovingDocId(id); removeDocMutation.mutate(id) }}
              isRemoving={removingDocId}
            />
          </div>
        )}

        {/* ── Bugs ── */}
        {tab === 'bugs' && (
          <div className="space-y-4">
            {/* Summary */}
            {session.totalBugs > 0 && (
              <div className="flex gap-3 text-xs">
                {[
                  { label: 'Open', count: session.bugs.filter(b => b.bugStatus === 'Open').length, color: 'text-danger-600' },
                  { label: 'In Progress', count: session.bugs.filter(b => b.bugStatus === 'InProgress').length, color: 'text-accent-600' },
                  { label: 'Resolved', count: session.resolvedBugs, color: 'text-success-600' },
                ].filter(x => x.count > 0).map(x => (
                  <span key={x.label} className={`font-semibold ${x.color}`}>
                    {x.count} {x.label}
                  </span>
                ))}
              </div>
            )}

            {/* Bug list */}
            <div className="space-y-2">
              {session.bugs.map(bug => (
                <BugRow
                  key={bug.bugId}
                  bug={bug}
                  onUpdate={(bugId, data) => updateBugMutation.mutate({ bugId, data })}
                />
              ))}
              {session.bugs.length === 0 && (
                <p className="text-xs text-slate-400 italic py-4 text-center">No bugs raised yet.</p>
              )}
            </div>

            {/* Add bug */}
            {!isDone && (
              <AddBugForm
                onSubmit={(data) => addBugMutation.mutate(data)}
                isLoading={addBugMutation.isPending}
              />
            )}
          </div>
        )}

        {/* ── Event Log ── */}
        {tab === 'log' && <EventLogTab firmwareId={firmwareId} />}
      </div>
    </div>
  )
}
