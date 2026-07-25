import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes, useParams } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AuthContext, type AuthContextValue } from '../../features/auth/AuthProvider'
import type { CartGateway } from '../../features/cart/cartApi'
import { MenuProductCard } from '../../features/menu/components/MenuProductCard'
import { catalogRepository } from '../../services/catalogRepository'
import { CartProvider } from '../../state/CartContext'
import type { Product } from '../../types/models'
import { ProductDetailPage } from './ProductDetailPage'

const product: Product = {
  id: 12,
  name: 'Bạc Xỉu',
  description: 'Bạc xỉu kiểu Việt với nền sữa đặc béo ngọt.',
  categoryId: 3,
  categoryName: 'Cà phê',
  price: 28_000,
  stock: 71,
  available: true,
  imageUrl: '/bac-xiu.jpg',
}

const auth: AuthContextValue = {
  status: 'authenticated',
  session: {
    token: 'customer-token',
    expiresAtUtc: '2030-01-01T00:00:00Z',
    user: {
      userId: 7,
      name: 'Customer',
      email: 'customer@example.com',
      phoneNumber: '0900000001',
      address: null,
      role: 'Customer',
      isActive: true,
      isEmailVerified: false,
    },
  },
  login: vi.fn(),
  register: vi.fn(),
  logout: vi.fn(),
}

function createGateway(): CartGateway {
  const emptyCart = { cartId: 1, userId: 7, items: [], total: 0 }
  return {
    get: vi.fn().mockResolvedValue(emptyCart),
    getCheckoutOptions: vi.fn().mockResolvedValue({
      orderTypes: ['Takeaway'],
      paymentMethods: [{
        paymentMethodId: 2,
        name: 'Bank transfer',
        description: null,
      }],
    }),
    add: vi.fn().mockResolvedValue({
      ...emptyCart,
      total: product.price,
      items: [{
        cartItemId: 1,
        productId: product.id,
        productName: product.name,
        picture: product.imageUrl,
        unitPrice: product.price,
        quantity: 1,
        lineTotal: product.price,
        availableStock: product.stock,
        isAvailable: true,
      }],
    }),
    update: vi.fn().mockResolvedValue(emptyCart),
    remove: vi.fn().mockResolvedValue(emptyCart),
    clear: vi.fn().mockResolvedValue(emptyCart),
    checkout: vi.fn(),
  }
}

function ProductRouteProbe() {
  const { productId } = useParams()
  return <p>Product {productId}</p>
}

afterEach(() => {
  vi.restoreAllMocks()
})

describe('product detail flow', () => {
  it('opens a product route when the product card is selected', async () => {
    render(
      <MemoryRouter initialEntries={['/menu']}>
        <Routes>
          <Route
            path="/menu"
            element={<MenuProductCard product={product} onAdd={vi.fn()} />}
          />
          <Route path="/menu/:productId" element={<ProductRouteProbe />} />
        </Routes>
      </MemoryRouter>,
    )

    await userEvent.click(
      screen.getByRole('link', { name: `Xem chi tiết ${product.name}` }),
    )

    expect(screen.getByText(`Product ${product.id}`)).toBeInTheDocument()
  })

  it('loads product details and adds the product to the cart', async () => {
    const gateway = createGateway()
    vi.spyOn(catalogRepository, 'getProduct').mockResolvedValue(product)

    render(
      <AuthContext.Provider value={auth}>
        <CartProvider gateway={gateway}>
          <MemoryRouter initialEntries={[`/menu/${product.id}`]}>
            <Routes>
              <Route path="/menu/:productId" element={<ProductDetailPage />} />
            </Routes>
          </MemoryRouter>
        </CartProvider>
      </AuthContext.Provider>,
    )

    expect(
      await screen.findByRole('heading', { name: product.name }),
    ).toBeInTheDocument()
    expect(screen.getByText('CÒN 71')).toBeInTheDocument()
    expect(screen.getByText('28.000 ₫')).toBeInTheDocument()

    await userEvent.click(
      screen.getByRole('button', { name: 'THÊM VÀO GIỎ' }),
    )

    await waitFor(() => {
      expect(gateway.add).toHaveBeenCalledWith(
        'customer-token',
        product.id,
        1,
      )
    })
  })
})
