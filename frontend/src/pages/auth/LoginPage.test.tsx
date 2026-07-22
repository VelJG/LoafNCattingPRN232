import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/httpClient'
import { AuthContext, type AuthContextValue } from '../../features/auth/AuthProvider'
import type { Session, UserRole } from '../../features/auth/authModels'
import { LoginPage } from './LoginPage'

function sessionFor(role: UserRole): Session {
  return {
    token: 'signed-token',
    expiresAtUtc: '2030-01-01T00:00:00Z',
    user: {
      userId: 1,
      name: 'Minh Anh',
      email: 'minh@example.com',
      phoneNumber: '0900000001',
      address: null,
      role,
      isActive: true,
      isEmailVerified: false,
    },
  }
}

function renderLogin(login: AuthContextValue['login']) {
  const value: AuthContextValue = {
    status: 'unauthenticated',
    session: null,
    login,
    register: vi.fn(),
    logout: vi.fn(),
  }
  return render(
    <AuthContext.Provider value={value}>
      <MemoryRouter initialEntries={['/login']}>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/menu" element={<p>Customer destination</p>} />
          <Route path="/admin" element={<p>Admin destination</p>} />
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>,
  )
}

describe('LoginPage', () => {
  it.each([
    ['Customer', 'Customer destination'],
    ['Staff', 'Admin destination'],
    ['Admin', 'Admin destination'],
  ] as const)('routes %s after login', async (role, destination) => {
    const login = vi.fn().mockResolvedValue(sessionFor(role))
    renderLogin(login)

    await userEvent.type(screen.getByLabelText('Email'), 'user@example.com')
    await userEvent.type(screen.getByLabelText('Mật khẩu'), 'Password1')
    await userEvent.click(screen.getByRole('button', { name: 'Đăng nhập' }))

    expect(await screen.findByText(destination)).toBeInTheDocument()
    expect(login).toHaveBeenCalledWith({
      email: 'user@example.com',
      password: 'Password1',
    })
  })

  it('validates fields before calling the backend', async () => {
    const login = vi.fn()
    renderLogin(login)

    await userEvent.click(screen.getByRole('button', { name: 'Đăng nhập' }))

    expect(screen.getByText('Vui lòng nhập email.')).toBeInTheDocument()
    expect(screen.getByText('Vui lòng nhập mật khẩu.')).toBeInTheDocument()
    expect(login).not.toHaveBeenCalled()
  })

  it('shows the backend ProblemDetails message', async () => {
    const login = vi
      .fn()
      .mockRejectedValue(
        new ApiError(401, 'Unauthorized', 'Invalid email or password.'),
      )
    renderLogin(login)

    await userEvent.type(screen.getByLabelText('Email'), 'user@example.com')
    await userEvent.type(screen.getByLabelText('Mật khẩu'), 'wrong')
    await userEvent.click(screen.getByRole('button', { name: 'Đăng nhập' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Invalid email or password.',
    )
  })
})
