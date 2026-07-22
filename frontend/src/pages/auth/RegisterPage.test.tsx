import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/httpClient'
import { AuthContext, type AuthContextValue } from '../../features/auth/AuthProvider'
import { RegisterPage } from './RegisterPage'

function LoginDestination() {
  const location = useLocation()
  const state = location.state as { registeredEmail?: string } | null
  return <p>Login for {state?.registeredEmail}</p>
}

function renderRegister(register: AuthContextValue['register']) {
  const value: AuthContextValue = {
    status: 'unauthenticated',
    session: null,
    login: vi.fn(),
    register,
    logout: vi.fn(),
  }
  return render(
    <AuthContext.Provider value={value}>
      <MemoryRouter initialEntries={['/register']}>
        <Routes>
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/login" element={<LoginDestination />} />
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>,
  )
}

async function fillRequiredFields() {
  await userEvent.type(screen.getByLabelText('Họ và tên'), 'Minh Anh')
  await userEvent.type(screen.getByLabelText('Email'), 'minh@example.com')
  await userEvent.type(screen.getByLabelText('Số điện thoại'), '0900000001')
  await userEvent.type(screen.getByLabelText('Mật khẩu'), 'Password1')
}

describe('RegisterPage', () => {
  it('sends the exact backend contract and returns to login', async () => {
    const register = vi.fn().mockResolvedValue({ userId: 1 })
    renderRegister(register)
    await fillRequiredFields()

    await userEvent.click(screen.getByRole('button', { name: 'Tạo tài khoản' }))

    expect(await screen.findByText('Login for minh@example.com')).toBeInTheDocument()
    expect(register).toHaveBeenCalledWith({
      name: 'Minh Anh',
      email: 'minh@example.com',
      phoneNumber: '0900000001',
      password: 'Password1',
      address: null,
    })
  })

  it('requires a password of at least eight characters', async () => {
    const register = vi.fn()
    renderRegister(register)
    await userEvent.type(screen.getByLabelText('Mật khẩu'), '1234567')

    await userEvent.click(screen.getByRole('button', { name: 'Tạo tài khoản' }))

    expect(screen.getByText('Mật khẩu cần ít nhất 8 ký tự.')).toBeInTheDocument()
    expect(register).not.toHaveBeenCalled()
  })

  it('shows duplicate account conflicts from the backend', async () => {
    const register = vi
      .fn()
      .mockRejectedValue(
        new ApiError(409, 'Request conflict', 'Email is already in use.'),
      )
    renderRegister(register)
    await fillRequiredFields()

    await userEvent.click(screen.getByRole('button', { name: 'Tạo tài khoản' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Email is already in use.',
    )
  })
})
