import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError, requestJson } from './httpClient'

describe('requestJson', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('adds JSON headers and returns a typed response', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ ok: true }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    )
    vi.stubGlobal('fetch', fetchMock)

    await expect(
      requestJson<{ ok: boolean }>('/auth/login', {
        method: 'POST',
        body: { email: 'cat@example.com' },
      }),
    ).resolves.toEqual({ ok: true })
    expect(fetchMock).toHaveBeenCalledWith(
      'http://localhost:5053/api/auth/login',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ email: 'cat@example.com' }),
      }),
    )
  })

  it('normalizes ProblemDetails into ApiError', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        new Response(
          JSON.stringify({
            status: 401,
            title: 'Unauthorized',
            detail: 'Invalid email or password.',
          }),
          { status: 401, headers: { 'Content-Type': 'application/problem+json' } },
        ),
      ),
    )

    await expect(requestJson('/auth/login')).rejects.toEqual(
      new ApiError(401, 'Unauthorized', 'Invalid email or password.'),
    )
  })

  it('returns undefined for a successful empty response', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 204 })))

    await expect(
      requestJson<void>('/auth/logout', { method: 'POST' }),
    ).resolves.toBeUndefined()
  })

  it('normalizes network failures', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))

    await expect(requestJson('/auth/login')).rejects.toEqual(
      new ApiError(0, 'Connection failed', 'Could not reach the server. Please try again.'),
    )
  })
})
