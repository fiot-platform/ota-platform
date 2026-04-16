'use client'

import * as React from 'react'
import * as Dialog from '@radix-ui/react-dialog'
import * as XLSX from 'xlsx'
import {
  X,
  Upload,
  FileSpreadsheet,
  Download,
  CheckCircle2,
  XCircle,
  AlertTriangle,
  Loader2,
  Trash2,
} from 'lucide-react'
import { RegisterDeviceRequest, BulkRegisterResult } from '@/types'

// ─── Types ────────────────────────────────────────────────────────────────────

interface ParsedRow extends RegisterDeviceRequest {
  _rowNum: number
  _valid: boolean
  _errors: string[]
}

type Stage = 'upload' | 'preview' | 'result'

// ─── Template columns (must match backend field order) ────────────────────────

const COLUMNS = [
  { key: 'projectName',            label: 'Project Name',              required: true  },
  { key: 'customerCode',           label: 'Customer Code',             required: true  },
  { key: 'macImeiIp',              label: 'MAC / IMEI / IP',           required: true  },
  { key: 'model',                  label: 'Model',                     required: true  },
  { key: 'currentFirmwareVersion', label: 'Initial Firmware Version',  required: false },
] as const

type ColKey = typeof COLUMNS[number]['key']

// ─── Helper: generate the downloadable Excel template ────────────────────────

function downloadTemplate() {
  const headers = COLUMNS.map((c) => c.label)
  const sample = ['Smart Metering Phase 2', 'CUST-001', 'AA:BB:CC:DD:EE:FF', 'EDGE-GW-V2', '1.0.0']

  const ws = XLSX.utils.aoa_to_sheet([headers, sample])

  // Column widths
  ws['!cols'] = COLUMNS.map(() => ({ wch: 28 }))

  // Bold + background on header row
  const headerStyle = { font: { bold: true }, fill: { fgColor: { rgb: 'E2E8F0' } } }
  headers.forEach((_, ci) => {
    const cell = XLSX.utils.encode_cell({ r: 0, c: ci })
    if (ws[cell]) ws[cell].s = headerStyle
  })

  const wb = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(wb, ws, 'Devices')
  XLSX.writeFile(wb, 'device_bulk_upload_template.xlsx')
}

// ─── Helper: validate and parse an Excel file ─────────────────────────────────

function parseExcel(file: File): Promise<ParsedRow[]> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = (e) => {
      try {
        const data = new Uint8Array(e.target!.result as ArrayBuffer)
        const wb = XLSX.read(data, { type: 'array' })
        const ws = wb.Sheets[wb.SheetNames[0]]
        const rows: string[][] = XLSX.utils.sheet_to_json(ws, { header: 1, defval: '' }) as string[][]

        if (rows.length < 2) {
          resolve([])
          return
        }

        // Map header labels → column indices
        const headerRow = rows[0].map((h) => String(h).trim())
        const colIndex: Partial<Record<ColKey, number>> = {}
        COLUMNS.forEach((col) => {
          const idx = headerRow.indexOf(col.label)
          if (idx !== -1) colIndex[col.key] = idx
        })

        const parsed: ParsedRow[] = rows.slice(1).map((row, i) => {
          const get = (key: ColKey) => String(row[colIndex[key] ?? -1] ?? '').trim()
          const errors: string[] = []

          const projectName            = get('projectName')
          const customerCode           = get('customerCode')
          const macImeiIp              = get('macImeiIp')
          const model                  = get('model')
          const currentFirmwareVersion = get('currentFirmwareVersion')

          COLUMNS.filter((c) => c.required).forEach((c) => {
            if (!get(c.key)) errors.push(`${c.label} is required`)
          })

          return {
            _rowNum: i + 2,
            _valid: errors.length === 0,
            _errors: errors,
            projectName,
            customerCode,
            macImeiIp,
            model,
            currentFirmwareVersion: currentFirmwareVersion || undefined,
          }
        }).filter((r) => r.projectName || r.customerCode || r.macImeiIp || r.model) // skip blank rows

        resolve(parsed)
      } catch (err) {
        reject(err)
      }
    }
    reader.onerror = () => reject(new Error('Failed to read file'))
    reader.readAsArrayBuffer(file)
  })
}

// ─── Props ────────────────────────────────────────────────────────────────────

interface BulkDeviceUploadFormProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onUpload: (devices: RegisterDeviceRequest[]) => Promise<BulkRegisterResult>
  isLoading?: boolean
}

// ─── Component ────────────────────────────────────────────────────────────────

export function BulkDeviceUploadForm({
  open,
  onOpenChange,
  onUpload,
  isLoading = false,
}: BulkDeviceUploadFormProps) {
  const [stage, setStage] = React.useState<Stage>('upload')
  const [rows, setRows] = React.useState<ParsedRow[]>([])
  const [result, setResult] = React.useState<BulkRegisterResult | null>(null)
  const [parseError, setParseError] = React.useState<string | null>(null)
  const [isDragging, setIsDragging] = React.useState(false)
  const fileInputRef = React.useRef<HTMLInputElement>(null)

  // Reset on close
  React.useEffect(() => {
    if (!open) {
      setStage('upload')
      setRows([])
      setResult(null)
      setParseError(null)
    }
  }, [open])

  const handleFile = async (file: File) => {
    setParseError(null)
    const ext = file.name.split('.').pop()?.toLowerCase()
    if (!['xlsx', 'xls'].includes(ext ?? '')) {
      setParseError('Please upload an Excel file (.xlsx or .xls).')
      return
    }
    try {
      const parsed = await parseExcel(file)
      if (parsed.length === 0) {
        setParseError('No data rows found. Please fill in the template and re-upload.')
        return
      }
      setRows(parsed)
      setStage('preview')
    } catch {
      setParseError('Failed to parse the file. Make sure it uses the provided template.')
    }
  }

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault()
    setIsDragging(false)
    const file = e.dataTransfer.files[0]
    if (file) handleFile(file)
  }

  const handleFileInput = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (file) handleFile(file)
    e.target.value = ''
  }

  const handleSubmit = async () => {
    const valid = rows.filter((r) => r._valid)
    const payload: RegisterDeviceRequest[] = valid.map(({ projectName, customerCode, macImeiIp, model, currentFirmwareVersion }) => ({
      projectName,
      customerCode,
      macImeiIp,
      model,
      ...(currentFirmwareVersion ? { currentFirmwareVersion } : {}),
    }))
    const res = await onUpload(payload)
    setResult(res)
    setStage('result')
  }

  const validCount   = rows.filter((r) => r._valid).length
  const invalidCount = rows.filter((r) => !r._valid).length

  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50" />
        <Dialog.Content className="fixed left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 bg-white rounded-2xl shadow-2xl z-50 w-full max-w-3xl max-h-[90vh] flex flex-col">

          {/* ── Header ───────────────────────────────────────────────── */}
          <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100 flex-shrink-0">
            <div>
              <Dialog.Title className="text-lg font-semibold text-primary-900">
                Bulk Device Upload
              </Dialog.Title>
              <Dialog.Description className="text-sm text-slate-500">
                {stage === 'upload'  && 'Upload an Excel file to register multiple devices at once.'}
                {stage === 'preview' && `${rows.length} row(s) parsed — ${validCount} valid, ${invalidCount} with errors.`}
                {stage === 'result'  && 'Upload complete.'}
              </Dialog.Description>
            </div>
            <Dialog.Close className="text-slate-400 hover:text-slate-600 transition-colors">
              <X className="w-5 h-5" />
            </Dialog.Close>
          </div>

          {/* ── Stage: Upload ─────────────────────────────────────────── */}
          {stage === 'upload' && (
            <div className="p-6 flex flex-col gap-5 overflow-y-auto">
              {/* Download template */}
              <div className="flex items-center justify-between p-4 bg-accent-50 border border-accent-200 rounded-xl">
                <div className="flex items-center gap-3">
                  <FileSpreadsheet className="w-5 h-5 text-accent-600" />
                  <div>
                    <p className="text-sm font-semibold text-accent-800">Download the Excel template</p>
                    <p className="text-xs text-accent-600">Fill in the required columns and re-upload.</p>
                  </div>
                </div>
                <button
                  type="button"
                  onClick={downloadTemplate}
                  className="btn-secondary text-sm"
                >
                  <Download className="w-4 h-4" />
                  Template
                </button>
              </div>

              {/* Template column reference */}
              <div className="grid grid-cols-5 gap-2">
                {COLUMNS.map((col) => (
                  <div key={col.key} className="text-center px-2 py-2 bg-slate-50 rounded-lg border border-slate-200">
                    <p className="text-xs font-semibold text-primary-700 leading-tight">{col.label}</p>
                    {col.required && <span className="text-xs text-danger-500">required</span>}
                  </div>
                ))}
              </div>

              {/* Drop zone */}
              <div
                onDragOver={(e) => { e.preventDefault(); setIsDragging(true) }}
                onDragLeave={() => setIsDragging(false)}
                onDrop={handleDrop}
                onClick={() => fileInputRef.current?.click()}
                className={`
                  flex flex-col items-center justify-center gap-3 p-10 border-2 border-dashed rounded-xl cursor-pointer transition-colors
                  ${isDragging
                    ? 'border-accent-400 bg-accent-50'
                    : 'border-slate-300 bg-slate-50 hover:border-accent-400 hover:bg-accent-50'
                  }
                `}
              >
                <Upload className={`w-8 h-8 ${isDragging ? 'text-accent-500' : 'text-slate-400'}`} />
                <div className="text-center">
                  <p className="text-sm font-medium text-primary-800">
                    {isDragging ? 'Drop to upload' : 'Drag & drop your Excel file here'}
                  </p>
                  <p className="text-xs text-slate-500 mt-1">or click to browse — .xlsx / .xls</p>
                </div>
                <input
                  ref={fileInputRef}
                  type="file"
                  accept=".xlsx,.xls"
                  className="hidden"
                  onChange={handleFileInput}
                />
              </div>

              {parseError && (
                <div className="flex items-center gap-2 p-3 bg-danger-50 border border-danger-200 rounded-lg text-danger-700 text-sm">
                  <AlertTriangle className="w-4 h-4 flex-shrink-0" />
                  {parseError}
                </div>
              )}
            </div>
          )}

          {/* ── Stage: Preview ────────────────────────────────────────── */}
          {stage === 'preview' && (
            <>
              <div className="overflow-y-auto flex-1 p-6">
                {invalidCount > 0 && (
                  <div className="flex items-center gap-2 mb-4 p-3 bg-warning-50 border border-warning-200 rounded-lg text-warning-800 text-sm">
                    <AlertTriangle className="w-4 h-4 flex-shrink-0" />
                    {invalidCount} row(s) have validation errors and will be skipped. Fix them in the file and re-upload, or proceed with {validCount} valid row(s).
                  </div>
                )}
                <div className="overflow-x-auto rounded-xl border border-slate-200">
                  <table className="w-full text-sm">
                    <thead className="bg-slate-50">
                      <tr>
                        <th className="text-left px-3 py-2.5 text-xs font-semibold text-slate-500 uppercase tracking-wide">Row</th>
                        {COLUMNS.map((c) => (
                          <th key={c.key} className="text-left px-3 py-2.5 text-xs font-semibold text-slate-500 uppercase tracking-wide whitespace-nowrap">
                            {c.label}
                          </th>
                        ))}
                        <th className="text-left px-3 py-2.5 text-xs font-semibold text-slate-500 uppercase tracking-wide">Status</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {rows.map((row) => (
                        <tr key={row._rowNum} className={row._valid ? '' : 'bg-danger-50'}>
                          <td className="px-3 py-2.5 text-slate-400 tabular-nums">{row._rowNum}</td>
                          <td className="px-3 py-2.5 text-primary-800">{row.projectName || <span className="text-danger-400">—</span>}</td>
                          <td className="px-3 py-2.5 text-primary-800">{row.customerCode || <span className="text-danger-400">—</span>}</td>
                          <td className="px-3 py-2.5 font-mono text-xs">{row.macImeiIp || <span className="text-danger-400">—</span>}</td>
                          <td className="px-3 py-2.5">{row.model || <span className="text-danger-400">—</span>}</td>
                          <td className="px-3 py-2.5 font-mono text-xs text-slate-500">{row.currentFirmwareVersion || '0.0.0'}</td>
                          <td className="px-3 py-2.5">
                            {row._valid
                              ? <span className="inline-flex items-center gap-1 text-success-600 text-xs font-medium"><CheckCircle2 className="w-3.5 h-3.5" />Valid</span>
                              : <span className="inline-flex items-center gap-1 text-danger-600 text-xs font-medium" title={row._errors.join(', ')}><XCircle className="w-3.5 h-3.5" />{row._errors[0]}</span>
                            }
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>

              <div className="flex items-center justify-between px-6 py-4 border-t border-slate-100 flex-shrink-0 bg-white rounded-b-2xl">
                <button
                  type="button"
                  onClick={() => { setStage('upload'); setRows([]) }}
                  className="btn-secondary"
                >
                  <Trash2 className="w-4 h-4" />
                  Re-upload
                </button>
                <div className="flex items-center gap-3">
                  <span className="text-sm text-slate-500">{validCount} device(s) will be registered</span>
                  <button
                    type="button"
                    disabled={validCount === 0 || isLoading}
                    onClick={handleSubmit}
                    className="btn-primary disabled:opacity-50"
                  >
                    {isLoading ? (
                      <><Loader2 className="w-4 h-4 animate-spin" />Uploading…</>
                    ) : (
                      <><Upload className="w-4 h-4" />Upload {validCount} Device(s)</>
                    )}
                  </button>
                </div>
              </div>
            </>
          )}

          {/* ── Stage: Result ─────────────────────────────────────────── */}
          {stage === 'result' && result && (
            <div className="p-6 flex flex-col gap-5 overflow-y-auto">
              {/* Summary cards */}
              <div className="grid grid-cols-3 gap-4">
                <div className="text-center p-4 bg-slate-50 rounded-xl border border-slate-200">
                  <p className="text-2xl font-bold text-primary-900">{result.total}</p>
                  <p className="text-xs text-slate-500 mt-1">Total Rows</p>
                </div>
                <div className="text-center p-4 bg-success-50 rounded-xl border border-success-200">
                  <p className="text-2xl font-bold text-success-700">{result.succeeded}</p>
                  <p className="text-xs text-success-600 mt-1">Registered</p>
                </div>
                <div className={`text-center p-4 rounded-xl border ${result.failed > 0 ? 'bg-danger-50 border-danger-200' : 'bg-slate-50 border-slate-200'}`}>
                  <p className={`text-2xl font-bold ${result.failed > 0 ? 'text-danger-700' : 'text-slate-400'}`}>{result.failed}</p>
                  <p className={`text-xs mt-1 ${result.failed > 0 ? 'text-danger-600' : 'text-slate-400'}`}>Failed</p>
                </div>
              </div>

              {/* Per-row errors */}
              {result.errors.length > 0 && (
                <div>
                  <p className="text-sm font-semibold text-primary-800 mb-2">Failed rows</p>
                  <div className="rounded-xl border border-danger-200 overflow-hidden">
                    <table className="w-full text-sm">
                      <thead className="bg-danger-50">
                        <tr>
                          <th className="text-left px-3 py-2 text-xs font-semibold text-danger-700">Row</th>
                          <th className="text-left px-3 py-2 text-xs font-semibold text-danger-700">MAC / IMEI / IP</th>
                          <th className="text-left px-3 py-2 text-xs font-semibold text-danger-700">Reason</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-danger-100">
                        {result.errors.map((err) => (
                          <tr key={err.row}>
                            <td className="px-3 py-2 text-slate-500 tabular-nums">{err.row}</td>
                            <td className="px-3 py-2 font-mono text-xs text-primary-800">{err.identifier}</td>
                            <td className="px-3 py-2 text-danger-700">{err.error}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              )}

              <div className="flex justify-end gap-3 pt-2 border-t border-slate-100">
                {result.failed > 0 && (
                  <button
                    type="button"
                    onClick={() => { setStage('upload'); setRows([]); setResult(null) }}
                    className="btn-secondary"
                  >
                    Upload Again
                  </button>
                )}
                <Dialog.Close className="btn-primary">Done</Dialog.Close>
              </div>
            </div>
          )}

        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  )
}
