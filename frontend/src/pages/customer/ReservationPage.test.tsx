import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AuthContext, type AuthContextValue } from '../../features/auth/AuthProvider'
import * as reservationApi from '../../features/reservations/reservationApi'
import { ReservationPage } from './ReservationPage'

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

function renderPage() {
  return render(
    <AuthContext.Provider value={auth}>
      <MemoryRouter><ReservationPage /></MemoryRouter>
    </AuthContext.Provider>,
  )
}

const availability = {
  isAvailable: true,
  reason: null,
  durationMinutes: 120,
  startAt: '2026-07-24T18:00:00+07:00',
  endAt: '2026-07-24T20:00:00+07:00',
  suggestedTable: {
    tableId: 4,
    tableName: 'Bàn Cửa Sổ 04',
    capacity: 4,
    area: 'Cửa sổ',
    description: null,
  },
}

afterEach(() => vi.restoreAllMocks())

describe('reservation page', () => {
  it('checks availability and creates a reservation with the authenticated guest', async () => {
    const getAvailability = vi.spyOn(reservationApi, 'getReservationAvailability')
      .mockResolvedValue(availability)
    const createReservation = vi.spyOn(reservationApi, 'createReservation')
      .mockResolvedValue({
        reservationId: 18,
        customerUserId: 7,
        date: '2026-07-24',
        time: '18:00',
        numberOfGuests: 4,
        guestName: 'Minh Anh',
        guestPhoneNumber: '0900000001',
        note: null,
        status: 'Pending',
        durationMinutes: 120,
        startAt: availability.startAt,
        endAt: availability.endAt,
        table: availability.suggestedTable,
        createdAtUtc: '2026-07-22T14:00:00Z',
      })
    renderPage()

    await userEvent.clear(screen.getByLabelText('Ngày đặt bàn'))
    await userEvent.type(screen.getByLabelText('Ngày đặt bàn'), '2026-07-24')
    await userEvent.clear(screen.getByLabelText('Giờ đặt bàn'))
    await userEvent.type(screen.getByLabelText('Giờ đặt bàn'), '18:00')
    await userEvent.click(screen.getByRole('button', { name: /bàn 4.*4 khách/i }))
    await userEvent.click(screen.getByRole('button', { name: /xác nhận đặt bàn/i }))

    expect(getAvailability).toHaveBeenCalledWith({
      date: '2026-07-24', time: '18:00', numberOfGuests: 4,
    })
    expect(createReservation).toHaveBeenCalledWith({
      date: '2026-07-24',
      time: '18:00',
      numberOfGuests: 4,
      guestName: 'Minh Anh',
      guestPhoneNumber: '0900000001',
      note: null,
    }, 'customer-token')
    expect(await screen.findByText('Đã giữ Bàn Cửa Sổ 04 cho bạn.')).toBeInTheDocument()
  })

  it('shows the backend availability reason without clearing the form', async () => {
    vi.spyOn(reservationApi, 'getReservationAvailability').mockResolvedValue({
      ...availability,
      isAvailable: false,
      reason: 'Khung giờ này đã kín chỗ.',
      suggestedTable: null,
    })
    const createReservation = vi.spyOn(reservationApi, 'createReservation')
    renderPage()

    await userEvent.clear(screen.getByLabelText('Ngày đặt bàn'))
    await userEvent.type(screen.getByLabelText('Ngày đặt bàn'), '2026-07-24')
    await userEvent.click(screen.getByRole('button', { name: /xác nhận đặt bàn/i }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Khung giờ này đã kín chỗ.')
    expect(screen.getByLabelText('Ngày đặt bàn')).toHaveValue('2026-07-24')
    expect(createReservation).not.toHaveBeenCalled()
  })
})
