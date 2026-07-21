import { cafeTables, cats, dashboardMetrics, products, recentOrders } from '../data/mockData'
import type { CafeTable, CatProfile, Category, DashboardMetric, Product, RecentOrder } from '../types/models'

const pause = (milliseconds = 220) =>
  new Promise<void>((resolve) => window.setTimeout(resolve, milliseconds))

export interface ProductQuery {
  keyword?: string
  categoryId?: number
}

export interface CatalogRepository {
  listCategories(): Promise<Category[]>
  listProducts(query?: ProductQuery): Promise<Product[]>
  listCats(): Promise<CatProfile[]>
  listAvailableTables(guests: number): Promise<CafeTable[]>
  getDashboard(): Promise<{ metrics: DashboardMetric[]; orders: RecentOrder[] }>
}

class MockCatalogRepository implements CatalogRepository {
  async listCategories() {
    await pause()
    return []
  }

  async listProducts(query: ProductQuery = {}) {
    await pause()
    const keyword = query.keyword?.trim().toLowerCase()
    return products.filter((product) => {
      const matchesKeyword =
        !keyword ||
        product.name.toLowerCase().includes(keyword) ||
        product.description.toLowerCase().includes(keyword)
      const matchesCategory = !query.categoryId || product.categoryId === query.categoryId
      return matchesKeyword && matchesCategory
    })
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
    await pause(280)
    return { metrics: dashboardMetrics, orders: recentOrders }
  }
}

const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.replace(/\/$/, '') ?? 'http://localhost:5053/api'

const fallbackProductImage =
  'https://images.unsplash.com/photo-1684246524496-180d5b07ee7e?auto=format&fit=crop&w=1400&q=86'
const fallbackCatImage =
  'https://images.unsplash.com/photo-1495360010541-f48722b34f7d?auto=format&fit=crop&w=900&q=82'

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
  canOrder: boolean
  pictureKey?: string | null
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
  pictureKey?: string | null
}

async function readJson<T>(path: string) {
  const response = await fetch(`${apiBaseUrl}${path}`)
  if (!response.ok) {
    throw new Error(`Request failed: ${response.status} ${response.statusText}`)
  }
  return (await response.json()) as T
}

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
    return items.map((item) => ({
      id: item.productId,
      name: item.name,
      description: item.description ?? '',
      categoryId: item.categoryId,
      categoryName: item.categoryName,
      price: item.price,
      discountPrice: item.discountPrice ?? undefined,
      stock: item.unitInStock,
      available: item.canOrder,
      imageUrl: item.picture || fallbackProductImage,
    }))
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
    await pause(280)
    return { metrics: dashboardMetrics, orders: recentOrders }
  }
}

export const catalogRepository: CatalogRepository =
  import.meta.env.DEV || import.meta.env.PROD
    ? new ApiCatalogRepository()
    : new MockCatalogRepository()
