'use client'

import * as React from 'react'
import { useRouter } from 'next/navigation'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Eye, EyeOff, Radio, Loader2 } from 'lucide-react'
import { authService } from '@/services/auth.service'
import { useToast } from '@/components/ui/ToastProvider'
import { isAuthenticated } from '@/lib/auth'

// ─── Schema ───────────────────────────────────────────────────────────────────

const loginSchema = z.object({
  email: z.string().email('Please enter a valid email address'),
  password: z.string().min(1, 'Password is required'),
})

type LoginFormValues = z.infer<typeof loginSchema>

// ─── Login Page ───────────────────────────────────────────────────────────────

export default function LoginPage() {
  const router = useRouter()
  const { toast } = useToast()
  const [showPassword, setShowPassword] = React.useState(false)
  const [isLoading, setIsLoading] = React.useState(false)
  const [emailError, setEmailError] = React.useState('')
  const [passwordError, setPasswordError] = React.useState('')

  React.useEffect(() => {
    if (isAuthenticated()) {
      router.push('/dashboard')
    }
  }, [router])

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '' },
  })

  const onSubmit = async (values: LoginFormValues) => {
    setIsLoading(true)
    setEmailError('')
    setPasswordError('')

    try {
      await authService.login(values)
      toast({
        title: 'Welcome back!',
        description: 'You have been signed in successfully.',
        variant: 'success',
      })
      router.push('/dashboard')
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } }; message?: string }
      const message = error?.response?.data?.message ?? error?.message ?? 'Sign in failed'

      if (message.toLowerCase().includes('email') || message.toLowerCase().includes('account found')) {
        setEmailError(message)
      } else if (message.toLowerCase().includes('password') || message.toLowerCase().includes('incorrect')) {
        setPasswordError(message)
      } else if (message.toLowerCase().includes('deactivated')) {
        setEmailError(message)
      } else {
        toast({ title: 'Sign in failed', description: message, variant: 'error' })
      }
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-primary-900 via-primary-800 to-accent-900 flex items-center justify-center p-4">
      {/* Background Pattern */}
      <div className="absolute inset-0 overflow-hidden pointer-events-none">
        <div className="absolute -top-40 -right-40 w-80 h-80 bg-accent-600/20 rounded-full blur-3xl" />
        <div className="absolute -bottom-40 -left-40 w-80 h-80 bg-accent-600/10 rounded-full blur-3xl" />
      </div>

      <div className="relative w-full max-w-md">
        {/* Card */}
        <div className="bg-white/95 backdrop-blur-sm rounded-2xl shadow-2xl p-8 border border-white/20">
          {/* Logo & Title */}
          <div className="text-center mb-8">
            <div className="w-16 h-16 bg-accent-600 rounded-2xl flex items-center justify-center mx-auto mb-4 shadow-lg">
              <Radio className="w-9 h-9 text-white" />
            </div>
            <h1 className="text-2xl font-bold text-primary-900">OTA Platform</h1>
            <p className="text-slate-500 text-sm mt-1">Sign in to your admin portal</p>
          </div>

          {/* Form */}
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
            {/* Email */}
            <div>
              <label htmlFor="email" className="label">
                Email Address
              </label>
              <input
                id="email"
                type="email"
                autoComplete="email"
                placeholder="admin@example.com"
                className={`input ${errors.email || emailError ? 'border-danger-400 focus:ring-danger-500' : ''}`}
                {...register('email', { onChange: () => setEmailError('') })}
              />
              {(errors.email || emailError) && (
                <p className="form-error">{errors.email?.message || emailError}</p>
              )}
            </div>

            {/* Password */}
            <div>
              <label htmlFor="password" className="label">
                Password
              </label>
              <div className="relative">
                <input
                  id="password"
                  type={showPassword ? 'text' : 'password'}
                  autoComplete="current-password"
                  placeholder="Enter your password"
                  className={`input pr-10 ${errors.password || passwordError ? 'border-danger-400 focus:ring-danger-500' : ''}`}
                  {...register('password', { onChange: () => setPasswordError('') })}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 transition-colors"
                  aria-label={showPassword ? 'Hide password' : 'Show password'}
                >
                  {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                </button>
              </div>
              {(errors.password || passwordError) && (
                <p className="form-error">{errors.password?.message || passwordError}</p>
              )}
            </div>

            {/* Forgot Password */}
            <div className="flex items-center justify-end">
              <a
                href="#"
                className="text-sm text-accent-600 hover:text-accent-700 font-medium transition-colors"
              >
                Forgot password?
              </a>
            </div>

            {/* Submit Button */}
            <button
              type="submit"
              disabled={isLoading}
              className="btn-primary w-full py-2.5 text-base disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isLoading ? (
                <>
                  <Loader2 className="w-5 h-5 animate-spin" />
                  Signing in...
                </>
              ) : (
                'Sign In'
              )}
            </button>
          </form>

          {/* Footer */}
          <div className="mt-8 pt-6 border-t border-slate-100 text-center">
            <p className="text-xs text-slate-400">
              OTA Platform Admin Portal &mdash; Enterprise Firmware Update Management
            </p>
            <p className="text-xs text-slate-300 mt-1">
              Secured access. All sessions are monitored and audited.
            </p>
          </div>
        </div>
      </div>
    </div>
  )
}
