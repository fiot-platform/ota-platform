'use client'

import * as React from 'react'
import * as Dialog from '@radix-ui/react-dialog'
import { AlertTriangle, X } from 'lucide-react'
import { clsx } from 'clsx'

interface ConfirmDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  title: string
  message: string | React.ReactNode
  confirmLabel?: string
  cancelLabel?: string
  variant?: 'destructive' | 'warning' | 'default'
  onConfirm: () => void
  isLoading?: boolean
  icon?: React.ReactNode
}

export function ConfirmDialog({
  open,
  onOpenChange,
  title,
  message,
  confirmLabel = 'Confirm',
  cancelLabel = 'Cancel',
  variant = 'destructive',
  onConfirm,
  isLoading = false,
  icon,
}: ConfirmDialogProps) {
  const confirmStyles = {
    destructive: 'bg-danger-600 hover:bg-danger-700 focus:ring-danger-500 text-white',
    warning: 'bg-warning-500 hover:bg-warning-600 focus:ring-warning-400 text-white',
    default: 'bg-accent-600 hover:bg-accent-700 focus:ring-accent-500 text-white',
  }

  const iconBgStyles = {
    destructive: 'bg-danger-100',
    warning: 'bg-warning-100',
    default: 'bg-accent-100',
  }

  const iconColorStyles = {
    destructive: 'text-danger-600',
    warning: 'text-warning-600',
    default: 'text-accent-600',
  }

  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50 data-[state=open]:animate-fade-in" />
        <Dialog.Content className="fixed left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 bg-white rounded-2xl shadow-2xl z-50 w-full max-w-md p-6 data-[state=open]:animate-fade-in">
          <div className="flex items-start gap-4">
            <div className={clsx('w-12 h-12 rounded-full flex items-center justify-center flex-shrink-0', iconBgStyles[variant])}>
              {icon ?? <AlertTriangle className={clsx('w-6 h-6', iconColorStyles[variant])} />}
            </div>
            <div className="flex-1 min-w-0">
              <Dialog.Title className="text-lg font-semibold text-primary-900 mb-2">
                {title}
              </Dialog.Title>
              <Dialog.Description asChild>
                <div className="text-sm text-slate-600 leading-relaxed">
                  {typeof message === 'string' ? <p>{message}</p> : message}
                </div>
              </Dialog.Description>
            </div>
            <Dialog.Close
              onClick={() => onOpenChange(false)}
              className="text-slate-400 hover:text-slate-600 transition-colors flex-shrink-0"
            >
              <X className="w-5 h-5" />
            </Dialog.Close>
          </div>

          <div className="flex items-center justify-end gap-3 mt-6">
            <button
              type="button"
              onClick={() => onOpenChange(false)}
              disabled={isLoading}
              className="btn-secondary disabled:opacity-50"
            >
              {cancelLabel}
            </button>
            <button
              type="button"
              onClick={onConfirm}
              disabled={isLoading}
              className={clsx(
                'inline-flex items-center justify-center gap-2 px-4 py-2 rounded-lg text-sm font-medium transition-all focus:outline-none focus:ring-2 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed',
                confirmStyles[variant]
              )}
            >
              {isLoading ? (
                <>
                  <svg className="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                  </svg>
                  Processing...
                </>
              ) : confirmLabel}
            </button>
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  )
}
