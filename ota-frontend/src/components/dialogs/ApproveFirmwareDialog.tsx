'use client'

import * as React from 'react'
import * as Dialog from '@radix-ui/react-dialog'
import { CheckCircle, X, Loader2 } from 'lucide-react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { firmwareService } from '@/services/firmware.service'
import { useToast } from '@/components/ui/ToastProvider'

interface ApproveFirmwareDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  firmwareId: string
  firmwareVersion: string
  onSuccess?: () => void
}

export function ApproveFirmwareDialog({
  open,
  onOpenChange,
  firmwareId,
  firmwareVersion,
  onSuccess,
}: ApproveFirmwareDialogProps) {
  const { toast } = useToast()
  const queryClient = useQueryClient()
  const [notes, setNotes] = React.useState('')

  const mutation = useMutation({
    mutationFn: () => firmwareService.approveFirmware(firmwareId, { approvalNotes: notes }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['firmware'] })
      queryClient.invalidateQueries({ queryKey: ['firmware', firmwareId] })
      toast({ title: 'Firmware approved', description: `${firmwareVersion} has been approved.`, variant: 'success' })
      onSuccess?.()
      onOpenChange(false)
      setNotes('')
    },
    onError: () => {
      toast({ title: 'Approval failed', description: 'Could not approve firmware at this time.', variant: 'error' })
    },
  })

  React.useEffect(() => {
    if (!open) setNotes('')
  }, [open])

  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50" />
        <Dialog.Content className="fixed left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 bg-white rounded-2xl shadow-2xl z-50 w-full max-w-md p-6">
          {/* Header */}
          <div className="flex items-start gap-4 mb-5">
            <div className="w-12 h-12 bg-success-100 rounded-full flex items-center justify-center flex-shrink-0">
              <CheckCircle className="w-6 h-6 text-success-600" />
            </div>
            <div className="flex-1">
              <Dialog.Title className="text-lg font-semibold text-primary-900">
                Approve Firmware
              </Dialog.Title>
              <Dialog.Description className="text-sm text-slate-500 mt-0.5">
                Approving firmware version <span className="font-mono font-semibold text-accent-600">{firmwareVersion}</span>. This will make it eligible for rollout.
              </Dialog.Description>
            </div>
            <Dialog.Close className="text-slate-400 hover:text-slate-600 transition-colors">
              <X className="w-5 h-5" />
            </Dialog.Close>
          </div>

          {/* Notes */}
          <div className="mb-6">
            <label className="label">Approval Notes (Optional)</label>
            <textarea
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder="Add any notes or comments about this approval..."
              rows={4}
              className="input resize-none"
            />
          </div>

          {/* Actions */}
          <div className="flex items-center justify-end gap-3">
            <Dialog.Close className="btn-secondary">Cancel</Dialog.Close>
            <button
              onClick={() => mutation.mutate()}
              disabled={mutation.isPending}
              className="btn-primary bg-success-600 hover:bg-success-700"
            >
              {mutation.isPending ? (
                <><Loader2 className="w-4 h-4 animate-spin" /> Approving...</>
              ) : (
                <><CheckCircle className="w-4 h-4" /> Approve Firmware</>
              )}
            </button>
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  )
}
