import { Navigate, Outlet } from 'react-router-dom'
import { LoadingScreen } from '../../components/ui/LoadingScreen'
import { homeForRole } from './authRouting'
import { useAuth } from './useAuth'

export function PublicOnlyRoute() {
  const auth = useAuth()
  if (auth.status === 'initializing') return <LoadingScreen />
  return auth.session ? (
    <Navigate to={homeForRole(auth.session.user.role)} replace />
  ) : (
    <Outlet />
  )
}
