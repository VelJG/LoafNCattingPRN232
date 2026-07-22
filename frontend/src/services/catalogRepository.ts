import { cafeTables, cats, dashboardMetrics, recentOrders } from '../data/mockData'
import type { CafeTable, CatProfile, DashboardMetric, Product, RecentOrder } from '../types/models'

const pause = (milliseconds = 220) =>
  new Promise<void>((resolve) => window.setTimeout(resolve, milliseconds))

export interface ProductQuery {
  keyword?: string
  categoryId?: string
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
  listProducts(query?: ProductQuery): Promise<Product[]>
  createProduct(product: AdminProductInput): Promise<Product>
  updateProduct(productId: number, product: AdminProductInput): Promise<Product>
  deleteProduct(productId: number): Promise<void>
  listCats(): Promise<CatProfile[]>
  listAvailableTables(guests: number): Promise<CafeTable[]>
  getDashboard(): Promise<{ metrics: DashboardMetric[]; orders: RecentOrder[] }>
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
}

const categoryKey = (name: string) =>
  name.toLowerCase().includes('coffee') ? 'coffee'
    : name.toLowerCase().includes('tea') || name.toLowerCase().includes('matcha') ? 'tea'
      : name.toLowerCase().includes('cake') || name.toLowerCase().includes('bak') ? 'cake'
        : 'combo'

const fallbackImage =
  'https://images.unsplash.com/photo-1509042239860-f550ce710b93?auto=format&fit=crop&w=900&q=82'

const toProduct = (product: ApiProduct): Product => ({
  id: product.productId,
  name: product.name,
  description: product.description ?? '',
  categoryId: categoryKey(product.categoryName),
  categoryName: product.categoryName,
  apiCategoryId: product.categoryId,
  price: product.price,
  discountPrice: product.discountPrice ?? undefined,
  stock: product.unitInStock,
  available: product.isAvailable,
  imageUrl: product.picture || fallbackImage,
})

const api = async <T>(path: string, init: RequestInit = {}) => {
  const response = await fetch(path, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      'X-User-Role': 'Admin',
      ...init.headers,
    },
  })

  if (!response.ok) {
    const message = await response.text()
    throw new Error(message || `Request failed with ${response.status}`)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return response.json() as Promise<T>
}

class ApiCatalogRepository implements CatalogRepository {
  async listProducts(query: ProductQuery = {}) {
    const params = new URLSearchParams()
    if (query.keyword?.trim()) params.set('search', query.keyword.trim())

    const apiProducts = await api<ApiProduct[]>(`/api/admin/products?${params}`)
    const mapped = apiProducts.map(toProduct)
    return query.categoryId ? mapped.filter((product) => product.categoryId === query.categoryId) : mapped
  }

  async createProduct(product: AdminProductInput) {
    const created = await api<ApiProduct>('/api/admin/products', {
      method: 'POST',
      body: JSON.stringify(product),
    })
    return toProduct(created)
  }

  async updateProduct(productId: number, product: AdminProductInput) {
    const updated = await api<ApiProduct>(`/api/admin/products/${productId}`, {
      method: 'PUT',
      body: JSON.stringify(product),
    })
    return toProduct(updated)
  }

  async deleteProduct(productId: number) {
    await api<void>(`/api/admin/products/${productId}`, { method: 'DELETE' })
  }

  async listCats() {
    await pause()
    return cats
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


