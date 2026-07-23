import { MdAdd, MdClose, MdDeleteOutline, MdRemove, MdShoppingBag } from 'react-icons/md'
import { useCart } from '../state/CartContext'
import { formatVnd } from '../utils/format'

export function CartDrawer() {
  const cart = useCart()

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

        <div className="cart-drawer__body">
          {cart.items.length === 0 ? (
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
                    <button type="button" onClick={() => cart.decrease(line.product.id)} aria-label="Giảm số lượng"><MdRemove /></button>
                    <span>{line.quantity}</span>
                    <button type="button" onClick={() => cart.add(line.product)} aria-label="Tăng số lượng"><MdAdd /></button>
                  </div>
                </div>
                <button
                  className="icon-button icon-button--danger"
                  type="button"
                  onClick={() => cart.remove(line.product.id)}
                  aria-label={`Xóa ${line.product.name}`}
                >
                  <MdDeleteOutline />
                </button>
              </article>
            ))
          )}
        </div>

        {cart.items.length > 0 && (
          <div className="cart-drawer__footer">
            <div className="price-row"><span>Tạm tính</span><strong>{formatVnd(cart.total)}</strong></div>
            <button className="button button--primary button--full" type="button" disabled>Thanh toán sắp ra mắt</button>
            <p>Giỏ hàng đã sẵn sàng để nối API đặt món ở bước tiếp theo.</p>
          </div>
        )}
      </aside>
    </>
  )
}
