export interface Category {
  id: number
  name: string
  description?: string
}

export interface Product {
  id: number
  name: string
  description: string
  categoryId: number
  categoryName: string
  price: number
  discountPrice?: number
  stock: number
  available: boolean
  imageUrl: string
  badge?: string
}

export interface CatProfile {
  id: number
  name: string
  breed: string
  age?: number
  gender?: string
  status: string
  description: string
  friendliness?: number
  playfulness?: number
  imageUrl: string
  cuteness?: number
}

export interface CafeTable {
  id: number
  name: string
  area: string
  capacity: number
  available: boolean
}

export interface DashboardMetric {
  id: string
  label: string
  value: string
  note: string
  tone: 'orange' | 'green' | 'blue' | 'rose'
}

export interface RecentOrder {
  id: string
  customer: string
  items: number
  total: number
  status: 'Pending' | 'Processing' | 'Completed'
  time: string
}

export interface CartLine {
  product: Product
  quantity: number
}
