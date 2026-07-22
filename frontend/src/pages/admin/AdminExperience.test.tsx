import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { adminDashboardApi } from '../../features/admin/adminDashboardApi'
import { AuthContext, type AuthContextValue } from '../../features/auth/AuthProvider'
import { AdminLayout } from '../../layouts/AdminLayout'
import { AdminDashboardPage } from './AdminDashboardPage'

const staffSession = {
  token: 'staff-token',
  expiresAtUtc: '2030-01-01T00:00:00Z',
  user: {
    userId: 2,
    name: 'Linh Nguyễn',
    email: 'linh@loaf.vn',
    phoneNumber: '0900000002',
    address: null,
    role: 'Staff' as const,
    isActive: true,
    isEmailVerified: true,
  },
}

function renderAdmin(logout: AuthContextValue['logout'] = vi.fn()) {
  const auth: AuthContextValue = {
    status: 'authenticated',
    session: staffSession,
    login: vi.fn(),
    register: vi.fn(),
    logout,
  }
  return render(
    <AuthContext.Provider value={auth}>
      <MemoryRouter initialEntries={['/admin']}>
        <Routes>
          <Route path="/" element={<p>Landing destination</p>} />
          <Route path="/admin" element={<AdminLayout />}>
            <Route index element={<AdminDashboardPage />} />
          </Route>
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>,
  )
}

afterEach(() => {
  vi.restoreAllMocks()
})

describe('admin experience', () => {
  it('loads live dashboard data for Staff and shows the authenticated profile', async () => {
    const load = vi.spyOn(adminDashboardApi, 'load').mockResolvedValue({
      orders: [
        {
          orderId: 1048,
          customerUserId: 7,
          customerName: 'Minh Anh',
          orderDate: '2026-07-22T10:42:00Z',
          totalPrice: 173000,
          orderType: 'DineIn',
          note: null,
          orderStatusId: 2,
          orderStatusName: 'Processing',
          items: [{ orderDetailId: 1, productId: 3, productName: 'Catpuccino', quantity: 3, unitPrice: 59000, subtotal: 177000 }],
        },
      ],
      products: [{ productId: 1, name: 'Cookie', unitInStock: 3, isAvailable: true }],
      cats: [{ catId: 1, name: 'Mochi', statusName: 'At the cafe' }],
    })

    renderAdmin()

    expect(await screen.findByText('Linh Nguyễn')).toBeInTheDocument()
    expect(screen.getByText('Nhân viên')).toBeInTheDocument()
    expect(await screen.findByText('#1048')).toBeInTheDocument()
    expect(screen.getByText('1 sản phẩm')).toBeInTheDocument()
    expect(load).toHaveBeenCalledWith('staff-token')
  })

  it('shows a recoverable error and signs out to the landing page', async () => {
    vi.spyOn(adminDashboardApi, 'load').mockRejectedValue(new Error('offline'))
    const logout = vi.fn().mockResolvedValue(undefined)
    renderAdmin(logout)

    expect(await screen.findByRole('alert')).toHaveTextContent('Không thể tải dashboard')
    await userEvent.click(screen.getByRole('button', { name: /đăng xuất/i }))

    expect(logout).toHaveBeenCalledOnce()
    expect(await screen.findByText('Landing destination')).toBeInTheDocument()
  })
})
