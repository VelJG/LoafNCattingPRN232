import { useState } from 'react'
import { MdLogout, MdMenu, MdPerson, MdShoppingBag } from 'react-icons/md'
import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { CartDrawer } from '../components/CartDrawer'
import { BrandWordmark } from '../components/brand/BrandWordmark'
import { useAuth } from '../features/auth/useAuth'
import { useCart } from '../state/CartContext'

const customerNavigation = [
  { to: '/menu', label: 'Thực đơn' },
  { to: '/cats', label: 'Gặp các bé mèo' },
  { to: '/reservations', label: 'Đặt bàn' },
]

export function CustomerLayout() {
  const cart = useCart()
  const auth = useAuth()
  const navigate = useNavigate()
  const [menuOpen, setMenuOpen] = useState(false)
  const [loggingOut, setLoggingOut] = useState(false)

  const handleLogout = async () => {
    setLoggingOut(true)
    await auth.logout()
    navigate('/', { replace: true })
  }

  return (
    <div className="customer-v2 site-shell">
      <a className="skip-link" href="#main-content">Đi tới nội dung</a>
      <header className="site-header">
        <div className="site-header__inner">
          <BrandWordmark to="/menu" />
          <button
            className="mobile-menu-button"
            type="button"
            onClick={() => setMenuOpen((value) => !value)}
            aria-label="Mở điều hướng"
            aria-expanded={menuOpen}
          >
            <MdMenu />
          </button>
          <nav
            className={menuOpen ? 'customer-nav customer-nav--open' : 'customer-nav'}
            aria-label="Điều hướng khách hàng"
          >
            {customerNavigation.map((item) => (
              <NavLink key={item.to} to={item.to} onClick={() => setMenuOpen(false)}>
                {item.label}
              </NavLink>
            ))}
          </nav>
          <div className="header-actions">
            <div className="customer-account" title={auth.session?.user.email}>
              <MdPerson />
              <span>{auth.session?.user.name}</span>
            </div>
            <button
              className="header-cart-button"
              type="button"
              onClick={cart.open}
              aria-label={`Mở giỏ hàng, ${cart.count} món`}
              aria-expanded={cart.isOpen}
              aria-controls="shopping-cart"
            >
              <MdShoppingBag />
              <span>Giỏ hàng</span>
              {cart.count > 0 && <strong>{cart.count}</strong>}
            </button>
            <button
              className="customer-logout"
              type="button"
              onClick={handleLogout}
              disabled={loggingOut}
              aria-label="Đăng xuất"
            >
              <MdLogout />
            </button>
          </div>
        </div>
      </header>
      <main id="main-content"><Outlet /></main>
      <footer className="site-footer">
        <div>
          <BrandWordmark to="/menu" />
          <p>Cà phê ấm, mèo hiền và những ngày thật chậm.</p>
        </div>
        <span>© 2026 Loaf'N Catting · Đà Nẵng</span>
      </footer>
      <CartDrawer />
    </div>
  )
}
