import { requestJson } from '../../api/httpClient'

export interface CustomerNotification {
  notificationId: number
  title: string
  content: string
  type: string | null
  isRead: boolean
  createdAtUtc: string
}

export interface MarkNotificationsReadResult {
  updatedCount: number
}

export function listNotifications(token: string) {
  return requestJson<CustomerNotification[]>('/notifications', { token })
}

export function markNotificationRead(notificationId: number, token: string) {
  return requestJson<CustomerNotification>(`/notifications/${notificationId}/read`, {
    method: 'PATCH',
    token,
  })
}

export function markAllNotificationsRead(token: string) {
  return requestJson<MarkNotificationsReadResult>('/notifications/read-all', {
    method: 'PATCH',
    token,
  })
}
