import { useState } from 'react'
import { MdLogout, MdNotificationsNone, MdSearch } from 'react-icons/md'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { BrandWordmark } from '../components/brand/BrandWordmark'
import { navigationForPath, navigationForRole } from '../features/admin/adminNavigation'
import { useAuth } from '../features/auth/useAuth'

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
  const { pathname } = useLocation()
  const [loggingOut, setLoggingOut] = useState(false)
  const user = auth.session?.user
  const currentPage = navigationForPath(pathname)
  const items = navigationForRole(user?.role)
  const roleLabel = user?.role === 'Admin' ? 'Quản trị viên' : 'Nhân viên'

  const handleLogout = async () => {
    setLoggingOut(true)
    await auth.logout()
    navigate('/', { replace: true })
  }

  return (
    <div className="admin-v2 admin-shell">
      <a className="skip-link" href="#admin-main">Đi tới nội dung</a>
      <aside className="admin-sidebar">
        <div className="admin-sidebar__brand">
          <BrandWordmark to="/admin" inverse />
          <div className="admin-sidebar__tagline">BẢNG ĐIỀU HÀNH</div>
        </div>
        <div className="admin-sidebar__label">VẬN HÀNH</div>
        <nav aria-label="Điều hướng quản trị">
          {items.map((item) => {
            const Icon = item.icon
            return (
              <NavLink
                className={({ isActive }) => isActive
                  ? 'admin-nav-item admin-nav-item--active'
                  : 'admin-nav-item'}
                end={item.to === '/admin'}
                key={item.key}
                to={item.to}
              >
                <Icon aria-hidden="true" />
                <span>{item.label}</span>
                {item.badge && <small>{item.badge}</small>}
              </NavLink>
            )
          })}
        </nav>
        <div className="admin-sidebar__footer">
          <button type="button" onClick={handleLogout} disabled={loggingOut} aria-label="Đăng xuất">
            <MdLogout aria-hidden="true" />
            <span>{loggingOut ? 'Đang đăng xuất...' : 'Đăng xuất'}</span>
          </button>
        </div>
      </aside>

      <div className="admin-content">
        <header className="admin-topbar">
          <div className="admin-topbar__title">
            <strong>{currentPage.label}</strong>
            <span>{currentPage.subtitle}</span>
          </div>
          <div className="admin-topbar__spacer" />
          <label className="admin-quick-search">
            <MdSearch aria-hidden="true" />
            <span className="sr-only">Tìm nhanh</span>
            <input type="search" placeholder="Tìm nhanh..." />
          </label>
          <button className="admin-notification-button" type="button" aria-label="Thông báo">
            <MdNotificationsNone aria-hidden="true" />
            <span />
          </button>
          <div className="admin-profile">
            <div className="admin-profile__avatar">{initials(user?.name)}</div>
            <span><strong>{user?.name}</strong><small>{roleLabel}</small></span>
          </div>
        </header>
        <main id="admin-main"><Outlet /></main>
      </div>
    </div>
  )
}
