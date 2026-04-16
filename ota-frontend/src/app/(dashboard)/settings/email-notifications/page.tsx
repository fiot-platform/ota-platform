'use client'

import * as React from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Mail, Plus, X, Send, Loader2, CheckCircle2, AlertCircle } from 'lucide-react'
import { PageHeader } from '@/components/ui/PageHeader'
import {
  settingsService,
  EmailNotificationSettings,
  UpdateEmailNotificationSettingsRequest,
} from '@/services/settings.service'
import api from '@/lib/api'

// ─── Toggle Switch Component ──────────────────────────────────────────────────

interface ToggleProps {
  enabled: boolean
  onChange: (val: boolean) => void
  label: string
  description?: string
}

function Toggle({ enabled, onChange, label, description }: ToggleProps) {
  return (
    <div className="flex items-center justify-between py-3">
      <div className="flex-1 min-w-0 pr-6">
        <p className="text-sm font-medium text-slate-800">{label}</p>
        {description && <p className="text-xs text-slate-500 mt-0.5">{description}</p>}
      </div>
      <button
        type="button"
        onClick={() => onChange(!enabled)}
        aria-checked={enabled}
        role="switch"
        className={`relative inline-flex h-6 w-11 flex-shrink-0 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-accent-500 focus:ring-offset-1 ${
          enabled ? 'bg-accent-600' : 'bg-slate-200'
        }`}
      >
        <span
          className={`inline-block h-4 w-4 transform rounded-full bg-white shadow transition-transform ${
            enabled ? 'translate-x-6' : 'translate-x-1'
          }`}
        />
      </button>
    </div>
  )
}

// ─── Section Card ─────────────────────────────────────────────────────────────

interface SectionCardProps {
  title: string
  icon?: React.ReactNode
  children: React.ReactNode
}

function SectionCard({ title, icon, children }: SectionCardProps) {
  return (
    <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
      <div className="flex items-center gap-2.5 px-5 py-4 border-b border-slate-100 bg-slate-50">
        {icon && <span className="text-accent-600">{icon}</span>}
        <h2 className="text-sm font-semibold text-slate-700 uppercase tracking-wide">{title}</h2>
      </div>
      <div className="px-5 divide-y divide-slate-100">{children}</div>
    </div>
  )
}

// ─── Default settings ─────────────────────────────────────────────────────────

const DEFAULT_SETTINGS: UpdateEmailNotificationSettingsRequest = {
  onFirmwareSubmitted: true,
  onFirmwareApproved: true,
  onFirmwareRejected: true,
  onFirmwareQAVerified: false,
  onRolloutStarted: false,
  onRolloutCompleted: true,
  onRolloutFailed: true,
  onDeviceOtaFailed: true,
  onDeviceRegistered: false,
  onNewUserCreated: true,
  onUserDeactivated: false,
  notifyEmails: [],
}

// ─── Page Component ───────────────────────────────────────────────────────────

export default function EmailNotificationsSettingsPage() {
  const queryClient = useQueryClient()

  // ── Local form state ──────────────────────────────────────────────────────
  const [form, setForm] = React.useState<UpdateEmailNotificationSettingsRequest>(DEFAULT_SETTINGS)
  const [newEmail, setNewEmail] = React.useState('')
  const [emailError, setEmailError] = React.useState('')
  const [successBanner, setSuccessBanner] = React.useState(false)
  const [testEmail, setTestEmail] = React.useState('')
  const [testSending, setTestSending] = React.useState(false)
  const [testResult, setTestResult] = React.useState<{ ok: boolean; msg: string } | null>(null)
  const bannerTimerRef = React.useRef<ReturnType<typeof setTimeout> | null>(null)

  // ── Fetch current settings ────────────────────────────────────────────────
  const { data, isLoading, isError } = useQuery<EmailNotificationSettings>({
    queryKey: ['settings', 'email-notifications'],
    queryFn: () => settingsService.getEmailNotificationSettings(),
  })

  // Populate form once data arrives
  React.useEffect(() => {
    if (data) {
      setForm({
        onFirmwareSubmitted: data.onFirmwareSubmitted,
        onFirmwareApproved: data.onFirmwareApproved,
        onFirmwareRejected: data.onFirmwareRejected,
        onFirmwareQAVerified: data.onFirmwareQAVerified,
        onRolloutStarted: data.onRolloutStarted,
        onRolloutCompleted: data.onRolloutCompleted,
        onRolloutFailed: data.onRolloutFailed,
        onDeviceOtaFailed: data.onDeviceOtaFailed,
        onDeviceRegistered: data.onDeviceRegistered,
        onNewUserCreated: data.onNewUserCreated,
        onUserDeactivated: data.onUserDeactivated,
        notifyEmails: data.notifyEmails ?? [],
      })
    }
  }, [data])

  // ── Save mutation ─────────────────────────────────────────────────────────
  const saveMutation = useMutation({
    mutationFn: (payload: UpdateEmailNotificationSettingsRequest) =>
      settingsService.updateEmailNotificationSettings(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['settings', 'email-notifications'] })
      setSuccessBanner(true)
      if (bannerTimerRef.current) clearTimeout(bannerTimerRef.current)
      bannerTimerRef.current = setTimeout(() => setSuccessBanner(false), 3000)
    },
  })

  // ── Toggle helper ─────────────────────────────────────────────────────────
  const toggle = (field: keyof Omit<UpdateEmailNotificationSettingsRequest, 'notifyEmails'>) => {
    setForm((prev) => ({ ...prev, [field]: !prev[field] }))
  }

  // ── Email list helpers ────────────────────────────────────────────────────
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

  const addEmail = () => {
    const trimmed = newEmail.trim().toLowerCase()
    if (!emailRegex.test(trimmed)) {
      setEmailError('Enter a valid email address.')
      return
    }
    if (form.notifyEmails.includes(trimmed)) {
      setEmailError('This email is already in the list.')
      return
    }
    setForm((prev) => ({ ...prev, notifyEmails: [...prev.notifyEmails, trimmed] }))
    setNewEmail('')
    setEmailError('')
  }

  const removeEmail = (email: string) => {
    setForm((prev) => ({ ...prev, notifyEmails: prev.notifyEmails.filter((e) => e !== email) }))
  }

  // ── SMTP test ─────────────────────────────────────────────────────────────
  const sendTestEmail = async () => {
    if (!emailRegex.test(testEmail.trim())) {
      setTestResult({ ok: false, msg: 'Enter a valid recipient email.' })
      return
    }
    setTestSending(true)
    setTestResult(null)
    try {
      await api.post('/email/test', { toEmail: testEmail.trim() })
      setTestResult({ ok: true, msg: `Test email sent to ${testEmail.trim()}.` })
    } catch (err: any) {
      const msg = err?.response?.data?.message ?? 'Failed to send test email.'
      setTestResult({ ok: false, msg })
    } finally {
      setTestSending(false)
    }
  }

  // ── Render ────────────────────────────────────────────────────────────────

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64 gap-3 text-slate-500">
        <Loader2 className="w-5 h-5 animate-spin" />
        <span>Loading settings…</span>
      </div>
    )
  }

  if (isError) {
    return (
      <div className="flex items-center gap-3 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm max-w-lg mx-auto mt-12">
        <AlertCircle className="w-5 h-5 flex-shrink-0" />
        <span>Failed to load email notification settings. Please try refreshing the page.</span>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <PageHeader
        title="Email Notification Settings"
        subtitle="Choose which platform events trigger email alerts"
        breadcrumbs={[
          { label: 'Administration' },
          { label: 'Settings' },
          { label: 'Email Notifications' },
        ]}
      />

      {/* Success banner */}
      {successBanner && (
        <div className="flex items-center gap-3 px-4 py-3 rounded-lg bg-green-50 border border-green-200 text-green-700 text-sm animate-fade-in">
          <CheckCircle2 className="w-4 h-4 flex-shrink-0" />
          <span>Settings saved successfully.</span>
        </div>
      )}

      {/* Save mutation error */}
      {saveMutation.isError && (
        <div className="flex items-center gap-3 px-4 py-3 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm">
          <AlertCircle className="w-4 h-4 flex-shrink-0" />
          <span>
            {(saveMutation.error as any)?.response?.data?.message ?? 'Failed to save settings.'}
          </span>
        </div>
      )}

      <div className="grid gap-5">
        {/* Firmware Events */}
        <SectionCard title="Firmware Events">
          <Toggle
            enabled={form.onFirmwareSubmitted}
            onChange={() => toggle('onFirmwareSubmitted')}
            label="On Firmware Submitted"
            description="Send an alert when a new firmware build is submitted for review."
          />
          <Toggle
            enabled={form.onFirmwareApproved}
            onChange={() => toggle('onFirmwareApproved')}
            label="On Firmware Approved"
            description="Notify when a firmware version is approved for release."
          />
          <Toggle
            enabled={form.onFirmwareRejected}
            onChange={() => toggle('onFirmwareRejected')}
            label="On Firmware Rejected"
            description="Notify the submitter when a firmware version is rejected."
          />
          <Toggle
            enabled={form.onFirmwareQAVerified}
            onChange={() => toggle('onFirmwareQAVerified')}
            label="On Firmware QA Verified"
            description="Send an alert after a QA session marks firmware as verified."
          />
        </SectionCard>

        {/* Rollout Events */}
        <SectionCard title="Rollout Events">
          <Toggle
            enabled={form.onRolloutStarted}
            onChange={() => toggle('onRolloutStarted')}
            label="On Rollout Started"
            description="Notify when a new OTA rollout begins executing."
          />
          <Toggle
            enabled={form.onRolloutCompleted}
            onChange={() => toggle('onRolloutCompleted')}
            label="On Rollout Completed"
            description="Send a completion summary when all devices in a rollout are processed."
          />
          <Toggle
            enabled={form.onRolloutFailed}
            onChange={() => toggle('onRolloutFailed')}
            label="On Rollout Failed"
            description="Immediately alert when a rollout encounters a critical failure."
          />
        </SectionCard>

        {/* Device Events */}
        <SectionCard title="Device Events">
          <Toggle
            enabled={form.onDeviceOtaFailed}
            onChange={() => toggle('onDeviceOtaFailed')}
            label="On Device OTA Failed"
            description="Alert when an individual device fails to apply a firmware update."
          />
          <Toggle
            enabled={form.onDeviceRegistered}
            onChange={() => toggle('onDeviceRegistered')}
            label="On Device Registered"
            description="Notify when a new device registers on the platform."
          />
        </SectionCard>

        {/* User Events */}
        <SectionCard title="User Events">
          <Toggle
            enabled={form.onNewUserCreated}
            onChange={() => toggle('onNewUserCreated')}
            label="On New User Created"
            description="Send a welcome email when a new platform account is created."
          />
          <Toggle
            enabled={form.onUserDeactivated}
            onChange={() => toggle('onUserDeactivated')}
            label="On User Deactivated"
            description="Notify admins when a user account is deactivated."
          />
        </SectionCard>

        {/* Notification Recipients */}
        <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
          <div className="flex items-center gap-2.5 px-5 py-4 border-b border-slate-100 bg-slate-50">
            <Mail className="w-4 h-4 text-accent-600" />
            <h2 className="text-sm font-semibold text-slate-700 uppercase tracking-wide">
              Notification Recipients
            </h2>
          </div>
          <div className="p-5 space-y-4">
            <p className="text-sm text-slate-500">
              Additional CC addresses that receive copies of all platform notification emails.
            </p>

            {/* Email chips */}
            {form.notifyEmails.length > 0 && (
              <div className="flex flex-wrap gap-2">
                {form.notifyEmails.map((email) => (
                  <span
                    key={email}
                    className="inline-flex items-center gap-1.5 bg-accent-50 text-accent-700 border border-accent-200 rounded-full px-3 py-1 text-sm font-medium"
                  >
                    <Mail className="w-3 h-3" />
                    {email}
                    <button
                      type="button"
                      onClick={() => removeEmail(email)}
                      className="ml-1 hover:text-red-600 transition-colors"
                      aria-label={`Remove ${email}`}
                    >
                      <X className="w-3.5 h-3.5" />
                    </button>
                  </span>
                ))}
              </div>
            )}

            {/* Add email input */}
            <div className="flex gap-2">
              <div className="flex-1">
                <input
                  type="email"
                  value={newEmail}
                  onChange={(e) => {
                    setNewEmail(e.target.value)
                    setEmailError('')
                  }}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') {
                      e.preventDefault()
                      addEmail()
                    }
                  }}
                  placeholder="cc@example.com"
                  className={`w-full px-3 py-2 text-sm border rounded-lg outline-none focus:ring-2 focus:ring-accent-500 transition ${
                    emailError ? 'border-red-400 focus:ring-red-400' : 'border-slate-300'
                  }`}
                />
                {emailError && (
                  <p className="text-xs text-red-600 mt-1">{emailError}</p>
                )}
              </div>
              <button
                type="button"
                onClick={addEmail}
                className="inline-flex items-center gap-1.5 px-4 py-2 text-sm font-medium rounded-lg bg-accent-600 text-white hover:bg-accent-700 transition-colors"
              >
                <Plus className="w-4 h-4" />
                Add
              </button>
            </div>
          </div>
        </div>

        {/* SMTP Test Card */}
        <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
          <div className="flex items-center gap-2.5 px-5 py-4 border-b border-slate-100 bg-slate-50">
            <Send className="w-4 h-4 text-accent-600" />
            <h2 className="text-sm font-semibold text-slate-700 uppercase tracking-wide">
              SMTP Test
            </h2>
          </div>
          <div className="p-5 space-y-4">
            <p className="text-sm text-slate-500">
              Send a test email to verify your SMTP configuration is working correctly.
            </p>

            <div className="flex gap-2">
              <div className="flex-1">
                <input
                  type="email"
                  value={testEmail}
                  onChange={(e) => {
                    setTestEmail(e.target.value)
                    setTestResult(null)
                  }}
                  placeholder="recipient@example.com"
                  className="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg outline-none focus:ring-2 focus:ring-accent-500 transition"
                />
              </div>
              <button
                type="button"
                onClick={sendTestEmail}
                disabled={testSending}
                className="inline-flex items-center gap-2 px-4 py-2 text-sm font-medium rounded-lg bg-slate-700 text-white hover:bg-slate-800 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                {testSending ? (
                  <Loader2 className="w-4 h-4 animate-spin" />
                ) : (
                  <Send className="w-4 h-4" />
                )}
                Send Test Email
              </button>
            </div>

            {testResult && (
              <div
                className={`flex items-center gap-2 px-3 py-2 rounded-lg text-sm ${
                  testResult.ok
                    ? 'bg-green-50 border border-green-200 text-green-700'
                    : 'bg-red-50 border border-red-200 text-red-700'
                }`}
              >
                {testResult.ok ? (
                  <CheckCircle2 className="w-4 h-4 flex-shrink-0" />
                ) : (
                  <AlertCircle className="w-4 h-4 flex-shrink-0" />
                )}
                <span>{testResult.msg}</span>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Footer — last updated info + Save button */}
      <div className="flex items-center justify-between pt-2 pb-6">
        <div className="text-xs text-slate-400">
          {data?.updatedAt && (
            <>
              Last saved{' '}
              {new Date(data.updatedAt).toLocaleString()}
              {data.updatedBy ? ` by ${data.updatedBy}` : ''}
            </>
          )}
        </div>
        <button
          type="button"
          onClick={() => saveMutation.mutate(form)}
          disabled={saveMutation.isPending}
          className="inline-flex items-center gap-2 px-6 py-2.5 rounded-lg bg-accent-600 text-white text-sm font-semibold hover:bg-accent-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors shadow-sm"
        >
          {saveMutation.isPending ? (
            <>
              <Loader2 className="w-4 h-4 animate-spin" />
              Saving…
            </>
          ) : (
            'Save Settings'
          )}
        </button>
      </div>
    </div>
  )
}
