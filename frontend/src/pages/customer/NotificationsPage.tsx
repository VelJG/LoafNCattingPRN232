import { useCallback, useEffect, useState, type ComponentType } from 'react'
import {
  MdEventAvailable,
  MdLocalOffer,
  MdMarkEmailUnread,
  MdNotifications,
  MdPets,
} from 'react-icons/md'
import { useAuth } from '../../features/auth/useAuth'
import {
  listNotifications,
  markAllNotificationsRead,
  markNotificationRead,
  type CustomerNotification,
} from '../../features/notifications/notificationApi'
import {
  createNotificationHubConnection,
  notificationRealtimeChangeTypes,
  notificationRealtimeEvents,
  startNotificationHubConnection,
  type NotificationChangedRealtimeEvent,
} from '../../features/notifications/notificationSignalR'

function iconFor(type: string | null): ComponentType {
  if (type?.includes('Reservation')) return MdEventAvailable
  if (type?.includes('Message') || type?.includes('Order')) return MdMarkEmailUnread
  if (type?.includes('Cat')) return MdPets
  if (type?.includes('Offer')) return MdLocalOffer
  return MdNotifications
}

export function NotificationsPage() {
  const token = useAuth().session?.token ?? ''
  const [items, setItems] = useState<CustomerNotification[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    setError(false)
    try {
      setItems(await listNotifications(token))
    } catch {
      setError(true)
    } finally {
      setLoading(false)
    }
  }, [token])

  useEffect(() => { void load() }, [load])

  useEffect(() => {
    if (!token) return

    const connection = createNotificationHubConnection(token)
    const handleChanged = (notificationEvent: NotificationChangedRealtimeEvent) => {
      setItems((current) => {
        switch (notificationEvent.changeType) {
          case notificationRealtimeChangeTypes.created:
            if (!notificationEvent.notification) return current
            return [
              notificationEvent.notification,
              ...current.filter((item) => item.notificationId !== notificationEvent.notification?.notificationId),
            ]
          case notificationRealtimeChangeTypes.read:
            if (!notificationEvent.notification) return current
            return current.map((item) =>
              item.notificationId === notificationEvent.notification?.notificationId
                ? notificationEvent.notification
                : item,
            )
          case notificationRealtimeChangeTypes.readAll:
            return current.map((item) => ({ ...item, isRead: true }))
          default:
            return current
        }
      })
    }

    connection.on(notificationRealtimeEvents.changed, handleChanged)
    void startNotificationHubConnection(connection).catch(() => {
      // Live updates are optional; the page still works with REST loading.
    })

    return () => {
      connection.off(notificationRealtimeEvents.changed, handleChanged)
      void connection.stop()
    }
  }, [token])

  const markOne = async (item: CustomerNotification) => {
    const updated = await markNotificationRead(item.notificationId, token)
    setItems((current) => current.map((candidate) =>
      candidate.notificationId === item.notificationId ? updated : candidate,
    ))
  }

  const markAll = async () => {
    await markAllNotificationsRead(token)
    setItems((current) => current.map((item) => ({ ...item, isRead: true })))
  }

  return (
    <section className="customer-v2-page notifications-v2-page">
      <header className="notifications-v2-heading">
        <h1>Thông báo</h1>
        {items.length > 0 && <button type="button" onClick={() => void markAll()}>ĐÁNH DẤU TẤT CẢ ĐÃ ĐỌC</button>}
      </header>
      {loading && <div className="notifications-v2-loading" aria-label="Đang tải thông báo" />}
      {!loading && error && <div className="customer-v2-feedback" role="alert"><MdNotifications /><div><h2>Không thể tải thông báo</h2><p>Vui lòng thử lại.</p></div><button type="button" onClick={() => void load()}>THỬ LẠI</button></div>}
      {!loading && !error && items.length === 0 && <div className="customer-v2-feedback customer-v2-feedback--empty"><MdNotifications /><div><h2>Chưa có thông báo</h2><p>Các cập nhật từ quán sẽ xuất hiện tại đây.</p></div></div>}
      {!loading && !error && items.map((item) => {
        const Icon = iconFor(item.type)
        return (
          <button
            className={item.isRead ? 'notification-v2-row is-read' : 'notification-v2-row'}
            type="button"
            aria-label={`Đánh dấu ${item.title} là đã đọc`}
            onClick={() => void markOne(item)}
            key={item.notificationId}
          >
            <span className="notification-v2-icon"><Icon aria-hidden="true" /></span>
            <span className="notification-v2-copy"><strong>{item.title}</strong><span>{item.content}</span></span>
          </button>
        )
      })}
    </section>
  )
}
