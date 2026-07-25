import { requestJson } from '../../api/httpClient'

export interface ApiCartItem {
  cartItemId: number
  productId: number
  productName: string
  picture: string | null
  unitPrice: number
  quantity: number
  lineTotal: number
  availableStock: number
  isAvailable: boolean
}

export interface ApiCart {
  cartId: number
  userId: number
  items: ApiCartItem[]
  total: number
}

export interface CheckoutPaymentMethod {
  paymentMethodId: number
  name: string
  description: string | null
}

export interface CheckoutOptions {
  orderTypes: string[]
  paymentMethods: CheckoutPaymentMethod[]
}

export interface CheckoutInput {
  orderType: string
  tableId: number | null
  reservationId: number | null
  paymentMethodId: number
  note: string | null
}

export interface CheckoutOrder {
  orderId: number
  totalPrice: number
  orderStatusId: number
  orderStatusName: string
}

export interface CartGateway {
  get(token: string, signal?: AbortSignal): Promise<ApiCart>
  add(token: string, productId: number, quantity: number): Promise<ApiCart>
  update(token: string, productId: number, quantity: number): Promise<ApiCart>
  remove(token: string, productId: number): Promise<ApiCart>
  clear(token: string): Promise<ApiCart>
  getCheckoutOptions(token: string, signal?: AbortSignal): Promise<CheckoutOptions>
  checkout(token: string, input: CheckoutInput): Promise<CheckoutOrder>
}

export const cartApi: CartGateway = {
  get: (token, signal) => requestJson<ApiCart>('/cart', { token, signal }),
  add: (token, productId, quantity) => requestJson<ApiCart>('/cart/items', {
    method: 'POST',
    token,
    body: { productId, quantity },
  }),
  update: (token, productId, quantity) => requestJson<ApiCart>(`/cart/items/${productId}`, {
    method: 'PATCH',
    token,
    body: { quantity },
  }),
  remove: (token, productId) => requestJson<ApiCart>(`/cart/items/${productId}`, {
    method: 'DELETE',
    token,
  }),
  clear: (token) => requestJson<ApiCart>('/cart', {
    method: 'DELETE',
    token,
  }),
  getCheckoutOptions: (token, signal) => requestJson<CheckoutOptions>(
    '/orders/checkout-options',
    { token, signal },
  ),
  checkout: (token, input) => requestJson<CheckoutOrder>('/orders/checkout', {
    method: 'POST',
    token,
    body: input,
  }),
}
