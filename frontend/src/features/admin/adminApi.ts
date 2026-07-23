import { requestJson } from '../../api/httpClient'
import type {
  AdminOrder,
  AdminProduct,
  AdminProductInput,
  CreatedStaff,
  CreateStaffInput,
  OperatorRole,
  ReservationTransition,
  StoreReservation,
} from './adminTypes'

export const listOrders = (token: string, signal?: AbortSignal) =>
  requestJson<AdminOrder[]>('/orders', { token, signal })

export const updateOrderStatus = (
  token: string,
  role: OperatorRole,
  orderId: number,
  orderStatusId: number,
) => requestJson<AdminOrder>(`/orders/${orderId}/status`, {
  method: 'PATCH',
  token,
  headers: { 'X-Role': role },
  body: { orderStatusId },
})

export const listStoreReservations = (token: string, signal?: AbortSignal) =>
  requestJson<StoreReservation[]>('/store/reservations', { token, signal })

export const transitionReservation = (
  token: string,
  reservationId: number,
  transition: ReservationTransition,
) => requestJson<StoreReservation>(
  `/store/reservations/${reservationId}/${transition}`,
  { method: 'PATCH', token },
)

export const listAdminProducts = (token: string, signal?: AbortSignal) =>
  requestJson<AdminProduct[]>('/admin/products', { token, signal })

export const createAdminProduct = (token: string, input: AdminProductInput) =>
  requestJson<AdminProduct>('/admin/products', { method: 'POST', token, body: input })

export const updateAdminProduct = (
  token: string,
  id: number,
  input: AdminProductInput,
) => requestJson<AdminProduct>(`/admin/products/${id}`, {
  method: 'PUT',
  token,
  body: input,
})

export const deleteAdminProduct = (token: string, id: number) =>
  requestJson<void>(`/admin/products/${id}`, { method: 'DELETE', token })

export const createStaff = (token: string, input: CreateStaffInput) =>
  requestJson<CreatedStaff>('/admin/users/staff', { method: 'POST', token, body: input })
