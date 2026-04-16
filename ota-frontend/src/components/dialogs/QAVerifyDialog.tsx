'use client'

import * as React from 'react'
import * as Dialog from '@radix-ui/react-dialog'
import { FlaskConical, X, Loader2, CheckSquare, Square } from 'lucide-react'
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

const QA_CHECKS = [
  {
    id: 'testCase',
    label: 'Test Cases reviewed',
    description: 'All test case documents have been reviewed and executed.',
  },
  {
    id: 'testResult',
    label: 'Test Results verified',
    description: 'Test result documents have been uploaded and all results are acceptable.',
  },
  {
    id: 'bugList',
    label: 'Bug List assessed',
    description: 'All reported bugs have been reviewed; no critical or blocker issues remain open.',
  },
  {
    id: 'eventLog',
    label: 'Event Log reviewed',
    description: 'QA session event log has been reviewed and all activities are accounted for.',
  },
] as const

type CheckId = typeof QA_CHECKS[number]['id']

function CheckboxRow({
  id,
  label,
  description,
  checked,
  onChange,
}: {
  id: CheckId
  label: string
  description: string
  checked: boolean
  onChange: (id: CheckId, value: boolean) => void
}) {
  return (
    <label
      htmlFor={id}
      className={`flex items-start gap-3 p-3 rounded-xl border cursor-pointer transition-all select-none
        ${checked
          ? 'bg-success-50 border-success-300'
          : 'bg-white border-slate-200 hover:border-accent-300 hover:bg-accent-50'
        }`}
    >
      <div className="mt-0.5 flex-shrink-0">
        <input
          id={id}
          type="checkbox"
          className="sr-only"
          checked={checked}
          onChange={(e) => onChange(id, e.target.checked)}
        />
        {checked ? (
          <CheckSquare className="w-5 h-5 text-success-600" />
        ) : (
          <Square className="w-5 h-5 text-slate-400" />
        )}
      </div>
      <div>
        <p className={`text-sm font-semibold ${checked ? 'text-success-800' : 'text-primary-800'}`}>
          {label}
        </p>
        <p className="text-xs text-slate-500 mt-0.5">{description}</p>
      </div>
    </label>
  )
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
  const [checks, setChecks] = React.useState<Record<CheckId, boolean>>({
    testCase: false,
    testResult: false,
    bugList: false,
    eventLog: false,
  })

  const allChecked = Object.values(checks).every(Boolean)
  const checkedCount = Object.values(checks).filter(Boolean).length

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
    },
    onError: () => {
      toast({ title: 'QA Verification failed', description: 'Could not complete QA verification.', variant: 'error' })
    },
  })

  // Reset state when dialog closes
  React.useEffect(() => {
    if (!open) {
      setRemarks('')
      setChecks({ testCase: false, testResult: false, bugList: false, eventLog: false })
    }
  }, [open])

  const handleCheckChange = (id: CheckId, value: boolean) => {
    setChecks(prev => ({ ...prev, [id]: value }))
  }

  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50" />
        <Dialog.Content className="fixed left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 bg-white rounded-2xl shadow-2xl z-50 w-full max-w-lg p-6 max-h-[90vh] overflow-y-auto">

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
                Verify firmware{' '}
                <span className="font-mono font-semibold text-accent-600">{firmwareVersion}</span>{' '}
                as QA-tested and ready for approval.
              </Dialog.Description>
            </div>
            <Dialog.Close className="text-slate-400 hover:text-slate-600 transition-colors p-1">
              <X className="w-5 h-5" />
            </Dialog.Close>
          </div>

          {/* Progress indicator */}
          <div className="flex items-center justify-between mb-3">
            <p className="text-sm font-semibold text-primary-800">
              Confirmation Checklist
            </p>
            <span className={`text-xs font-semibold px-2 py-0.5 rounded-full ${
              allChecked
                ? 'bg-success-100 text-success-700'
                : 'bg-slate-100 text-slate-500'
            }`}>
              {checkedCount} / {QA_CHECKS.length} confirmed
            </span>
          </div>

          {/* Checkboxes */}
          <div className="space-y-2 mb-5">
            {QA_CHECKS.map((check) => (
              <CheckboxRow
                key={check.id}
                id={check.id}
                label={check.label}
                description={check.description}
                checked={checks[check.id]}
                onChange={handleCheckChange}
              />
            ))}
          </div>

          {/* QA Remarks */}
          <div className="mb-6">
            <label className="label">QA Remarks (Optional)</label>
            <textarea
              value={remarks}
              onChange={(e) => setRemarks(e.target.value)}
              placeholder="Notes about testing performed, test environments, any observations..."
              rows={3}
              className="input resize-none"
            />
          </div>

          {/* Warning if not all checked */}
          {!allChecked && (
            <p className="text-xs text-warning-600 bg-warning-50 border border-warning-200 rounded-lg px-3 py-2 mb-4">
              Please confirm all {QA_CHECKS.length} checklist items before marking as QA Verified.
            </p>
          )}

          {/* Actions */}
          <div className="flex items-center justify-end gap-3">
            <Dialog.Close className="btn-secondary">Cancel</Dialog.Close>
            <button
              onClick={() => mutation.mutate()}
              disabled={!allChecked || mutation.isPending}
              className="btn-primary disabled:opacity-50 disabled:cursor-not-allowed"
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
