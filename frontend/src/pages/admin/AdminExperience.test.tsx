import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AuthContext, type AuthContextValue } from '../../features/auth/AuthProvider'
import { AdminLayout } from '../../layouts/AdminLayout'
import { catalogRepository } from '../../services/catalogRepository'
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
  it('loads the safe dashboard repository and shows all operational summaries', async () => {
    const load = vi.spyOn(catalogRepository, 'getDashboard').mockResolvedValue({
      metrics: [
        { id: 'orders', label: 'Đơn đang chờ', value: '1', note: 'Cần xử lý', tone: 'orange' },
        { id: 'reservations', label: 'Lịch đặt hôm nay', value: '2', note: '4 khách', tone: 'green' },
        { id: 'stock', label: 'Sắp hết hàng', value: '3', note: 'Kiểm tra kho', tone: 'rose' },
        { id: 'cats', label: 'Mèo trong ca', value: '9 / 12', note: '3 bé nghỉ', tone: 'blue' },
      ],
      orders: [{ id: '#LC-1048', customer: 'Minh Anh', items: 3, total: 173000, status: 'Processing', time: '10:42' }],
    })

    renderAdmin()

    expect(await screen.findByText('Linh Nguyễn')).toBeInTheDocument()
    expect(screen.getByText('Nhân viên')).toBeInTheDocument()
    expect(await screen.findByText('#LC-1048')).toBeInTheDocument()
    expect(screen.getByRole('heading', { level: 2, name: /lịch đặt hôm nay/i })).toBeInTheDocument()
    expect(screen.getByRole('heading', { level: 2, name: /cảnh báo tồn kho/i })).toBeInTheDocument()
    expect(load).toHaveBeenCalledOnce()
  })

  it('shows a recoverable error and signs out to the landing page', async () => {
    vi.spyOn(catalogRepository, 'getDashboard').mockRejectedValue(new Error('offline'))
    const logout = vi.fn().mockResolvedValue(undefined)
    renderAdmin(logout)

    expect(await screen.findByRole('alert')).toHaveTextContent('Không thể tải dashboard')
    await userEvent.click(screen.getByRole('button', { name: /đăng xuất/i }))

    expect(logout).toHaveBeenCalledOnce()
    expect(await screen.findByText('Landing destination')).toBeInTheDocument()
  })
})
