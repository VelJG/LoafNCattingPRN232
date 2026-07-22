import { useCallback, useEffect, useMemo, useState } from 'react'
import {
  MdErrorOutline,
  MdInventory2,
  MdOutlinePayments,
  MdOutlineReceiptLong,
  MdPets,
  MdRefresh,
} from 'react-icons/md'
import {
  adminDashboardApi,
  type AdminDashboardData,
} from '../../features/admin/adminDashboardApi'
import { useAuth } from '../../features/auth/useAuth'
import { formatVnd } from '../../utils/format'

const metricIcons = [MdOutlineReceiptLong, MdOutlinePayments, MdInventory2, MdPets]

function translateStatus(status: string) {
  const normalized = status.toLowerCase()
  if (normalized.includes('pending') || normalized.includes('chờ')) return 'Đang chờ'
  if (normalized.includes('completed') || normalized.includes('hoàn')) return 'Hoàn tất'
  if (normalized.includes('cancel')) return 'Đã hủy'
  return 'Đang xử lý'
}

function statusClass(status: string) {
  const normalized = status.toLowerCase()
  if (normalized.includes('pending') || normalized.includes('chờ')) return 'pending'
  if (normalized.includes('completed') || normalized.includes('hoàn')) return 'completed'
  if (normalized.includes('cancel')) return 'cancelled'
  return 'processing'
}

export function AdminDashboardPage() {
  const auth = useAuth()
  const [data, setData] = useState<AdminDashboardData | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(false)

  const load = useCallback(() => {
    const token = auth.session?.token
    if (!token) return
    setLoading(true)
    setError(false)
    adminDashboardApi
      .load(token)
      .then((result) => {
        setData(result)
        setLoading(false)
      })
      .catch(() => {
        setData(null)
        setError(true)
        setLoading(false)
      })
  }, [auth.session?.token])

  useEffect(load, [load])

  const metrics = useMemo(() => {
    if (!data) return []
    const pending = data.orders.filter((order) => {
      const status = order.orderStatusName.toLowerCase()
      return status.includes('pending') || status.includes('chờ')
    }).length
    const revenue = data.orders
      .filter((order) => !order.orderStatusName.toLowerCase().includes('cancel'))
      .reduce((sum, order) => sum + order.totalPrice, 0)
    const lowStock = data.products.filter((product) => product.unitInStock <= 5)
    const activeCats = data.cats.filter((cat) => {
      const status = cat.statusName.toLowerCase()
      return status.includes('cafe') || status.includes('quán') || status.includes('available')
    }).length

    return [
      { label: 'Đơn đang chờ', value: String(pending), note: `${data.orders.length} đơn gần đây` },
      { label: 'Doanh thu ghi nhận', value: formatVnd(revenue), note: 'Không tính đơn đã hủy' },
      { label: 'Sắp hết hàng', value: String(lowStock.length), note: `${lowStock.length} sản phẩm` },
      { label: 'Mèo tại quán', value: `${activeCats} / ${data.cats.length}`, note: 'Theo trạng thái hiện tại' },
    ]
  }, [data])

  return (
    <section className="admin-page">
      <div className="admin-page__heading">
        <div>
          <span className="eyebrow">Tổng quan cửa hàng</span>
          <h1>Hôm nay tại Loaf'N Catting</h1>
          <p>Theo dõi vận hành quán từ dữ liệu mới nhất của hệ thống.</p>
        </div>
        <button className="button button--secondary" type="button" onClick={load} disabled={loading}>
          <MdRefresh />Làm mới dữ liệu
        </button>
      </div>

      <div className="admin-hero">
        <div>
          <span>Ca làm việc hiện tại</span>
          <h2>Mọi thứ trong tầm mắt.</h2>
          <p>Đơn hàng, tồn kho và lịch của các bé mèo được gom về một nơi để đội ngũ xử lý nhanh hơn.</p>
        </div>
        <div className="admin-hero__index"><span>LIVE</span><strong>{data?.orders.length ?? '—'}</strong><small>đơn được ghi nhận</small></div>
      </div>

      {error ? (
        <div className="admin-error" role="alert">
          <MdErrorOutline />
          <div><h2>Không thể tải dashboard</h2><p>Hệ thống backend chưa phản hồi. Hãy kiểm tra kết nối và thử lại.</p></div>
          <button type="button" onClick={load}><MdRefresh />Thử lại</button>
        </div>
      ) : (
        <>
          <div className="metric-grid">
            {(loading ? [1, 2, 3, 4] : metrics).map((metric, index) => {
              if (typeof metric === 'number') {
                return <div className="metric-card metric-card--skeleton" key={metric} />
              }
              const Icon = metricIcons[index]
              return (
                <article className="metric-card" key={metric.label}>
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
              <div><span className="eyebrow">Luồng trực tiếp</span><h2>Đơn hàng gần đây</h2><p>Dữ liệu lấy từ API đơn hàng của backend.</p></div>
            </div>
            <div className="table-scroll">
              <table>
                <thead><tr><th>Đơn</th><th>Khách hàng</th><th>Món</th><th>Tổng tiền</th><th>Trạng thái</th><th>Thời gian</th></tr></thead>
                <tbody>
                  {data?.orders.map((order) => (
                    <tr key={order.orderId}>
                      <td><strong>#{order.orderId}</strong></td>
                      <td>{order.customerName || 'Khách tại quán'}</td>
                      <td>{order.items.reduce((sum, item) => sum + item.quantity, 0)}</td>
                      <td>{formatVnd(order.totalPrice)}</td>
                      <td><span className={`order-status order-status--${statusClass(order.orderStatusName)}`}>{translateStatus(order.orderStatusName)}</span></td>
                      <td>{new Intl.DateTimeFormat('vi-VN', { hour: '2-digit', minute: '2-digit' }).format(new Date(order.orderDate))}</td>
                    </tr>
                  ))}
                  {!loading && data?.orders.length === 0 && (
                    <tr><td colSpan={6} className="admin-table-empty">Chưa có đơn hàng nào.</td></tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </>
      )}
    </section>
  )
}
