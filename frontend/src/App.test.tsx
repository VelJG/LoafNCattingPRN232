import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import App from './App'
import { adminDashboardApi } from './features/admin/adminDashboardApi'
import { AuthContext, type AuthContextValue } from './features/auth/AuthProvider'
import type { Session, UserRole } from './features/auth/authModels'
import { catalogRepository } from './services/catalogRepository'
import { CartProvider } from './state/CartContext'

function sessionFor(role: UserRole): Session {
  return {
    token: `${role.toLowerCase()}-token`,
    expiresAtUtc: '2030-01-01T00:00:00Z',
    user: {
      userId: 1,
      name: role === 'Customer' ? 'Minh Anh' : 'Linh Nguyễn',
      email: 'user@loaf.vn',
      phoneNumber: '0900000001',
      address: null,
      role,
      isActive: true,
      isEmailVerified: true,
    },
  }
}

function renderApp(path: string, session: Session | null = null) {
  const auth: AuthContextValue = {
    status: session ? 'authenticated' : 'unauthenticated',
    session,
    login: vi.fn(),
    register: vi.fn(),
    logout: vi.fn(),
  }
  return render(
    <AuthContext.Provider value={auth}>
      <CartProvider>
        <MemoryRouter initialEntries={[path]}>
          <App />
        </MemoryRouter>
      </CartProvider>
    </AuthContext.Provider>,
  )
}

beforeEach(() => {
  vi.spyOn(catalogRepository, 'listCategories').mockResolvedValue([])
  vi.spyOn(catalogRepository, 'listProducts').mockResolvedValue([])
  vi.spyOn(adminDashboardApi, 'load').mockResolvedValue({
    orders: [],
    products: [],
    cats: [],
  })
})

describe('role-aware application routing', () => {
  it('serves the public landing page at the root', () => {
    renderApp('/')
    expect(screen.getByRole('heading', { name: /chỗ ngồi ấm/i })).toBeInTheDocument()
  })

  it('sends anonymous users from customer pages to login', () => {
    renderApp('/menu')
    expect(screen.getByRole('heading', { name: /đăng nhập vào góc quen/i })).toBeInTheDocument()
  })

  it('redirects a Customer away from admin to the customer menu', async () => {
    renderApp('/admin', sessionFor('Customer'))
    expect(await screen.findByRole('heading', { name: /một tách cà phê ngon hơn/i })).toBeInTheDocument()
  })

  it.each(['Staff', 'Admin'] as const)('redirects %s from customer pages to admin', async (role) => {
    renderApp('/menu', sessionFor(role))
    expect(await screen.findByRole('heading', { name: /hôm nay tại loaf'n catting/i })).toBeInTheDocument()
  })

  it('redirects an authenticated Customer away from login', async () => {
    renderApp('/login', sessionFor('Customer'))
    expect(await screen.findByRole('heading', { name: /một tách cà phê ngon hơn/i })).toBeInTheDocument()
  })
})
