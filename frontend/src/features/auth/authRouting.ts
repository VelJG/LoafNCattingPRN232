import type { UserRole } from './authModels'

export function normalizeRole(value: string): UserRole {
  const role = value.trim().toLowerCase()
  if (role === 'customer') return 'Customer'
  if (role === 'staff') return 'Staff'
  if (role === 'admin') return 'Admin'
  throw new Error(`Unsupported role: ${value}`)
}

export const homeForRole = (role: UserRole) =>
  role === 'Customer' ? '/menu' : '/admin'

export const canAccessRole = (
  role: UserRole,
  allowed: readonly UserRole[],
) => allowed.includes(role)

const matchesRoute = (path: string, route: string) =>
  path === route || path.startsWith(`${route}/`) || path.startsWith(`${route}?`)

export function safeRedirectForRole(
  role: UserRole,
  requested?: string,
): string {
  if (!requested || !requested.startsWith('/') || requested.startsWith('//')) {
    return homeForRole(role)
  }

  if (role === 'Customer') {
    return ['/menu', '/cats', '/reservations'].some((route) =>
      matchesRoute(requested, route),
    )
      ? requested
      : '/menu'
  }

  return matchesRoute(requested, '/admin') ? requested : '/admin'
}
