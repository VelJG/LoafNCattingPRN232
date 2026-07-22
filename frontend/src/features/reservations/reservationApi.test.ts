import { afterEach, describe, expect, it, vi } from 'vitest'
import { createReservation, getReservationAvailability } from './reservationApi'

const suggestedTable = {
  tableId: 4,
  tableName: 'Bàn 4',
  capacity: 4,
  area: 'Cửa sổ',
  description: 'Gần khu mèo',
}

describe('reservationApi', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('requests availability with the backend query contract', async () => {
    const response = {
      isAvailable: true,
      reason: null,
      durationMinutes: 120,
      startAt: '2026-07-24T18:00:00+07:00',
      endAt: '2026-07-24T20:00:00+07:00',
      suggestedTable,
    }
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify(response), { status: 200 }),
    )
    vi.stubGlobal('fetch', fetchMock)

    const result = await getReservationAvailability({
      date: '2026-07-24',
      time: '18:00',
      numberOfGuests: 4,
    })

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/reservations/availability?date=2026-07-24&time=18%3A00&numberOfGuests=4',
      expect.objectContaining({ method: 'GET' }),
    )
    expect(result.suggestedTable).toEqual(suggestedTable)
  })

  it('creates a reservation with the bearer token and exact payload', async () => {
    const request = {
      date: '2026-07-24',
      time: '18:00',
      numberOfGuests: 4,
      guestName: 'Minh Anh',
      guestPhoneNumber: '0900000001',
      note: 'Bàn gần cửa sổ',
    }
    const response = {
      reservationId: 18,
      customerUserId: 7,
      ...request,
      status: 'Pending',
      durationMinutes: 120,
      startAt: '2026-07-24T18:00:00+07:00',
      endAt: '2026-07-24T20:00:00+07:00',
      table: suggestedTable,
      createdAtUtc: '2026-07-22T14:00:00Z',
    }
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify(response), { status: 201 }),
    )
    vi.stubGlobal('fetch', fetchMock)

    const result = await createReservation(request, 'customer-token')

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/reservations',
      expect.objectContaining({
        method: 'POST',
        headers: expect.objectContaining({ Authorization: 'Bearer customer-token' }),
        body: JSON.stringify(request),
      }),
    )
    expect(result.reservationId).toBe(18)
    expect(result.table.tableId).toBe(4)
  })
})
