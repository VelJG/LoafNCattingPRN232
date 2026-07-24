import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import * as adminApi from '../../features/admin/adminApi'
import type { AdminUser } from '../../features/admin/adminTypes'
import { AuthContext, type AuthContextValue } from '../../features/auth/AuthProvider'
import { AdminUsersPage } from './AdminUsersPage'

const auth: AuthContextValue = {
  status: 'authenticated',
  session: { token: 'admin-token', expiresAtUtc: '2030-01-01T00:00:00Z', user: { userId: 1, name: 'Minh Anh', email: 'admin@loaf.vn', phoneNumber: '0900', address: null, role: 'Admin', isActive: true, isEmailVerified: true } },
  login: vi.fn(), register: vi.fn(), logout: vi.fn(),
}

const staffUser: AdminUser = {
  userId: 11,
  name: 'Hà Linh',
  email: 'linh@loaf.vn',
  phoneNumber: '0900000002',
  address: null,
  avatarUrl: null,
  role: 'Staff',
  isActive: true,
  isEmailVerified: false,
  createdAt: '2026-07-24T00:00:00Z',
  updatedAt: null,
}

function renderPage() {
  return render(<AuthContext.Provider value={auth}><MemoryRouter><AdminUsersPage /></MemoryRouter></AuthContext.Provider>)
}

afterEach(() => vi.restoreAllMocks())

describe('AdminUsersPage', () => {
  it('loads users and creates a user through the admin CRUD API', async () => {
    vi.spyOn(adminApi, 'listAdminUsers').mockResolvedValue([staffUser])
    vi.spyOn(adminApi, 'getAdminUserOptions').mockResolvedValue({ roles: ['Admin', 'Staff', 'Customer'] })
    const create = vi.spyOn(adminApi, 'createAdminUser').mockResolvedValue({ ...staffUser, userId: 12, name: 'Bảo An', email: 'bao@loaf.vn' })

    renderPage()

    expect(await screen.findByText('Hà Linh')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: /thêm người dùng/i }))
    await userEvent.type(screen.getByLabelText('Họ và tên'), 'Bảo An')
    await userEvent.type(screen.getByLabelText('Email'), 'bao@loaf.vn')
    await userEvent.type(screen.getByLabelText('Số điện thoại'), '0900000003')
    await userEvent.selectOptions(screen.getByLabelText('Vai trò'), 'Staff')
    await userEvent.type(screen.getByLabelText('Mật khẩu'), 'Password1')
    await userEvent.click(screen.getByRole('button', { name: /lưu người dùng/i }))

    expect(create).toHaveBeenCalledWith('admin-token', {
      name: 'Bảo An',
      email: 'bao@loaf.vn',
      phoneNumber: '0900000003',
      address: null,
      avatarUrl: null,
      role: 'Staff',
      isActive: true,
      isEmailVerified: false,
      password: 'Password1',
    })
    expect(await screen.findByText('Bảo An')).toBeInTheDocument()
  })

  it('updates and deletes users with confirmation', async () => {
    vi.spyOn(adminApi, 'listAdminUsers').mockResolvedValue([staffUser])
    vi.spyOn(adminApi, 'getAdminUserOptions').mockResolvedValue({ roles: ['Admin', 'Staff', 'Customer'] })
    const update = vi.spyOn(adminApi, 'updateAdminUser').mockResolvedValue({ ...staffUser, name: 'Hà Linh Updated', isActive: false })
    const remove = vi.spyOn(adminApi, 'deleteAdminUser').mockResolvedValue(undefined)

    renderPage()

    expect(await screen.findByText('Hà Linh')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: /sửa hà linh/i }))
    await userEvent.clear(screen.getByLabelText('Họ và tên'))
    await userEvent.type(screen.getByLabelText('Họ và tên'), 'Hà Linh Updated')
    await userEvent.click(screen.getByLabelText('Tài khoản đang hoạt động'))
    await userEvent.click(screen.getByRole('button', { name: /lưu người dùng/i }))

    expect(update).toHaveBeenCalledWith('admin-token', 11, expect.objectContaining({
      name: 'Hà Linh Updated',
      isActive: false,
      password: null,
    }))
    expect(await screen.findByText('Hà Linh Updated')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: /xóa hà linh updated/i }))
    await userEvent.click(screen.getByRole('button', { name: /^xóa người dùng$/i }))

    expect(remove).toHaveBeenCalledWith('admin-token', 11)
  })
})