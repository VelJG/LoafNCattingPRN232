import { cafeTables, dashboardMetrics, recentOrders } from '../data/mockData'
import type { CafeTable, CatProfile, Category, DashboardMetric, Product, RecentOrder } from '../types/models'

const pause = (milliseconds = 220) =>
  new Promise<void>((resolve) => window.setTimeout(resolve, milliseconds))

const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.replace(/\/$/, '') ?? '/api'

const fallbackProductImage =
  'https://images.unsplash.com/photo-1684246524496-180d5b07ee7e?auto=format&fit=crop&w=1400&q=86'
const fallbackCatImage =
  'https://images.unsplash.com/photo-1495360010541-f48722b34f7d?auto=format&fit=crop&w=900&q=82'

export interface ProductQuery {
  keyword?: string
  categoryId?: number
}

export interface AdminProductInput {
  name: string
  description: string
  price: number
  discountPrice?: number
  unitInStock: number
  picture: string
  categoryId: number
  isAvailable: boolean
}

export interface CatalogRepository {
  listCategories(): Promise<Category[]>
  listProducts(query?: ProductQuery): Promise<Product[]>
  createProduct(product: AdminProductInput): Promise<Product>
  updateProduct(productId: number, product: AdminProductInput): Promise<Product>
  deleteProduct(productId: number): Promise<void>
  listCats(): Promise<CatProfile[]>
  listAvailableTables(guests: number): Promise<CafeTable[]>
  getDashboard(): Promise<{ metrics: DashboardMetric[]; orders: RecentOrder[] }>
}

interface ApiCategory {
  categoryId: number
  name: string
  description?: string | null
}

interface ApiProduct {
  productId: number
  name: string
  description?: string | null
  price: number
  discountPrice?: number | null
  unitInStock: number
  picture?: string | null
  categoryId: number
  categoryName: string
  isAvailable: boolean
  canOrder?: boolean
}

interface ApiCat {
  catId: number
  name: string
  age?: number | null
  genderName?: string | null
  breed?: string | null
  picture?: string | null
  description?: string | null
  friendlinessRating?: number | null
  cutenessRating?: number | null
  playfulnessRating?: number | null
  statusName: string
}

const toProduct = (item: ApiProduct): Product => ({
  id: item.productId,
  name: item.name,
  description: item.description ?? '',
  categoryId: item.categoryId,
  categoryName: item.categoryName,
  apiCategoryId: item.categoryId,
  price: item.price,
  discountPrice: item.discountPrice ?? undefined,
  stock: item.unitInStock,
  available: item.canOrder ?? item.isAvailable,
  imageUrl: item.picture || fallbackProductImage,
})

const readJson = async <T>(path: string, init: RequestInit = {}) => {
  const response = await fetch(`${apiBaseUrl}${path}`, init)
  if (!response.ok) {
    const message = await response.text()
    throw new Error(message || `Request failed: ${response.status} ${response.statusText}`)
  }
  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}

const writeAdminJson = async <T>(path: string, init: RequestInit = {}) =>
  readJson<T>(path, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      'X-User-Role': 'Admin',
      ...init.headers,
    },
  })

class ApiCatalogRepository implements CatalogRepository {
  async listCategories() {
    const items = await readJson<ApiCategory[]>('/categories')
    return items.map((item) => ({
      id: item.categoryId,
      name: item.name,
      description: item.description ?? undefined,
    }))
  }

  async listProducts(query: ProductQuery = {}) {
    const searchParams = new URLSearchParams()
    if (query.keyword?.trim()) searchParams.set('search', query.keyword.trim())
    if (query.categoryId) searchParams.set('categoryId', String(query.categoryId))
    const suffix = searchParams.size > 0 ? `?${searchParams.toString()}` : ''
    const items = await readJson<ApiProduct[]>(`/products${suffix}`)
    return items.map(toProduct)
  }

  async createProduct(product: AdminProductInput) {
    const created = await writeAdminJson<ApiProduct>('/admin/products', {
      method: 'POST',
      body: JSON.stringify(product),
    })
    return toProduct(created)
  }

  async updateProduct(productId: number, product: AdminProductInput) {
    const updated = await writeAdminJson<ApiProduct>(`/admin/products/${productId}`, {
      method: 'PUT',
      body: JSON.stringify(product),
    })
    return toProduct(updated)
  }

  async deleteProduct(productId: number) {
    await writeAdminJson<void>(`/admin/products/${productId}`, { method: 'DELETE' })
  }

  async listCats() {
    const items = await readJson<ApiCat[]>('/cats')
    return items.map((item) => ({
      id: item.catId,
      name: item.name,
      breed: item.breed ?? 'Unknown breed',
      age: item.age ?? undefined,
      gender: item.genderName ?? undefined,
      status: item.statusName,
      description: item.description ?? '',
      friendliness: item.friendlinessRating ?? undefined,
      playfulness: item.playfulnessRating ?? undefined,
      cuteness: item.cutenessRating ?? undefined,
      imageUrl: item.picture || fallbackCatImage,
    }))
  }

  async listAvailableTables(guests: number) {
    await pause(320)
    return cafeTables.filter((table) => table.available && table.capacity >= guests)
  }

  async getDashboard() {
    const menu = await this.listProducts()
    return {
      metrics: dashboardMetrics.map((metric) =>
        metric.id === 'stock'
          ? { ...metric, value: String(menu.filter((product) => product.stock <= 5).length) }
          : metric.id === 'orders'
            ? { ...metric, value: String(menu.length), label: 'Menu items', note: 'Loaded from API' }
            : metric,
      ),
      orders: recentOrders,
    }
  }
}

export const catalogRepository: CatalogRepository = new ApiCatalogRepository()
