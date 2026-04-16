'use client'

import * as React from 'react'
import { SlidersHorizontal, X, ChevronDown } from 'lucide-react'
import { clsx } from 'clsx'

// ─── Types ────────────────────────────────────────────────────────────────────

export interface FilterOption {
  label: string
  value: string
}

export interface FilterField {
  key: string
  label: string
  options: FilterOption[]
}

export type FilterValues = Record<string, string>

interface FilterPopoverProps {
  fields: FilterField[]
  values: FilterValues
  onChange: (values: FilterValues) => void
  onClear: () => void
}

// ─── Active Filter Chips ──────────────────────────────────────────────────────

export function ActiveFilterChips({
  fields,
  values,
  onChange,
}: {
  fields: FilterField[]
  values: FilterValues
  onChange: (values: FilterValues) => void
}) {
  const active = fields.filter((f) => values[f.key])
  if (!active.length) return null

  return (
    <div className="flex flex-wrap gap-2">
      {active.map((f) => {
        const opt = f.options.find((o) => o.value === values[f.key])
        return (
          <span
            key={f.key}
            className="inline-flex items-center gap-1.5 bg-accent-50 text-accent-700 border border-accent-200 rounded-full px-3 py-0.5 text-xs font-medium"
          >
            <span className="text-accent-500">{f.label}:</span>
            {opt?.label ?? values[f.key]}
            <button
              type="button"
              onClick={() => onChange({ ...values, [f.key]: '' })}
              className="hover:text-red-500 transition-colors ml-0.5"
              aria-label={`Remove ${f.label} filter`}
            >
              <X className="w-3 h-3" />
            </button>
          </span>
        )
      })}
    </div>
  )
}

// ─── FilterPopover ────────────────────────────────────────────────────────────

export function FilterPopover({ fields, values, onChange, onClear }: FilterPopoverProps) {
  const [open, setOpen] = React.useState(false)
  const panelRef = React.useRef<HTMLDivElement>(null)
  const triggerRef = React.useRef<HTMLButtonElement>(null)

  const activeCount = fields.filter((f) => values[f.key]).length

  // Close on outside click
  React.useEffect(() => {
    if (!open) return
    const handler = (e: MouseEvent) => {
      if (
        panelRef.current && !panelRef.current.contains(e.target as Node) &&
        triggerRef.current && !triggerRef.current.contains(e.target as Node)
      ) {
        setOpen(false)
      }
    }
    document.addEventListener('mousedown', handler)
    return () => document.removeEventListener('mousedown', handler)
  }, [open])

  // Close on Escape
  React.useEffect(() => {
    if (!open) return
    const handler = (e: KeyboardEvent) => { if (e.key === 'Escape') setOpen(false) }
    document.addEventListener('keydown', handler)
    return () => document.removeEventListener('keydown', handler)
  }, [open])

  return (
    <div className="relative">
      {/* Trigger */}
      <button
        ref={triggerRef}
        type="button"
        onClick={() => setOpen((v) => !v)}
        className={clsx(
          'inline-flex items-center gap-2 px-3 py-2 text-sm font-medium rounded-lg border transition-colors',
          activeCount > 0
            ? 'border-accent-400 bg-accent-50 text-accent-700 hover:bg-accent-100'
            : 'border-slate-300 bg-white text-slate-600 hover:bg-slate-50',
        )}
      >
        <SlidersHorizontal className="w-4 h-4" />
        Filters
        {activeCount > 0 && (
          <span className="inline-flex items-center justify-center w-5 h-5 rounded-full bg-accent-600 text-white text-xs font-bold">
            {activeCount}
          </span>
        )}
        <ChevronDown className={clsx('w-3.5 h-3.5 transition-transform', open && 'rotate-180')} />
      </button>

      {/* Panel */}
      {open && (
        <div
          ref={panelRef}
          className="absolute left-0 top-full mt-2 z-30 bg-white border border-slate-200 rounded-xl shadow-lg p-4 min-w-[260px] space-y-4"
        >
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-slate-500 uppercase tracking-wide">
              Filters
            </span>
            {activeCount > 0 && (
              <button
                type="button"
                onClick={() => { onClear(); setOpen(false) }}
                className="text-xs text-red-500 hover:text-red-700 font-medium transition-colors"
              >
                Clear all
              </button>
            )}
          </div>

          {fields.map((field) => (
            <div key={field.key} className="space-y-1">
              <label className="block text-xs font-medium text-slate-600">{field.label}</label>
              <select
                value={values[field.key] ?? ''}
                onChange={(e) => onChange({ ...values, [field.key]: e.target.value })}
                className="w-full text-sm border border-slate-300 rounded-lg px-2 py-1.5 outline-none focus:ring-2 focus:ring-accent-500 bg-white"
              >
                <option value="">All</option>
                {field.options.map((opt) => (
                  <option key={opt.value} value={opt.value}>
                    {opt.label}
                  </option>
                ))}
              </select>
            </div>
          ))}

          <button
            type="button"
            onClick={() => setOpen(false)}
            className="w-full mt-1 px-3 py-1.5 text-sm font-medium rounded-lg bg-accent-600 text-white hover:bg-accent-700 transition-colors"
          >
            Apply
          </button>
        </div>
      )}
    </div>
  )
}
