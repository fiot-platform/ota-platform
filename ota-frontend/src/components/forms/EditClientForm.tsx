'use client'

import * as React from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import * as Dialog from '@radix-ui/react-dialog'
import { X, Loader2 } from 'lucide-react'
import { Client, UpdateClientRequest } from '@/types'

// ─── Schema ───────────────────────────────────────────────────────────────────

const schema = z.object({
  name:         z.string().min(1, 'Name is required').max(200),
  code:         z.string().min(1, 'Code is required').max(50).regex(/^[A-Z0-9_-]+$/i, 'Only letters, numbers, hyphens and underscores'),
  contactEmail: z.string().email('Must be a valid e-mail').max(200).optional().or(z.literal('')),
  contactPhone: z.string().max(50).optional().or(z.literal('')),
  address:      z.string().max(500).optional().or(z.literal('')),
  notes:        z.string().max(1000).optional().or(z.literal('')),
  isActive:     z.boolean(),
})

type FormValues = z.infer<typeof schema>

// ─── Props ────────────────────────────────────────────────────────────────────

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  client: Client
  onSubmit: (data: UpdateClientRequest) => Promise<void>
  isLoading?: boolean
}

// ─── Component ────────────────────────────────────────────────────────────────

export function EditClientForm({ open, onOpenChange, client, onSubmit, isLoading = false }: Props) {
  const { register, handleSubmit, reset, formState: { errors } } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      name:         client.name,
      code:         client.code,
      contactEmail: client.contactEmail ?? '',
      contactPhone: client.contactPhone ?? '',
      address:      client.address ?? '',
      notes:        client.notes ?? '',
      isActive:     client.isActive,
    },
  })

  React.useEffect(() => {
    if (open) {
      reset({
        name:         client.name,
        code:         client.code,
        contactEmail: client.contactEmail ?? '',
        contactPhone: client.contactPhone ?? '',
        address:      client.address ?? '',
        notes:        client.notes ?? '',
        isActive:     client.isActive,
      })
    }
  }, [open, client, reset])

  const handleFormSubmit = async (values: FormValues) => {
    await onSubmit({
      name:         values.name,
      code:         values.code.toUpperCase(),
      contactEmail: values.contactEmail || undefined,
      contactPhone: values.contactPhone || undefined,
      address:      values.address || undefined,
      notes:        values.notes || undefined,
      isActive:     values.isActive,
    })
  }

  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50" />
        <Dialog.Content className="fixed left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 bg-white rounded-2xl shadow-2xl z-50 w-full max-w-md max-h-[90vh] overflow-y-auto">
          <div className="p-6">
            <div className="flex items-center justify-between mb-6">
              <div>
                <Dialog.Title className="text-lg font-semibold text-primary-900">Edit Client</Dialog.Title>
                <Dialog.Description className="text-sm text-slate-500">Update client details for <strong>{client.name}</strong></Dialog.Description>
              </div>
              <Dialog.Close className="text-slate-400 hover:text-slate-600 transition-colors">
                <X className="w-5 h-5" />
              </Dialog.Close>
            </div>

            <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-4">

              {/* Name */}
              <div>
                <label className="label">Name <span className="text-danger-500">*</span></label>
                <input {...register('name')} className={`input ${errors.name ? 'border-danger-400' : ''}`} />
                {errors.name && <p className="form-error">{errors.name.message}</p>}
              </div>

              {/* Code */}
              <div>
                <label className="label">Code <span className="text-danger-500">*</span></label>
                <input
                  {...register('code')}
                  className={`input font-mono uppercase ${errors.code ? 'border-danger-400' : ''}`}
                  style={{ textTransform: 'uppercase' }}
                />
                {errors.code && <p className="form-error">{errors.code.message}</p>}
              </div>

              {/* Contact Email */}
              <div>
                <label className="label">Contact Email</label>
                <input {...register('contactEmail')} type="email" className={`input ${errors.contactEmail ? 'border-danger-400' : ''}`} />
                {errors.contactEmail && <p className="form-error">{errors.contactEmail.message}</p>}
              </div>

              {/* Contact Phone */}
              <div>
                <label className="label">Contact Phone</label>
                <input {...register('contactPhone')} className="input" />
              </div>

              {/* Address */}
              <div>
                <label className="label">Address</label>
                <textarea {...register('address')} rows={2} className="input resize-none" />
              </div>

              {/* Notes */}
              <div>
                <label className="label">Notes</label>
                <textarea {...register('notes')} rows={2} className="input resize-none" />
              </div>

              {/* Active Toggle */}
              <div className="flex items-center gap-3 p-3 bg-slate-50 rounded-lg">
                <input
                  {...register('isActive')}
                  id="isActive"
                  type="checkbox"
                  className="w-4 h-4 accent-accent-600 cursor-pointer"
                />
                <label htmlFor="isActive" className="text-sm font-medium text-primary-800 cursor-pointer select-none">
                  Active — client can register devices and receive OTA updates
                </label>
              </div>

              {/* Actions */}
              <div className="flex justify-end gap-3 pt-2">
                <Dialog.Close asChild>
                  <button type="button" className="btn-secondary" disabled={isLoading}>Cancel</button>
                </Dialog.Close>
                <button type="submit" className="btn-primary" disabled={isLoading}>
                  {isLoading ? <><Loader2 className="w-4 h-4 animate-spin" /> Saving…</> : 'Save Changes'}
                </button>
              </div>
            </form>
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  )
}
