import { requestJson } from '../../api/httpClient'

export interface ReservationAvailabilityInput {
  date: string
  time: string
  numberOfGuests: number
}

export interface SuggestedTable {
  tableId: number
  tableName: string
  capacity: number
  area: string | null
  description: string | null
}

export interface ReservationAvailability {
  isAvailable: boolean
  reason: string | null
  durationMinutes: number
  startAt: string
  endAt: string
  suggestedTable: SuggestedTable | null
}

export interface CreateReservationInput extends ReservationAvailabilityInput {
  guestName: string
  guestPhoneNumber: string
  note: string | null
}

export interface Reservation extends CreateReservationInput {
  reservationId: number
  customerUserId: number
  status: string
  durationMinutes: number
  startAt: string
  endAt: string
  table: SuggestedTable
  createdAtUtc: string
}

export function getReservationAvailability(input: ReservationAvailabilityInput) {
  const search = new URLSearchParams({
    date: input.date,
    time: input.time,
    numberOfGuests: String(input.numberOfGuests),
  })
  return requestJson<ReservationAvailability>(`/reservations/availability?${search.toString()}`)
}

export function createReservation(input: CreateReservationInput, token: string) {
  return requestJson<Reservation>('/reservations', {
    method: 'POST',
    body: input,
    token,
  })
}

export function listMyReservations(token: string, signal?: AbortSignal) {
  return requestJson<Reservation[]>('/reservations/mine', { token, signal })
}

export function cancelReservation(token: string, reservationId: number) {
  return requestJson<Reservation>(`/reservations/${reservationId}/cancel`, {
    method: 'PATCH',
    token,
  })
}
