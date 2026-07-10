import { useState } from 'react'
import { MdDashboard, MdMenu, MdPersonOutline, MdShoppingBag } from 'react-icons/md'
import { NavLink, Outlet } from 'react-router-dom'
import { BrandLogo } from '../components/BrandLogo'
import { CartDrawer } from '../components/CartDrawer'
import { useCart } from '../state/CartContext'

const customerNavigation = [
  { to: '/menu', label: 'Menu' },
  { to: '/cats', label: 'Meet the cats' },
  { to: '/reservations', label: 'Book a table' },
]

export function CustomerLayout() {
  const cart = useCart()
  const [menuOpen, setMenuOpen] = useState(false)

  return (
    <div className="site-shell">
      <header className="site-header">
        <div className="site-header__inner">
          <BrandLogo compact />
          <button className="mobile-menu-button" type="button" onClick={() => setMenuOpen((value) => !value)} aria-label="Toggle navigation"><MdMenu /></button>
          <nav className={menuOpen ? 'customer-nav customer-nav--open' : 'customer-nav'} aria-label="Customer navigation">
            {customerNavigation.map((item) => (
              <NavLink key={item.to} to={item.to} onClick={() => setMenuOpen(false)}>{item.label}</NavLink>
            ))}
          </nav>
          <div className="header-actions">
            <NavLink className="admin-preview-link" to="/admin"><MdDashboard /><span>Staff preview</span></NavLink>
            <button className="header-cart-button" type="button" onClick={cart.open}>
              <MdShoppingBag />
              <span>Cart</span>
              {cart.count > 0 && <strong>{cart.count}</strong>}
            </button>
            <button className="avatar-button" type="button" aria-label="Open account menu"><MdPersonOutline /></button>
          </div>
        </div>
      </header>
      <main><Outlet /></main>
      <footer className="site-footer">
        <div><BrandLogo compact /><p>Warm coffee, gentle cats, calmer days.</p></div>
        <span>Orange Meow UI · Frontend architecture preview</span>
      </footer>
      <CartDrawer />
    </div>
  )
}
