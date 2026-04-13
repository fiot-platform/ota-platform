'use client'

import * as React from 'react'
import Image from 'next/image'
import Link from 'next/link'
import { usePathname } from 'next/navigation'
import {
  LayoutDashboard,
  FolderOpen,
  GitBranch,
  Cpu,
  MonitorSmartphone,
  RefreshCw,
  Users,
  FileText,
  BarChart3,
  Webhook,
  ChevronLeft,
  ChevronRight,
  LogOut,
  Shield,
  Radio,
} from 'lucide-react'
import { clsx } from 'clsx'
import { useAuth } from '@/hooks/useAuth'
import { usePermissions } from '@/hooks/usePermissions'
import { useGiteaProfile } from '@/hooks/useGiteaProfile'
import { RoleBadge } from '@/components/ui/Badge'
import { PermissionModule } from '@/lib/permissions'

// ─── Navigation Structure ─────────────────────────────────────────────────────

interface NavItem {
  label: string
  href: string
  icon: React.ReactNode
  module: PermissionModule
  badge?: string
}

interface NavSection {
  section: string
  items: NavItem[]
}

const navigation: NavSection[] = [
  {
    section: 'Main',
    items: [
      {
        label: 'Dashboard',
        href: '/dashboard',
        icon: <LayoutDashboard className="w-4 h-4" />,
        module: 'Dashboard',
      },
    ],
  },
  {
    section: 'Management',
    items: [
      {
        label: 'Projects',
        href: '/projects',
        icon: <FolderOpen className="w-4 h-4" />,
        module: 'Projects',
      },
      {
        label: 'Repositories',
        href: '/repositories',
        icon: <GitBranch className="w-4 h-4" />,
        module: 'Repositories',
      },
      {
        label: 'Firmware',
        href: '/firmware',
        icon: <Cpu className="w-4 h-4" />,
        module: 'Firmware',
      },
      {
        label: 'Devices',
        href: '/devices',
        icon: <MonitorSmartphone className="w-4 h-4" />,
        module: 'Devices',
      },
    ],
  },
  {
    section: 'Operations',
    items: [
      {
        label: 'OTA Rollouts',
        href: '/ota-rollouts',
        icon: <RefreshCw className="w-4 h-4" />,
        module: 'OtaRollouts',
      },
    ],
  },
  {
    section: 'Administration',
    items: [
      {
        label: 'Users',
        href: '/users',
        icon: <Users className="w-4 h-4" />,
        module: 'Users',
      },
      {
        label: 'Audit Logs',
        href: '/audit-logs',
        icon: <FileText className="w-4 h-4" />,
        module: 'AuditLogs',
      },
      {
        label: 'Reports',
        href: '/reports',
        icon: <BarChart3 className="w-4 h-4" />,
        module: 'Reports',
      },
    ],
  },
  {
    section: 'Developer',
    items: [
      {
        label: 'Webhook Events',
        href: '/webhook-events',
        icon: <Webhook className="w-4 h-4" />,
        module: 'WebhookEvents',
      },
    ],
  },
]

// ─── Sidebar Component ────────────────────────────────────────────────────────

interface SidebarProps {
  collapsed: boolean
  onToggle: () => void
}

export function Sidebar({ collapsed, onToggle }: SidebarProps) {
  const pathname = usePathname()
  const { user, role, logout } = useAuth()
  const { can } = usePermissions()
  const { data: giteaProfile } = useGiteaProfile()
  const [imgError, setImgError] = React.useState(false)

  const displayName = giteaProfile?.login ?? user?.fullName ?? user?.email?.split('@')[0] ?? 'User'
  const avatarUrl   = (!imgError && giteaProfile?.avatar_url) ? giteaProfile.avatar_url : null

  const isActive = (href: string) => {
    if (href === '/dashboard') return pathname === '/dashboard'
    return pathname.startsWith(href)
  }

  return (
    <aside
      className={clsx(
        'h-screen flex flex-col bg-primary-900 fixed left-0 top-0 z-40 transition-all duration-300 shadow-sidebar',
        collapsed ? 'w-16' : 'w-[260px]'
      )}
    >
      {/* Logo */}
      <div className={clsx(
        'flex items-center h-16 border-b border-primary-800 flex-shrink-0',
        collapsed ? 'justify-center px-3' : 'px-4 gap-3'
      )}>
        <div className="w-8 h-8 bg-accent-600 rounded-lg flex items-center justify-center flex-shrink-0">
          <Radio className="w-5 h-5 text-white" />
        </div>
        {!collapsed && (
          <div className="min-w-0">
            <span className="text-white font-bold text-sm leading-tight block truncate">OTA Platform</span>
            <span className="text-slate-400 text-xs">Admin Portal</span>
          </div>
        )}
      </div>

      {/* Navigation */}
      <nav className="flex-1 overflow-y-auto py-4 scrollbar-thin px-2">
        {navigation.map((section) => {
          const visibleItems = section.items.filter((item) => can(item.module, 'view'))
          if (visibleItems.length === 0) return null

          return (
            <div key={section.section} className="mb-4">
              {!collapsed && (
                <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider px-3 mb-1">
                  {section.section}
                </p>
              )}
              {visibleItems.map((item) => (
                <Link
                  key={item.href}
                  href={item.href}
                  className={clsx(
                    'flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-all duration-150 mb-0.5',
                    collapsed ? 'justify-center' : '',
                    isActive(item.href)
                      ? 'bg-accent-600 text-white shadow-sm'
                      : 'text-slate-400 hover:text-white hover:bg-primary-800'
                  )}
                  title={collapsed ? item.label : undefined}
                >
                  <span className="flex-shrink-0">{item.icon}</span>
                  {!collapsed && <span className="truncate">{item.label}</span>}
                  {!collapsed && item.badge && (
                    <span className="ml-auto bg-accent-500 text-white text-xs rounded-full px-1.5 py-0.5 leading-none">
                      {item.badge}
                    </span>
                  )}
                </Link>
              ))}
            </div>
          )
        })}
      </nav>

      {/* User Profile */}
      <div className="flex-shrink-0 border-t border-primary-800 p-3">
        {!collapsed ? (
          <div className="space-y-2">
            <div className="flex items-center gap-3 p-2 rounded-lg hover:bg-primary-800 transition-colors">
              {/* Avatar */}
              {avatarUrl ? (
                <div className="w-8 h-8 rounded-full overflow-hidden flex-shrink-0 ring-2 ring-accent-500 bg-white flex items-center justify-center">
                  <Image
                    src={avatarUrl}
                    alt={displayName}
                    width={32}
                    height={32}
                    className="w-8 h-8 object-contain"
                    onError={() => setImgError(true)}
                    unoptimized
                  />
                </div>
              ) : (
                <div className="w-8 h-8 bg-accent-600 rounded-full flex items-center justify-center flex-shrink-0">
                  <span className="text-white text-sm font-semibold">
                    {displayName.charAt(0).toUpperCase()}
                  </span>
                </div>
              )}
              <div className="flex-1 min-w-0">
                <p className="text-white text-sm font-medium truncate">{displayName}</p>
                {role && <RoleBadge role={role} className="mt-0.5" />}
              </div>
              <Shield className="w-4 h-4 text-slate-400 flex-shrink-0" />
            </div>
            <button
              onClick={logout}
              className="w-full flex items-center gap-2 px-3 py-2 text-sm text-slate-400 hover:text-white hover:bg-primary-800 rounded-lg transition-colors"
            >
              <LogOut className="w-4 h-4" />
              <span>Sign Out</span>
            </button>
          </div>
        ) : (
          <div className="flex flex-col items-center gap-2">
            {avatarUrl ? (
              <div className="w-8 h-8 rounded-full overflow-hidden ring-2 ring-accent-500 bg-white flex items-center justify-center" title={displayName}>
                <Image
                  src={avatarUrl}
                  alt={displayName}
                  width={32}
                  height={32}
                  className="w-8 h-8 object-contain"
                  onError={() => setImgError(true)}
                  unoptimized
                />
              </div>
            ) : (
              <div className="w-8 h-8 bg-accent-600 rounded-full flex items-center justify-center" title={displayName}>
                <span className="text-white text-sm font-semibold">
                  {displayName.charAt(0).toUpperCase()}
                </span>
              </div>
            )}
            <button
              onClick={logout}
              title="Sign Out"
              className="w-8 h-8 flex items-center justify-center text-slate-400 hover:text-white hover:bg-primary-800 rounded-lg transition-colors"
            >
              <LogOut className="w-4 h-4" />
            </button>
          </div>
        )}
      </div>

      {/* Collapse Toggle */}
      <button
        onClick={onToggle}
        className="absolute -right-3 top-20 w-6 h-6 bg-white border border-slate-200 rounded-full flex items-center justify-center shadow-sm hover:bg-slate-50 transition-colors z-10"
        aria-label={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
      >
        {collapsed ? (
          <ChevronRight className="w-3 h-3 text-primary-700" />
        ) : (
          <ChevronLeft className="w-3 h-3 text-primary-700" />
        )}
      </button>
    </aside>
  )
}
