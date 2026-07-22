import { requestJson } from '../../api/httpClient'

export interface AdminOrderItem {
  orderDetailId: number
  productId: number
  productName: string
  quantity: number
  unitPrice: number
  subtotal: number
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
}

export interface AdminProduct {
  productId: number
  name: string
  unitInStock: number
  isAvailable: boolean
}

export interface AdminCat {
  catId: number
  name: string
  statusName: string
}

export interface AdminDashboardData {
  orders: AdminOrder[]
  products: AdminProduct[]
  cats: AdminCat[]
}

export const adminDashboardApi = {
  async load(token: string): Promise<AdminDashboardData> {
    const [orders, products, cats] = await Promise.all([
      requestJson<AdminOrder[]>('/orders', { token }),
      requestJson<AdminProduct[]>('/admin/products', { token }),
      requestJson<AdminCat[]>('/cats', { token }),
    ])
    return { orders, products, cats }
  },
}
