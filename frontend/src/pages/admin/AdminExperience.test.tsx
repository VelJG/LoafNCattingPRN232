import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import * as adminApi from '../../features/admin/adminApi'
import { AuthContext, type AuthContextValue } from '../../features/auth/AuthProvider'
import type { AdminOrder, AdminProduct, StoreReservation } from '../../features/admin/adminTypes'
import { AdminDashboardPage } from './AdminDashboardPage'

const auth: AuthContextValue = {
  status: 'authenticated',
  session: {
    token: 'staff-token',
    expiresAtUtc: '2030-01-01T00:00:00Z',
    user: {
      userId: 2,
      name: 'Linh Nguyễn',
      email: 'linh@loaf.vn',
      phoneNumber: '0900000002',
      address: null,
      role: 'Staff',
      isActive: true,
      isEmailVerified: true,
    },
  },
  login: vi.fn(),
  register: vi.fn(),
  logout: vi.fn(),
}

const today = new Date().toISOString().slice(0, 10)

const orders: AdminOrder[] = Array.from({ length: 6 }, (_, index) => ({
  orderId: 1042 - index,
  customerUserId: index + 1,
  customerName: ['Nguyễn Minh Anh', 'Trần Bảo', 'Lê Thu Hà'][index % 3],
  orderDate: `${today}T0${9 - Math.min(index, 5)}:12:00Z`,
  totalPrice: 145000 + index * 10000,
  orderType: 'DineIn',
  note: null,
  orderStatusId: index < 2 ? 1 : 3,
  orderStatusName: index < 2 ? 'Chờ xử lý' : 'Hoàn thành',
  items: [],
  payments: [],
}))

const reservations: StoreReservation[] = Array.from({ length: 3 }, (_, index) => ({
  reservationId: index + 1,
  customerUserId: index + 1,
  customerName: `Khách ${index + 1}`,
  customerEmail: null,
  date: today,
  time: `${18 + index}:00:00`,
  numberOfGuests: index + 2,
  guestName: `Khách ${index + 1}`,
  guestPhoneNumber: '0900000000',
  note: null,
  status: index === 0 ? 'Đang chờ' : 'Đã xác nhận',
  durationMinutes: 120,
  startAt: `${today}T${18 + index}:00:00+07:00`,
  endAt: `${today}T${19 + index}:30:00+07:00`,
  table: { tableId: index + 1, tableName: `Bàn ${index + 2}`, capacity: 4, area: 'Tầng 1', description: null },
  tableStatus: 'Reserved',
  createdAtUtc: `${today}T01:00:00Z`,
  updatedAtUtc: null,
}))

const products: AdminProduct[] = Array.from({ length: 3 }, (_, index) => ({
  productId: index + 1,
  name: ['Bánh su kem mèo', 'Matcha đá xay', 'Panini gà nướng'][index],
  description: null,
  price: 65000,
  discountPrice: null,
  unitInStock: index * 3,
  picture: null,
  categoryId: index + 1,
  categoryName: ['Bánh ngọt', 'Đồ uống', 'Món ăn'][index],
  isAvailable: true,
  createdAt: `${today}T00:00:00Z`,
  updatedAt: null,
}))

function renderDashboard() {
  return render(
    <AuthContext.Provider value={auth}>
      <MemoryRouter><AdminDashboardPage /></MemoryRouter>
    </AuthContext.Provider>,
  )
}

afterEach(() => vi.restoreAllMocks())

describe('admin dashboard', () => {
  it('derives every operational summary from live API collections', async () => {
    vi.spyOn(adminApi, 'listOrders').mockResolvedValue(orders)
    vi.spyOn(adminApi, 'listStoreReservations').mockResolvedValue(reservations)
    vi.spyOn(adminApi, 'listAdminProducts').mockResolvedValue(products)

    renderDashboard()

    expect(await screen.findByText('#1042')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Đơn hàng gần đây' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Đặt bàn hôm nay' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Sắp hết hàng' })).toBeInTheDocument()
    expect(screen.getByText('2', { selector: '.admin-stat__value' })).toBeInTheDocument()
    expect(screen.getAllByText('3', { selector: '.admin-stat__value' })).toHaveLength(2)
    expect(screen.getByText('Bánh su kem mèo')).toBeInTheDocument()
  })

  it('keeps successful order data visible when stock fails', async () => {
    vi.spyOn(adminApi, 'listOrders').mockResolvedValue(orders.slice(0, 1))
    vi.spyOn(adminApi, 'listStoreReservations').mockResolvedValue([])
    vi.spyOn(adminApi, 'listAdminProducts').mockRejectedValue(new Error('offline'))

    renderDashboard()

    expect(await screen.findByText('#1042')).toBeInTheDocument()
    expect(screen.getByRole('alert')).toHaveTextContent('Không thể tải tồn kho')
  })
})
