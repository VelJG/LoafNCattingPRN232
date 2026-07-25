import { requestJson } from '../../api/httpClient'

export interface CustomerOrderItem {
  orderDetailId: number
  productId: number
  productName: string
  quantity: number
  unitPrice: number
  subtotal: number
}

export interface CustomerPayment {
  paymentId: number
  paymentAmount: number
  methodId: number
  methodName: string
  paymentStatus: string
  transactionCode: string | null
  paymentDate: string
  paidAt: string | null
}

export interface CustomerOrder {
  orderId: number
  customerUserId: number | null
  customerName: string | null
  orderDate: string
  totalPrice: number
  orderType: string | null
  note: string | null
  orderStatusId: number
  orderStatusName: string
  items: CustomerOrderItem[]
  payments: CustomerPayment[]
  tableId: number | null
  tableName: string | null
  reservationId: number | null
}

export interface PaymentLink {
  orderId: number
  orderCode: number
  amount: number
  checkoutUrl: string
  qrCode: string
  paymentLinkId: string
}

export interface PaymentStatus {
  orderId: number
  paymentStatus: string
  orderStatus: string
  isPaid: boolean
}

export function listMyOrders(token: string, signal?: AbortSignal) {
  return requestJson<CustomerOrder[]>('/orders/mine', { token, signal })
}

export function createPaymentLink(token: string, orderId: number) {
  return requestJson<PaymentLink>('/payments/links', {
    method: 'POST',
    token,
    body: { orderId },
  })
}

export function getPaymentStatus(token: string, orderId: number) {
  return requestJson<PaymentStatus>(`/payments/${orderId}/status`, { token })
}
