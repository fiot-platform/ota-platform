import api from '@/lib/api'

export interface InboxNotification {
  id: string
  title: string
  body: string
  data?: Record<string, string>
  isRead: boolean
  createdAt: string
}

export interface InboxResponse {
  notifications: InboxNotification[]
  unreadCount: number
}

const NotificationService = {
  async getInbox(limit = 50): Promise<InboxResponse> {
    const res = await api.get<{ data: InboxResponse }>('/notifications/inbox', {
      params: { limit },
    })
    return res.data.data
  },

  async markAsRead(id: string): Promise<void> {
    await api.patch(`/notifications/inbox/${id}/read`)
  },

  async markAllAsRead(): Promise<void> {
    await api.post('/notifications/inbox/read-all')
  },
}

export default NotificationService
