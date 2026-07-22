import { describe, expect, it } from 'vitest'
import {
  canAccessRole,
  homeForRole,
  normalizeRole,
  safeRedirectForRole,
} from './authRouting'

describe('auth routing', () => {
  it.each([
    ['customer', 'Customer'],
    ['STAFF', 'Staff'],
    [' Admin ', 'Admin'],
  ] as const)('normalizes %s', (value, expected) => {
    expect(normalizeRole(value)).toBe(expected)
  })

  it('rejects unknown roles', () => {
    expect(() => normalizeRole('Owner')).toThrow('Unsupported role')
  })

  it.each([
    ['Customer', '/menu'],
    ['Staff', '/admin'],
    ['Admin', '/admin'],
  ] as const)('routes %s to %s', (role, path) => {
    expect(homeForRole(role)).toBe(path)
  })

  it('prevents cross-role access', () => {
    expect(canAccessRole('Customer', ['Staff', 'Admin'])).toBe(false)
    expect(canAccessRole('Staff', ['Staff', 'Admin'])).toBe(true)
    expect(canAccessRole('Admin', ['Staff', 'Admin'])).toBe(true)
  })

  it('honors only redirects that belong to the signed-in role', () => {
    expect(safeRedirectForRole('Customer', '/reservations')).toBe('/reservations')
    expect(safeRedirectForRole('Customer', '/admin')).toBe('/menu')
    expect(safeRedirectForRole('Staff', '/menu')).toBe('/admin')
    expect(safeRedirectForRole('Admin', '/admin/products')).toBe('/admin/products')
    expect(safeRedirectForRole('Admin', 'https://malicious.example')).toBe('/admin')
  })
})
