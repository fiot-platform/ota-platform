'use client'

import * as React from 'react'
import * as DropdownMenu from '@radix-ui/react-dropdown-menu'
import { useRouter } from 'next/navigation'
import {
  Zap, RefreshCw, ShieldCheck, MonitorSmartphone, Clock,
  FileText, FlaskConical, FolderPlus, GitBranch, Users, Package,
  ListChecks, Activity, Building2, BarChart3,
} from 'lucide-react'
import { useAuth } from '@/hooks/useAuth'
import { UserRole } from '@/types'

interface QuickAction {
  label: string
  href: string
  icon: React.ReactNode
  description?: string
}

// ─── Per-role action sets ────────────────────────────────────────────────────

function getActions(role: UserRole | null): QuickAction[] {
  switch (role) {
    case UserRole.SuperAdmin:
    case UserRole.PlatformAdmin:
      return [
        { label: 'Push OTA',           href: '/device-ota',                   icon: <RefreshCw className="w-4 h-4 text-accent-600" />,    description: 'Send firmware to one or many devices' },
        { label: 'Approved firmware',  href: '/firmware?status=Approved',     icon: <ShieldCheck className="w-4 h-4 text-success-600" />, description: 'Browse production-ready releases' },
        { label: 'Register device',    href: '/devices',                      icon: <MonitorSmartphone className="w-4 h-4 text-primary-700" />, description: 'Add a single device or bulk import' },
        { label: 'New project',        href: '/projects',                     icon: <FolderPlus className="w-4 h-4 text-purple-600" />,   description: 'Spin up a new project' },
        { label: 'New repository',     href: '/repositories',                 icon: <GitBranch className="w-4 h-4 text-cyan-600" />,      description: 'Create a Gitea-backed firmware repo' },
        { label: 'New client',         href: '/clients',                      icon: <Building2 className="w-4 h-4 text-indigo-600" />,    description: 'Onboard a customer organisation' },
        { label: 'User management',    href: '/users',                        icon: <Users className="w-4 h-4 text-slate-600" />,         description: 'Invite or manage platform users' },
        { label: 'Reports',            href: '/reports/device-status',        icon: <BarChart3 className="w-4 h-4 text-amber-600" />,     description: 'Fleet, OTA history, project reports' },
        { label: 'Audit logs',         href: '/audit-logs',                   icon: <FileText className="w-4 h-4 text-slate-600" />,      description: 'Recent platform activity' },
      ]

    case UserRole.ReleaseManager:
      return [
        { label: 'Push OTA',           href: '/device-ota',                   icon: <RefreshCw className="w-4 h-4 text-accent-600" />,    description: 'Send firmware to one or many devices' },
        { label: 'Approved firmware',  href: '/firmware?status=Approved',     icon: <ShieldCheck className="w-4 h-4 text-success-600" />, description: 'Browse production-ready releases' },
        { label: 'Pending approvals',  href: '/firmware?status=PendingApproval', icon: <ListChecks className="w-4 h-4 text-warning-600" />, description: 'Firmware awaiting your sign-off' },
        { label: 'Device status',      href: '/reports/device-status',        icon: <MonitorSmartphone className="w-4 h-4 text-primary-700" />, description: 'Per-device fleet snapshot' },
        { label: 'OTA history',        href: '/reports/device-ota',           icon: <Clock className="w-4 h-4 text-slate-600" />,         description: 'Per-event OTA report' },
        { label: 'Daily progress',     href: '/reports/daily-progress',       icon: <Activity className="w-4 h-4 text-indigo-600" />,     description: 'Today\'s OTA activity' },
        { label: 'Audit logs',         href: '/audit-logs',                   icon: <FileText className="w-4 h-4 text-slate-600" />,      description: 'Recent platform activity' },
      ]

    case UserRole.QA:
      return [
        { label: 'Pending QA',         href: '/firmware?status=PendingQA',    icon: <FlaskConical className="w-4 h-4 text-warning-600" />, description: 'Firmware awaiting QA verification' },
        { label: 'All firmware',       href: '/firmware',                     icon: <Package className="w-4 h-4 text-accent-600" />,      description: 'Browse all firmware versions' },
        { label: 'Firmware report',    href: '/reports/firmware-versions',    icon: <BarChart3 className="w-4 h-4 text-amber-600" />,     description: 'Firmware status across projects' },
        { label: 'Audit logs',         href: '/audit-logs',                   icon: <FileText className="w-4 h-4 text-slate-600" />,      description: 'Recent platform activity' },
      ]

    case UserRole.CustomerAdmin:
      return [
        { label: 'My devices',         href: '/devices',                      icon: <MonitorSmartphone className="w-4 h-4 text-primary-700" />, description: 'Devices in your organisation' },
        { label: 'Device status',      href: '/reports/device-status',        icon: <BarChart3 className="w-4 h-4 text-amber-600" />,     description: 'Per-device fleet snapshot' },
        { label: 'OTA history',        href: '/reports/device-ota',           icon: <Clock className="w-4 h-4 text-slate-600" />,         description: 'Per-event OTA report' },
      ]

    case UserRole.Viewer:
      return [
        { label: 'Devices',            href: '/devices',                      icon: <MonitorSmartphone className="w-4 h-4 text-primary-700" />, description: 'Read-only fleet view' },
        { label: 'Firmware',           href: '/firmware',                     icon: <Package className="w-4 h-4 text-accent-600" />,      description: 'Browse firmware versions' },
        { label: 'Reports',            href: '/reports/device-status',        icon: <BarChart3 className="w-4 h-4 text-amber-600" />,     description: 'Read-only reports' },
      ]

    default:
      return []
  }
}

// ─── Component ───────────────────────────────────────────────────────────────

export function QuickActionsMenu() {
  const { role } = useAuth()
  const router = useRouter()
  const actions = getActions(role)

  if (!actions.length) return null

  return (
    <DropdownMenu.Root>
      <DropdownMenu.Trigger asChild>
        <button
          aria-label="Quick actions"
          title="Quick actions"
          className="relative p-2 rounded-lg text-slate-500 hover:text-accent-600 hover:bg-slate-100 transition-colors focus:outline-none focus:ring-2 focus:ring-accent-500"
        >
          <Zap className="w-5 h-5" />
        </button>
      </DropdownMenu.Trigger>

      <DropdownMenu.Portal>
        <DropdownMenu.Content
          align="end"
          sideOffset={8}
          className="bg-white rounded-xl border border-slate-200 shadow-lg py-1 z-50 w-[320px] max-w-[calc(100vw-2rem)] animate-fade-in"
        >
          <div className="px-4 py-3 border-b border-slate-100">
            <p className="text-sm font-semibold text-primary-900">Quick actions</p>
            <p className="text-xs text-slate-500 mt-0.5">Shortcuts for your role</p>
          </div>

          <div className="py-1 max-h-[420px] overflow-y-auto">
            {actions.map((a) => (
              <DropdownMenu.Item
                key={a.href + a.label}
                onSelect={() => router.push(a.href)}
                className="flex items-start gap-3 px-4 py-2.5 cursor-pointer transition-colors outline-none hover:bg-slate-50 focus:bg-slate-50"
              >
                <div className="flex-shrink-0 mt-0.5">{a.icon}</div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-primary-800 truncate">{a.label}</p>
                  {a.description && (
                    <p className="text-xs text-slate-500 truncate">{a.description}</p>
                  )}
                </div>
              </DropdownMenu.Item>
            ))}
          </div>
        </DropdownMenu.Content>
      </DropdownMenu.Portal>
    </DropdownMenu.Root>
  )
}
