import { useState } from 'react'
import { MdClose, MdMenu } from 'react-icons/md'
import { Link, Outlet } from 'react-router-dom'
import { BrandWordmark } from '../components/brand/BrandWordmark'

const anchors = [
  { href: '/#menu', label: 'Thực đơn' },
  { href: '/#cats', label: 'Mèo' },
  { href: '/#about', label: 'Về quán' },
  { href: '/#reserve', label: 'Liên hệ' },
]

export function PublicLayout() {
  const [open, setOpen] = useState(false)

  return (
    <div className="public-shell">
      <a className="skip-link" href="#public-main">Bỏ qua điều hướng</a>
      <header className="public-header">
        <div className="public-header__inner">
          <BrandWordmark />
          <nav className={open ? 'public-nav public-nav--open' : 'public-nav'} aria-label="Điều hướng chính">
            {anchors.map((item) => (
              <a key={item.href} href={item.href} onClick={() => setOpen(false)}>
                {item.label}
              </a>
            ))}
          </nav>
          <div className="public-header__actions">
            <span>08:00 — 22:00</span>
            <Link to="/login">Đăng nhập</Link>
            <Link className="public-register" to="/register">Tạo tài khoản</Link>
          </div>
          <button
            className="public-menu-button"
            type="button"
            aria-label={open ? 'Đóng điều hướng' : 'Mở điều hướng'}
            aria-expanded={open}
            onClick={() => setOpen((value) => !value)}
          >
            {open ? <MdClose /> : <MdMenu />}
          </button>
        </div>
      </header>
      <main id="public-main"><Outlet /></main>
      <footer className="public-footer">
        <BrandWordmark />
        <p>Cà phê rang mộc · Bánh nướng · Những người bạn bốn chân.</p>
        <div><a href="#about">Quyền riêng tư</a><a href="#about">Điều khoản</a><span>© 2026</span></div>
      </footer>
    </div>
  )
}
