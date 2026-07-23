import { useState } from 'react'
import { MdMenu, MdShoppingBag } from 'react-icons/md'
import { NavLink, Outlet } from 'react-router-dom'
import { CartDrawer } from '../components/CartDrawer'
import { BrandWordmark } from '../components/brand/BrandWordmark'
import { useAuth } from '../features/auth/useAuth'
import { useCart } from '../state/CartContext'

const customerNavigation = [
  { to: '/menu', label: 'THỰC ĐƠN' },
  { to: '/reservations', label: 'ĐẶT BÀN' },
  { to: '/cats', label: 'MÈO' },
  { to: '/location', label: 'VỊ TRÍ' },
  { to: '/notifications', label: 'THÔNG BÁO' },
  { to: '/chat', label: 'TRÒ CHUYỆN' },
]

function initials(name = '') {
  const parts = name.trim().split(/\s+/).filter(Boolean)
  if (parts.length === 0) return 'LN'
  if (parts.length === 1) return parts[0].slice(0, 2).toLocaleUpperCase('vi-VN')
  return `${parts[0][0]}${parts.at(-1)?.[0] ?? ''}`.toLocaleUpperCase('vi-VN')
}

export function CustomerLayout() {
  const cart = useCart()
  const auth = useAuth()
  const [menuOpen, setMenuOpen] = useState(false)

  return (
    <div className="customer-v2 site-shell">
      <a className="skip-link" href="#main-content">Đi tới nội dung</a>
      <header className="site-header">
        <div className="site-header__inner">
          <BrandWordmark to="/menu" />
          <button
            className="mobile-menu-button customer-mobile-menu"
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
            <button
              className="header-cart-button"
              type="button"
              onClick={cart.open}
              aria-label={`Mở giỏ hàng, ${cart.count} món`}
              aria-expanded={cart.isOpen}
              aria-controls="shopping-cart"
            >
              <MdShoppingBag />
              {cart.count > 0 && <strong>{cart.count}</strong>}
            </button>
            <NavLink
              to="/profile"
              className="customer-avatar"
              title={`${auth.session?.user.name ?? ''} · ${auth.session?.user.email ?? ''}`}
              aria-label={`Tài khoản ${auth.session?.user.name ?? ''}`}
            >
              {initials(auth.session?.user.name)}
            </NavLink>
          </div>
        </div>
      </header>
      <main id="main-content"><Outlet /></main>
      <CartDrawer />
    </div>
  )
}
