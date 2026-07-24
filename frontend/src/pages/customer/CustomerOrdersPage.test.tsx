import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AuthContext, type AuthContextValue } from '../../features/auth/AuthProvider'
import * as orderApi from '../../features/orders/orderApi'
import { CustomerOrdersPage } from './CustomerOrdersPage'

const auth: AuthContextValue = {
  status: 'authenticated',
  session: {
    token: 'customer-token',
    expiresAtUtc: '2030-01-01T00:00:00Z',
    user: {
      userId: 7,
      name: 'Minh Anh',
      email: 'minh@example.com',
      phoneNumber: '0900000001',
      address: null,
      role: 'Customer',
      isActive: true,
      isEmailVerified: true,
    },
  },
  login: vi.fn(),
  register: vi.fn(),
  logout: vi.fn(),
}

const order: orderApi.CustomerOrder = {
  orderId: 42,
  customerUserId: 7,
  customerName: 'Minh Anh',
  orderDate: '2026-07-24T11:00:00Z',
  totalPrice: 59000,
  orderType: 'DineIn',
  note: null,
  orderStatusId: 1,
  orderStatusName: 'Pending',
  items: [{
    orderDetailId: 1,
    productId: 4,
    productName: 'Catpuccino',
    quantity: 1,
    unitPrice: 59000,
    subtotal: 59000,
  }],
  payments: [{
    paymentId: 8,
    paymentAmount: 59000,
    methodId: 2,
    methodName: 'Bank transfer',
    paymentStatus: 'Pending',
    transactionCode: null,
    paymentDate: '2026-07-24T11:00:00Z',
    paidAt: null,
  }],
  tableId: 4,
  tableName: 'Cửa Sổ 04',
  reservationId: 18,
}

function renderPage() {
  return render(
    <AuthContext.Provider value={auth}>
      <MemoryRouter><CustomerOrdersPage /></MemoryRouter>
    </AuthContext.Provider>,
  )
}

afterEach(() => vi.restoreAllMocks())

describe('customer orders page', () => {
  it('shows order items, table, reservation, and pending payment action', async () => {
    vi.spyOn(orderApi, 'listMyOrders').mockResolvedValue([order])
    renderPage()

    expect(await screen.findByRole('heading', { name: /24.*2026/i }))
      .toBeInTheDocument()
    expect(screen.getByText('1 × Catpuccino')).toBeInTheDocument()
    expect(screen.getByText('Bàn Cửa Sổ 04')).toBeInTheDocument()
    expect(screen.getByText('Đặt bàn #18')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Thanh toán PayOS' }))
      .toBeInTheDocument()
  })

  it('offers a retry after the orders request fails', async () => {
    const list = vi.spyOn(orderApi, 'listMyOrders')
      .mockRejectedValueOnce(new Error('offline'))
      .mockResolvedValueOnce([])
    renderPage()

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Không thể tải đơn hàng',
    )
    screen.getByRole('button', { name: /thử lại/i }).click()

    expect(await screen.findByText('Chưa có đơn nào trong mục này'))
      .toBeInTheDocument()
    expect(list).toHaveBeenCalledTimes(2)
  })
})
