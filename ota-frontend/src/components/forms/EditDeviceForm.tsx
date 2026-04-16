'use client'

import * as React from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import * as Dialog from '@radix-ui/react-dialog'
import { X, Loader2 } from 'lucide-react'
import { Device, UpdateDeviceRequest } from '@/types'

// ─── Schema ───────────────────────────────────────────────────────────────────

const editDeviceSchema = z.object({
  model: z.string().min(1, 'Model is required').max(100),
  currentFirmwareVersion: z.string().max(50).optional().or(z.literal('')),
  publishTopic: z.string().max(200).optional().or(z.literal('')),
})

type EditDeviceFormValues = z.infer<typeof editDeviceSchema>

// ─── Props ────────────────────────────────────────────────────────────────────

interface EditDeviceFormProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  device: Device
  onSubmit: (data: UpdateDeviceRequest) => Promise<void>
  isLoading?: boolean
}

// ─── Component ────────────────────────────────────────────────────────────────

export function EditDeviceForm({
  open,
  onOpenChange,
  device,
  onSubmit,
  isLoading = false,
}: EditDeviceFormProps) {
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<EditDeviceFormValues>({
    resolver: zodResolver(editDeviceSchema),
    defaultValues: {
      model: device.model ?? '',
      currentFirmwareVersion: device.currentFirmwareVersion ?? '',
      publishTopic: device.publishTopic ?? `OTA/${device.macImeiIp ?? device.serialNumber ?? device.deviceId}/Status`,
    },
  })

  // Reset with fresh device data each time the dialog opens
  React.useEffect(() => {
    if (open) {
      reset({
        model: device.model ?? '',
        currentFirmwareVersion: device.currentFirmwareVersion ?? '',
        publishTopic: device.publishTopic ?? `OTA/${device.macImeiIp ?? device.serialNumber ?? device.deviceId}/Status`,
      })
    }
  }, [open, device, reset])

  async function handleFormSubmit(values: EditDeviceFormValues) {
    await onSubmit({
      model: values.model,
      currentFirmwareVersion: values.currentFirmwareVersion || undefined,
      publishTopic: values.publishTopic || undefined,
    })
  }

  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50" />
        <Dialog.Content className="fixed left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 bg-white rounded-2xl shadow-2xl z-50 w-full max-w-md max-h-[90vh] overflow-y-auto">
          <div className="p-6">
            {/* Header */}
            <div className="flex items-center justify-between mb-4">
              <div>
                <Dialog.Title className="text-lg font-semibold text-primary-900">
                  Edit Device
                </Dialog.Title>
                <Dialog.Description className="text-sm text-slate-500">
                  {device.macImeiIp ?? device.serialNumber ?? device.deviceId}
                </Dialog.Description>
              </div>
              <Dialog.Close className="text-slate-400 hover:text-slate-600 transition-colors">
                <X className="w-5 h-5" />
              </Dialog.Close>
            </div>

            {/* Read-only info strip */}
            <div className="grid grid-cols-2 gap-3 mb-5 p-3 bg-slate-50 rounded-xl text-sm">
              <div>
                <p className="text-xs text-slate-400 mb-0.5">Project</p>
                <p className="font-medium text-primary-800">{device.projectName ?? '—'}</p>
              </div>
              <div>
                <p className="text-xs text-slate-400 mb-0.5">Customer</p>
                <p className="font-medium text-primary-800">{device.customerName ?? device.customerId}</p>
              </div>
            </div>

            <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-4">

              {/* Model */}
              <div>
                <label className="label">
                  Model <span className="text-danger-500">*</span>
                </label>
                <input
                  type="text"
                  placeholder="e.g. EDGE-GW-V2"
                  className={`input ${errors.model ? 'border-danger-400' : ''}`}
                  {...register('model')}
                />
                {errors.model && (
                  <p className="form-error">{errors.model.message}</p>
                )}
              </div>

              {/* Current Firmware Version */}
              <div>
                <label className="label">Initial Firmware Version</label>
                <input
                  type="text"
                  placeholder="e.g. 1.0.0"
                  className="input font-mono"
                  {...register('currentFirmwareVersion')}
                />
                <p className="text-xs text-slate-400 mt-1">
                  Leave blank to keep the current value.
                </p>
              </div>

              {/* Publish Topic */}
              <div>
                <label className="label">Publish Topic</label>
                <input
                  type="text"
                  placeholder={`OTA/${device.macImeiIp ?? device.serialNumber ?? 'DEVICE'}/Status`}
                  className="input font-mono text-sm"
                  {...register('publishTopic')}
                />
                <p className="text-xs text-slate-400 mt-1">
                  Updates the MQTT topic and re-publishes device info when saved.
                </p>
              </div>

              {/* Actions */}
              <div className="flex items-center justify-end gap-3 pt-4 border-t border-slate-100">
                <Dialog.Close className="btn-secondary">Cancel</Dialog.Close>
                <button type="submit" disabled={isLoading} className="btn-primary">
                  {isLoading ? (
                    <>
                      <Loader2 className="w-4 h-4 animate-spin" />
                      Saving...
                    </>
                  ) : (
                    'Save Changes'
                  )}
                </button>
              </div>
            </form>
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  )
}
