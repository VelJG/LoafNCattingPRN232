import { MdDashboard, MdEvent, MdLocalCafe, MdLogout, MdOutlineReceiptLong, MdPets, MdStorefront } from 'react-icons/md'
import { NavLink, Outlet } from 'react-router-dom'
import { BrandLogo } from '../components/BrandLogo'

const adminItems = [
  { icon: MdDashboard, label: 'Overview', active: true },
  { icon: MdOutlineReceiptLong, label: 'Orders' },
  { icon: MdEvent, label: 'Reservations' },
  { icon: MdLocalCafe, label: 'Catalog' },
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
            return <button className={item.active ? 'admin-nav-item admin-nav-item--active' : 'admin-nav-item'} key={item.label} type="button" disabled={!item.active}><Icon /><span>{item.label}</span></button>
          })}
        </nav>
        <div className="admin-sidebar__footer">
          <NavLink to="/menu"><MdStorefront />Customer portal</NavLink>
          <button type="button"><MdLogout />Sign out</button>
        </div>
      </aside>
      <div className="admin-content">
        <header className="admin-topbar">
          <div><span className="eyebrow">Friday, 10 July</span><strong>Good morning, Linh</strong></div>
          <div className="admin-profile"><span><strong>Linh Nguyen</strong><small>Store manager</small></span><div>LN</div></div>
        </header>
        <main id="admin-main"><Outlet /></main>
      </div>
    </div>
  )
}
