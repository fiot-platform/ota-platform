'use client'

import * as React from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useQuery } from '@tanstack/react-query'
import * as Dialog from '@radix-ui/react-dialog'
import { X, Loader2 } from 'lucide-react'
import { clientService } from '@/services/client.service'
import { CreateClientRequest } from '@/types'

// ─── Schema ───────────────────────────────────────────────────────────────────

const schema = z.object({
  name:         z.string().min(1, 'Name is required').max(200),
  contactEmail: z.string().email('Must be a valid e-mail').max(200).optional().or(z.literal('')),
  contactPhone: z.string().max(50).optional().or(z.literal('')),
  address:      z.string().max(500).optional().or(z.literal('')),
  notes:        z.string().max(1000).optional().or(z.literal('')),
})

type FormValues = z.infer<typeof schema>

// ─── Props ────────────────────────────────────────────────────────────────────

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSubmit: (data: CreateClientRequest) => Promise<void>
  isLoading?: boolean
}

// ─── Component ────────────────────────────────────────────────────────────────

export function CreateClientForm({ open, onOpenChange, onSubmit, isLoading = false }: Props) {
  const { register, handleSubmit, reset, formState: { errors } } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { name: '', contactEmail: '', contactPhone: '', address: '', notes: '' },
  })

  // Fetch the next sequential code silently — used at submit time
  const { data: nextCode, isLoading: codeLoading } = useQuery({
    queryKey: ['clients-next-code'],
    queryFn: clientService.getNextCode,
    enabled: open,
    staleTime: 0,
  })

  React.useEffect(() => {
    if (!open) reset()
  }, [open, reset])

  const handleFormSubmit = async (values: FormValues) => {
    await onSubmit({
      name:         values.name,
      code:         nextCode ?? '',
      contactEmail: values.contactEmail || undefined,
      contactPhone: values.contactPhone || undefined,
      address:      values.address || undefined,
      notes:        values.notes || undefined,
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
                <Dialog.Title className="text-lg font-semibold text-primary-900">Create Client</Dialog.Title>
                <Dialog.Description className="text-sm text-slate-500">Add a new client organisation</Dialog.Description>
              </div>
              <Dialog.Close className="text-slate-400 hover:text-slate-600 transition-colors">
                <X className="w-5 h-5" />
              </Dialog.Close>
            </div>

            <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-4">

              {/* Name */}
              <div>
                <label className="label">Name <span className="text-danger-500">*</span></label>
                <input
                  {...register('name')}
                  className={`input ${errors.name ? 'border-danger-400' : ''}`}
                  placeholder="Acme Corporation"
                />
                {errors.name && <p className="form-error">{errors.name.message}</p>}
              </div>

              {/* Contact Email */}
              <div>
                <label className="label">Contact Email</label>
                <input
                  {...register('contactEmail')}
                  type="email"
                  className={`input ${errors.contactEmail ? 'border-danger-400' : ''}`}
                  placeholder="contact@acme.com"
                />
                {errors.contactEmail && <p className="form-error">{errors.contactEmail.message}</p>}
              </div>

              {/* Contact Phone */}
              <div>
                <label className="label">Contact Phone</label>
                <input {...register('contactPhone')} className="input" placeholder="+91 98765 43210" />
              </div>

              {/* Address */}
              <div>
                <label className="label">Address</label>
                <textarea {...register('address')} rows={2} className="input resize-none" placeholder="123 Main St, City, Country" />
              </div>

              {/* Notes */}
              <div>
                <label className="label">Notes</label>
                <textarea {...register('notes')} rows={2} className="input resize-none" placeholder="Any additional notes…" />
              </div>

              {/* Actions */}
              <div className="flex justify-end gap-3 pt-2">
                <Dialog.Close asChild>
                  <button type="button" className="btn-secondary" disabled={isLoading}>Cancel</button>
                </Dialog.Close>
                <button type="submit" className="btn-primary" disabled={isLoading || codeLoading}>
                  {isLoading ? <><Loader2 className="w-4 h-4 animate-spin" /> Creating…</> : 'Create Client'}
                </button>
              </div>
            </form>
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  )
}
