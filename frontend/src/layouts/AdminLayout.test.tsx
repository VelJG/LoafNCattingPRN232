import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Outlet, Route, Routes } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import { AuthContext, type AuthContextValue } from '../features/auth/AuthProvider'
import type { Session, UserRole } from '../features/auth/authModels'
import { AdminOnlyRoute } from '../features/admin/AdminOnlyRoute'
import { AdminLayout } from './AdminLayout'

function sessionFor(role: UserRole): Session {
  return {
    token: `${role.toLowerCase()}-token`,
    expiresAtUtc: '2030-01-01T00:00:00Z',
    user: {
      userId: 3,
      name: 'Minh Anh',
      email: 'minh@loaf.vn',
      phoneNumber: '0900000003',
      address: null,
      role,
      isActive: true,
      isEmailVerified: true,
    },
  }
}

function renderLayout(path: string, role: UserRole, logout = vi.fn()) {
  const auth: AuthContextValue = {
    status: 'authenticated',
    session: sessionFor(role),
    login: vi.fn(),
    register: vi.fn(),
    logout,
  }
  return render(
    <AuthContext.Provider value={auth}>
      <MemoryRouter initialEntries={[path]}>
        <Routes>
          <Route path="/" element={<p>Landing destination</p>} />
          <Route path="/admin" element={<AdminLayout />}>
            <Route index element={<h1>Dashboard destination</h1>} />
            <Route path="orders" element={<h1>Orders destination</h1>} />
          </Route>
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>,
  )
}

describe('AdminLayout', () => {
  it('renders route links and omits Admin-only links for Staff', () => {
    renderLayout('/admin/orders', 'Staff')

    expect(screen.getByRole('link', { name: /đơn hàng/i })).toHaveAttribute('href', '/admin/orders')
    expect(screen.getByRole('link', { name: /đơn hàng/i })).toHaveAttribute('aria-current', 'page')
    expect(screen.queryByRole('link', { name: /người dùng/i })).not.toBeInTheDocument()
    expect(screen.queryByRole('link', { name: /vị trí cửa hàng/i })).not.toBeInTheDocument()
    expect(screen.queryByRole('link', { name: /trang khách hàng/i })).not.toBeInTheDocument()
  })

  it('renders all navigation entries for Admin', () => {
    renderLayout('/admin', 'Admin')

    expect(screen.getByRole('link', { name: /người dùng/i })).toHaveAttribute('href', '/admin/users')
    expect(screen.getByRole('link', { name: /vị trí cửa hàng/i })).toHaveAttribute('href', '/admin/store')
    expect(screen.getByText('Quản trị viên')).toBeInTheDocument()
  })

  it('logs out to the landing page', async () => {
    const logout = vi.fn().mockResolvedValue(undefined)
    renderLayout('/admin', 'Staff', logout)

    await userEvent.click(screen.getByRole('button', { name: /đăng xuất/i }))

    expect(logout).toHaveBeenCalledOnce()
    expect(await screen.findByText('Landing destination')).toBeInTheDocument()
  })
})

describe('AdminOnlyRoute', () => {
  it('redirects Staff away from an Admin-only route', () => {
    const auth: AuthContextValue = {
      status: 'authenticated',
      session: sessionFor('Staff'),
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
    }

    render(
      <AuthContext.Provider value={auth}>
        <MemoryRouter initialEntries={['/admin/users']}>
          <Routes>
            <Route path="/admin" element={<><p>Admin home</p><Outlet /></>}>
              <Route element={<AdminOnlyRoute />}>
                <Route path="users" element={<p>Users destination</p>} />
              </Route>
            </Route>
          </Routes>
        </MemoryRouter>
      </AuthContext.Provider>,
    )

    expect(screen.getByText('Admin home')).toBeInTheDocument()
    expect(screen.queryByText('Users destination')).not.toBeInTheDocument()
  })
})
