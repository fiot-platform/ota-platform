'use client'

import * as React from 'react'
import Image from 'next/image'
import { usePathname } from 'next/navigation'
import * as DropdownMenu from '@radix-ui/react-dropdown-menu'
import { ChevronDown, LogOut, User, Settings } from 'lucide-react'
import { clsx } from 'clsx'
import { useAuth } from '@/hooks/useAuth'
import { useGiteaProfile } from '@/hooks/useGiteaProfile'
import { RoleBadge } from '@/components/ui/Badge'
import { useRouter } from 'next/navigation'
import { NotificationBell } from '@/components/layout/NotificationBell'

// ─── Route Title Map ──────────────────────────────────────────────────────────

const routeTitles: Record<string, string> = {
  '/dashboard': 'Dashboard',
  '/projects': 'Projects',
  '/repositories': 'Repositories',
  '/firmware': 'Firmware Versions',
  '/devices': 'Devices',
  '/ota-rollouts': 'OTA Rollouts',
  '/users': 'User Management',
  '/audit-logs': 'Audit Logs',
  '/reports': 'Reports & Analytics',
  '/webhook-events': 'Webhook Events',
  '/profile': 'My Profile',
}

function getPageTitle(pathname: string): string {
  if (routeTitles[pathname]) return routeTitles[pathname]

  const segments = pathname.split('/').filter(Boolean)
  if (segments.length >= 2) {
    const base = `/${segments[0]}`
    const baseTitle = routeTitles[base]
    if (baseTitle) {
      if (segments[1] === 'new') return `New ${baseTitle.slice(0, -1)}`
      return `${baseTitle.slice(0, -1)} Details`
    }
  }
  return 'OTA Platform'
}

// ─── Avatar ───────────────────────────────────────────────────────────────────

function UserAvatar({ avatarUrl, displayName }: { avatarUrl?: string | null; displayName: string }) {
  const [imgError, setImgError] = React.useState(false)

  if (avatarUrl && !imgError) {
    return (
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
    )
  }

  return (
    <div className="w-8 h-8 bg-accent-600 rounded-full flex items-center justify-center">
      <span className="text-white text-sm font-semibold">
        {displayName.charAt(0).toUpperCase()}
      </span>
    </div>
  )
}

// ─── Header Component ─────────────────────────────────────────────────────────

interface HeaderProps {
  sidebarCollapsed: boolean
}

export function Header({ sidebarCollapsed }: HeaderProps) {
  const pathname = usePathname()
  const router = useRouter()
  const { user, role, logout } = useAuth()
  const { data: giteaProfile } = useGiteaProfile()
  const pageTitle = getPageTitle(pathname)

  // Prefer Gitea login name → email prefix → fallback
  const displayName = giteaProfile?.login
    ?? user?.fullName
    ?? user?.email?.split('@')[0]
    ?? 'User'

  const avatarUrl = giteaProfile?.avatar_url ?? null

  return (
    <header
      className={clsx(
        'fixed top-0 right-0 z-30 h-16 bg-white border-b border-slate-200 flex items-center justify-between px-6 transition-all duration-300 shadow-sm',
        sidebarCollapsed ? 'left-16' : 'left-[260px]'
      )}
    >
      {/* Left: Page title */}
      <div>
        <h2 className="text-lg font-semibold text-primary-900">{pageTitle}</h2>
        <p className="text-xs text-slate-400 capitalize">
          {new Date().toLocaleDateString('en-US', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}
        </p>
      </div>

      {/* Right: Actions */}
      <div className="flex items-center gap-3">
        {/* Notification Bell */}
        <NotificationBell />

        {/* User Menu */}
        <DropdownMenu.Root>
          <DropdownMenu.Trigger asChild>
            <button className="flex items-center gap-2.5 px-3 py-1.5 rounded-lg hover:bg-slate-100 transition-colors focus:outline-none focus:ring-2 focus:ring-accent-500">
              <UserAvatar avatarUrl={avatarUrl} displayName={displayName} />
              <div className="text-left hidden sm:block">
                <p className="text-sm font-medium text-primary-900 leading-tight">{displayName}</p>
                {role && <RoleBadge role={role} />}
              </div>
              <ChevronDown className="w-4 h-4 text-slate-400 hidden sm:block" />
            </button>
          </DropdownMenu.Trigger>

          <DropdownMenu.Portal>
            <DropdownMenu.Content
              align="end"
              sideOffset={8}
              className="bg-white rounded-xl border border-slate-200 shadow-lg py-1 z-50 min-w-[220px] animate-fade-in"
            >
              <div className="px-4 py-3 border-b border-slate-100 flex items-center gap-3">
                <UserAvatar avatarUrl={avatarUrl} displayName={displayName} />
                <div className="min-w-0">
                  <p className="text-sm font-semibold text-primary-900 truncate">{displayName}</p>
                  <p className="text-xs text-slate-500 truncate">{user?.email}</p>
                  {giteaProfile && (
                    <p className="text-xs text-accent-600 truncate">@{giteaProfile.login} on Gitea</p>
                  )}
                </div>
              </div>

              <DropdownMenu.Item
                className="flex items-center gap-2 px-4 py-2 text-sm text-primary-700 hover:bg-slate-50 cursor-pointer transition-colors outline-none"
                onClick={() => router.push('/profile')}
              >
                <User className="w-4 h-4 text-slate-400" />
                Profile Settings
              </DropdownMenu.Item>

              <DropdownMenu.Item
                className="flex items-center gap-2 px-4 py-2 text-sm text-primary-700 hover:bg-slate-50 cursor-pointer transition-colors outline-none"
                onClick={() => router.push('/profile')}
              >
                <Settings className="w-4 h-4 text-slate-400" />
                Preferences
              </DropdownMenu.Item>

              <DropdownMenu.Separator className="h-px bg-slate-100 my-1" />

              <DropdownMenu.Item
                className="flex items-center gap-2 px-4 py-2 text-sm text-danger-600 hover:bg-danger-50 cursor-pointer transition-colors outline-none"
                onClick={logout}
              >
                <LogOut className="w-4 h-4" />
                Sign Out
              </DropdownMenu.Item>
            </DropdownMenu.Content>
          </DropdownMenu.Portal>
        </DropdownMenu.Root>
      </div>
    </header>
  )
}
