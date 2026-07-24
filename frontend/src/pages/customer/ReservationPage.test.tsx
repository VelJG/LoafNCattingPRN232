import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
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
  durationMinutes: 90,
  startAt: '2026-07-24T18:00:00+07:00',
  endAt: '2026-07-24T19:30:00+07:00',
  suggestedTable: {
    tableId: 4,
    tableName: 'Bàn Cửa Sổ 04',
    capacity: 4,
    area: 'Cửa sổ',
    description: null,
  },
}

const reservation: reservationApi.Reservation = {
  reservationId: 18,
  customerUserId: 7,
  date: '2026-07-24',
  time: '18:00',
  numberOfGuests: 4,
  guestName: 'Minh Anh',
  guestPhoneNumber: '0900000001',
  note: null,
  status: 'Pending',
  durationMinutes: 90,
  startAt: availability.startAt,
  endAt: availability.endAt,
  table: availability.suggestedTable,
  createdAtUtc: '2026-07-22T14:00:00Z',
}

beforeEach(() => {
  vi.spyOn(reservationApi, 'listMyReservations').mockResolvedValue([])
})

afterEach(() => vi.restoreAllMocks())

describe('reservation page', () => {
  it('shows 30-minute slots and creates a reservation with the authenticated guest', async () => {
    const getAvailability = vi.spyOn(reservationApi, 'getReservationAvailability')
      .mockResolvedValue(availability)
    const createReservation = vi.spyOn(reservationApi, 'createReservation')
      .mockResolvedValue(reservation)
    renderPage()

    expect(screen.getByRole('button', { name: '08:30' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: '20:30' })).toBeInTheDocument()
    await userEvent.clear(screen.getByLabelText('Ngày đặt bàn'))
    await userEvent.type(screen.getByLabelText('Ngày đặt bàn'), '2026-07-24')
    await userEvent.click(screen.getByRole('button', { name: '18:00' }))
    await userEvent.click(screen.getByRole('button', { name: /bàn 4.*4 khách/i }))
    await screen.findByText('Bàn Cửa Sổ 04')
    const submit = screen.getByRole('button', { name: /xác nhận đặt bàn/i })
    await waitFor(() => expect(submit).toBeEnabled())
    await userEvent.click(submit)

    expect(getAvailability).toHaveBeenLastCalledWith({
      date: '2026-07-24',
      time: '18:00',
      numberOfGuests: 4,
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

  it('shows an unavailable slot reason and prevents submission', async () => {
    vi.spyOn(reservationApi, 'getReservationAvailability').mockResolvedValue({
      ...availability,
      isAvailable: false,
      reason: 'Khung giờ này đã kín chỗ.',
      suggestedTable: null,
    })
    const createReservation = vi.spyOn(reservationApi, 'createReservation')
    renderPage()

    expect(await screen.findByText('Khung giờ này đã kín chỗ.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /xác nhận đặt bàn/i })).toBeDisabled()
    expect(createReservation).not.toHaveBeenCalled()
  })

  it('lists the customers table and confirms cancellation inline', async () => {
    const futureReservation: reservationApi.Reservation = {
      ...reservation,
      date: '2030-01-02',
      startAt: '2030-01-02T18:00:00+07:00',
      endAt: '2030-01-02T19:30:00+07:00',
      status: 'Đã xác nhận',
    }
    vi.mocked(reservationApi.listMyReservations)
      .mockResolvedValue([futureReservation])
    vi.spyOn(reservationApi, 'getReservationAvailability')
      .mockResolvedValue(availability)
    const cancel = vi.spyOn(reservationApi, 'cancelReservation')
      .mockResolvedValue({ ...futureReservation, status: 'Đã hủy' })
    renderPage()

    expect(await screen.findByRole('heading', { name: 'Bàn Cửa Sổ 04' }))
      .toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Hủy bàn' }))
    expect(screen.getByText('Hủy lịch này? Thao tác không thể hoàn tác.'))
      .toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Xác nhận hủy' }))

    expect(cancel).toHaveBeenCalledWith('customer-token', 18)
    expect(await screen.findByText('Đã hủy lịch đặt bàn #18.')).toBeInTheDocument()
    expect(screen.getByText('Đã hủy')).toBeInTheDocument()
  })
})
