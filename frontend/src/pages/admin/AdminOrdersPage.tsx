import { useCallback, useEffect, useMemo, useState } from 'react'
import { ApiError } from '../../api/httpClient'
import { listOrders, updateOrderStatus } from '../../features/admin/adminApi'
import { AdminFeedback } from '../../features/admin/components/AdminFeedback'
import { AdminStatusChip } from '../../features/admin/components/AdminStatusChip'
import { AdminToast } from '../../features/admin/components/AdminToast'
import type { AdminOrder } from '../../features/admin/adminTypes'
import { useAuth } from '../../features/auth/useAuth'

const filters = [
  { key: 'all', label: 'Tất cả', matches: () => true },
  { key: 'pending', label: 'Chờ xử lý', matches: (status: string) => /(pending|chờ|xử lý)/i.test(status) },
  { key: 'processing', label: 'Đang pha chế', matches: (status: string) => /(processing|pha chế)/i.test(status) },
  { key: 'completed', label: 'Hoàn thành', matches: (status: string) => /(completed|hoàn)/i.test(status) },
  { key: 'cancelled', label: 'Đã hủy', matches: (status: string) => /(cancel|hủy|huỷ)/i.test(status) },
] as const

const nextStatus: Record<number, number | undefined> = { 1: 2, 2: 3, 3: 4 }

function money(value: number) {
  return `${Math.round(value).toLocaleString('vi-VN')} VND`
}

function dateTime(value: string) {
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : new Intl.DateTimeFormat('vi-VN', {
    day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit', hour12: false,
  }).format(date)
}

export function AdminOrdersPage() {
  const session = useAuth().session
  const token = session?.token ?? ''
  const [orders, setOrders] = useState<AdminOrder[] | null>(null)
  const [filter, setFilter] = useState<(typeof filters)[number]['key']>('all')
  const [error, setError] = useState(false)
  const [updating, setUpdating] = useState<number | null>(null)
  const [toast, setToast] = useState<{ message: string; tone: 'success' | 'error' } | null>(null)
  const [reloadKey, setReloadKey] = useState(0)

  const reload = useCallback(() => setReloadKey((value) => value + 1), [])
  useEffect(() => {
    const controller = new AbortController()
    let alive = true
    setOrders(null)
    setError(false)
    listOrders(token, controller.signal)
      .then((items) => { if (alive) setOrders(items) })
      .catch(() => { if (alive) { setOrders([]); setError(true) } })
    return () => { alive = false; controller.abort() }
  }, [reloadKey, token])

  const visibleOrders = useMemo(() => {
    const active = filters.find((item) => item.key === filter) ?? filters[0]
    return [...(orders ?? [])]
      .filter((order) => active.matches(order.orderStatusName))
      .sort((a, b) => new Date(b.orderDate).getTime() - new Date(a.orderDate).getTime())
  }, [filter, orders])

  const advance = async (order: AdminOrder) => {
    const orderStatusId = nextStatus[order.orderStatusId]
    if (!orderStatusId) return
    setUpdating(order.orderId)
    try {
      const updated = await updateOrderStatus(token, order.orderId, orderStatusId)
      setOrders((current) => current?.map((item) => item.orderId === updated.orderId ? updated : item) ?? [])
      setToast({ message: `Đã cập nhật đơn #${order.orderId}`, tone: 'success' })
    } catch (caught) {
      setToast({ message: caught instanceof ApiError ? caught.detail : 'Không thể cập nhật đơn hàng.', tone: 'error' })
    } finally {
      setUpdating(null)
    }
  }

  return (
    <section className="admin-page admin-list-page">
      <div className="admin-filter-row" aria-label="Lọc trạng thái đơn hàng">
        {filters.map((item) => (
          <button className={filter === item.key ? 'is-active' : ''} type="button" key={item.key} onClick={() => setFilter(item.key)}>{item.label}</button>
        ))}
      </div>

      {error ? <AdminFeedback state="error" title="Không thể tải đơn hàng" message="Vui lòng kiểm tra kết nối tới máy chủ." onRetry={reload} />
        : orders === null ? <AdminFeedback state="loading" />
          : visibleOrders.length === 0 ? <AdminFeedback state="empty" title="Không có đơn phù hợp" message="Hãy chọn trạng thái khác để xem đơn hàng." />
            : (
              <div className="admin-entity-grid">
                {visibleOrders.map((order) => {
                  const canAdvance = Boolean(nextStatus[order.orderStatusId])
                  const payment = order.payments[0]
                  return (
                    <article className="admin-order-card" key={order.orderId}>
                      <header><h2>#{order.orderId}</h2><strong>{money(order.totalPrice)}</strong></header>
                      <p>Khách: {order.customerName || 'Khách tại quán'}</p>
                      <time>{dateTime(order.orderDate)}</time>
                      <div className="admin-card-chips">
                        <AdminStatusChip value={order.orderStatusName} />
                        <AdminStatusChip value={payment?.paymentStatus || 'Chưa thanh toán'} />
                      </div>
                      {canAdvance && (
                        <div className="admin-card-actions">
                          <button type="button" disabled={updating === order.orderId} onClick={() => advance(order)} aria-label={`Cập nhật trạng thái đơn #${order.orderId}`}>
                            {updating === order.orderId ? 'ĐANG CẬP NHẬT...' : 'CẬP NHẬT TRẠNG THÁI'}
                          </button>
                        </div>
                      )}
                    </article>
                  )
                })}
              </div>
            )}
      {toast && <AdminToast message={toast.message} tone={toast.tone} onDismiss={() => setToast(null)} />}
    </section>
  )
}
