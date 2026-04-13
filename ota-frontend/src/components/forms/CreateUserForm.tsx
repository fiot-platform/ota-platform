'use client'

import * as React from 'react'
import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import * as Dialog from '@radix-ui/react-dialog'
import { X, Eye, EyeOff, Loader2 } from 'lucide-react'
import { UserRole } from '@/types'
import { useAuth } from '@/hooks/useAuth'

// ─── Schema ───────────────────────────────────────────────────────────────────

const createUserSchema = z.object({
  name: z.string().min(2, 'Name must be at least 2 characters'),
  email: z.string().email('Invalid email address'),
  password: z.string().min(8, 'Password must be at least 8 characters'),
  role: z.nativeEnum(UserRole),
  customerId: z.string().optional(),
  isActive: z.boolean(),
})

type CreateUserFormValues = z.infer<typeof createUserSchema>

// ─── Role options (filtered by caller's role) ─────────────────────────────────

function getAssignableRoles(callerRole: UserRole | null): UserRole[] {
  switch (callerRole) {
    case UserRole.SuperAdmin:
      return Object.values(UserRole).filter((r) => r !== UserRole.Device)
    case UserRole.PlatformAdmin:
      return [
        UserRole.ReleaseManager,
        UserRole.QA,
        UserRole.DevOpsEngineer,
        UserRole.SupportEngineer,
        UserRole.CustomerAdmin,
        UserRole.Viewer,
        UserRole.Auditor,
      ]
    default:
      return []
  }
}

interface CreateUserFormProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSubmit: (data: CreateUserFormValues) => Promise<void>
  isLoading?: boolean
}

// ─── Create User Form ─────────────────────────────────────────────────────────

export function CreateUserForm({
  open,
  onOpenChange,
  onSubmit,
  isLoading = false,
}: CreateUserFormProps) {
  const { role } = useAuth()
  const [showPassword, setShowPassword] = React.useState(false)
  const assignableRoles = getAssignableRoles(role)

  const {
    register,
    handleSubmit,
    control,
    watch,
    reset,
    formState: { errors },
  } = useForm<CreateUserFormValues>({
    resolver: zodResolver(createUserSchema),
    defaultValues: {
      name: '',
      email: '',
      password: '',
      role: UserRole.Viewer,
      customerId: '',
      isActive: true,
    },
  })

  const selectedRole = watch('role')
  const requiresCustomer = [UserRole.CustomerAdmin].includes(selectedRole)

  React.useEffect(() => {
    if (!open) {
      reset()
      setShowPassword(false)
    }
  }, [open, reset])

  const handleFormSubmit = async (values: CreateUserFormValues) => {
    await onSubmit(values)
  }

  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50" />
        <Dialog.Content className="fixed left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 bg-white rounded-2xl shadow-2xl z-50 w-full max-w-lg max-h-[90vh] overflow-y-auto">
          <div className="p-6">
            <div className="flex items-center justify-between mb-6">
              <div>
                <Dialog.Title className="text-lg font-semibold text-primary-900">Invite User</Dialog.Title>
                <Dialog.Description className="text-sm text-slate-500">Create a new platform user account</Dialog.Description>
              </div>
              <Dialog.Close className="text-slate-400 hover:text-slate-600 transition-colors">
                <X className="w-5 h-5" />
              </Dialog.Close>
            </div>

            <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-4">
              {/* Name */}
              <div>
                <label className="label">Full Name <span className="text-danger-500">*</span></label>
                <input
                  type="text"
                  placeholder="John Doe"
                  className={`input ${errors.name ? 'border-danger-400' : ''}`}
                  {...register('name')}
                />
                {errors.name && <p className="form-error">{errors.name.message}</p>}
              </div>

              {/* Email */}
              <div>
                <label className="label">Email Address <span className="text-danger-500">*</span></label>
                <input
                  type="email"
                  placeholder="john@company.com"
                  className={`input ${errors.email ? 'border-danger-400' : ''}`}
                  {...register('email')}
                />
                {errors.email && <p className="form-error">{errors.email.message}</p>}
              </div>

              {/* Password */}
              <div>
                <label className="label">Password <span className="text-danger-500">*</span></label>
                <div className="relative">
                  <input
                    type={showPassword ? 'text' : 'password'}
                    placeholder="Min. 8 characters"
                    className={`input pr-10 ${errors.password ? 'border-danger-400' : ''}`}
                    {...register('password')}
                  />
                  <button
                    type="button"
                    onClick={() => setShowPassword(!showPassword)}
                    className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600"
                  >
                    {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                  </button>
                </div>
                {errors.password && <p className="form-error">{errors.password.message}</p>}
              </div>

              {/* Role */}
              <div>
                <label className="label">Role <span className="text-danger-500">*</span></label>
                <Controller
                  name="role"
                  control={control}
                  render={({ field }) => (
                    <select {...field} className={`input ${errors.role ? 'border-danger-400' : ''}`}>
                      {assignableRoles.map((r) => (
                        <option key={r} value={r}>{r.replace(/([A-Z])/g, ' $1').trim()}</option>
                      ))}
                    </select>
                  )}
                />
                {errors.role && <p className="form-error">{errors.role.message}</p>}
              </div>

              {/* Customer ID (conditional) */}
              {(requiresCustomer || [UserRole.CustomerAdmin, UserRole.SupportEngineer].includes(selectedRole)) && (
                <div>
                  <label className="label">
                    Customer ID {requiresCustomer && <span className="text-danger-500">*</span>}
                  </label>
                  <input
                    type="text"
                    placeholder="CUST-001"
                    className="input"
                    {...register('customerId')}
                  />
                  <p className="text-xs text-slate-400 mt-1">Scopes this user to a specific customer.</p>
                </div>
              )}

              {/* Is Active */}
              <div className="flex items-center justify-between p-3 bg-slate-50 rounded-lg">
                <div>
                  <p className="text-sm font-medium text-primary-800">Active Account</p>
                  <p className="text-xs text-slate-500">User can log in immediately after creation</p>
                </div>
                <Controller
                  name="isActive"
                  control={control}
                  render={({ field }) => (
                    <button
                      type="button"
                      onClick={() => field.onChange(!field.value)}
                      className={`w-11 h-6 rounded-full transition-colors relative ${
                        field.value ? 'bg-accent-600' : 'bg-slate-300'
                      }`}
                      aria-checked={field.value}
                      role="switch"
                    >
                      <span className={`absolute top-0.5 left-0.5 w-5 h-5 bg-white rounded-full shadow transition-transform ${
                        field.value ? 'translate-x-5' : 'translate-x-0'
                      }`} />
                    </button>
                  )}
                />
              </div>

              {/* Actions */}
              <div className="flex items-center justify-end gap-3 pt-4 border-t border-slate-100">
                <Dialog.Close className="btn-secondary">Cancel</Dialog.Close>
                <button type="submit" disabled={isLoading} className="btn-primary">
                  {isLoading ? (
                    <><Loader2 className="w-4 h-4 animate-spin" /> Creating User...</>
                  ) : (
                    'Create User'
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
