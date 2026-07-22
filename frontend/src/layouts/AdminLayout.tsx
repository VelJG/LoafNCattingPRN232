import { useState } from 'react'
import {
  MdDashboard,
  MdEvent,
  MdLocalCafe,
  MdLogout,
  MdOutlineReceiptLong,
  MdPets,
  MdStorefront,
} from 'react-icons/md'
import { Link, Outlet, useNavigate } from 'react-router-dom'
import { BrandWordmark } from '../components/brand/BrandWordmark'
import { useAuth } from '../features/auth/useAuth'

const adminItems = [
  { icon: MdDashboard, label: 'Tổng quan', active: true },
  { icon: MdOutlineReceiptLong, label: 'Đơn hàng' },
  { icon: MdEvent, label: 'Đặt bàn' },
  { icon: MdLocalCafe, label: 'Thực đơn' },
  { icon: MdPets, label: 'Các bé mèo' },
]

function initials(name = '') {
  return name
    .trim()
    .split(/\s+/)
    .slice(-2)
    .map((part) => part[0]?.toUpperCase())
    .join('') || 'LC'
}

export function AdminLayout() {
  const auth = useAuth()
  const navigate = useNavigate()
  const [loggingOut, setLoggingOut] = useState(false)
  const user = auth.session?.user
  const roleLabel = user?.role === 'Admin' ? 'Quản trị viên' : 'Nhân viên'
  const today = new Intl.DateTimeFormat('vi-VN', {
    weekday: 'long',
    day: '2-digit',
    month: 'long',
  }).format(new Date())

  const handleLogout = async () => {
    setLoggingOut(true)
    await auth.logout()
    navigate('/', { replace: true })
  }

  return (
    <div className="admin-v2 admin-shell">
      <a className="skip-link" href="#admin-main">Đi tới nội dung</a>
      <aside className="admin-sidebar">
        <BrandWordmark to="/admin" inverse />
        <div className="admin-sidebar__label">Không gian quản trị</div>
        <nav aria-label="Điều hướng quản trị">
          {adminItems.map((item) => {
            const Icon = item.icon
            return (
              <button
                className={item.active ? 'admin-nav-item admin-nav-item--active' : 'admin-nav-item'}
                key={item.label}
                type="button"
                disabled={!item.active}
              >
                <Icon /><span>{item.label}</span>
                {!item.active && <small>Sắp có</small>}
              </button>
            )
          })}
        </nav>
        <div className="admin-sidebar__footer">
          <Link to="/"><MdStorefront />Trang giới thiệu</Link>
          <button type="button" onClick={handleLogout} disabled={loggingOut} aria-label="Đăng xuất">
            <MdLogout />Đăng xuất
          </button>
        </div>
      </aside>
      <div className="admin-content">
        <header className="admin-topbar">
          <div>
            <span className="eyebrow">{today}</span>
            <strong>Chào ngày mới, {user?.name?.split(' ').at(-1)}</strong>
          </div>
          <div className="admin-profile">
            <span><strong>{user?.name}</strong><small>{roleLabel}</small></span>
            <div>{initials(user?.name)}</div>
          </div>
        </header>
        <main id="admin-main"><Outlet /></main>
      </div>
    </div>
  )
}
