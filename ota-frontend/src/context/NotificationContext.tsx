'use client'

import * as React from 'react'
import NotificationService, { InboxNotification } from '@/services/notification.service'
import { useToast } from '@/components/ui/ToastProvider'
import { useAuth } from '@/hooks/useAuth'

// ─── Types ────────────────────────────────────────────────────────────────────

interface NotificationContextValue {
  notifications: InboxNotification[]
  unreadCount: number
  loading: boolean
  markAsRead: (id: string) => Promise<void>
  markAllAsRead: () => Promise<void>
  refresh: () => Promise<void>
}

const NotificationContext = React.createContext<NotificationContextValue | null>(null)

export function useNotifications(): NotificationContextValue {
  const ctx = React.useContext(NotificationContext)
  if (!ctx) throw new Error('useNotifications must be used within NotificationProvider')
  return ctx
}

// ─── Provider ─────────────────────────────────────────────────────────────────

const POLL_INTERVAL_MS = 30_000 // 30 seconds

export function NotificationProvider({ children }: { children: React.ReactNode }) {
  const { user } = useAuth()
  const { toast } = useToast()

  const [notifications, setNotifications] = React.useState<InboxNotification[]>([])
  const [unreadCount, setUnreadCount] = React.useState(0)
  const [loading, setLoading] = React.useState(false)

  // Track last seen unread count to trigger toasts only on new ones
  const prevUnreadRef = React.useRef<number>(0)
  const prevIdsRef    = React.useRef<Set<string>>(new Set())

  const fetchInbox = React.useCallback(async () => {
    if (!user) return
    try {
      const data = await NotificationService.getInbox(50)

      // Show toast for each new unread notification
      const newItems = data.notifications.filter(
        (n) => !n.isRead && !prevIdsRef.current.has(n.id)
      )
      newItems.forEach((n) => {
        toast({
          title: n.title,
          description: n.body,
          variant: 'info',
          duration: 6000,
        })
      })

      // Update tracking sets
      data.notifications.forEach((n) => prevIdsRef.current.add(n.id))
      prevUnreadRef.current = data.unreadCount

      setNotifications(data.notifications)
      setUnreadCount(data.unreadCount)
    } catch {
      // Fail silently — notifications are non-critical
    }
  }, [user, toast])

  // Initial load
  React.useEffect(() => {
    if (!user) return
    setLoading(true)
    fetchInbox().finally(() => setLoading(false))
  }, [user, fetchInbox])

  // Polling
  React.useEffect(() => {
    if (!user) return
    const id = setInterval(fetchInbox, POLL_INTERVAL_MS)
    return () => clearInterval(id)
  }, [user, fetchInbox])

  const markAsRead = React.useCallback(async (id: string) => {
    await NotificationService.markAsRead(id)
    setNotifications((prev) =>
      prev.map((n) => (n.id === id ? { ...n, isRead: true } : n))
    )
    setUnreadCount((c) => Math.max(0, c - 1))
  }, [])

  const markAllAsRead = React.useCallback(async () => {
    await NotificationService.markAllAsRead()
    setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })))
    setUnreadCount(0)
  }, [])

  const refresh = React.useCallback(async () => {
    await fetchInbox()
  }, [fetchInbox])

  return (
    <NotificationContext.Provider value={{ notifications, unreadCount, loading, markAsRead, markAllAsRead, refresh }}>
      {children}
    </NotificationContext.Provider>
  )
}
