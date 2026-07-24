import { requestJson } from '../../api/httpClient'
import type {
  AdminCat,
  AdminCatInput,
  AdminCatOptions,
  AdminOrder,
  AdminProduct,
  AdminProductInput,
  AdminTable,
  AdminTableInput,
  AdminTableOptions,
  AdminUser,
  AdminUserInput,
  AdminUserOptions,
  CreatedStaff,
  CreateStaffInput,
  ReservationTransition,
  StoreReservation,
} from './adminTypes'

export const listOrders = (token: string, signal?: AbortSignal) =>
  requestJson<AdminOrder[]>('/store/orders', { token, signal })

export const updateOrderStatus = (
  token: string,
  orderId: number,
  orderStatusId: number,
) => requestJson<AdminOrder>(`/store/orders/${orderId}/status`, {
  method: 'PATCH',
  token,
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

export const listAdminCats = (token: string, signal?: AbortSignal) =>
  requestJson<AdminCat[]>('/admin/cats', { token, signal })

export const getAdminCatOptions = (token: string, signal?: AbortSignal) =>
  requestJson<AdminCatOptions>('/admin/cats/options', { token, signal })

export const createAdminCat = (token: string, input: AdminCatInput) =>
  requestJson<AdminCat>('/admin/cats', { method: 'POST', token, body: input })

export const updateAdminCat = (
  token: string,
  id: number,
  input: AdminCatInput,
) => requestJson<AdminCat>(`/admin/cats/${id}`, {
  method: 'PUT',
  token,
  body: input,
})

export const deleteAdminCat = (token: string, id: number) =>
  requestJson<void>(`/admin/cats/${id}`, { method: 'DELETE', token })

export const listAdminUsers = (token: string, signal?: AbortSignal) =>
  requestJson<AdminUser[]>('/admin/users', { token, signal })

export const getAdminUserOptions = (token: string, signal?: AbortSignal) =>
  requestJson<AdminUserOptions>('/admin/users/options', { token, signal })

export const createAdminUser = (token: string, input: AdminUserInput) =>
  requestJson<AdminUser>('/admin/users', { method: 'POST', token, body: input })

export const updateAdminUser = (
  token: string,
  id: number,
  input: AdminUserInput,
) => requestJson<AdminUser>(`/admin/users/${id}`, {
  method: 'PUT',
  token,
  body: input,
})

export const deleteAdminUser = (token: string, id: number) =>
  requestJson<void>(`/admin/users/${id}`, { method: 'DELETE', token })

export const createStaff = (token: string, input: CreateStaffInput) =>
  requestJson<CreatedStaff>('/admin/users/staff', { method: 'POST', token, body: input })

export const listAdminTables = (token: string, signal?: AbortSignal) =>
  requestJson<AdminTable[]>('/admin/tables', { token, signal })

export const getAdminTableOptions = (token: string, signal?: AbortSignal) =>
  requestJson<AdminTableOptions>('/admin/tables/options', { token, signal })

export const createAdminTable = (token: string, input: AdminTableInput) =>
  requestJson<AdminTable>('/admin/tables', { method: 'POST', token, body: input })

export const updateAdminTable = (
  token: string,
  id: number,
  input: AdminTableInput,
) => requestJson<AdminTable>(`/admin/tables/${id}`, {
  method: 'PUT',
  token,
  body: input,
})

export const deleteAdminTable = (token: string, id: number) =>
  requestJson<void>(`/admin/tables/${id}`, { method: 'DELETE', token })