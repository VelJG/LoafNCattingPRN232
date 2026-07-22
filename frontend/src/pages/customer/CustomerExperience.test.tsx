import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AuthContext, type AuthContextValue } from '../../features/auth/AuthProvider'
import { CustomerLayout } from '../../layouts/CustomerLayout'
import { catalogRepository } from '../../services/catalogRepository'
import { CartProvider } from '../../state/CartContext'
import type { Product } from '../../types/models'
import { MenuPage } from './MenuPage'
import { ProfilePage } from './ProfilePage'

const customerSession = {
  token: 'customer-token',
  expiresAtUtc: '2030-01-01T00:00:00Z',
  user: {
    userId: 7,
    name: 'Minh Anh',
    email: 'minh@example.com',
    phoneNumber: '0900000001',
    address: null,
    role: 'Customer' as const,
    isActive: true,
    isEmailVerified: false,
  },
}

const product: Product = {
  id: 1,
  name: 'Caramel Catpuccino',
  description: 'Cà phê espresso, sữa mịn và caramel nhà làm.',
  categoryId: 1,
  categoryName: 'Cà phê',
  price: 59000,
  stock: 18,
  available: true,
  imageUrl: '/catpuccino.jpg',
}

function renderCustomer(logout: AuthContextValue['logout'] = vi.fn()) {
  const auth: AuthContextValue = {
    status: 'authenticated',
    session: customerSession,
    login: vi.fn(),
    register: vi.fn(),
    logout,
  }

  return render(
    <AuthContext.Provider value={auth}>
      <CartProvider>
        <MemoryRouter initialEntries={['/menu']}>
          <Routes>
            <Route path="/" element={<p>Landing destination</p>} />
            <Route element={<CustomerLayout />}>
              <Route path="/menu" element={<MenuPage />} />
              <Route path="/profile" element={<ProfilePage />} />
            </Route>
          </Routes>
        </MemoryRouter>
      </CartProvider>
    </AuthContext.Provider>,
  )
}

afterEach(() => {
  vi.restoreAllMocks()
})

describe('customer experience', () => {
  it('matches the customer header and moves logout into the profile screen', async () => {
    vi.spyOn(catalogRepository, 'listCategories').mockResolvedValue([])
    vi.spyOn(catalogRepository, 'listProducts').mockResolvedValue([product])
    const logout = vi.fn().mockResolvedValue(undefined)
    renderCustomer(logout)

    expect(await screen.findByText('MA')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: "Loaf'N Catting" })).toHaveTextContent('Loaf’N Catting')
    expect(screen.getByText('THỰC ĐƠN HÔM NAY')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /đặt bàn ngay/i })).toBeInTheDocument()
    expect(screen.queryByText('QUẢN TRỊ')).not.toBeInTheDocument()
    expect(screen.queryByText(/staff preview/i)).not.toBeInTheDocument()
    expect(
      within(screen.getByRole('navigation', { name: /khách hàng/i }))
        .getAllByRole('link')
        .map((link) => link.textContent),
    ).toEqual(['THỰC ĐƠN', 'ĐẶT BÀN', 'MÈO', 'VỊ TRÍ', 'THÔNG BÁO', 'TRÒ CHUYỆN'])
    expect(screen.queryByRole('button', { name: /đăng xuất/i })).not.toBeInTheDocument()

    await userEvent.click(screen.getByRole('link', { name: /tài khoản minh anh/i }))
    expect(await screen.findByRole('heading', { name: 'Minh Anh' })).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: /đăng xuất/i }))

    expect(logout).toHaveBeenCalledOnce()
    expect(await screen.findByText('Landing destination')).toBeInTheDocument()
  })

  it('loads the backend menu, supports search, and adds an item to the cart', async () => {
    vi.spyOn(catalogRepository, 'listCategories').mockResolvedValue([])
    const listProducts = vi
      .spyOn(catalogRepository, 'listProducts')
      .mockResolvedValue([product])
    renderCustomer()

    expect(await screen.findByText('Caramel Catpuccino')).toBeInTheDocument()
    await userEvent.type(
      screen.getByRole('searchbox', { name: /tìm món/i }),
      'caramel',
    )
    await waitFor(() =>
      expect(listProducts).toHaveBeenLastCalledWith({
        keyword: 'caramel',
        categoryId: undefined,
      }),
    )

    await userEvent.click(screen.getByRole('button', { name: /thêm caramel catpuccino/i }))
    expect(screen.getByRole('button', { name: /mở giỏ hàng, 1 món/i })).toBeInTheDocument()
  })

  it('offers a retry when the products API fails', async () => {
    vi.spyOn(catalogRepository, 'listCategories').mockResolvedValue([])
    const listProducts = vi
      .spyOn(catalogRepository, 'listProducts')
      .mockRejectedValueOnce(new Error('offline'))
      .mockResolvedValueOnce([product])
    renderCustomer()

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Không thể tải thực đơn',
    )
    await userEvent.click(screen.getByRole('button', { name: /thử lại/i }))

    expect(await screen.findByText('Caramel Catpuccino')).toBeInTheDocument()
    expect(listProducts).toHaveBeenCalledTimes(2)
  })

  it('makes a failed category request visible and retryable', async () => {
    const listCategories = vi
      .spyOn(catalogRepository, 'listCategories')
      .mockRejectedValueOnce(new Error('offline'))
      .mockResolvedValueOnce([{ id: 1, name: 'Cà phê' }])
    vi.spyOn(catalogRepository, 'listProducts').mockResolvedValue([product])
    renderCustomer()

    expect(await screen.findByRole('alert', { name: /lỗi danh mục/i })).toHaveTextContent(
      'Không thể tải danh mục',
    )
    await userEvent.click(screen.getByRole('button', { name: /tải lại danh mục/i }))

    expect(await screen.findByRole('button', { name: 'Cà phê' })).toBeInTheDocument()
    expect(listCategories).toHaveBeenCalledTimes(2)
  })

  it('shows backend stock state and prevents adding an unavailable product', async () => {
    vi.spyOn(catalogRepository, 'listCategories').mockResolvedValue([])
    vi.spyOn(catalogRepository, 'listProducts').mockResolvedValue([
      { ...product, stock: 0, available: false },
    ])
    renderCustomer()

    expect(await screen.findByText('HẾT MÓN')).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: /thêm caramel catpuccino/i }),
    ).toBeDisabled()
  })

  it('replaces an unavailable backend image with the designed placeholder', async () => {
    vi.spyOn(catalogRepository, 'listCategories').mockResolvedValue([])
    vi.spyOn(catalogRepository, 'listProducts').mockResolvedValue([product])
    renderCustomer()

    fireEvent.error(await screen.findByRole('img', { name: product.name }))

    expect(
      screen.getByRole('img', { name: `Ảnh minh họa ${product.name}` }),
    ).toBeInTheDocument()
  })
})
