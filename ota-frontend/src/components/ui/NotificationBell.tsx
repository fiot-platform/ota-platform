'use client'

import * as React from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Bell, CheckCheck, Info, CheckCircle, AlertCircle, AlertTriangle } from 'lucide-react'
import { clsx } from 'clsx'
import NotificationService from '@/services/notification.service'

function NotificationIcon({ title }: { title: string }) {
  const lower = title.toLowerCase()
  if (lower.includes('fail') || lower.includes('error') || lower.includes('denied') || lower.includes('reject'))
    return <AlertCircle className="w-3.5 h-3.5 text-danger-500 flex-shrink-0 mt-0.5" />
  if (lower.includes('warn') || lower.includes('deprecated') || lower.includes('decommission'))
    return <AlertTriangle className="w-3.5 h-3.5 text-warning-500 flex-shrink-0 mt-0.5" />
  if (lower.includes('success') || lower.includes('complet') || lower.includes('approved') || lower.includes('verified') || lower.includes('registered'))
    return <CheckCircle className="w-3.5 h-3.5 text-success-500 flex-shrink-0 mt-0.5" />
  return <Info className="w-3.5 h-3.5 text-accent-500 flex-shrink-0 mt-0.5" />
}

function formatTime(dateStr: string) {
  const diff = Date.now() - new Date(dateStr).getTime()
  const mins = Math.floor(diff / 60_000)
  if (mins < 1) return 'just now'
  if (mins < 60) return `${mins}m ago`
  const hrs = Math.floor(mins / 60)
  if (hrs < 24) return `${hrs}h ago`
  return new Date(dateStr).toLocaleDateString()
}

export function NotificationBell() {
  const queryClient = useQueryClient()
  const [open, setOpen] = React.useState(false)

  const { data, isLoading } = useQuery({
    queryKey: ['notifications-inbox'],
    queryFn: () => NotificationService.getInbox(50),
    refetchInterval: 30_000,
    staleTime: 10_000,
  })

  const markAllMutation = useMutation({
    mutationFn: () => NotificationService.markAllAsRead(),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['notifications-inbox'] }),
  })

  const notifications = data?.notifications ?? []
  const unreadCount = data?.unreadCount ?? 0

  const handleToggle = () => {
    if (!open && unreadCount > 0) markAllMutation.mutate()
    setOpen((v) => !v)
  }

  return (
    <div className="relative">
      <button
        onClick={handleToggle}
        className="relative p-2 rounded-lg text-slate-500 hover:text-primary-700 hover:bg-slate-100 transition-colors"
        title="Notifications"
      >
        <Bell className="w-5 h-5" />
        {unreadCount > 0 && (
          <span className="absolute top-1 right-1 w-2 h-2 rounded-full bg-danger-500 border border-white" />
        )}
      </button>

      {open && (
        <>
          <div className="fixed inset-0 z-40" onClick={() => setOpen(false)} />
          <div className="absolute right-0 top-full mt-2 w-96 bg-white rounded-xl shadow-xl border border-slate-200 z-50 overflow-hidden">
            {/* Header */}
            <div className="flex items-center justify-between px-4 py-3 border-b border-slate-100">
              <span className="text-sm font-semibold text-primary-900 flex items-center gap-2">
                Notifications
                {unreadCount > 0 && (
                  <span className="text-[10px] font-bold px-1.5 py-0.5 rounded-full bg-danger-100 text-danger-600">
                    {unreadCount} unread
                  </span>
                )}
              </span>
              {notifications.length > 0 && (
                <button
                  onClick={() => markAllMutation.mutate()}
                  disabled={unreadCount === 0 || markAllMutation.isPending}
                  className="flex items-center gap-1 text-xs text-slate-400 hover:text-accent-600 disabled:opacity-40 transition-colors"
                >
                  <CheckCheck className="w-3.5 h-3.5" /> Mark all read
                </button>
              )}
            </div>

            {/* List */}
            <div className="max-h-80 overflow-y-auto divide-y divide-slate-50">
              {isLoading ? (
                <div className="flex items-center justify-center py-8 text-sm text-slate-400">
                  Loading…
                </div>
              ) : notifications.length === 0 ? (
                <div className="flex flex-col items-center gap-2 py-8 text-slate-400">
                  <Bell className="w-8 h-8 opacity-30" />
                  <p className="text-sm">No notifications yet</p>
                </div>
              ) : notifications.map((n) => (
                <div
                  key={n.id}
                  className={clsx(
                    'flex items-start gap-3 px-4 py-3 transition-colors',
                    !n.isRead ? 'bg-accent-50/40' : 'hover:bg-slate-50'
                  )}
                >
                  <NotificationIcon title={n.title} />
                  <div className="flex-1 min-w-0">
                    <p className={clsx('text-sm font-medium leading-snug', n.isRead ? 'text-slate-500' : 'text-primary-900')}>
                      {n.title}
                    </p>
                    {n.body && (
                      <p className="text-xs text-slate-400 mt-0.5 line-clamp-2">{n.body}</p>
                    )}
                    <p className="text-[10px] text-slate-300 mt-1">{formatTime(n.createdAt)}</p>
                  </div>
                  {!n.isRead && (
                    <span className="w-2 h-2 rounded-full bg-accent-500 flex-shrink-0 mt-1.5" />
                  )}
                </div>
              ))}
            </div>
          </div>
        </>
      )}
    </div>
  )
}
