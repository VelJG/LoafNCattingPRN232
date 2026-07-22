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
