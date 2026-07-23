import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import * as adminApi from '../../features/admin/adminApi'
import { AuthContext, type AuthContextValue } from '../../features/auth/AuthProvider'
import { AdminUsersPage } from './AdminUsersPage'

const auth: AuthContextValue = {
  status: 'authenticated',
  session: { token: 'admin-token', expiresAtUtc: '2030-01-01T00:00:00Z', user: { userId: 1, name: 'Minh Anh', email: 'admin@loaf.vn', phoneNumber: '0900', address: null, role: 'Admin', isActive: true, isEmailVerified: true } },
  login: vi.fn(), register: vi.fn(), logout: vi.fn(),
}

afterEach(() => vi.restoreAllMocks())

describe('AdminUsersPage', () => {
  it('creates Staff through the only supported user-management endpoint', async () => {
    const create = vi.spyOn(adminApi, 'createStaff').mockResolvedValue({
      userId: 11, name: 'Hà Linh', email: 'linh@loaf.vn', phoneNumber: '0900000002', address: null,
      role: 'Staff', isActive: true, isEmailVerified: false,
    })
    render(<AuthContext.Provider value={auth}><MemoryRouter><AdminUsersPage /></MemoryRouter></AuthContext.Provider>)

    expect(screen.getByPlaceholderText('Tìm theo tên/email/SĐT')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: /tạo nhân viên/i }))
    await userEvent.type(screen.getByLabelText('Họ và tên'), 'Hà Linh')
    await userEvent.type(screen.getByLabelText('Email'), 'linh@loaf.vn')
    await userEvent.type(screen.getByLabelText('Số điện thoại'), '0900000002')
    await userEvent.type(screen.getByLabelText('Mật khẩu'), 'Password1')
    await userEvent.click(screen.getByRole('button', { name: /lưu nhân viên/i }))

    expect(create).toHaveBeenCalledWith('admin-token', {
      name: 'Hà Linh', email: 'linh@loaf.vn', phoneNumber: '0900000002', password: 'Password1', address: null,
    })
    expect(await screen.findByText('Hà Linh')).toBeInTheDocument()
    expect(screen.getByRole('status')).toHaveTextContent('Đã tạo nhân viên Hà Linh')
  })
})
