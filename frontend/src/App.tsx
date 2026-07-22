import { useEffect } from 'react'
import { Navigate, Route, Routes, useLocation } from 'react-router-dom'
import { LoadingScreen } from './components/ui/LoadingScreen'
import { ProtectedRoute } from './features/auth/ProtectedRoute'
import { PublicOnlyRoute } from './features/auth/PublicOnlyRoute'
import { homeForRole } from './features/auth/authRouting'
import { useAuth } from './features/auth/useAuth'
import { AdminLayout } from './layouts/AdminLayout'
import { AuthLayout } from './layouts/AuthLayout'
import { CustomerLayout } from './layouts/CustomerLayout'
import { PublicLayout } from './layouts/PublicLayout'
import { AdminDashboardPage } from './pages/admin/AdminDashboardPage'
import { AdminOrdersPage } from './pages/admin/AdminOrdersPage'
import { AdminReservationsPage } from './pages/admin/AdminReservationsPage'
import { LoginPage } from './pages/auth/LoginPage'
import { RegisterPage } from './pages/auth/RegisterPage'
import { CatsPage } from './pages/customer/CatsPage'
import { CatDetailPage } from './pages/customer/CatDetailPage'
import { ChatPage } from './pages/customer/ChatPage'
import { LocationPage } from './pages/customer/LocationPage'
import { MenuPage } from './pages/customer/MenuPage'
import { NotificationsPage } from './pages/customer/NotificationsPage'
import { ProfilePage } from './pages/customer/ProfilePage'
import { ReservationPage } from './pages/customer/ReservationPage'
import { LandingPage } from './pages/public/LandingPage'

function RoleAwareFallback() {
  const auth = useAuth()
  if (auth.status === 'initializing') return <LoadingScreen />
  return <Navigate to={auth.session ? homeForRole(auth.session.user.role) : '/'} replace />
}

function App() {
  const { pathname } = useLocation()

  useEffect(() => {
    if (typeof window.scrollTo === 'function') window.scrollTo(0, 0)
  }, [pathname])

  return (
    <Routes>
      <Route element={<PublicLayout />}>
        <Route index element={<LandingPage />} />
      </Route>

      <Route element={<PublicOnlyRoute />}>
        <Route element={<AuthLayout />}>
          <Route path="login" element={<LoginPage />} />
          <Route path="register" element={<RegisterPage />} />
        </Route>
      </Route>

      <Route element={<ProtectedRoute allowed={['Customer']} />}>
        <Route element={<CustomerLayout />}>
          <Route path="menu" element={<MenuPage />} />
          <Route path="cats" element={<CatsPage />} />
          <Route path="cats/:catId" element={<CatDetailPage />} />
          <Route path="reservations" element={<ReservationPage />} />
          <Route path="location" element={<LocationPage />} />
          <Route path="notifications" element={<NotificationsPage />} />
          <Route path="chat" element={<ChatPage />} />
          <Route path="profile" element={<ProfilePage />} />
        </Route>
      </Route>

      <Route element={<ProtectedRoute allowed={['Staff', 'Admin']} />}>
        <Route path="admin" element={<AdminLayout />}>
          <Route index element={<AdminDashboardPage />} />
          <Route path="orders" element={<AdminOrdersPage />} />
          <Route path="reservations" element={<AdminReservationsPage />} />
        </Route>
      </Route>

      <Route path="*" element={<RoleAwareFallback />} />
    </Routes>
  )
}

export default App
