import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import * as adminApi from '../../features/admin/adminApi'
import type { AdminOrder } from '../../features/admin/adminTypes'
import { AuthContext, type AuthContextValue } from '../../features/auth/AuthProvider'
import { AdminOrdersPage } from './AdminOrdersPage'

const auth: AuthContextValue = {
  status: 'authenticated',
  session: {
    token: 'staff-token', expiresAtUtc: '2030-01-01T00:00:00Z',
    user: { userId: 2, name: 'Linh', email: 'linh@loaf.vn', phoneNumber: '0900', address: null, role: 'Staff', isActive: true, isEmailVerified: true },
  },
  login: vi.fn(), register: vi.fn(), logout: vi.fn(),
}

const pending: AdminOrder = {
  orderId: 1042, customerUserId: 1, customerName: 'Nguyễn Minh Anh', orderDate: '2026-07-22T09:12:00Z',
  totalPrice: 145000, orderType: 'DineIn', note: null, orderStatusId: 1, orderStatusName: 'Chờ xử lý',
  items: [], payments: [{ paymentId: 1, paymentAmount: 145000, methodId: 1, methodName: 'Tiền mặt', paymentStatus: 'Paid', transactionCode: null, paymentDate: '2026-07-22T09:12:00Z', paidAt: '2026-07-22T09:12:00Z' }],
}

function renderPage() {
  return render(<AuthContext.Provider value={auth}><MemoryRouter><AdminOrdersPage /></MemoryRouter></AuthContext.Provider>)
}

afterEach(() => vi.restoreAllMocks())

describe('AdminOrdersPage', () => {
  it('filters reference cards without requesting mock data', async () => {
    vi.spyOn(adminApi, 'listOrders').mockResolvedValue([pending])
    renderPage()

    expect(await screen.findByText('#1042')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Hoàn thành' }))
    expect(screen.queryByText('#1042')).not.toBeInTheDocument()
  })

  it('updates the next status through the Staff API contract', async () => {
    vi.spyOn(adminApi, 'listOrders').mockResolvedValue([pending])
    const update = vi.spyOn(adminApi, 'updateOrderStatus').mockResolvedValue({ ...pending, orderStatusId: 2, orderStatusName: 'Đang pha chế' })
    renderPage()

    await userEvent.click(await screen.findByRole('button', { name: /cập nhật trạng thái đơn #1042/i }))

    expect(update).toHaveBeenCalledWith('staff-token', 1042, 2)
    expect(await screen.findByRole('status')).toHaveTextContent('Đã cập nhật đơn #1042')
  })
})
