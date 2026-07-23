import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { LoadingScreen } from '../../components/ui/LoadingScreen'
import type { UserRole } from './authModels'
import { canAccessRole, homeForRole } from './authRouting'
import { useAuth } from './useAuth'

export function ProtectedRoute({
  allowed,
}: {
  allowed: readonly UserRole[]
}) {
  const auth = useAuth()
  const location = useLocation()

  if (auth.status === 'initializing') return <LoadingScreen />
  if (!auth.session) {
    return (
      <Navigate
        to="/login"
        replace
        state={{ from: `${location.pathname}${location.search}` }}
      />
    )
  }
  if (!canAccessRole(auth.session.user.role, allowed)) {
    return <Navigate to={homeForRole(auth.session.user.role)} replace />
  }
  return <Outlet />
}
