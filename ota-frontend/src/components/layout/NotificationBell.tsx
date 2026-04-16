'use client'

import * as React from 'react'
import * as Popover from '@radix-ui/react-popover'
import { Bell, CheckCheck, Inbox, Loader2 } from 'lucide-react'
import { clsx } from 'clsx'
import { useNotifications } from '@/context/NotificationContext'
import { InboxNotification } from '@/services/notification.service'

// ─── Relative time helper ─────────────────────────────────────────────────────

function relativeTime(iso: string): string {
  const diff = Date.now() - new Date(iso).getTime()
  const mins  = Math.floor(diff / 60_000)
  const hours = Math.floor(diff / 3_600_000)
  const days  = Math.floor(diff / 86_400_000)
  if (mins < 1)   return 'Just now'
  if (mins < 60)  return `${mins}m ago`
  if (hours < 24) return `${hours}h ago`
  return `${days}d ago`
}

// ─── Notification type icon colour ───────────────────────────────────────────

function typeColour(data?: Record<string, string>): string {
  const type = data?.type ?? ''
  if (type.includes('deleted') || type.includes('failed') || type.includes('rejected'))
    return 'bg-danger-100 text-danger-600'
  if (type.includes('created') || type.includes('registered') || type.includes('approved'))
    return 'bg-success-100 text-success-600'
  if (type.includes('bug'))
    return 'bg-warning-100 text-warning-600'
  return 'bg-accent-100 text-accent-600'
}

// ─── Single notification row ──────────────────────────────────────────────────

function NotificationRow({
  item,
  onRead,
}: {
  item: InboxNotification
  onRead: (id: string) => void
}) {
  return (
    <button
      onClick={() => { if (!item.isRead) onRead(item.id) }}
      className={clsx(
        'w-full text-left px-4 py-3 flex gap-3 items-start hover:bg-slate-50 transition-colors border-b border-slate-100 last:border-0',
        !item.isRead && 'bg-accent-50/40'
      )}
    >
      {/* Colour dot */}
      <span className={clsx('mt-0.5 w-2 h-2 rounded-full flex-shrink-0', !item.isRead ? 'bg-accent-500' : 'bg-slate-300')} />

      <div className="flex-1 min-w-0">
        <p className={clsx('text-sm leading-tight truncate', !item.isRead ? 'font-semibold text-primary-900' : 'font-medium text-slate-700')}>
          {item.title}
        </p>
        <p className="text-xs text-slate-500 mt-0.5 line-clamp-2">{item.body}</p>
        <p className="text-[11px] text-slate-400 mt-1">{relativeTime(item.createdAt)}</p>
      </div>
    </button>
  )
}

// ─── NotificationBell ─────────────────────────────────────────────────────────

export function NotificationBell() {
  const { notifications, unreadCount, loading, markAsRead, markAllAsRead } = useNotifications()
  const [open, setOpen] = React.useState(false)

  // Mark visible unread as read when panel opens
  const handleOpenChange = (isOpen: boolean) => {
    setOpen(isOpen)
  }

  return (
    <Popover.Root open={open} onOpenChange={handleOpenChange}>
      <Popover.Trigger asChild>
        <button
          className="relative w-9 h-9 flex items-center justify-center rounded-lg text-slate-500 hover:text-primary-700 hover:bg-slate-100 transition-colors"
          aria-label="Notifications"
        >
          <Bell className="w-5 h-5" />
          {unreadCount > 0 && (
            <span className="absolute top-1 right-1 min-w-[18px] h-[18px] bg-danger-500 text-white text-[10px] font-bold rounded-full flex items-center justify-center px-1 leading-none">
              {unreadCount > 99 ? '99+' : unreadCount}
            </span>
          )}
        </button>
      </Popover.Trigger>

      <Popover.Portal>
        <Popover.Content
          align="end"
          sideOffset={8}
          className="z-50 w-[380px] bg-white rounded-xl border border-slate-200 shadow-xl animate-fade-in"
        >
          {/* Header */}
          <div className="flex items-center justify-between px-4 py-3 border-b border-slate-100">
            <div className="flex items-center gap-2">
              <Bell className="w-4 h-4 text-accent-600" />
              <span className="text-sm font-semibold text-primary-900">Notifications</span>
              {unreadCount > 0 && (
                <span className="bg-accent-100 text-accent-700 text-xs font-bold px-2 py-0.5 rounded-full">
                  {unreadCount} new
                </span>
              )}
            </div>
            {unreadCount > 0 && (
              <button
                onClick={() => markAllAsRead()}
                className="flex items-center gap-1 text-xs text-accent-600 hover:text-accent-800 font-medium transition-colors"
              >
                <CheckCheck className="w-3.5 h-3.5" />
                Mark all read
              </button>
            )}
          </div>

          {/* List */}
          <div className="max-h-[420px] overflow-y-auto">
            {loading && notifications.length === 0 ? (
              <div className="flex items-center justify-center py-10 text-slate-400">
                <Loader2 className="w-5 h-5 animate-spin mr-2" />
                <span className="text-sm">Loading…</span>
              </div>
            ) : notifications.length === 0 ? (
              <div className="flex flex-col items-center justify-center py-12 text-slate-400 gap-2">
                <Inbox className="w-8 h-8" />
                <p className="text-sm">No notifications yet</p>
              </div>
            ) : (
              notifications.map((n) => (
                <NotificationRow
                  key={n.id}
                  item={n}
                  onRead={markAsRead}
                />
              ))
            )}
          </div>

          {/* Footer */}
          {notifications.length > 0 && (
            <div className="px-4 py-2.5 border-t border-slate-100 text-center">
              <p className="text-xs text-slate-400">
                Showing last {notifications.length} notification{notifications.length !== 1 ? 's' : ''}
              </p>
            </div>
          )}

          <Popover.Arrow className="fill-white drop-shadow-sm" />
        </Popover.Content>
      </Popover.Portal>
    </Popover.Root>
  )
}
