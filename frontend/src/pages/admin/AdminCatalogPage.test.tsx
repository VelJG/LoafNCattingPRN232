import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import * as adminApi from '../../features/admin/adminApi'
import type { AdminProduct } from '../../features/admin/adminTypes'
import { AuthContext, type AuthContextValue } from '../../features/auth/AuthProvider'
import { catalogRepository } from '../../services/catalogRepository'
import { AdminCatalogPage } from './AdminCatalogPage'

const auth: AuthContextValue = {
  status: 'authenticated',
  session: { token: 'staff-token', expiresAtUtc: '2030-01-01T00:00:00Z', user: { userId: 2, name: 'Linh', email: 'linh@loaf.vn', phoneNumber: '0900', address: null, role: 'Staff', isActive: true, isEmailVerified: true } },
  login: vi.fn(), register: vi.fn(), logout: vi.fn(),
}

const product: AdminProduct = {
  productId: 8, name: 'Matcha đá xay', description: 'Thơm mát', price: 65000, discountPrice: null,
  unitInStock: 6, picture: null, categoryId: 2, categoryName: 'Đồ uống', isAvailable: true,
  createdAt: '2026-07-22T00:00:00Z', updatedAt: null,
}

function renderPage() {
  return render(<AuthContext.Provider value={auth}><MemoryRouter><AdminCatalogPage /></MemoryRouter></AuthContext.Provider>)
}

afterEach(() => vi.restoreAllMocks())

describe('AdminCatalogPage', () => {
  it('renders the reference product table from the admin API', async () => {
    vi.spyOn(adminApi, 'listAdminProducts').mockResolvedValue([product])
    vi.spyOn(catalogRepository, 'listCategories').mockResolvedValue([{ id: 2, name: 'Đồ uống' }])
    renderPage()

    expect(await screen.findByText('Matcha đá xay')).toBeInTheDocument()
    expect(screen.getByText('1 SẢN PHẨM')).toBeInTheDocument()
    expect(screen.getByText('65.000 VND')).toBeInTheDocument()
  })

  it('deletes only after confirmation and removes the returned row', async () => {
    vi.spyOn(adminApi, 'listAdminProducts').mockResolvedValue([product])
    vi.spyOn(catalogRepository, 'listCategories').mockResolvedValue([{ id: 2, name: 'Đồ uống' }])
    const remove = vi.spyOn(adminApi, 'deleteAdminProduct').mockResolvedValue(undefined)
    renderPage()

    await userEvent.click(await screen.findByRole('button', { name: /xóa matcha đá xay/i }))
    await userEvent.click(screen.getByRole('button', { name: /^xóa sản phẩm$/i }))

    expect(remove).toHaveBeenCalledWith('staff-token', 8)
    expect(screen.queryByText('Matcha đá xay')).not.toBeInTheDocument()
  })
})
