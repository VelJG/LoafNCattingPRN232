import { cafeTables, cats, dashboardMetrics, products, recentOrders } from '../data/mockData'
import type { CafeTable, CatProfile, DashboardMetric, Product, RecentOrder } from '../types/models'

const pause = (milliseconds = 220) =>
  new Promise<void>((resolve) => window.setTimeout(resolve, milliseconds))

export interface ProductQuery {
  keyword?: string
  categoryId?: string
}

export interface CatalogRepository {
  listProducts(query?: ProductQuery): Promise<Product[]>
  listCats(): Promise<CatProfile[]>
  listAvailableTables(guests: number): Promise<CafeTable[]>
  getDashboard(): Promise<{ metrics: DashboardMetric[]; orders: RecentOrder[] }>
}

class MockCatalogRepository implements CatalogRepository {
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

// Swap this instance for an API-backed repository when the ASP.NET controllers are ready.
export const catalogRepository: CatalogRepository = new MockCatalogRepository()
