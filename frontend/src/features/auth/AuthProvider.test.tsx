import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/httpClient'
import type { AuthGateway } from './authApi'
import { AuthProvider } from './AuthProvider'
import { useAuth } from './useAuth'

const customer = {
  userId: 1,
  name: 'Minh Anh',
  email: 'minh@example.com',
  phoneNumber: '0900000001',
  address: null,
  role: 'Customer' as const,
  isActive: true,
  isEmailVerified: false,
}

const loginResponse = {
  accessToken: 'signed-token',
  tokenType: 'Bearer' as const,
  expiresAtUtc: '2030-01-01T00:00:00Z',
  user: customer,
}

function createGateway(overrides: Partial<AuthGateway> = {}): AuthGateway {
  return {
    login: vi.fn().mockResolvedValue(loginResponse),
    register: vi.fn().mockResolvedValue(customer),
    verify: vi.fn().mockResolvedValue({
      user: customer,
      expiresAtUtc: loginResponse.expiresAtUtc,
    }),
    logout: vi.fn().mockResolvedValue(undefined),
    ...overrides,
  }
}

function Probe() {
  const auth = useAuth()
  return (
    <div>
      <span>{auth.status}</span>
      <span>{auth.session?.user.role}</span>
      <button
        type="button"
        onClick={() =>
          auth.login({ email: 'minh@example.com', password: 'Password1' })
        }
      >
        login
      </button>
      <button type="button" onClick={() => auth.logout()}>
        logout
      </button>
    </div>
  )
}

describe('AuthProvider', () => {
  beforeEach(() => localStorage.clear())

  it('logs in and persists the session', async () => {
    const gateway = createGateway()
    render(
      <AuthProvider gateway={gateway}>
        <Probe />
      </AuthProvider>,
    )

    await userEvent.click(screen.getByRole('button', { name: 'login' }))

    await screen.findByText('Customer')
    expect(gateway.login).toHaveBeenCalledWith({
      email: 'minh@example.com',
      password: 'Password1',
    })
    expect(localStorage.getItem('loafncatting.session')).toContain('signed-token')
  })

  it('verifies a stored token before authenticating', async () => {
    localStorage.setItem(
      'loafncatting.session',
      JSON.stringify({
        token: 'stored-token',
        expiresAtUtc: loginResponse.expiresAtUtc,
        user: customer,
      }),
    )
    const gateway = createGateway()

    render(
      <AuthProvider gateway={gateway}>
        <Probe />
      </AuthProvider>,
    )

    await screen.findByText('Customer')
    expect(gateway.verify).toHaveBeenCalledWith('stored-token')
  })

  it('clears invalid stored sessions', async () => {
    localStorage.setItem(
      'loafncatting.session',
      JSON.stringify({ token: 'expired-token', user: customer }),
    )
    const gateway = createGateway({
      verify: vi.fn().mockRejectedValue(new ApiError(401, 'Unauthorized', 'Expired')),
    })

    render(
      <AuthProvider gateway={gateway}>
        <Probe />
      </AuthProvider>,
    )

    await screen.findByText('unauthenticated')
    expect(localStorage.getItem('loafncatting.session')).toBeNull()
  })

  it('keeps the stored session during a transient verification failure', async () => {
    localStorage.setItem(
      'loafncatting.session',
      JSON.stringify({
        token: 'stored-token',
        expiresAtUtc: loginResponse.expiresAtUtc,
        user: customer,
      }),
    )
    const gateway = createGateway({
      verify: vi.fn().mockRejectedValue(new ApiError(0, 'Connection failed', 'Offline')),
    })

    render(
      <AuthProvider gateway={gateway}>
        <Probe />
      </AuthProvider>,
    )

    await screen.findByText('Customer')
    expect(screen.getByText('authenticated')).toBeInTheDocument()
    expect(localStorage.getItem('loafncatting.session')).toContain('stored-token')
  })

  it('clears local state even when the logout request fails', async () => {
    const gateway = createGateway({
      logout: vi.fn().mockRejectedValue(new Error('offline')),
    })
    render(
      <AuthProvider gateway={gateway}>
        <Probe />
      </AuthProvider>,
    )
    await userEvent.click(screen.getByRole('button', { name: 'login' }))
    await screen.findByText('Customer')

    await userEvent.click(screen.getByRole('button', { name: 'logout' }))

    await waitFor(() =>
      expect(screen.getByText('unauthenticated')).toBeInTheDocument(),
    )
    expect(localStorage.getItem('loafncatting.session')).toBeNull()
  })
})
