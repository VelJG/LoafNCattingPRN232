import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import * as adminApi from '../../features/admin/adminApi'
import type { AdminCat } from '../../features/admin/adminTypes'
import { AuthContext, type AuthContextValue } from '../../features/auth/AuthProvider'
import { AdminCatsPage } from './AdminCatsPage'

const auth: AuthContextValue = {
  status: 'authenticated',
  session: { token: 'staff-token', expiresAtUtc: '2030-01-01T00:00:00Z', user: { userId: 2, name: 'Linh', email: 'linh@loaf.vn', phoneNumber: '0900', address: null, role: 'Staff', isActive: true, isEmailVerified: true } },
  login: vi.fn(), register: vi.fn(), logout: vi.fn(),
}

const cat: AdminCat = {
  catId: 1,
  name: 'Mochi',
  age: 2,
  gender: 'Female',
  breed: 'British Shorthair',
  picture: null,
  description: null,
  friendlinessRating: 5,
  cutenessRating: 5,
  playfulnessRating: 4,
  status: 'At cafe',
  createdAt: '2026-07-23T00:00:00Z',
  updatedAt: null,
}

function renderPage() {
  return render(<AuthContext.Provider value={auth}><MemoryRouter><AdminCatsPage /></MemoryRouter></AuthContext.Provider>)
}

function mockLoad(cats: AdminCat[] = [cat]) {
  vi.spyOn(adminApi, 'listAdminCats').mockResolvedValue(cats)
  vi.spyOn(adminApi, 'getAdminCatOptions').mockResolvedValue({
    statuses: ['At cafe', 'Adopted'],
    genders: ['Female', 'Male'],
  })
}

afterEach(() => vi.restoreAllMocks())

describe('AdminCatsPage', () => {
  it('shows database status and gender names in the add form', async () => {
    mockLoad()
    renderPage()

    await userEvent.click(await screen.findByRole('button', { name: /add cat/i }))

    expect(screen.getByRole('option', { name: 'At cafe' })).toBeInTheDocument()
    expect(screen.getByRole('option', { name: 'Adopted' })).toBeInTheDocument()
    expect(screen.getByRole('option', { name: 'Female' })).toBeInTheDocument()
    expect(screen.getByLabelText('Mức độ thân thiện, tối đa 5')).toHaveAttribute('max', '5')
  })

  it('creates a cat with semantic status and gender values', async () => {
    mockLoad([])
    const create = vi.spyOn(adminApi, 'createAdminCat').mockResolvedValue(cat)
    renderPage()

    await userEvent.click(await screen.findByRole('button', { name: /add cat/i }))
    await userEvent.type(screen.getByLabelText('Tên mèo'), 'Mochi')
    await userEvent.selectOptions(screen.getByLabelText('Trạng thái'), 'At cafe')
    await userEvent.selectOptions(screen.getByLabelText('Giới tính'), 'Female')
    await userEvent.type(screen.getByLabelText('Mức độ thân thiện, tối đa 5'), '5')
    await userEvent.click(screen.getByRole('button', { name: /lưu mèo/i }))

    expect(create).toHaveBeenCalledWith('staff-token', expect.objectContaining({
      name: 'Mochi',
      status: 'At cafe',
      gender: 'Female',
      friendlinessRating: 5,
    }))
  })
})
