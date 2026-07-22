import { MdDashboard, MdEvent, MdLocalCafe, MdLogout, MdOutlineReceiptLong, MdPets, MdStorefront } from 'react-icons/md'
import { NavLink, Outlet } from 'react-router-dom'
import { BrandLogo } from '../components/BrandLogo'

const adminItems = [
  { icon: MdDashboard, label: 'Overview', to: '/admin' },
  { icon: MdLocalCafe, label: 'Catalog', to: '/admin/catalog' },
]

const disabledItems = [
  { icon: MdOutlineReceiptLong, label: 'Orders' },
  { icon: MdEvent, label: 'Reservations' },
  { icon: MdPets, label: 'Cats' },
]

export function AdminLayout() {
  return (
    <div className="admin-shell">
      <a className="skip-link" href="#admin-main">Skip to content</a>
      <aside className="admin-sidebar">
        <BrandLogo compact />
        <div className="admin-sidebar__label">Workspace</div>
        <nav aria-label="Admin navigation">
          {adminItems.map((item) => {
            const Icon = item.icon
            return <NavLink className={({ isActive }) => isActive ? 'admin-nav-item admin-nav-item--active' : 'admin-nav-item'} end={item.to === '/admin'} key={item.label} to={item.to}><Icon /><span>{item.label}</span></NavLink>
          })}
          {disabledItems.map((item) => {
            const Icon = item.icon
            return <button className="admin-nav-item" key={item.label} type="button" disabled><Icon /><span>{item.label}</span></button>
          })}
        </nav>
        <div className="admin-sidebar__footer">
          <NavLink to="/menu"><MdStorefront />Customer portal</NavLink>
          <button type="button"><MdLogout />Sign out</button>
        </div>
      </aside>
      <div className="admin-content">
        <header className="admin-topbar">
          <div><span className="eyebrow">Admin workspace</span><strong>LoafNCatting operations</strong></div>
          <div className="admin-profile"><span><strong>Admin</strong><small>API role header</small></span><div>AD</div></div>
        </header>
        <main id="admin-main"><Outlet /></main>
      </div>
    </div>
  )
}
