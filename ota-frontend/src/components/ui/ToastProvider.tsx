'use client'

import * as React from 'react'
import * as ToastPrimitives from '@radix-ui/react-toast'
import { X, CheckCircle, AlertCircle, AlertTriangle, Info } from 'lucide-react'
import { clsx } from 'clsx'

// ─── Toast Context ────────────────────────────────────────────────────────────

type ToastVariant = 'success' | 'error' | 'warning' | 'info'

interface Toast {
  id: string
  title: string
  description?: string
  variant?: ToastVariant
  duration?: number
}

interface ToastContextValue {
  toast: (options: Omit<Toast, 'id'>) => void
  dismiss: (id: string) => void
}

const ToastContext = React.createContext<ToastContextValue | null>(null)

export function useToast(): ToastContextValue {
  const ctx = React.useContext(ToastContext)
  if (!ctx) throw new Error('useToast must be used within ToastProvider')
  return ctx
}

// ─── Toast Provider ───────────────────────────────────────────────────────────

export function ToastProvider({ children }: { children: React.ReactNode }) {
  const [toasts, setToasts] = React.useState<Toast[]>([])

  const toast = React.useCallback((options: Omit<Toast, 'id'>) => {
    const id = Math.random().toString(36).slice(2)
    setToasts((prev) => [...prev, { id, ...options }])
  }, [])

  const dismiss = React.useCallback((id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id))
  }, [])

  return (
    <ToastContext.Provider value={{ toast, dismiss }}>
      <ToastPrimitives.Provider swipeDirection="right">
        {children}
        {toasts.map((t) => (
          <ToastItem key={t.id} toast={t} onDismiss={dismiss} />
        ))}
        <ToastPrimitives.Viewport className="fixed top-4 right-4 z-[100] flex flex-col gap-2 w-full max-w-sm" />
      </ToastPrimitives.Provider>
    </ToastContext.Provider>
  )
}

// ─── Toast Item ───────────────────────────────────────────────────────────────

function ToastItem({
  toast,
  onDismiss,
}: {
  toast: Toast
  onDismiss: (id: string) => void
}) {
  const variantStyles: Record<ToastVariant, string> = {
    success: 'border-success-500 bg-success-50',
    error: 'border-danger-500 bg-danger-50',
    warning: 'border-warning-500 bg-warning-50',
    info: 'border-accent-500 bg-accent-50',
  }

  const iconMap: Record<ToastVariant, React.ReactNode> = {
    success: <CheckCircle className="w-5 h-5 text-success-600 flex-shrink-0" />,
    error: <AlertCircle className="w-5 h-5 text-danger-600 flex-shrink-0" />,
    warning: <AlertTriangle className="w-5 h-5 text-warning-600 flex-shrink-0" />,
    info: <Info className="w-5 h-5 text-accent-600 flex-shrink-0" />,
  }

  const variant = toast.variant ?? 'info'

  return (
    <ToastPrimitives.Root
      duration={toast.duration ?? 5000}
      onOpenChange={(open) => { if (!open) onDismiss(toast.id) }}
      className={clsx(
        'pointer-events-auto flex items-start gap-3 w-full rounded-xl border-l-4 p-4 shadow-lg bg-white',
        'data-[state=open]:animate-fade-in data-[state=closed]:opacity-0 transition-all duration-200',
        variantStyles[variant]
      )}
    >
      {iconMap[variant]}
      <div className="flex-1 min-w-0">
        <ToastPrimitives.Title className="text-sm font-semibold text-primary-900">
          {toast.title}
        </ToastPrimitives.Title>
        {toast.description && (
          <ToastPrimitives.Description className="text-xs text-slate-600 mt-0.5">
            {toast.description}
          </ToastPrimitives.Description>
        )}
      </div>
      <ToastPrimitives.Close
        onClick={() => onDismiss(toast.id)}
        className="flex-shrink-0 text-slate-400 hover:text-slate-600 transition-colors"
      >
        <X className="w-4 h-4" />
      </ToastPrimitives.Close>
    </ToastPrimitives.Root>
  )
}
