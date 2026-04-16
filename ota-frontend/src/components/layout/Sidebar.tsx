'use client'

import * as React from 'react'
import Image from 'next/image'
import Link from 'next/link'
import { usePathname, useRouter } from 'next/navigation'
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
  ChevronDown,
  LogOut,
  Shield,
  TrendingUp,
  PieChart,
  FolderGit2,
  Package,
  Layers,
  History,
  Activity,
  GitCommit,
  Settings,
  Mail,
} from 'lucide-react'
import { clsx } from 'clsx'
import { useAuth } from '@/hooks/useAuth'
import { usePermissions } from '@/hooks/usePermissions'
import { useGiteaProfile } from '@/hooks/useGiteaProfile'
import { RoleBadge } from '@/components/ui/Badge'
import { PermissionModule } from '@/lib/permissions'

// ─── Navigation Structure ─────────────────────────────────────────────────────

interface SubNavItem {
  label: string
  href: string
  icon: React.ReactNode
}

interface NavItem {
  label: string
  href?: string
  icon: React.ReactNode
  module: PermissionModule
  badge?: string
  children?: SubNavItem[]
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
        label: 'Settings',
        icon: <Settings className="w-4 h-4" />,
        module: 'Users',
        children: [
          { label: 'Email Notifications', href: '/settings/email-notifications', icon: <Mail className="w-3.5 h-3.5" /> },
        ],
      },
      {
        label: 'Reports',
        icon: <BarChart3 className="w-4 h-4" />,
        module: 'Reports',
        children: [
          { label: 'Firmware Trends',          href: '/reports/firmware-trends',   icon: <TrendingUp    className="w-3.5 h-3.5" /> },
          { label: 'Rollout Success',           href: '/reports/rollout-success',   icon: <BarChart3     className="w-3.5 h-3.5" /> },
          { label: 'Device Status',             href: '/reports/device-status',     icon: <PieChart      className="w-3.5 h-3.5" /> },
          { label: 'Users',                     href: '/reports/users',             icon: <Users         className="w-3.5 h-3.5" /> },
          { label: 'Projects',                  href: '/reports/projects',          icon: <FolderGit2    className="w-3.5 h-3.5" /> },
          { label: 'Repositories',              href: '/reports/repositories',      icon: <GitBranch     className="w-3.5 h-3.5" /> },
          { label: 'Firmware Versions',         href: '/reports/firmware-versions', icon: <Package       className="w-3.5 h-3.5" /> },
          { label: 'Devices',                   href: '/reports/devices',           icon: <MonitorSmartphone className="w-3.5 h-3.5" /> },
          { label: 'Project Repos & Firmware',  href: '/reports/project-repos',     icon: <Layers        className="w-3.5 h-3.5" /> },
          { label: 'Device OTA History',        href: '/reports/device-ota',        icon: <History       className="w-3.5 h-3.5" /> },
          { label: 'Daily OTA Progress',        href: '/reports/daily-progress',    icon: <Activity      className="w-3.5 h-3.5" /> },
          { label: 'Firmware Stage',            href: '/reports/firmware-stage',    icon: <GitCommit     className="w-3.5 h-3.5" /> },
        ],
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
  const router = useRouter()
  const { user, role, logout } = useAuth()
  const { can } = usePermissions()
  const { data: giteaProfile } = useGiteaProfile()
  const [imgError, setImgError] = React.useState(false)
  const [openItems, setOpenItems] = React.useState<Set<string>>(new Set())

  const displayName = giteaProfile?.login ?? user?.fullName ?? user?.email?.split('@')[0] ?? 'User'
  const avatarUrl   = (!imgError && giteaProfile?.avatar_url) ? giteaProfile.avatar_url : null

  // Auto-open parent if a child route is currently active
  React.useEffect(() => {
    navigation.forEach((section) => {
      section.items.forEach((item) => {
        if (item.children?.some((child) => pathname.startsWith(child.href))) {
          setOpenItems((prev) => new Set([...prev, item.label]))
        }
      })
    })
  }, [pathname])

  const toggleItem = (label: string) => {
    setOpenItems((prev) => {
      const next = new Set(prev)
      next.has(label) ? next.delete(label) : next.add(label)
      return next
    })
  }

  const isActive = (href: string) => {
    if (href === '/dashboard') return pathname === '/dashboard'
    return pathname.startsWith(href)
  }

  const isParentActive = (item: NavItem) =>
    item.children?.some((child) => pathname.startsWith(child.href)) ?? false

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
        <Image
          src="/logo.png"
          alt="OTA Rax Logo"
          width={collapsed ? 36 : 40}
          height={collapsed ? 36 : 40}
          className="flex-shrink-0 object-contain"
        />
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

              {visibleItems.map((item) => {
                // ── Item with children (sub-menu) ───────────────────────────
                if (item.children) {
                  const parentActive = isParentActive(item)
                  const isOpen = openItems.has(item.label)

                  return (
                    <div key={item.label}>
                      <button
                        onClick={() =>
                          collapsed
                            ? router.push(item.children![0].href)
                            : toggleItem(item.label)
                        }
                        title={collapsed ? item.label : undefined}
                        className={clsx(
                          'w-full flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-all duration-150 mb-0.5',
                          collapsed ? 'justify-center' : '',
                          parentActive
                            ? 'text-accent-300 bg-primary-800'
                            : 'text-slate-400 hover:text-white hover:bg-primary-800'
                        )}
                      >
                        <span className="flex-shrink-0">{item.icon}</span>
                        {!collapsed && (
                          <>
                            <span className="truncate flex-1 text-left">{item.label}</span>
                            <ChevronDown
                              className={clsx(
                                'w-3.5 h-3.5 flex-shrink-0 transition-transform duration-200',
                                isOpen ? 'rotate-180' : ''
                              )}
                            />
                          </>
                        )}
                      </button>

                      {/* Sub-items */}
                      {!collapsed && isOpen && (
                        <div className="ml-3 pl-2.5 border-l border-primary-700 mb-1 mt-0.5">
                          {item.children.map((child) => (
                            <Link
                              key={child.href}
                              href={child.href}
                              className={clsx(
                                'flex items-center gap-2.5 px-3 py-1.5 rounded-lg text-xs font-medium transition-all duration-150 mb-0.5',
                                isActive(child.href)
                                  ? 'bg-accent-600 text-white shadow-sm'
                                  : 'text-slate-400 hover:text-white hover:bg-primary-800'
                              )}
                            >
                              <span className="flex-shrink-0">{child.icon}</span>
                              <span className="truncate">{child.label}</span>
                            </Link>
                          ))}
                        </div>
                      )}
                    </div>
                  )
                }

                // ── Regular nav item ────────────────────────────────────────
                return (
                  <Link
                    key={item.href}
                    href={item.href!}
                    className={clsx(
                      'flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-all duration-150 mb-0.5',
                      collapsed ? 'justify-center' : '',
                      isActive(item.href!)
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
                )
              })}
            </div>
          )
        })}
      </nav>

      {/* User Profile */}
      <div className="flex-shrink-0 border-t border-primary-800 p-3">
        {!collapsed ? (
          <div className="space-y-2">
            <div className="flex items-center gap-3 p-2 rounded-lg hover:bg-primary-800 transition-colors">
              {avatarUrl ? (
                <div className="w-8 h-8 rounded-full overflow-hidden flex-shrink-0 ring-2 ring-accent-500 bg-white flex items-center justify-center">
                  <Image src={avatarUrl} alt={displayName} width={32} height={32}
                    className="w-8 h-8 object-contain" onError={() => setImgError(true)} unoptimized />
                </div>
              ) : (
                <div className="w-8 h-8 bg-accent-600 rounded-full flex items-center justify-center flex-shrink-0">
                  <span className="text-white text-sm font-semibold">{displayName.charAt(0).toUpperCase()}</span>
                </div>
              )}
              <div className="flex-1 min-w-0">
                <p className="text-white text-sm font-medium truncate">{displayName}</p>
                {role && <RoleBadge role={role} className="mt-0.5" />}
              </div>
              <Shield className="w-4 h-4 text-slate-400 flex-shrink-0" />
            </div>
            <button onClick={logout}
              className="w-full flex items-center gap-2 px-3 py-2 text-sm text-slate-400 hover:text-white hover:bg-primary-800 rounded-lg transition-colors">
              <LogOut className="w-4 h-4" /><span>Sign Out</span>
            </button>
          </div>
        ) : (
          <div className="flex flex-col items-center gap-2">
            {avatarUrl ? (
              <div className="w-8 h-8 rounded-full overflow-hidden ring-2 ring-accent-500 bg-white flex items-center justify-center" title={displayName}>
                <Image src={avatarUrl} alt={displayName} width={32} height={32}
                  className="w-8 h-8 object-contain" onError={() => setImgError(true)} unoptimized />
              </div>
            ) : (
              <div className="w-8 h-8 bg-accent-600 rounded-full flex items-center justify-center" title={displayName}>
                <span className="text-white text-sm font-semibold">{displayName.charAt(0).toUpperCase()}</span>
              </div>
            )}
            <button onClick={logout} title="Sign Out"
              className="w-8 h-8 flex items-center justify-center text-slate-400 hover:text-white hover:bg-primary-800 rounded-lg transition-colors">
              <LogOut className="w-4 h-4" />
            </button>
          </div>
        )}
      </div>

      {/* Collapse Toggle */}
      <button onClick={onToggle}
        className="absolute -right-3 top-20 w-6 h-6 bg-white border border-slate-200 rounded-full flex items-center justify-center shadow-sm hover:bg-slate-50 transition-colors z-10"
        aria-label={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}>
        {collapsed
          ? <ChevronRight className="w-3 h-3 text-primary-700" />
          : <ChevronLeft  className="w-3 h-3 text-primary-700" />}
      </button>
    </aside>
  )
}
