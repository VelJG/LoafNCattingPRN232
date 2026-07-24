import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import * as adminApi from '../../features/admin/adminApi'
import type { AdminTable } from '../../features/admin/adminTypes'
import { AuthContext, type AuthContextValue } from '../../features/auth/AuthProvider'
import { AdminTablesPage } from './AdminTablesPage'

const auth: AuthContextValue = {
  status: 'authenticated',
  session: { token: 'staff-token', expiresAtUtc: '2030-01-01T00:00:00Z', user: { userId: 2, name: 'Nhân viên', email: 'staff@loaf.vn', phoneNumber: '0900', address: null, role: 'Staff', isActive: true, isEmailVerified: true } },
  login: vi.fn(), register: vi.fn(), logout: vi.fn(),
}

const table: AdminTable = {
  tableId: 4,
  tableName: 'Bàn 4',
  area: 'Tầng 1',
  capacity: 4,
  description: null,
  status: 'Trống',
}

function renderPage() {
  return render(<AuthContext.Provider value={auth}><MemoryRouter><AdminTablesPage /></MemoryRouter></AuthContext.Provider>)
}

afterEach(() => vi.restoreAllMocks())

describe('AdminTablesPage', () => {
  it('loads tables and creates a table through the admin CRUD API', async () => {
    vi.spyOn(adminApi, 'listAdminTables').mockResolvedValue([table])
    vi.spyOn(adminApi, 'getAdminTableOptions').mockResolvedValue({ statuses: ['Trống', 'Đã đặt', 'Đang sử dụng', 'Bảo trì'] })
    const create = vi.spyOn(adminApi, 'createAdminTable').mockResolvedValue({ ...table, tableId: 5, tableName: 'Bàn sân vườn', capacity: 6 })

    renderPage()

    expect(await screen.findByText('Bàn 4')).toBeInTheDocument()
    expect(screen.getByText('1 BÀN')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: /thêm bàn/i }))
    await userEvent.type(screen.getByLabelText('Tên bàn'), 'Bàn sân vườn')
    await userEvent.type(screen.getByLabelText('Sức chứa'), '6')
    await userEvent.selectOptions(screen.getByLabelText('Trạng thái'), 'Trống')
    await userEvent.type(screen.getByLabelText('Khu vực'), 'Sân vườn')
    await userEvent.click(screen.getByRole('button', { name: /lưu bàn/i }))

    expect(create).toHaveBeenCalledWith('staff-token', {
      tableName: 'Bàn sân vườn',
      capacity: 6,
      area: 'Sân vườn',
      description: null,
      status: 'Trống',
    })
    expect(await screen.findByText('Bàn sân vườn')).toBeInTheDocument()
  })

  it('updates and deletes tables with confirmation', async () => {
    vi.spyOn(adminApi, 'listAdminTables').mockResolvedValue([table])
    vi.spyOn(adminApi, 'getAdminTableOptions').mockResolvedValue({ statuses: ['Trống', 'Bảo trì'] })
    const update = vi.spyOn(adminApi, 'updateAdminTable').mockResolvedValue({ ...table, capacity: 5, status: 'Bảo trì' })
    const remove = vi.spyOn(adminApi, 'deleteAdminTable').mockResolvedValue(undefined)

    renderPage()

    expect(await screen.findByText('Bàn 4')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: /sửa bàn 4/i }))
    await userEvent.clear(screen.getByLabelText('Sức chứa'))
    await userEvent.type(screen.getByLabelText('Sức chứa'), '5')
    await userEvent.selectOptions(screen.getByLabelText('Trạng thái'), 'Bảo trì')
    await userEvent.click(screen.getByRole('button', { name: /lưu bàn/i }))

    expect(update).toHaveBeenCalledWith('staff-token', 4, expect.objectContaining({
      capacity: 5,
      status: 'Bảo trì',
    }))

    await userEvent.click(screen.getByRole('button', { name: /xóa bàn 4/i }))
    await userEvent.click(screen.getByRole('button', { name: /^xóa bàn$/i }))

    expect(remove).toHaveBeenCalledWith('staff-token', 4)
  })
})