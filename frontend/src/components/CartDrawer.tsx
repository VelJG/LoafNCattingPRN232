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
        aria-label="Close cart"
        onClick={cart.close}
      />
      <aside id="shopping-cart" className={cart.isOpen ? 'cart-drawer cart-drawer--open' : 'cart-drawer'} aria-label="Shopping cart" aria-hidden={!cart.isOpen}>
        <div className="cart-drawer__header">
          <div>
            <span className="eyebrow">Your order</span>
            <h2>Cart · {cart.count} items</h2>
          </div>
          <button className="icon-button" type="button" onClick={cart.close} aria-label="Close cart">
            <MdClose />
          </button>
        </div>

        <div className="cart-drawer__body">
          {cart.items.length === 0 ? (
            <div className="empty-state">
              <span className="icon-badge icon-badge--large"><MdShoppingBag /></span>
              <h3>Your cart is taking a catnap</h3>
              <p>Add a drink or a treat from today’s menu.</p>
            </div>
          ) : (
            cart.items.map((line) => (
              <article className="cart-line" key={line.product.id}>
                <img src={line.product.imageUrl} alt="" />
                <div className="cart-line__content">
                  <strong>{line.product.name}</strong>
                  <span>{formatVnd(line.product.discountPrice ?? line.product.price)}</span>
                  <div className="quantity-control" aria-label={`Quantity for ${line.product.name}`}>
                    <button type="button" onClick={() => cart.decrease(line.product.id)} aria-label="Decrease quantity"><MdRemove /></button>
                    <span>{line.quantity}</span>
                    <button type="button" onClick={() => cart.add(line.product)} aria-label="Increase quantity"><MdAdd /></button>
                  </div>
                </div>
                <button className="icon-button icon-button--danger" type="button" onClick={() => cart.remove(line.product.id)} aria-label={`Remove ${line.product.name}`}><MdDeleteOutline /></button>
              </article>
            ))
          )}
        </div>

        {cart.items.length > 0 && (
          <div className="cart-drawer__footer">
            <div className="price-row"><span>Total</span><strong>{formatVnd(cart.total)}</strong></div>
            <button className="button button--primary button--full" type="button" disabled>Checkout is not available yet</button>
            <p>Mock cart for UI architecture · checkout comes next.</p>
          </div>
        )}
      </aside>
    </>
  )
}
