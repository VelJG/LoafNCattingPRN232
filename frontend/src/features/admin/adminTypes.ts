import type { User } from '../auth/authModels'

export type OperatorRole = 'Admin' | 'Staff'
export type ReservationTransition = 'confirm' | 'cancel' | 'check-in' | 'complete'

export interface AdminOrderItem {
  orderDetailId: number
  productId: number
  productName: string
  quantity: number
  unitPrice: number
  subtotal: number
}

export interface AdminPayment {
  paymentId: number
  paymentAmount: number
  methodId: number
  methodName: string
  paymentStatus: string
  transactionCode: string | null
  paymentDate: string
  paidAt: string | null
}

export interface AdminOrder {
  orderId: number
  customerUserId: number | null
  customerName: string | null
  orderDate: string
  totalPrice: number
  orderType: string | null
  note: string | null
  orderStatusId: number
  orderStatusName: string
  items: AdminOrderItem[]
  payments: AdminPayment[]
}

export interface StoreReservationTable {
  tableId: number
  tableName: string
  capacity: number
  area: string | null
  description: string | null
}

export interface StoreReservation {
  reservationId: number
  customerUserId: number | null
  customerName: string | null
  customerEmail: string | null
  date: string
  time: string
  numberOfGuests: number
  guestName: string
  guestPhoneNumber: string
  note: string | null
  status: string
  durationMinutes: number
  startAt: string
  endAt: string
  table: StoreReservationTable
  tableStatus: string
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface AdminProduct {
  productId: number
  name: string
  description: string | null
  price: number
  discountPrice: number | null
  unitInStock: number
  picture: string | null
  categoryId: number
  categoryName: string
  isAvailable: boolean
  createdAt: string
  updatedAt: string | null
}

export type AdminProductInput = Pick<
  AdminProduct,
  | 'name'
  | 'description'
  | 'price'
  | 'discountPrice'
  | 'unitInStock'
  | 'picture'
  | 'categoryId'
  | 'isAvailable'
>

export interface CreateStaffInput {
  name: string
  email: string
  password: string
  phoneNumber: string
  address: string | null
}

export type CreatedStaff = User
