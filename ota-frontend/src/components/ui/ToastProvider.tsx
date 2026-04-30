'use client'

import * as React from 'react'
import * as ToastPrimitives from '@radix-ui/react-toast'
import { X, CheckCircle, AlertCircle, AlertTriangle, Info, Bell } from 'lucide-react'
import { clsx } from 'clsx'

// ─── Toast Context ────────────────────────────────────────────────────────────

type ToastVariant = 'success' | 'error' | 'warning' | 'info'

interface Toast {
  id: string
  title: string
  description?: string
  variant?: ToastVariant
  duration?: number
  /** Fires when the toast is closed (X clicked, swiped, or auto-timeout). Optional. */
  onDismiss?: () => void
}

interface NotificationRecord extends Omit<Toast, 'duration'> {
  createdAt: Date
  read: boolean
}

interface ToastContextValue {
  toast: (options: Omit<Toast, 'id'>) => void
  dismiss: (id: string) => void
  notifications: NotificationRecord[]
  unreadCount: number
  markAllRead: () => void
  clearNotifications: () => void
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
  const [notifications, setNotifications] = React.useState<NotificationRecord[]>([])

  const toast = React.useCallback((options: Omit<Toast, 'id'>) => {
    const id = Math.random().toString(36).slice(2)
    setToasts((prev) => [...prev, { id, duration: 5000, ...options }])
    setNotifications((prev) => [
      { id, title: options.title, description: options.description, variant: options.variant, createdAt: new Date(), read: false },
      ...prev.slice(0, 49),
    ])
  }, [])

  const dismiss = React.useCallback((id: string) => {
    setToasts((prev) => {
      const closing = prev.find((t) => t.id === id)
      // Fire the caller's hook so it can mark the server-side notification as read.
      try { closing?.onDismiss?.() } catch { /* swallow — UI close must not be blocked */ }
      return prev.filter((t) => t.id !== id)
    })
    // Mark as read in the local notification history.
    setNotifications((prev) => prev.map((n) => n.id === id ? { ...n, read: true } : n))
  }, [])

  const markAllRead = React.useCallback(() => {
    setNotifications((prev) => prev.map((n) => ({ ...n, read: true })))
  }, [])

  const clearNotifications = React.useCallback(() => {
    setNotifications([])
  }, [])

  const unreadCount = notifications.filter((n) => !n.read).length

  return (
    <ToastContext.Provider value={{ toast, dismiss, notifications, unreadCount, markAllRead, clearNotifications }}>
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

// ─── Notification Bell (re-exported for use in Header) ────────────────────────

export function NotificationHistory() {
  const { notifications, unreadCount, markAllRead, clearNotifications } = useToast()
  const [open, setOpen] = React.useState(false)

  const variantIcon: Record<ToastVariant, React.ReactNode> = {
    success: <CheckCircle className="w-3.5 h-3.5 text-success-500" />,
    error: <AlertCircle className="w-3.5 h-3.5 text-danger-500" />,
    warning: <AlertTriangle className="w-3.5 h-3.5 text-warning-500" />,
    info: <Info className="w-3.5 h-3.5 text-accent-500" />,
  }

  return (
    <div className="relative">
      <button
        onClick={() => { setOpen(!open); if (!open) markAllRead() }}
        className="relative p-2 rounded-lg text-slate-500 hover:text-primary-700 hover:bg-slate-100 transition-colors"
        title="Notifications"
      >
        <Bell className="w-5 h-5" />
        {unreadCount > 0 && (
          <span className="absolute top-1 right-1 w-2 h-2 rounded-full bg-danger-500 border border-white" />
        )}
      </button>

      {open && (
        <>
          <div className="fixed inset-0 z-40" onClick={() => setOpen(false)} />
          <div className="absolute right-0 top-full mt-2 w-80 bg-white rounded-xl shadow-xl border border-slate-200 z-50 overflow-hidden">
            <div className="flex items-center justify-between px-4 py-3 border-b border-slate-100">
              <span className="text-sm font-semibold text-primary-900">Notifications</span>
              {notifications.length > 0 && (
                <button onClick={clearNotifications} className="text-xs text-slate-400 hover:text-slate-600 transition-colors">
                  Clear all
                </button>
              )}
            </div>
            <div className="max-h-72 overflow-y-auto divide-y divide-slate-50">
              {notifications.length === 0 ? (
                <div className="flex flex-col items-center gap-2 py-8 text-slate-400">
                  <Bell className="w-8 h-8 opacity-30" />
                  <p className="text-sm">No notifications yet</p>
                </div>
              ) : notifications.map((n) => (
                <div key={n.id} className={clsx('flex items-start gap-3 px-4 py-3 text-sm', !n.read && 'bg-accent-50/30')}>
                  {variantIcon[n.variant ?? 'info']}
                  <div className="flex-1 min-w-0">
                    <p className={clsx('font-medium truncate', n.read ? 'text-slate-500' : 'text-primary-900')}>{n.title}</p>
                    {n.description && <p className="text-xs text-slate-400 truncate">{n.description}</p>}
                    <p className="text-[10px] text-slate-300 mt-0.5">{n.createdAt.toLocaleTimeString()}</p>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </>
      )}
    </div>
  )
}
