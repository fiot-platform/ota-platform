'use client'

import * as React from 'react'
import * as Dialog from '@radix-ui/react-dialog'
import { XCircle, X, Loader2, AlertCircle } from 'lucide-react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { firmwareService } from '@/services/firmware.service'
import { useToast } from '@/components/ui/ToastProvider'

interface RejectFirmwareDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  firmwareId: string
  firmwareVersion: string
  onSuccess?: () => void
}

export function RejectFirmwareDialog({
  open,
  onOpenChange,
  firmwareId,
  firmwareVersion,
  onSuccess,
}: RejectFirmwareDialogProps) {
  const { toast } = useToast()
  const queryClient = useQueryClient()
  const [reason, setReason] = React.useState('')
  const [error, setError] = React.useState('')

  const mutation = useMutation({
    mutationFn: () => firmwareService.rejectFirmware(firmwareId, { rejectionReason: reason }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['firmware'] })
      queryClient.invalidateQueries({ queryKey: ['firmware', firmwareId] })
      toast({ title: 'Firmware rejected', description: `${firmwareVersion} has been rejected.`, variant: 'warning' })
      onSuccess?.()
      onOpenChange(false)
      setReason('')
      setError('')
    },
    onError: () => {
      toast({ title: 'Rejection failed', description: 'Could not reject firmware at this time.', variant: 'error' })
    },
  })

  const handleSubmit = () => {
    if (!reason.trim()) {
      setError('Rejection reason is required')
      return
    }
    setError('')
    mutation.mutate()
  }

  React.useEffect(() => {
    if (!open) {
      setReason('')
      setError('')
    }
  }, [open])

  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50" />
        <Dialog.Content className="fixed left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 bg-white rounded-2xl shadow-2xl z-50 w-full max-w-md p-6">
          {/* Header */}
          <div className="flex items-start gap-4 mb-5">
            <div className="w-12 h-12 bg-danger-100 rounded-full flex items-center justify-center flex-shrink-0">
              <XCircle className="w-6 h-6 text-danger-600" />
            </div>
            <div className="flex-1">
              <Dialog.Title className="text-lg font-semibold text-primary-900">
                Reject Firmware
              </Dialog.Title>
              <Dialog.Description className="text-sm text-slate-500 mt-0.5">
                Rejecting <span className="font-mono font-semibold text-accent-600">{firmwareVersion}</span>. Please provide a reason for the rejection.
              </Dialog.Description>
            </div>
            <Dialog.Close className="text-slate-400 hover:text-slate-600 transition-colors">
              <X className="w-5 h-5" />
            </Dialog.Close>
          </div>

          {/* Warning Banner */}
          <div className="flex items-start gap-2 p-3 bg-danger-50 border border-danger-200 rounded-lg mb-4 text-sm text-danger-700">
            <AlertCircle className="w-4 h-4 flex-shrink-0 mt-0.5" />
            <p>This action will set the firmware status to Rejected. The firmware cannot be deployed until re-submitted.</p>
          </div>

          {/* Reason */}
          <div className="mb-6">
            <label className="label">
              Rejection Reason <span className="text-danger-500">*</span>
            </label>
            <textarea
              value={reason}
              onChange={(e) => { setReason(e.target.value); if (error) setError('') }}
              placeholder="Provide a clear reason for rejection (e.g., security vulnerability found, failed integration tests...)"
              rows={4}
              className={`input resize-none ${error ? 'border-danger-400' : ''}`}
            />
            {error && <p className="form-error">{error}</p>}
          </div>

          {/* Actions */}
          <div className="flex items-center justify-end gap-3">
            <Dialog.Close className="btn-secondary">Cancel</Dialog.Close>
            <button
              onClick={handleSubmit}
              disabled={mutation.isPending}
              className="btn-danger"
            >
              {mutation.isPending ? (
                <><Loader2 className="w-4 h-4 animate-spin" /> Rejecting...</>
              ) : (
                <><XCircle className="w-4 h-4" /> Reject Firmware</>
              )}
            </button>
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  )
}
