import { afterEach, describe, expect, it, vi } from 'vitest'
import { cartApi } from './cartApi'

const jsonResponse = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })

describe('cartApi', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('uses bearer auth for the current customer cart', async () => {
    const fetchMock = vi.fn()
      .mockImplementation(() => Promise.resolve(jsonResponse({ items: [] })))
    vi.stubGlobal('fetch', fetchMock)

    await cartApi.get('customer-token')
    await cartApi.add('customer-token', 8, 2)

    expect(fetchMock).toHaveBeenNthCalledWith(1, '/api/cart', expect.objectContaining({
      headers: expect.objectContaining({ Authorization: 'Bearer customer-token' }),
    }))
    expect(fetchMock).toHaveBeenNthCalledWith(2, '/api/cart/items', expect.objectContaining({
      method: 'POST',
      body: JSON.stringify({ productId: 8, quantity: 2 }),
    }))
  })

  it('checks out without sending a user id', async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse({ orderId: 10 }))
    vi.stubGlobal('fetch', fetchMock)

    await cartApi.checkout('customer-token', {
      orderType: 'Takeaway',
      tableId: null,
      reservationId: null,
      paymentMethodId: 1,
      note: null,
    })

    expect(fetchMock).toHaveBeenCalledWith('/api/orders/checkout', expect.objectContaining({
      method: 'POST',
      body: JSON.stringify({
        orderType: 'Takeaway',
        tableId: null,
        reservationId: null,
        paymentMethodId: 1,
        note: null,
      }),
    }))
    expect(fetchMock.mock.calls[0][1].body).not.toContain('userId')
  })
})
