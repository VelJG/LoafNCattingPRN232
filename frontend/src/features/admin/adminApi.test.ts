import { afterEach, describe, expect, it, vi } from 'vitest'
import {
  createAdminProduct,
  createStaff,
  deleteAdminProduct,
  listAdminProducts,
  listOrders,
  listStoreReservations,
  transitionReservation,
  updateAdminProduct,
  updateOrderStatus,
} from './adminApi'

const jsonResponse = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })

describe('adminApi', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('loads the three operational collections with bearer auth', async () => {
    const fetchMock = vi.fn().mockImplementation(() => Promise.resolve(jsonResponse([])))
    vi.stubGlobal('fetch', fetchMock)

    await listOrders('token')
    await listStoreReservations('token')
    await listAdminProducts('token')

    expect(fetchMock).toHaveBeenNthCalledWith(1, '/api/orders', expect.objectContaining({
      headers: expect.objectContaining({ Authorization: 'Bearer token' }),
    }))
    expect(fetchMock).toHaveBeenNthCalledWith(2, '/api/store/reservations', expect.any(Object))
    expect(fetchMock).toHaveBeenNthCalledWith(3, '/api/admin/products', expect.any(Object))
  })

  it('updates an order with the role header required by the backend', async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse({ orderId: 1042 }))
    vi.stubGlobal('fetch', fetchMock)

    await updateOrderStatus('token', 'Staff', 1042, 2)

    expect(fetchMock).toHaveBeenCalledWith('/api/orders/1042/status', expect.objectContaining({
      method: 'PATCH',
      body: JSON.stringify({ orderStatusId: 2 }),
      headers: expect.objectContaining({ 'X-Role': 'Staff' }),
    }))
  })

  it('uses the exact reservation transition endpoint', async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse({ reservationId: 9 }))
    vi.stubGlobal('fetch', fetchMock)

    await transitionReservation('token', 9, 'check-in')

    expect(fetchMock).toHaveBeenCalledWith('/api/store/reservations/9/check-in', expect.objectContaining({
      method: 'PATCH',
    }))
  })

  it('creates, updates, and deletes products with the admin contract', async () => {
    const input = {
      name: 'Latte mèo',
      description: 'Cà phê sữa',
      price: 65000,
      discountPrice: null,
      unitInStock: 8,
      picture: null,
      categoryId: 2,
      isAvailable: true,
    }
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(jsonResponse({ productId: 8, ...input }, 201))
      .mockResolvedValueOnce(jsonResponse({ productId: 8, ...input }))
      .mockResolvedValueOnce(new Response(null, { status: 204 }))
    vi.stubGlobal('fetch', fetchMock)

    await createAdminProduct('token', input)
    await updateAdminProduct('token', 8, input)
    await deleteAdminProduct('token', 8)

    expect(fetchMock).toHaveBeenNthCalledWith(1, '/api/admin/products', expect.objectContaining({ method: 'POST' }))
    expect(fetchMock).toHaveBeenNthCalledWith(2, '/api/admin/products/8', expect.objectContaining({ method: 'PUT' }))
    expect(fetchMock).toHaveBeenNthCalledWith(3, '/api/admin/products/8', expect.objectContaining({ method: 'DELETE' }))
  })

  it('posts the exact create-staff payload', async () => {
    const input = {
      name: 'Hà Linh',
      email: 'linh@loaf.vn',
      password: 'Password1',
      phoneNumber: '0900000002',
      address: null,
    }
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse({ userId: 11, ...input, role: 'Staff' }, 201))
    vi.stubGlobal('fetch', fetchMock)

    await createStaff('admin-token', input)

    expect(fetchMock).toHaveBeenCalledWith('/api/admin/users/staff', expect.objectContaining({
      method: 'POST',
      body: JSON.stringify(input),
    }))
  })
})
