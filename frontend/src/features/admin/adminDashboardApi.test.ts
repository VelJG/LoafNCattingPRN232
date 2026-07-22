import { afterEach, describe, expect, it, vi } from 'vitest'
import { adminDashboardApi } from './adminDashboardApi'

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('adminDashboardApi', () => {
  it('loads live orders, protected inventory, and cats with the access token', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(new Response(JSON.stringify([{ orderId: 12 }]), { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify([{ productId: 3 }]), { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify([{ catId: 5 }]), { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)

    await expect(adminDashboardApi.load('staff-token')).resolves.toEqual({
      orders: [{ orderId: 12 }],
      products: [{ productId: 3 }],
      cats: [{ catId: 5 }],
    })
    expect(fetchMock).toHaveBeenCalledTimes(3)
    expect(fetchMock.mock.calls[0][0]).toMatch(/\/orders$/)
    expect(fetchMock.mock.calls[1][0]).toMatch(/\/admin\/products$/)
    expect(fetchMock.mock.calls[2][0]).toMatch(/\/cats$/)
    for (const [, options] of fetchMock.mock.calls) {
      expect(options.headers.Authorization).toBe('Bearer staff-token')
    }
  })
})
