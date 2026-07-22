import { afterEach, describe, expect, it, vi } from 'vitest'
import { authApi } from './authApi'

const user = {
  userId: 7,
  name: 'Minh Anh',
  email: 'minh@example.com',
  phoneNumber: '0900000001',
  address: null,
  role: 'Customer',
  isActive: true,
  isEmailVerified: false,
}

describe('authApi', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('posts credentials to login', async () => {
    const response = {
      accessToken: 'token',
      tokenType: 'Bearer',
      expiresAtUtc: '2030-01-01T00:00:00Z',
      user,
    }
    const fetchMock = vi
      .fn()
      .mockResolvedValue(new Response(JSON.stringify(response), { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)

    await authApi.login({ email: 'minh@example.com', password: 'Password1' })

    expect(fetchMock).toHaveBeenCalledWith(
      'http://localhost:5053/api/auth/login',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ email: 'minh@example.com', password: 'Password1' }),
      }),
    )
  })

  it('posts the exact customer registration contract', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValue(new Response(JSON.stringify(user), { status: 201 }))
    vi.stubGlobal('fetch', fetchMock)
    const request = {
      name: 'Minh Anh',
      email: 'minh@example.com',
      password: 'Password1',
      phoneNumber: '0900000001',
      address: null,
    }

    await authApi.register(request)

    expect(fetchMock).toHaveBeenCalledWith(
      'http://localhost:5053/api/auth/register',
      expect.objectContaining({ method: 'POST', body: JSON.stringify(request) }),
    )
  })

  it('verifies and logs out with the bearer token', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(
        new Response(
          JSON.stringify({ user, expiresAtUtc: '2030-01-01T00:00:00Z' }),
          { status: 200 },
        ),
      )
      .mockResolvedValueOnce(new Response(null, { status: 204 }))
    vi.stubGlobal('fetch', fetchMock)

    await authApi.verify('signed-token')
    await authApi.logout('signed-token')

    expect(fetchMock).toHaveBeenNthCalledWith(
      1,
      'http://localhost:5053/api/auth/verify',
      expect.objectContaining({
        headers: expect.objectContaining({ Authorization: 'Bearer signed-token' }),
      }),
    )
    expect(fetchMock).toHaveBeenNthCalledWith(
      2,
      'http://localhost:5053/api/auth/logout',
      expect.objectContaining({
        method: 'POST',
        headers: expect.objectContaining({ Authorization: 'Bearer signed-token' }),
      }),
    )
  })
})
