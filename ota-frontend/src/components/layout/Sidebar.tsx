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
  FlaskConical,
  Users,
  FileText,
  BarChart3,
  Webhook,
  ChevronLeft,
  ChevronRight,
  ChevronDown,
  LogOut,
  Shield,
  PieChart,
  FolderGit2,
  Package,
  Layers,
  History,
  Activity,
  Settings,
  Mail,
  Building2,
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
      {
        label: 'Clients',
        href: '/clients',
        icon: <Building2 className="w-4 h-4" />,
        module: 'Clients',
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
      {
        label: 'Device OTA',
        href: '/device-ota',
        icon: <FlaskConical className="w-4 h-4" />,
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
          { label: 'Device Status',             href: '/reports/device-status',     icon: <PieChart      className="w-3.5 h-3.5" /> },
          { label: 'Users',                     href: '/reports/users',             icon: <Users         className="w-3.5 h-3.5" /> },
          { label: 'Projects',                  href: '/reports/projects',          icon: <FolderGit2    className="w-3.5 h-3.5" /> },
          { label: 'Repositories',              href: '/reports/repositories',      icon: <GitBranch     className="w-3.5 h-3.5" /> },
          { label: 'Firmware Versions',         href: '/reports/firmware-versions', icon: <Package       className="w-3.5 h-3.5" /> },
          { label: 'Devices',                   href: '/reports/devices',           icon: <MonitorSmartphone className="w-3.5 h-3.5" /> },
          { label: 'Project Repos & Firmware',  href: '/reports/project-repos',     icon: <Layers        className="w-3.5 h-3.5" /> },
          { label: 'Device OTA History',        href: '/reports/device-ota',        icon: <History       className="w-3.5 h-3.5" /> },
          { label: 'Daily OTA Progress',        href: '/reports/daily-progress',    icon: <Activity      className="w-3.5 h-3.5" /> },
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
  const [isMobile, setIsMobile] = React.useState(false)
  const [popup, setPopup] = React.useState<{ item: NavItem; y: number } | null>(null)
  const hideTimer = React.useRef<ReturnType<typeof setTimeout>>()

  const displayName = giteaProfile?.login ?? user?.fullName ?? user?.email?.split('@')[0] ?? 'User'
  const avatarUrl   = (!imgError && giteaProfile?.avatar_url) ? giteaProfile.avatar_url : null

  // Track mobile breakpoint
  React.useEffect(() => {
    const check = () => setIsMobile(window.innerWidth < 768)
    check()
    window.addEventListener('resize', check)
    return () => window.removeEventListener('resize', check)
  }, [])

  // Close popup on route change
  React.useEffect(() => { setPopup(null) }, [pathname])

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

  // "showText" = desktop expanded state. On mobile sidebar is always icon-only visually.
  const showText = !collapsed
  // Icon-only when collapsed OR on mobile
  const isIconOnly = collapsed || isMobile

  const openPopup = (item: NavItem, el: HTMLElement) => {
    if (!isIconOnly) return
    clearTimeout(hideTimer.current)
    const rect = el.getBoundingClientRect()
    setPopup({ item, y: rect.top })
  }

  const scheduleClose = () => {
    hideTimer.current = setTimeout(() => setPopup(null), 150)
  }

  const cancelClose = () => clearTimeout(hideTimer.current)

  return (
    <aside
      className={clsx(
        'h-screen flex flex-col bg-primary-900 fixed left-0 top-0 z-40 transition-all duration-300 shadow-sidebar',
        // Mobile: always w-16. Desktop: w-16 or w-[260px]
        'w-16',
        collapsed ? 'md:w-16' : 'md:w-[260px]'
      )}
    >
      {/* Logo */}
      <div className={clsx(
        'flex items-center h-16 border-b border-primary-800 flex-shrink-0 justify-center px-3',
        showText && 'md:justify-start md:px-4 md:gap-3'
      )}>
        <div className="flex-shrink-0 w-9 h-9 rounded-lg bg-white flex items-center justify-center p-1">
          <Image
            src="/logo.png"
            alt="OTA Rax Logo"
            width={36}
            height={36}
            className="object-contain"
          />
        </div>
        {showText && (
          <div className="hidden md:block min-w-0">
            <span className="text-white font-bold text-sm leading-tight block truncate">OTA Platform</span>
            <span className="text-slate-400 text-xs">Admin Portal</span>
          </div>
        )}
      </div>

      {/* Navigation */}
      <nav className="flex-1 overflow-y-auto py-4 px-2 [&::-webkit-scrollbar]:hidden [-ms-overflow-style:none] [scrollbar-width:none]">
        {navigation.map((section) => {
          const visibleItems = section.items.filter((item) => can(item.module, 'view'))
          if (visibleItems.length === 0) return null

          return (
            <div key={section.section} className="mb-4">
              {showText && (
                <p className="hidden md:block text-xs font-semibold text-slate-500 uppercase tracking-wider px-3 mb-1">
                  {section.section}
                </p>
              )}

              {visibleItems.map((item) => {
                // ── Item with children (sub-menu) ───────────────────────────
                if (item.children) {
                  const parentActive = isParentActive(item)
                  const isOpen = openItems.has(item.label)

                  return (
                    <div
                      key={item.label}
                      onMouseEnter={(e) => openPopup(item, e.currentTarget)}
                      onMouseLeave={scheduleClose}
                    >
                      <button
                        onClick={() => showText ? toggleItem(item.label) : undefined}
                        className={clsx(
                          'w-full flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-all duration-150 mb-0.5',
                          'justify-center',
                          showText && 'md:justify-start',
                          parentActive
                            ? 'text-accent-300 bg-primary-800'
                            : 'text-slate-400 hover:text-white hover:bg-primary-800'
                        )}
                      >
                        <span className="flex-shrink-0">{item.icon}</span>
                        {showText && (
                          <>
                            <span className="hidden md:block truncate flex-1 text-left">{item.label}</span>
                            <ChevronDown className={clsx('hidden md:block w-3.5 h-3.5 flex-shrink-0 transition-transform duration-200', isOpen ? 'rotate-180' : '')} />
                          </>
                        )}
                      </button>

                      {/* Sub-items inline — desktop expanded only */}
                      {showText && isOpen && (
                        <div className="hidden md:block ml-3 pl-2.5 border-l border-primary-700 mb-1 mt-0.5">
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
                  <div
                    key={item.href}
                    onMouseEnter={(e) => openPopup(item, e.currentTarget)}
                    onMouseLeave={scheduleClose}
                  >
                    <Link
                      href={item.href!}
                      className={clsx(
                        'flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-all duration-150 mb-0.5',
                        'justify-center',
                        showText && 'md:justify-start',
                        isActive(item.href!)
                          ? 'bg-accent-600 text-white shadow-sm'
                          : 'text-slate-400 hover:text-white hover:bg-primary-800'
                      )}
                    >
                      <span className="flex-shrink-0">{item.icon}</span>
                      {showText && <span className="hidden md:block truncate">{item.label}</span>}
                      {showText && item.badge && (
                        <span className="hidden md:block ml-auto bg-accent-500 text-white text-xs rounded-full px-1.5 py-0.5 leading-none">
                          {item.badge}
                        </span>
                      )}
                    </Link>
                  </div>
                )
              })}
            </div>
          )
        })}
      </nav>

      {/* User Profile */}
      <div className="flex-shrink-0 border-t border-primary-800 p-3">
        {/* Expanded — desktop only when not collapsed */}
        {showText && (
          <div className="hidden md:block space-y-2">
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
        )}
        {/* Collapsed — always visible on mobile, visible on desktop when collapsed */}
        <div className={clsx('flex flex-col items-center gap-2', showText && 'md:hidden')}>
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
      </div>

      {/* Collapse Toggle — desktop only */}
      <button onClick={onToggle}
        className="absolute -right-3 top-20 w-6 h-6 bg-white border border-slate-200 rounded-full hidden md:flex items-center justify-center shadow-sm hover:bg-slate-50 transition-colors z-10"
        aria-label={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}>
        {collapsed
          ? <ChevronRight className="w-3 h-3 text-primary-700" />
          : <ChevronLeft  className="w-3 h-3 text-primary-700" />}
      </button>

      {/* Hover Popup — fixed positioned, shown in icon-only mode */}
      {popup && (() => {
        const top = Math.min(popup.y, window.innerHeight - 320)
        const availableHeight = window.innerHeight - top - 16
        return (
        <div
          className="fixed z-[9999]"
          style={{ top, left: 64 }}
          onMouseEnter={cancelClose}
          onMouseLeave={scheduleClose}
        >
          {popup.item.children ? (
            <div className="ml-2 bg-white rounded-xl shadow-xl border border-slate-200 py-2 min-w-[200px] flex flex-col" style={{ maxHeight: availableHeight }}>
              <p className="px-3 py-1.5 text-xs font-semibold text-slate-400 uppercase tracking-wider border-b border-slate-100 mb-1 flex-shrink-0">
                {popup.item.label}
              </p>
              <div className="overflow-y-auto">
              {popup.item.children.map((child) => (
                <Link
                  key={child.href}
                  href={child.href}
                  onClick={() => setPopup(null)}
                  className={clsx(
                    'flex items-center gap-2.5 px-3 py-2 text-sm font-medium transition-colors',
                    isActive(child.href)
                      ? 'text-accent-600 bg-accent-50'
                      : 'text-slate-700 hover:bg-slate-50'
                  )}
                >
                  <span className={isActive(child.href) ? 'text-accent-500' : 'text-slate-400'}>
                    {child.icon}
                  </span>
                  <span className="truncate">{child.label}</span>
                </Link>
              ))}
              </div>
            </div>
          ) : (
            <div className="ml-2 bg-slate-900 text-white text-xs font-medium rounded-lg px-3 py-2 whitespace-nowrap shadow-xl">
              {popup.item.label}
            </div>
          )}
        </div>
        )
      })()}
    </aside>
  )
}
