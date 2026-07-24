import { useCallback, useEffect, useMemo, useState } from 'react'
import {
  MdAccessTime,
  MdDining,
  MdOutlineReceiptLong,
  MdPayment,
  MdRefresh,
} from 'react-icons/md'
import { Link } from 'react-router-dom'
import { ApiError } from '../../api/httpClient'
import { useAuth } from '../../features/auth/useAuth'
import {
  createPaymentLink,
  getPaymentStatus,
  listMyOrders,
  type CustomerOrder,
} from '../../features/orders/orderApi'
import { formatVnd } from '../../utils/format'

type OrderFilter = 'all' | 'active' | 'finished'

function normalize(value: string) {
  return value
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLocaleLowerCase('vi-VN')
}

function isFinished(order: CustomerOrder) {
  const value = normalize(order.orderStatusName)
  return ['completed', 'hoan thanh', 'cancelled', 'da huy'].some((status) =>
    value.includes(status),
  )
}

function isPendingPayment(order: CustomerOrder) {
  const value = normalize(order.payments[0]?.paymentStatus ?? '')
  return value.includes('pending') || value.includes('dang cho') ||
    value.includes('cho thanh toan')
}

function statusTone(value: string) {
  const normalized = normalize(value)
  if (normalized.includes('cancel') || normalized.includes('huy') ||
      normalized.includes('expired') || normalized.includes('het han')) {
    return 'danger'
  }
  if (normalized.includes('paid') || normalized.includes('complete') ||
      normalized.includes('hoan thanh') || normalized.includes('ready') ||
      normalized.includes('san sang')) {
    return 'success'
  }
  if (normalized.includes('process') || normalized.includes('chuan bi')) {
    return 'info'
  }
  return 'warning'
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat('vi-VN', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}

function errorMessage(error: unknown) {
  return error instanceof ApiError
    ? error.detail
    : 'Không thể tải đơn hàng. Vui lòng thử lại.'
}

export function CustomerOrdersPage() {
  const auth = useAuth()
  const token = auth.session?.token
  const [orders, setOrders] = useState<CustomerOrder[] | null>(null)
  const [filter, setFilter] = useState<OrderFilter>('all')
  const [error, setError] = useState('')
  const [actionError, setActionError] = useState('')
  const [payingOrderId, setPayingOrderId] = useState<number | null>(null)
  const [checkingOrderId, setCheckingOrderId] = useState<number | null>(null)

  const load = useCallback(async (signal?: AbortSignal) => {
    if (!token) return
    setError('')
    try {
      const nextOrders = await listMyOrders(token, signal)
      setOrders(nextOrders)

      const linkedPendingOrders = nextOrders.filter((order) =>
        isPendingPayment(order) &&
        Boolean(order.payments[0]?.transactionCode),
      )
      if (linkedPendingOrders.length === 0 || signal?.aborted) return

      const statuses = await Promise.allSettled(
        linkedPendingOrders.map((order) =>
          getPaymentStatus(token, order.orderId),
        ),
      )
      const changed = statuses.some((result) =>
        result.status === 'fulfilled' &&
        (result.value.isPaid ||
          normalize(result.value.paymentStatus).includes('cancel')),
      )
      if (changed && !signal?.aborted) {
        setOrders(await listMyOrders(token, signal))
      }
    } catch (caught) {
      if (!signal?.aborted) {
        setError(errorMessage(caught))
        setOrders([])
      }
    }
  }, [token])

  useEffect(() => {
    const controller = new AbortController()
    void load(controller.signal)
    return () => controller.abort()
  }, [load])

  const visibleOrders = useMemo(() => {
    const current = orders ?? []
    if (filter === 'active') return current.filter((order) => !isFinished(order))
    if (filter === 'finished') return current.filter(isFinished)
    return current
  }, [filter, orders])

  const startPayment = async (orderId: number) => {
    if (!token || payingOrderId !== null) return
    setPayingOrderId(orderId)
    setActionError('')
    try {
      const link = await createPaymentLink(token, orderId)
      window.location.assign(link.checkoutUrl)
    } catch (caught) {
      setActionError(errorMessage(caught))
      setPayingOrderId(null)
    }
  }

  const checkPayment = async (orderId: number) => {
    if (!token || checkingOrderId !== null) return
    setCheckingOrderId(orderId)
    setActionError('')
    try {
      await getPaymentStatus(token, orderId)
      await load()
    } catch (caught) {
      setActionError(errorMessage(caught))
    } finally {
      setCheckingOrderId(null)
    }
  }

  return (
    <section className="customer-v2-page customer-orders-page">
      <header className="customer-orders-hero">
        <div>
          <p>ĐƠN HÀNG CỦA BẠN</p>
          <h1>Mỗi món ngon đều có <em>một hành trình.</em></h1>
          <span>Theo dõi chế biến, bàn phục vụ và trạng thái thanh toán tại đây.</span>
        </div>
        <MdOutlineReceiptLong aria-hidden="true" />
      </header>

      <div className="customer-orders-toolbar" aria-label="Lọc đơn hàng">
        {([
          ['all', 'Tất cả'],
          ['active', 'Đang xử lý'],
          ['finished', 'Đã kết thúc'],
        ] as const).map(([value, label]) => (
          <button
            className={filter === value ? 'is-active' : ''}
            type="button"
            onClick={() => setFilter(value)}
            aria-pressed={filter === value}
            key={value}
          >
            {label}
          </button>
        ))}
      </div>

      {actionError && (
        <div className="customer-orders-message customer-orders-message--error" role="alert">
          {actionError}
        </div>
      )}

      {orders === null ? (
        <div className="customer-orders-feedback" role="status">
          <span className="customer-orders-skeleton" />
          <span className="customer-orders-skeleton" />
          <span className="customer-orders-skeleton" />
        </div>
      ) : error ? (
        <div className="customer-orders-feedback" role="alert">
          <h2>Chưa tải được đơn hàng</h2>
          <p>{error}</p>
          <button className="v2-button v2-button--secondary" type="button" onClick={() => void load()}>
            <MdRefresh aria-hidden="true" /> Thử lại
          </button>
        </div>
      ) : visibleOrders.length === 0 ? (
        <div className="customer-orders-feedback">
          <MdOutlineReceiptLong aria-hidden="true" />
          <h2>Chưa có đơn nào trong mục này</h2>
          <p>Chọn một món yêu thích, đơn mới sẽ xuất hiện ở đây ngay sau khi đặt.</p>
          <Link className="v2-button v2-button--primary" to="/menu">Xem thực đơn</Link>
        </div>
      ) : (
        <div className="customer-orders-list">
          {visibleOrders.map((order) => {
            const payment = order.payments[0]
            const pendingPayment = isPendingPayment(order)
            return (
              <article className="customer-order-card" key={order.orderId}>
                <header>
                  <div>
                    <p>ĐƠN #{order.orderId}</p>
                    <h2>{formatDateTime(order.orderDate)}</h2>
                  </div>
                  <span className={`status-chip status-chip--${statusTone(order.orderStatusName)}`}>
                    {order.orderStatusName}
                  </span>
                </header>

                <div className="customer-order-meta">
                  <span><MdDining aria-hidden="true" />{order.orderType === 'Takeaway' ? 'Mang đi' : 'Tại quán'}</span>
                  {order.tableName && <span>Bàn {order.tableName}</span>}
                  {order.reservationId && <span>Đặt bàn #{order.reservationId}</span>}
                  <span><MdAccessTime aria-hidden="true" />{order.items.length} món</span>
                </div>

                <div className="customer-order-items">
                  {order.items.map((item) => (
                    <div key={item.orderDetailId}>
                      <span>{item.quantity} × {item.productName}</span>
                      <strong>{formatVnd(item.subtotal)}</strong>
                    </div>
                  ))}
                </div>

                <footer>
                  <div className="customer-order-payment">
                    <MdPayment aria-hidden="true" />
                    <span>
                      <small>{payment?.methodName ?? 'Thanh toán'}</small>
                      <strong>{payment?.paymentStatus ?? 'Chưa có trạng thái'}</strong>
                    </span>
                  </div>
                  <div className="customer-order-total">
                    <small>TỔNG CỘNG</small>
                    <strong>{formatVnd(order.totalPrice)}</strong>
                  </div>
                </footer>

                {pendingPayment && (
                  <div className="customer-order-actions">
                    <button
                      className="v2-button v2-button--primary"
                      type="button"
                      disabled={payingOrderId !== null}
                      onClick={() => void startPayment(order.orderId)}
                    >
                      {payingOrderId === order.orderId ? 'Đang tạo link…' : 'Thanh toán PayOS'}
                    </button>
                    {payment?.transactionCode && (
                      <button
                        className="v2-button v2-button--secondary"
                        type="button"
                        disabled={checkingOrderId !== null}
                        onClick={() => void checkPayment(order.orderId)}
                      >
                        <MdRefresh aria-hidden="true" />
                        {checkingOrderId === order.orderId ? 'Đang kiểm tra…' : 'Kiểm tra thanh toán'}
                      </button>
                    )}
                  </div>
                )}
              </article>
            )
          })}
        </div>
      )}
    </section>
  )
}
