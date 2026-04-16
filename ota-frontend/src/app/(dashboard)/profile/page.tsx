'use client'

import * as React from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  User,
  Mail,
  Shield,
  Calendar,
  Clock,
  Key,
  FolderKanban,
  CheckCircle,
  XCircle,
  Eye,
  EyeOff,
  Save,
  Hash,
  Building2,
} from 'lucide-react'
import { authService } from '@/services/auth.service'
import { userService } from '@/services/user.service'
import { PageHeader } from '@/components/ui/PageHeader'
import { StatusBadge, RoleBadge } from '@/components/ui/Badge'
import { useToast } from '@/components/ui/ToastProvider'
import { useAuth } from '@/hooks/useAuth'
import { useGiteaProfile } from '@/hooks/useGiteaProfile'
import { formatDate, formatRelativeTime } from '@/utils/formatters'
import { UserRole } from '@/types'

// ── Helper: info row ────────────────────────────────────────────────────────

function InfoRow({
  icon,
  label,
  value,
}: {
  icon: React.ReactNode
  label: string
  value: React.ReactNode
}) {
  return (
    <div className="flex items-start gap-3 py-3 border-b border-slate-100 last:border-0">
      <span className="text-slate-400 mt-0.5 flex-shrink-0">{icon}</span>
      <div className="min-w-0 flex-1">
        <p className="text-xs font-semibold text-slate-400 uppercase tracking-wide mb-0.5">{label}</p>
        <div className="text-sm font-medium text-primary-800 break-all">{value}</div>
      </div>
    </div>
  )
}

// ── Helper: password input ──────────────────────────────────────────────────

function PasswordInput({
  id,
  label,
  value,
  onChange,
  error,
  placeholder,
}: {
  id: string
  label: string
  value: string
  onChange: (v: string) => void
  error?: string
  placeholder?: string
}) {
  const [show, setShow] = React.useState(false)
  return (
    <div>
      <label htmlFor={id} className="label">{label}</label>
      <div className="relative">
        <input
          id={id}
          type={show ? 'text' : 'password'}
          value={value}
          onChange={(e) => onChange(e.target.value)}
          placeholder={placeholder}
          className={`input pr-10 ${error ? 'border-danger-400 focus:ring-danger-400' : ''}`}
        />
        <button
          type="button"
          onClick={() => setShow((s) => !s)}
          className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600"
          tabIndex={-1}
        >
          {show ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
        </button>
      </div>
      {error && <p className="form-error">{error}</p>}
    </div>
  )
}

// ── Main Page ───────────────────────────────────────────────────────────────

export default function ProfilePage() {
  const queryClient = useQueryClient()
  const { toast } = useToast()
  const { user: jwtUser } = useAuth()
  const { data: giteaProfile } = useGiteaProfile()

  // Fetch live user data from the API
  const { data: profile, isLoading } = useQuery({
    queryKey: ['auth-me'],
    queryFn: () => authService.getCurrentUser(),
  })

  // ── Edit Profile state ────────────────────────────────────────────────────
  const [editName, setEditName] = React.useState('')
  const [nameError, setNameError] = React.useState('')

  React.useEffect(() => {
    if (profile?.name) setEditName(profile.name)
  }, [profile?.name])

  const updateProfileMutation = useMutation({
    mutationFn: () =>
      userService.updateUser(profile!.id, { name: editName.trim() }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['auth-me'] })
      toast({ title: 'Profile updated successfully', variant: 'success' })
    },
    onError: (e: any) =>
      toast({
        title: 'Failed to update profile',
        description: e?.response?.data?.message,
        variant: 'error',
      }),
  })

  const handleSaveProfile = () => {
    if (!editName.trim()) {
      setNameError('Name is required.')
      return
    }
    if (editName.trim().length < 2) {
      setNameError('Name must be at least 2 characters.')
      return
    }
    setNameError('')
    updateProfileMutation.mutate()
  }

  // ── Change Password state ─────────────────────────────────────────────────
  const [currentPassword, setCurrentPassword] = React.useState('')
  const [newPassword, setNewPassword] = React.useState('')
  const [confirmPassword, setConfirmPassword] = React.useState('')
  const [pwErrors, setPwErrors] = React.useState<Record<string, string>>({})

  const changePasswordMutation = useMutation({
    mutationFn: () => authService.changePassword(currentPassword, newPassword),
    onSuccess: () => {
      toast({ title: 'Password changed successfully', variant: 'success' })
      setCurrentPassword('')
      setNewPassword('')
      setConfirmPassword('')
      setPwErrors({})
    },
    onError: (e: any) =>
      toast({
        title: 'Failed to change password',
        description: e?.response?.data?.message,
        variant: 'error',
      }),
  })

  const handleChangePassword = () => {
    const errors: Record<string, string> = {}
    if (!currentPassword) errors.currentPassword = 'Current password is required.'
    if (!newPassword) errors.newPassword = 'New password is required.'
    else if (newPassword.length < 8) errors.newPassword = 'Password must be at least 8 characters.'
    if (!confirmPassword) errors.confirmPassword = 'Please confirm your new password.'
    else if (newPassword !== confirmPassword) errors.confirmPassword = 'Passwords do not match.'
    if (currentPassword && newPassword && currentPassword === newPassword)
      errors.newPassword = 'New password must differ from the current password.'

    setPwErrors(errors)
    if (Object.keys(errors).length > 0) return

    changePasswordMutation.mutate()
  }

  // ── Derived display values ────────────────────────────────────────────────

  const displayName =
    giteaProfile?.login ?? profile?.name ?? jwtUser?.fullName ?? jwtUser?.email?.split('@')[0] ?? 'User'

  const avatarInitial = displayName.charAt(0).toUpperCase()

  const roleLabel = (profile?.role ?? jwtUser?.role ?? '') as UserRole

  // Password strength indicator
  const passwordStrength = React.useMemo(() => {
    if (!newPassword) return null
    let score = 0
    if (newPassword.length >= 8) score++
    if (newPassword.length >= 12) score++
    if (/[A-Z]/.test(newPassword)) score++
    if (/[0-9]/.test(newPassword)) score++
    if (/[^A-Za-z0-9]/.test(newPassword)) score++
    if (score <= 1) return { label: 'Weak', color: 'bg-danger-500', width: 'w-1/5' }
    if (score === 2) return { label: 'Fair', color: 'bg-warning-500', width: 'w-2/5' }
    if (score === 3) return { label: 'Good', color: 'bg-info-500', width: 'w-3/5' }
    if (score === 4) return { label: 'Strong', color: 'bg-accent-500', width: 'w-4/5' }
    return { label: 'Very Strong', color: 'bg-accent-600', width: 'w-full' }
  }, [newPassword])

  if (isLoading) {
    return (
      <div className="space-y-6 animate-fade-in">
        <PageHeader
          title="My Profile"
          subtitle="View and manage your account settings"
          breadcrumbs={[{ label: 'Dashboard', href: '/dashboard' }, { label: 'My Profile' }]}
        />
        <div className="card p-8 flex items-center justify-center">
          <div className="w-8 h-8 border-4 border-accent-200 border-t-accent-600 rounded-full animate-spin" />
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6 animate-fade-in">
      <PageHeader
        title="My Profile"
        subtitle="View and manage your account settings"
        breadcrumbs={[{ label: 'Dashboard', href: '/dashboard' }, { label: 'My Profile' }]}
      />

      {/* ── Profile Hero Card ───────────────────────────────────────────────── */}
      <div className="card p-6">
        <div className="flex flex-col sm:flex-row items-start sm:items-center gap-5">
          {/* Avatar */}
          <div className="w-20 h-20 bg-accent-600 rounded-2xl flex items-center justify-center flex-shrink-0 shadow-lg">
            <span className="text-white text-3xl font-bold">{avatarInitial}</span>
          </div>

          {/* Identity */}
          <div className="flex-1 min-w-0">
            <h2 className="text-2xl font-bold text-primary-900 truncate">{profile?.name ?? displayName}</h2>
            <p className="text-slate-500 text-sm mt-0.5">{profile?.email ?? jwtUser?.email}</p>
            <div className="flex flex-wrap items-center gap-2 mt-2">
              {roleLabel && <RoleBadge role={roleLabel} />}
              <StatusBadge status={profile?.isActive ? 'Active' : 'Inactive'} dot />
              {giteaProfile && (
                <span className="inline-flex items-center gap-1 text-xs text-accent-700 bg-accent-50 rounded-full px-2 py-0.5 border border-accent-200">
                  @{giteaProfile.login} on Gitea
                </span>
              )}
            </div>
          </div>

          {/* Stats */}
          <div className="hidden lg:flex items-center gap-6">
            <div className="text-center">
              <p className="text-2xl font-bold text-primary-900">{profile?.projectScope?.length ?? 0}</p>
              <p className="text-xs text-slate-500 mt-0.5">Projects</p>
            </div>
            <div className="w-px h-10 bg-slate-200" />
            <div className="text-center">
              <p className="text-sm font-semibold text-primary-900">
                {profile?.lastLoginAt ? formatRelativeTime(profile.lastLoginAt) : 'Never'}
              </p>
              <p className="text-xs text-slate-500 mt-0.5">Last Login</p>
            </div>
            <div className="w-px h-10 bg-slate-200" />
            <div className="text-center">
              <p className="text-sm font-semibold text-primary-900">
                {profile?.createdAt ? formatDate(profile.createdAt) : '—'}
              </p>
              <p className="text-xs text-slate-500 mt-0.5">Member Since</p>
            </div>
          </div>
        </div>
      </div>

      {/* ── Two-column layout ───────────────────────────────────────────────── */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">

        {/* ── Left column: forms ─────────────────────────────────────────── */}
        <div className="lg:col-span-2 space-y-6">

          {/* Edit Profile */}
          <div className="card p-6">
            <div className="flex items-center gap-2 mb-5">
              <User className="w-5 h-5 text-accent-600" />
              <h3 className="text-base font-semibold text-primary-900">Edit Profile</h3>
            </div>

            <div className="space-y-4">
              <div>
                <label htmlFor="profile-name" className="label">Display Name</label>
                <input
                  id="profile-name"
                  type="text"
                  value={editName}
                  onChange={(e) => {
                    setEditName(e.target.value)
                    if (nameError) setNameError('')
                  }}
                  placeholder="Your full name"
                  className={`input ${nameError ? 'border-danger-400 focus:ring-danger-400' : ''}`}
                />
                {nameError && <p className="form-error">{nameError}</p>}
              </div>

              <div>
                <label className="label">Email Address</label>
                <input
                  type="email"
                  value={profile?.email ?? jwtUser?.email ?? ''}
                  disabled
                  className="input bg-slate-50 text-slate-500 cursor-not-allowed"
                />
                <p className="text-xs text-slate-400 mt-1">Email cannot be changed here. Contact a platform admin.</p>
              </div>

              <div>
                <label className="label">Role</label>
                <input
                  type="text"
                  value={roleLabel || '—'}
                  disabled
                  className="input bg-slate-50 text-slate-500 cursor-not-allowed"
                />
                <p className="text-xs text-slate-400 mt-1">Role is assigned by a platform administrator.</p>
              </div>

              <div className="flex justify-end pt-2">
                <button
                  onClick={handleSaveProfile}
                  disabled={updateProfileMutation.isPending || editName.trim() === (profile?.name ?? '')}
                  className="btn-primary flex items-center gap-2"
                >
                  <Save className="w-4 h-4" />
                  {updateProfileMutation.isPending ? 'Saving…' : 'Save Changes'}
                </button>
              </div>
            </div>
          </div>

          {/* Change Password */}
          <div className="card p-6">
            <div className="flex items-center gap-2 mb-5">
              <Key className="w-5 h-5 text-accent-600" />
              <h3 className="text-base font-semibold text-primary-900">Change Password</h3>
            </div>

            <div className="space-y-4">
              <PasswordInput
                id="current-password"
                label="Current Password"
                value={currentPassword}
                onChange={setCurrentPassword}
                error={pwErrors.currentPassword}
                placeholder="Enter your current password"
              />

              <PasswordInput
                id="new-password"
                label="New Password"
                value={newPassword}
                onChange={setNewPassword}
                error={pwErrors.newPassword}
                placeholder="At least 8 characters"
              />

              {/* Password strength bar */}
              {newPassword && passwordStrength && (
                <div>
                  <div className="flex items-center justify-between mb-1">
                    <span className="text-xs text-slate-500">Password strength</span>
                    <span className="text-xs font-medium text-slate-600">{passwordStrength.label}</span>
                  </div>
                  <div className="h-1.5 bg-slate-200 rounded-full overflow-hidden">
                    <div
                      className={`h-full rounded-full transition-all duration-300 ${passwordStrength.color} ${passwordStrength.width}`}
                    />
                  </div>
                </div>
              )}

              <PasswordInput
                id="confirm-password"
                label="Confirm New Password"
                value={confirmPassword}
                onChange={setConfirmPassword}
                error={pwErrors.confirmPassword}
                placeholder="Re-enter your new password"
              />

              {/* Match indicator */}
              {newPassword && confirmPassword && (
                <div className="flex items-center gap-1.5 text-xs">
                  {newPassword === confirmPassword ? (
                    <>
                      <CheckCircle className="w-4 h-4 text-success-600" />
                      <span className="text-success-700">Passwords match</span>
                    </>
                  ) : (
                    <>
                      <XCircle className="w-4 h-4 text-danger-500" />
                      <span className="text-danger-600">Passwords do not match</span>
                    </>
                  )}
                </div>
              )}

              <div className="flex justify-end pt-2">
                <button
                  onClick={handleChangePassword}
                  disabled={changePasswordMutation.isPending || !currentPassword || !newPassword || !confirmPassword}
                  className="btn-primary flex items-center gap-2"
                >
                  <Key className="w-4 h-4" />
                  {changePasswordMutation.isPending ? 'Changing…' : 'Change Password'}
                </button>
              </div>
            </div>
          </div>
        </div>

        {/* ── Right column: read-only info ───────────────────────────────── */}
        <div className="space-y-6">

          {/* Account Details */}
          <div className="card p-6">
            <div className="flex items-center gap-2 mb-4">
              <Shield className="w-5 h-5 text-accent-600" />
              <h3 className="text-base font-semibold text-primary-900">Account Details</h3>
            </div>

            <div>
              <InfoRow
                icon={<Hash className="w-4 h-4" />}
                label="User ID"
                value={
                  <span className="font-mono text-xs text-slate-600 break-all">
                    {profile?.userId ?? jwtUser?.userId ?? '—'}
                  </span>
                }
              />
              <InfoRow
                icon={<Mail className="w-4 h-4" />}
                label="Email"
                value={profile?.email ?? jwtUser?.email ?? '—'}
              />
              <InfoRow
                icon={<Shield className="w-4 h-4" />}
                label="Role"
                value={roleLabel ? <RoleBadge role={roleLabel} /> : '—'}
              />
              {(profile?.customerId ?? jwtUser?.customerId) && (
                <InfoRow
                  icon={<Building2 className="w-4 h-4" />}
                  label="Customer ID"
                  value={
                    <span className="font-mono text-xs text-slate-600">
                      {profile?.customerId ?? jwtUser?.customerId}
                    </span>
                  }
                />
              )}
              <InfoRow
                icon={<CheckCircle className="w-4 h-4" />}
                label="Account Status"
                value={<StatusBadge status={profile?.isActive ? 'Active' : 'Inactive'} dot />}
              />
              <InfoRow
                icon={<Calendar className="w-4 h-4" />}
                label="Member Since"
                value={profile?.createdAt ? formatDate(profile.createdAt) : '—'}
              />
              <InfoRow
                icon={<Clock className="w-4 h-4" />}
                label="Last Login"
                value={
                  profile?.lastLoginAt
                    ? `${formatRelativeTime(profile.lastLoginAt)} (${formatDate(profile.lastLoginAt)})`
                    : 'Never'
                }
              />
              <InfoRow
                icon={<Clock className="w-4 h-4" />}
                label="Last Updated"
                value={profile?.updatedAt ? formatRelativeTime(profile.updatedAt) : '—'}
              />
            </div>
          </div>

          {/* Project Scope */}
          <div className="card p-6">
            <div className="flex items-center gap-2 mb-4">
              <FolderKanban className="w-5 h-5 text-accent-600" />
              <h3 className="text-base font-semibold text-primary-900">Project Access</h3>
            </div>

            {(!profile?.projectScope || profile.projectScope.length === 0) ? (
              <div className="text-center py-6">
                <FolderKanban className="w-8 h-8 text-slate-300 mx-auto mb-2" />
                <p className="text-sm font-medium text-slate-500">Unrestricted Access</p>
                <p className="text-xs text-slate-400 mt-1">
                  You have access to all projects within your scope.
                </p>
              </div>
            ) : (
              <div className="space-y-2">
                <p className="text-xs text-slate-500 mb-3">
                  Your access is scoped to {profile.projectScope.length} project
                  {profile.projectScope.length !== 1 ? 's' : ''}:
                </p>
                {profile.projectScope.map((pid) => (
                  <div
                    key={pid}
                    className="flex items-center gap-2 px-3 py-2 bg-accent-50 border border-accent-200 rounded-lg"
                  >
                    <FolderKanban className="w-3.5 h-3.5 text-accent-600 flex-shrink-0" />
                    <span className="text-xs font-mono text-accent-800 truncate">{pid}</span>
                  </div>
                ))}
              </div>
            )}
          </div>

        </div>
      </div>
    </div>
  )
}
