import { useEffect } from 'react'
import { Navigate, Route, Routes, useLocation } from 'react-router-dom'
import { AdminLayout } from './layouts/AdminLayout'
import { CustomerLayout } from './layouts/CustomerLayout'
import { AdminCatalogPage } from './pages/admin/AdminCatalogPage'
import { AdminDashboardPage } from './pages/admin/AdminDashboardPage'
import { CatsPage } from './pages/customer/CatsPage'
import { MenuPage } from './pages/customer/MenuPage'
import { ReservationPage } from './pages/customer/ReservationPage'

function App() {
  const { pathname } = useLocation()

  useEffect(() => {
    window.scrollTo(0, 0)
  }, [pathname])

  return (
    <Routes>
      <Route element={<CustomerLayout />}>
        <Route index element={<Navigate to="/menu" replace />} />
        <Route path="menu" element={<MenuPage />} />
        <Route path="cats" element={<CatsPage />} />
        <Route path="reservations" element={<ReservationPage />} />
      </Route>

      <Route path="admin" element={<AdminLayout />}>
        <Route index element={<AdminDashboardPage />} />`r`n        <Route path="catalog" element={<AdminCatalogPage />} />
      </Route>

      <Route path="*" element={<Navigate to="/menu" replace />} />
    </Routes>
  )
}

export default App

