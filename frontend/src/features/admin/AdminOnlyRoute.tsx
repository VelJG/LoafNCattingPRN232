import { Navigate, Outlet } from 'react-router-dom'
import { LoadingScreen } from '../../components/ui/LoadingScreen'
import { useAuth } from '../auth/useAuth'

export function AdminOnlyRoute() {
  const auth = useAuth()

  if (auth.status === 'initializing') return <LoadingScreen />
  if (auth.session?.user.role !== 'Admin') return <Navigate to="/admin" replace />
  return <Outlet />
}
