import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import * as adminApi from '../../features/admin/adminApi'
import type { StoreReservation } from '../../features/admin/adminTypes'
import { AuthContext, type AuthContextValue } from '../../features/auth/AuthProvider'
import { AdminReservationsPage } from './AdminReservationsPage'

const auth: AuthContextValue = {
  status: 'authenticated',
  session: {
    token: 'staff-token', expiresAtUtc: '2030-01-01T00:00:00Z',
    user: { userId: 2, name: 'Linh', email: 'linh@loaf.vn', phoneNumber: '0900', address: null, role: 'Staff', isActive: true, isEmailVerified: true },
  },
  login: vi.fn(), register: vi.fn(), logout: vi.fn(),
}

const reservation: StoreReservation = {
  reservationId: 9, customerUserId: 1, customerName: 'Minh Anh', customerEmail: 'minh@loaf.vn',
  date: '2026-07-22', time: '18:00:00', numberOfGuests: 4, guestName: 'Minh Anh', guestPhoneNumber: '0900000001',
  note: null, status: 'Đang chờ', durationMinutes: 90, startAt: '2026-07-22T18:00:00+07:00', endAt: '2026-07-22T19:30:00+07:00',
  table: { tableId: 4, tableName: 'Bàn 4', capacity: 4, area: 'Tầng 1', description: null }, tableStatus: 'Available',
  createdAtUtc: '2026-07-22T02:00:00Z', updatedAtUtc: null,
}

function renderPage() {
  return render(<AuthContext.Provider value={auth}><MemoryRouter><AdminReservationsPage /></MemoryRouter></AuthContext.Provider>)
}

afterEach(() => vi.restoreAllMocks())

describe('AdminReservationsPage', () => {
  it('confirms a pending reservation through the store API', async () => {
    vi.spyOn(adminApi, 'listStoreReservations').mockResolvedValue([reservation])
    const transition = vi.spyOn(adminApi, 'transitionReservation').mockResolvedValue({ ...reservation, status: 'Đã xác nhận' })
    renderPage()

    expect(await screen.findByText(/bàn 4/i)).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: /cập nhật trạng thái đặt bàn #9/i }))

    expect(transition).toHaveBeenCalledWith('staff-token', 9, 'confirm')
    expect(await screen.findByText('Đã xác nhận', { selector: '.admin-status' })).toBeInTheDocument()
  })

  it('filters reservations by status', async () => {
    vi.spyOn(adminApi, 'listStoreReservations').mockResolvedValue([reservation])
    renderPage()

    expect(await screen.findByText('Minh Anh')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Hoàn thành' }))
    expect(screen.queryByText('Minh Anh')).not.toBeInTheDocument()
  })
})
