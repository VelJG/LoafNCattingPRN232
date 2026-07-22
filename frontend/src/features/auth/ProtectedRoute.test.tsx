import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import { AuthContext, type AuthContextValue } from './AuthProvider'
import { ProtectedRoute } from './ProtectedRoute'
import { PublicOnlyRoute } from './PublicOnlyRoute'
import type { UserRole } from './authModels'

function authValue(
  role: UserRole | null,
  status: AuthContextValue['status'] = role
    ? 'authenticated'
    : 'unauthenticated',
): AuthContextValue {
  return {
    status,
    session: role
      ? {
          token: 'token',
          expiresAtUtc: '2030-01-01',
          user: {
            userId: 1,
            name: 'User',
            email: 'u@example.com',
            phoneNumber: '0900000001',
            address: null,
            role,
            isActive: true,
            isEmailVerified: false,
          },
        }
      : null,
    login: vi.fn(),
    register: vi.fn(),
    logout: vi.fn(),
  }
}

function renderProtected(
  value: AuthContextValue,
  entry: string,
  allowed: readonly UserRole[],
) {
  render(
    <AuthContext.Provider value={value}>
      <MemoryRouter initialEntries={[entry]}>
        <Routes>
          <Route element={<ProtectedRoute allowed={allowed} />}>
            <Route path="/admin" element={<p>admin home</p>} />
          </Route>
          <Route path="/menu" element={<p>customer home</p>} />
          <Route path="/login" element={<p>login page</p>} />
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>,
  )
}

describe('route guards', () => {
  it('shows a session loading state while initializing', () => {
    renderProtected(authValue(null, 'initializing'), '/admin', ['Staff', 'Admin'])
    expect(screen.getByRole('status')).toHaveTextContent('Đang xác thực phiên')
  })

  it('redirects unauthenticated protected access to login', () => {
    renderProtected(authValue(null), '/admin', ['Staff', 'Admin'])
    expect(screen.getByText('login page')).toBeInTheDocument()
  })

  it('redirects a Customer away from admin', () => {
    renderProtected(authValue('Customer'), '/admin', ['Staff', 'Admin'])
    expect(screen.getByText('customer home')).toBeInTheDocument()
  })

  it('allows Staff and Admin into admin', () => {
    renderProtected(authValue('Staff'), '/admin', ['Staff', 'Admin'])
    expect(screen.getByText('admin home')).toBeInTheDocument()
  })

  it('redirects authenticated users away from login', () => {
    render(
      <AuthContext.Provider value={authValue('Staff')}>
        <MemoryRouter initialEntries={['/login']}>
          <Routes>
            <Route element={<PublicOnlyRoute />}>
              <Route path="/login" element={<p>login form</p>} />
            </Route>
            <Route path="/admin" element={<p>admin home</p>} />
          </Routes>
        </MemoryRouter>
      </AuthContext.Provider>,
    )

    expect(screen.getByText('admin home')).toBeInTheDocument()
    expect(screen.queryByText('login form')).not.toBeInTheDocument()
  })
})
