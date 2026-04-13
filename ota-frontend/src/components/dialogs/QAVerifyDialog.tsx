'use client'

import * as React from 'react'
import * as Dialog from '@radix-ui/react-dialog'
import { FlaskConical, X, Loader2 } from 'lucide-react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { firmwareService } from '@/services/firmware.service'
import { useToast } from '@/components/ui/ToastProvider'

interface QAVerifyDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  firmwareId: string
  firmwareVersion: string
  onSuccess?: () => void
}

export function QAVerifyDialog({
  open,
  onOpenChange,
  firmwareId,
  firmwareVersion,
  onSuccess,
}: QAVerifyDialogProps) {
  const { toast } = useToast()
  const queryClient = useQueryClient()
  const [remarks, setRemarks] = React.useState('')

  const mutation = useMutation({
    mutationFn: () => firmwareService.qaVerifyFirmware(firmwareId, { qaRemarks: remarks }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['firmware'] })
      queryClient.invalidateQueries({ queryKey: ['firmware', firmwareId] })
      toast({
        title: 'QA Verification complete',
        description: `${firmwareVersion} has been QA verified.`,
        variant: 'success',
      })
      onSuccess?.()
      onOpenChange(false)
      setRemarks('')
    },
    onError: () => {
      toast({ title: 'QA Verification failed', description: 'Could not complete QA verification.', variant: 'error' })
    },
  })

  React.useEffect(() => {
    if (!open) setRemarks('')
  }, [open])

  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50" />
        <Dialog.Content className="fixed left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 bg-white rounded-2xl shadow-2xl z-50 w-full max-w-md p-6">
          {/* Header */}
          <div className="flex items-start gap-4 mb-5">
            <div className="w-12 h-12 bg-accent-100 rounded-full flex items-center justify-center flex-shrink-0">
              <FlaskConical className="w-6 h-6 text-accent-600" />
            </div>
            <div className="flex-1">
              <Dialog.Title className="text-lg font-semibold text-primary-900">
                QA Verification
              </Dialog.Title>
              <Dialog.Description className="text-sm text-slate-500 mt-0.5">
                Verify firmware <span className="font-mono font-semibold text-accent-600">{firmwareVersion}</span> as QA-tested and ready for approval.
              </Dialog.Description>
            </div>
            <Dialog.Close className="text-slate-400 hover:text-slate-600 transition-colors">
              <X className="w-5 h-5" />
            </Dialog.Close>
          </div>

          {/* QA Checklist */}
          <div className="p-3 bg-accent-50 border border-accent-200 rounded-lg mb-4 text-sm">
            <p className="font-semibold text-accent-800 mb-2">By verifying this firmware, you confirm:</p>
            <ul className="space-y-1 text-accent-700">
              <li className="flex items-start gap-2">
                <span className="text-accent-500 mt-0.5">•</span>
                All automated tests have passed
              </li>
              <li className="flex items-start gap-2">
                <span className="text-accent-500 mt-0.5">•</span>
                Manual testing was completed on target hardware
              </li>
              <li className="flex items-start gap-2">
                <span className="text-accent-500 mt-0.5">•</span>
                No critical bugs or security issues were found
              </li>
              <li className="flex items-start gap-2">
                <span className="text-accent-500 mt-0.5">•</span>
                Firmware binary integrity is verified
              </li>
            </ul>
          </div>

          {/* QA Remarks */}
          <div className="mb-6">
            <label className="label">QA Remarks (Optional)</label>
            <textarea
              value={remarks}
              onChange={(e) => setRemarks(e.target.value)}
              placeholder="Notes about testing performed, test environments, any observations..."
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
              className="btn-primary"
            >
              {mutation.isPending ? (
                <><Loader2 className="w-4 h-4 animate-spin" /> Verifying...</>
              ) : (
                <><FlaskConical className="w-4 h-4" /> Mark as QA Verified</>
              )}
            </button>
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  )
}
