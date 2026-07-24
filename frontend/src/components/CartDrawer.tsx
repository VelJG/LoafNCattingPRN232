import { useEffect, useState, type FormEvent } from 'react'
import {
  MdAdd,
  MdCheckCircle,
  MdClose,
  MdDeleteOutline,
  MdRemove,
  MdShoppingBag,
} from 'react-icons/md'
import { Link } from 'react-router-dom'
import { useCart } from '../state/CartContext'
import { formatVnd } from '../utils/format'

export function CartDrawer() {
  const cart = useCart()
  const [orderType, setOrderType] = useState('Takeaway')
  const [paymentMethodId, setPaymentMethodId] = useState(0)
  const [note, setNote] = useState('')

  useEffect(() => {
    const options = cart.checkoutOptions
    if (!options) return
    if (!options.orderTypes.includes(orderType) && options.orderTypes[0]) {
      setOrderType(options.orderTypes[0])
    }
    if (!options.paymentMethods.some((method) => method.paymentMethodId === paymentMethodId)) {
      setPaymentMethodId(options.paymentMethods[0]?.paymentMethodId ?? 0)
    }
  }, [cart.checkoutOptions, orderType, paymentMethodId])

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    if (!paymentMethodId) return
    await cart.checkout({
      orderType,
      tableId: null,
      reservationId: null,
      paymentMethodId,
      note: note.trim() || null,
    })
  }

  return (
    <>
      <button
        className={cart.isOpen ? 'drawer-scrim drawer-scrim--visible' : 'drawer-scrim'}
        type="button"
        aria-label="Đóng giỏ hàng"
        onClick={cart.close}
      />
      <aside
        id="shopping-cart"
        className={cart.isOpen ? 'cart-drawer cart-drawer--open' : 'cart-drawer'}
        aria-label="Giỏ hàng"
        aria-hidden={!cart.isOpen}
      >
        <div className="cart-drawer__header">
          <div>
            <span className="eyebrow">Đơn của bạn</span>
            <h2>Giỏ hàng · {cart.count} món</h2>
          </div>
          <button className="icon-button" type="button" onClick={cart.close} aria-label="Đóng giỏ hàng">
            <MdClose />
          </button>
        </div>

        {cart.error && (
          <div className="cart-api-message cart-api-message--error" role="alert">
            <span>{cart.error}</span>
            <button type="button" onClick={cart.dismissError} aria-label="Đóng thông báo lỗi">
              <MdClose />
            </button>
          </div>
        )}

        {cart.completedOrder && (
          <div className="cart-api-message cart-api-message--success" role="status">
            <MdCheckCircle />
            <span>
              Đã tạo đơn #{cart.completedOrder.orderId} · {formatVnd(cart.completedOrder.totalPrice)}
            </span>
            <Link className="cart-order-link" to="/orders" onClick={cart.close}>
              Xem đơn / thanh toán
            </Link>
            <button type="button" onClick={cart.dismissCompletedOrder} aria-label="Đóng thông báo đơn hàng">
              <MdClose />
            </button>
          </div>
        )}

        <div className="cart-drawer__body">
          {cart.isLoading ? (
            <div className="empty-state"><p>Đang tải giỏ hàng...</p></div>
          ) : cart.items.length === 0 ? (
            <div className="empty-state">
              <span className="icon-badge icon-badge--large"><MdShoppingBag /></span>
              <h3>Giỏ hàng đang ngủ trưa</h3>
              <p>Thêm một món nước hoặc bánh từ thực đơn hôm nay nhé.</p>
            </div>
          ) : (
            cart.items.map((line) => (
              <article className="cart-line" key={line.product.id}>
                <img src={line.product.imageUrl} alt="" />
                <div className="cart-line__content">
                  <strong>{line.product.name}</strong>
                  <span>{formatVnd(line.product.discountPrice ?? line.product.price)}</span>
                  <div className="quantity-control" aria-label={`Số lượng ${line.product.name}`}>
                    <button
                      type="button"
                      disabled={cart.isMutating}
                      onClick={() => void cart.decrease(line.product.id)}
                      aria-label="Giảm số lượng"
                    ><MdRemove /></button>
                    <span>{line.quantity}</span>
                    <button
                      type="button"
                      disabled={cart.isMutating || line.quantity >= line.product.stock}
                      onClick={() => void cart.add(line.product)}
                      aria-label="Tăng số lượng"
                    ><MdAdd /></button>
                  </div>
                </div>
                <button
                  className="icon-button icon-button--danger"
                  type="button"
                  disabled={cart.isMutating}
                  onClick={() => void cart.remove(line.product.id)}
                  aria-label={`Xóa ${line.product.name}`}
                >
                  <MdDeleteOutline />
                </button>
              </article>
            ))
          )}
        </div>

        {cart.items.length > 0 && (
          <form className="cart-drawer__footer cart-checkout-form" onSubmit={submit}>
            <div className="price-row"><span>Tạm tính</span><strong>{formatVnd(cart.total)}</strong></div>
            <label>
              Hình thức nhận món
              <select value={orderType} onChange={(event) => setOrderType(event.target.value)}>
                {(cart.checkoutOptions?.orderTypes ?? []).map((type) => (
                  <option value={type} key={type}>{type === 'Takeaway' ? 'Mang đi' : 'Tại quán'}</option>
                ))}
              </select>
            </label>
            <label>
              Thanh toán
              <select
                value={paymentMethodId}
                onChange={(event) => setPaymentMethodId(Number(event.target.value))}
              >
                {(cart.checkoutOptions?.paymentMethods ?? []).map((method) => (
                  <option value={method.paymentMethodId} key={method.paymentMethodId}>{method.name}</option>
                ))}
              </select>
            </label>
            <label>
              Ghi chú
              <textarea
                value={note}
                maxLength={500}
                onChange={(event) => setNote(event.target.value)}
                placeholder="Ít đá, không đường..."
              />
            </label>
            <button
              className="button button--primary button--full"
              type="submit"
              disabled={cart.isMutating || !paymentMethodId}
            >
              {cart.isMutating ? 'ĐANG XỬ LÝ...' : 'ĐẶT MÓN'}
            </button>
            <button className="button button--ghost button--full" type="button" disabled={cart.isMutating} onClick={() => void cart.clear()}>
              XÓA GIỎ HÀNG
            </button>
          </form>
        )}
      </aside>
    </>
  )
}
