import { useEffect, useState } from 'react'
import { MdArrowForward, MdEventAvailable, MdInventory2, MdOutlineReceiptLong, MdPets, MdRefresh } from 'react-icons/md'
import { catalogRepository } from '../../services/catalogRepository'
import type { DashboardMetric, RecentOrder } from '../../types/models'
import { formatVnd } from '../../utils/format'

const metricIcons = { orders: MdOutlineReceiptLong, reservations: MdEventAvailable, stock: MdInventory2, cats: MdPets }

export function AdminDashboardPage() {
  const [metrics, setMetrics] = useState<DashboardMetric[]>([])
  const [orders, setOrders] = useState<RecentOrder[]>([])
  const [loading, setLoading] = useState(true)

  const load = () => {
    setLoading(true)
    catalogRepository.getDashboard().then((data) => { setMetrics(data.metrics); setOrders(data.orders); setLoading(false) })
  }
  useEffect(load, [])

  return (
    <section className="admin-page">
      <div className="admin-page__heading"><div><span className="eyebrow">Store overview</span><h1>Today at Loaf’NCatting</h1><p>Keep an eye on the cafe floor without losing the warm brand feeling.</p></div><button className="button button--secondary" type="button" onClick={load}><MdRefresh />Refresh data</button></div>
      <div className="admin-hero"><div><span>Morning shift</span><h2>The cafe is running smoothly.</h2><p>Two reservations arrive in the next 30 minutes. Window 03 is still occupied.</p></div><button type="button">Open shift brief <MdArrowForward /></button></div>
      <div className="metric-grid">
        {(loading ? [1, 2, 3, 4] : metrics).map((metric) => {
          if (typeof metric === 'number') return <div className="metric-card metric-card--skeleton" key={metric} />
          const Icon = metricIcons[metric.id as keyof typeof metricIcons]
          return <article className={`metric-card metric-card--${metric.tone}`} key={metric.id}><span className="metric-card__icon"><Icon /></span><strong>{metric.value}</strong><h3>{metric.label}</h3><p>{metric.note}</p></article>
        })}
      </div>
      <div className="admin-table-card">
        <div className="admin-table-card__heading"><div><h2>Recent orders</h2><p>Newest orders from the customer portal.</p></div><button type="button">View all orders <MdArrowForward /></button></div>
        <div className="table-scroll"><table><thead><tr><th>Order</th><th>Customer</th><th>Items</th><th>Total</th><th>Status</th><th>Time</th></tr></thead><tbody>{orders.map((order) => <tr key={order.id}><td><strong>{order.id}</strong></td><td>{order.customer}</td><td>{order.items}</td><td>{formatVnd(order.total)}</td><td><span className={`order-status order-status--${order.status.toLowerCase()}`}>{order.status}</span></td><td>{order.time}</td></tr>)}</tbody></table></div>
      </div>
    </section>
  )
}
