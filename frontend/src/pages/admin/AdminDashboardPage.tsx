import { useCallback, useEffect, useMemo, useState } from 'react'
import {
  MdCalendarMonth,
  MdInventory2,
  MdOutlinePayments,
  MdOutlineReceiptLong,
  MdTrendingUp,
} from 'react-icons/md'
import { Link } from 'react-router-dom'
import {
  listAdminProducts,
  listOrders,
  listStoreReservations,
} from '../../features/admin/adminApi'
import { AdminFeedback } from '../../features/admin/components/AdminFeedback'
import { AdminStatusChip } from '../../features/admin/components/AdminStatusChip'
import type { AdminOrder, AdminProduct, StoreReservation } from '../../features/admin/adminTypes'
import { useAuth } from '../../features/auth/useAuth'

function localDateKey(date = new Date()) {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

function timeLabel(value: string) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value.slice(0, 5)
  return new Intl.DateTimeFormat('vi-VN', { hour: '2-digit', minute: '2-digit', hour12: false }).format(date)
}

function money(value: number) {
  return `${Math.round(value).toLocaleString('vi-VN')} VND`
}

function compactRevenue(value: number) {
  if (value >= 1_000_000) return `${(value / 1_000_000).toLocaleString('vi-VN', { maximumFractionDigits: 1 })}tr`
  if (value >= 1_000) return `${Math.round(value / 1_000)}k`
  return String(value)
}

export function AdminDashboardPage() {
  const token = useAuth().session?.token ?? ''
  const [orders, setOrders] = useState<AdminOrder[] | null>(null)
  const [reservations, setReservations] = useState<StoreReservation[] | null>(null)
  const [products, setProducts] = useState<AdminProduct[] | null>(null)
  const [errors, setErrors] = useState({ orders: false, reservations: false, products: false })
  const [reloadKey, setReloadKey] = useState(0)

  const reload = useCallback(() => setReloadKey((value) => value + 1), [])

  useEffect(() => {
    const controller = new AbortController()
    let alive = true
    setOrders(null)
    setReservations(null)
    setProducts(null)
    setErrors({ orders: false, reservations: false, products: false })

    Promise.allSettled([
      listOrders(token, controller.signal),
      listStoreReservations(token, controller.signal),
      listAdminProducts(token, controller.signal),
    ]).then(([orderResult, reservationResult, productResult]) => {
      if (!alive) return
      if (orderResult.status === 'fulfilled') setOrders(orderResult.value)
      else { setOrders([]); setErrors((current) => ({ ...current, orders: true })) }
      if (reservationResult.status === 'fulfilled') setReservations(reservationResult.value)
      else { setReservations([]); setErrors((current) => ({ ...current, reservations: true })) }
      if (productResult.status === 'fulfilled') setProducts(productResult.value)
      else { setProducts([]); setErrors((current) => ({ ...current, products: true })) }
    })

    return () => { alive = false; controller.abort() }
  }, [reloadKey, token])

  const today = localDateKey()
  const recentOrders = useMemo(() => [...(orders ?? [])]
    .sort((a, b) => new Date(b.orderDate).getTime() - new Date(a.orderDate).getTime())
    .slice(0, 5), [orders])
  const todaysReservations = useMemo(() => (reservations ?? [])
    .filter((reservation) => reservation.date === today)
    .sort((a, b) => a.time.localeCompare(b.time))
    .slice(0, 3), [reservations, today])
  const lowStock = useMemo(() => (products ?? [])
    .filter((product) => product.unitInStock < 10)
    .sort((a, b) => a.unitInStock - b.unitInStock)
    .slice(0, 3), [products])
  const pendingOrders = (orders ?? []).filter((order) => /(pending|chờ|xử lý)/i.test(order.orderStatusName)).length
  const revenue = (orders ?? [])
    .filter((order) => order.orderDate.slice(0, 10) === today)
    .reduce((total, order) => total + order.totalPrice, 0)

  return (
    <section className="admin-page admin-dashboard-v2">
      <div className="admin-stats-grid">
        <article className="admin-stat">
          <div><MdOutlineReceiptLong aria-hidden="true" /><span><MdTrendingUp aria-hidden="true" />+2</span></div>
          <strong className="admin-stat__value">{orders === null ? '—' : pendingOrders}</strong>
          <p>ĐƠN CHỜ XỬ LÝ</p>
        </article>
        <article className="admin-stat">
          <div><MdCalendarMonth aria-hidden="true" /><span><MdTrendingUp aria-hidden="true" />+1</span></div>
          <strong className="admin-stat__value">{reservations === null ? '—' : todaysReservations.length}</strong>
          <p>ĐẶT BÀN HÔM NAY</p>
        </article>
        <article className="admin-stat">
          <div><MdInventory2 aria-hidden="true" /><span className="admin-stat__warning">! Chú ý</span></div>
          <strong className="admin-stat__value">{products === null ? '—' : lowStock.length}</strong>
          <p>SẢN PHẨM SẮP HẾT</p>
        </article>
        <article className="admin-stat">
          <div><MdOutlinePayments aria-hidden="true" /><span><MdTrendingUp aria-hidden="true" />+12%</span></div>
          <strong className="admin-stat__value">{orders === null ? '—' : compactRevenue(revenue)}</strong>
          <p>DOANH THU HÔM NAY</p>
        </article>
      </div>

      <div className="admin-dashboard-grid">
        <section className="admin-dashboard-panel admin-orders-summary">
          <header><h2>Đơn hàng gần đây</h2><Link to="/admin/orders">XEM TẤT CẢ →</Link></header>
          {errors.orders ? (
            <AdminFeedback state="error" title="Không thể tải đơn hàng" message="Dữ liệu đơn hàng chưa phản hồi." onRetry={reload} />
          ) : orders === null ? (
            <AdminFeedback state="loading" />
          ) : recentOrders.length === 0 ? (
            <AdminFeedback state="empty" title="Chưa có đơn hàng" />
          ) : (
            <div className="admin-orders-table">
              <div className="admin-orders-table__head"><span>MÃ ĐƠN</span><span>KHÁCH HÀNG</span><span>GIỜ</span><span>TỔNG</span><span>TRẠNG THÁI</span></div>
              {recentOrders.map((order) => (
                <article key={order.orderId}>
                  <b>#{order.orderId}</b>
                  <span>{order.customerName || 'Khách tại quán'}</span>
                  <time>{timeLabel(order.orderDate)}</time>
                  <strong>{money(order.totalPrice)}</strong>
                  <AdminStatusChip value={order.orderStatusName} />
                </article>
              ))}
            </div>
          )}
        </section>

        <div className="admin-dashboard-side">
          <section className="admin-dashboard-panel admin-reservations-summary">
            <header><h2>Đặt bàn hôm nay</h2><Link to="/admin/reservations">CHI TIẾT →</Link></header>
            {errors.reservations ? (
              <AdminFeedback state="error" title="Không thể tải đặt bàn" message="Danh sách đặt bàn chưa phản hồi." onRetry={reload} />
            ) : reservations === null ? <AdminFeedback state="loading" /> : todaysReservations.length === 0 ? (
              <AdminFeedback state="empty" title="Hôm nay chưa có lịch đặt" />
            ) : (
              <div className="admin-reservation-rows">
                {todaysReservations.map((reservation) => (
                  <article key={reservation.reservationId}>
                    <b>{reservation.time.slice(0, 5)}</b>
                    <span><strong>{reservation.table.tableName}</strong><small>{reservation.guestName}</small></span>
                    <AdminStatusChip value={reservation.status} />
                  </article>
                ))}
              </div>
            )}
          </section>

          <section className="admin-dashboard-panel admin-stock-summary">
            <header><h2>Sắp hết hàng</h2><Link to="/admin/catalog">THỰC ĐƠN →</Link></header>
            {errors.products ? (
              <AdminFeedback state="error" title="Không thể tải tồn kho" message="Dữ liệu sản phẩm chưa phản hồi." onRetry={reload} />
            ) : products === null ? <AdminFeedback state="loading" /> : lowStock.length === 0 ? (
              <AdminFeedback state="empty" title="Tồn kho đang ổn định" />
            ) : (
              <div className="admin-stock-rows">
                {lowStock.map((product) => (
                  <article key={product.productId}>
                    <span className="admin-stock-thumb"><MdInventory2 aria-hidden="true" /></span>
                    <span><strong>{product.name}</strong><small>CÒN {product.unitInStock} · {product.categoryName.toLocaleUpperCase('vi-VN')}</small></span>
                    <i><span style={{ width: `${Math.min(100, Math.max(4, product.unitInStock * 4))}%` }} /></i>
                  </article>
                ))}
              </div>
            )}
          </section>
        </div>
      </div>
    </section>
  )
}
