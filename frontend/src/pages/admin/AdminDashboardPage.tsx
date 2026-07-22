import { useCallback, useEffect, useRef, useState } from 'react'
import {
  MdErrorOutline,
  MdEventAvailable,
  MdInventory2,
  MdOutlineReceiptLong,
  MdPets,
  MdRefresh,
} from 'react-icons/md'
import { products } from '../../data/mockData'
import { catalogRepository } from '../../services/catalogRepository'
import type { DashboardMetric, RecentOrder } from '../../types/models'
import { formatVnd } from '../../utils/format'

const metricIcons = {
  orders: MdOutlineReceiptLong,
  reservations: MdEventAvailable,
  stock: MdInventory2,
  cats: MdPets,
}

const todayReservations = [
  { time: '11:30', guest: 'Trần Khánh', people: 2, table: 'Cửa sổ 01' },
  { time: '13:00', guest: 'Mai Linh', people: 4, table: 'Vườn 04' },
  { time: '15:30', guest: 'Hoàng Nam', people: 2, table: 'Gác 02' },
]

const lowStockProducts = products.filter((product) => product.stock <= 7)

const statusLabel: Record<RecentOrder['status'], string> = {
  Pending: 'Đang chờ',
  Processing: 'Đang xử lý',
  Completed: 'Hoàn tất',
}

export function AdminDashboardPage() {
  const [metrics, setMetrics] = useState<DashboardMetric[]>([])
  const [orders, setOrders] = useState<RecentOrder[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(false)
  const requestId = useRef(0)

  const load = useCallback(() => {
    const currentRequest = ++requestId.current
    setLoading(true)
    setError(false)
    catalogRepository
      .getDashboard()
      .then((result) => {
        if (currentRequest !== requestId.current) return
        setMetrics(result.metrics)
        setOrders(result.orders)
        setLoading(false)
      })
      .catch(() => {
        if (currentRequest !== requestId.current) return
        setMetrics([])
        setOrders([])
        setError(true)
        setLoading(false)
      })
  }, [])

  useEffect(() => {
    load()
    return () => { requestId.current += 1 }
  }, [load])

  return (
    <section className="admin-page">
      <div className="admin-page__heading">
        <div>
          <span className="eyebrow">Tổng quan cửa hàng</span>
          <h1>Hôm nay tại Loaf'N Catting</h1>
          <p>Một nhịp nhìn gọn cho ca làm việc, từ đơn mới đến bàn đặt và tồn kho.</p>
        </div>
        <button className="button button--secondary" type="button" onClick={load} disabled={loading}>
          <MdRefresh />Làm mới dữ liệu
        </button>
      </div>

      <div className="admin-hero">
        <div>
          <span>Ca làm việc hiện tại</span>
          <h2>Mọi thứ trong tầm mắt.</h2>
          <p>Đơn hàng, tồn kho và lịch đặt bàn được gom về một nơi để đội ngũ xử lý nhanh hơn.</p>
        </div>
        <div className="admin-hero__index"><span>SHIFT</span><strong>{orders.length || '—'}</strong><small>đơn gần đây</small></div>
      </div>

      {error ? (
        <div className="admin-error" role="alert">
          <MdErrorOutline />
          <div><h2>Không thể tải dashboard</h2><p>Dữ liệu vận hành chưa phản hồi. Hãy thử làm mới sau một chút.</p></div>
          <button type="button" onClick={load}><MdRefresh />Thử lại</button>
        </div>
      ) : (
        <>
          <div className="metric-grid">
            {(loading ? [1, 2, 3, 4] : metrics).map((metric) => {
              if (typeof metric === 'number') {
                return <div className="metric-card metric-card--skeleton" key={metric} />
              }
              const Icon = metricIcons[metric.id as keyof typeof metricIcons] ?? MdOutlineReceiptLong
              return (
                <article className={`metric-card metric-card--${metric.tone}`} key={metric.id}>
                  <span className="metric-card__icon"><Icon /></span>
                  <strong>{metric.value}</strong>
                  <h3>{metric.label}</h3>
                  <p>{metric.note}</p>
                </article>
              )
            })}
          </div>

          <div className="admin-table-card">
            <div className="admin-table-card__heading">
              <div><span className="eyebrow">Luồng vận hành</span><h2>Đơn hàng gần đây</h2><p>Các đơn mới nhất trong không gian quản trị.</p></div>
            </div>
            <div className="table-scroll">
              <table>
                <thead><tr><th>Đơn</th><th>Khách hàng</th><th>Món</th><th>Tổng tiền</th><th>Trạng thái</th><th>Thời gian</th></tr></thead>
                <tbody>
                  {orders.map((order) => (
                    <tr key={order.id}>
                      <td><strong>{order.id}</strong></td>
                      <td>{order.customer}</td>
                      <td>{order.items}</td>
                      <td>{formatVnd(order.total)}</td>
                      <td><span className={`order-status order-status--${order.status.toLowerCase()}`}>{statusLabel[order.status]}</span></td>
                      <td>{order.time}</td>
                    </tr>
                  ))}
                  {!loading && orders.length === 0 && (
                    <tr><td colSpan={6} className="admin-table-empty">Chưa có đơn hàng nào.</td></tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>

          <div className="admin-operations-grid">
            <section className="admin-summary-card" aria-labelledby="reservation-summary-title">
              <header><span className="summary-icon"><MdEventAvailable /></span><div><span className="eyebrow">Theo giờ</span><h2 id="reservation-summary-title">Lịch đặt hôm nay</h2></div></header>
              <div className="reservation-list">
                {todayReservations.map((reservation) => (
                  <article key={`${reservation.time}-${reservation.guest}`}>
                    <strong>{reservation.time}</strong>
                    <div><b>{reservation.guest}</b><span>{reservation.people} khách · {reservation.table}</span></div>
                  </article>
                ))}
              </div>
            </section>

            <section className="admin-summary-card" aria-labelledby="stock-summary-title">
              <header><span className="summary-icon"><MdInventory2 /></span><div><span className="eyebrow">Cần chú ý</span><h2 id="stock-summary-title">Cảnh báo tồn kho</h2></div></header>
              <div className="stock-list">
                {lowStockProducts.map((product) => (
                  <article key={product.id}>
                    <div><b>{product.name}</b><span>{product.categoryName}</span></div>
                    <strong>{product.stock} còn lại</strong>
                  </article>
                ))}
              </div>
            </section>
          </div>
        </>
      )}
    </section>
  )
}
