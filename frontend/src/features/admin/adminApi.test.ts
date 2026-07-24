import { afterEach, describe, expect, it, vi } from 'vitest'
import {
  createAdminProduct,
  createAdminTable,
  createAdminUser,
  createStaff,
  deleteAdminProduct,
  deleteAdminTable,
  deleteAdminUser,
  getAdminTableOptions,
  getAdminUserOptions,
  listAdminProducts,
  listAdminTables,
  listAdminUsers,
  listOrders,
  listStoreReservations,
  transitionReservation,
  updateAdminProduct,
  updateAdminTable,
  updateAdminUser,
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

  it('uses admin user CRUD and options endpoints', async () => {
    const input = {
      name: 'Hà Linh',
      email: 'linh@loaf.vn',
      password: 'Password1',
      phoneNumber: '0900000002',
      address: null,
      avatarUrl: null,
      role: 'Staff',
      isActive: true,
      isEmailVerified: false,
    }
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(jsonResponse([]))
      .mockResolvedValueOnce(jsonResponse({ roles: ['Admin', 'Staff'] }))
      .mockResolvedValueOnce(jsonResponse({ userId: 11, ...input }, 201))
      .mockResolvedValueOnce(jsonResponse({ userId: 11, ...input }))
      .mockResolvedValueOnce(new Response(null, { status: 204 }))
    vi.stubGlobal('fetch', fetchMock)

    await listAdminUsers('admin-token')
    await getAdminUserOptions('admin-token')
    await createAdminUser('admin-token', input)
    await updateAdminUser('admin-token', 11, input)
    await deleteAdminUser('admin-token', 11)

    expect(fetchMock).toHaveBeenNthCalledWith(1, '/api/admin/users', expect.objectContaining({ method: 'GET' }))
    expect(fetchMock).toHaveBeenNthCalledWith(2, '/api/admin/users/options', expect.objectContaining({ method: 'GET' }))
    expect(fetchMock).toHaveBeenNthCalledWith(3, '/api/admin/users', expect.objectContaining({ method: 'POST', body: JSON.stringify(input) }))
    expect(fetchMock).toHaveBeenNthCalledWith(4, '/api/admin/users/11', expect.objectContaining({ method: 'PUT' }))
    expect(fetchMock).toHaveBeenNthCalledWith(5, '/api/admin/users/11', expect.objectContaining({ method: 'DELETE' }))
  })

  it('uses admin table CRUD and options endpoints', async () => {
    const input = {
      tableName: 'Bàn 4',
      capacity: 4,
      area: 'Tầng 1',
      description: null,
      status: 'Trống',
    }
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(jsonResponse([]))
      .mockResolvedValueOnce(jsonResponse({ statuses: ['Trống'] }))
      .mockResolvedValueOnce(jsonResponse({ tableId: 4, ...input }, 201))
      .mockResolvedValueOnce(jsonResponse({ tableId: 4, ...input }))
      .mockResolvedValueOnce(new Response(null, { status: 204 }))
    vi.stubGlobal('fetch', fetchMock)

    await listAdminTables('staff-token')
    await getAdminTableOptions('staff-token')
    await createAdminTable('staff-token', input)
    await updateAdminTable('staff-token', 4, input)
    await deleteAdminTable('staff-token', 4)

    expect(fetchMock).toHaveBeenNthCalledWith(1, '/api/admin/tables', expect.objectContaining({ method: 'GET' }))
    expect(fetchMock).toHaveBeenNthCalledWith(2, '/api/admin/tables/options', expect.objectContaining({ method: 'GET' }))
    expect(fetchMock).toHaveBeenNthCalledWith(3, '/api/admin/tables', expect.objectContaining({ method: 'POST', body: JSON.stringify(input) }))
    expect(fetchMock).toHaveBeenNthCalledWith(4, '/api/admin/tables/4', expect.objectContaining({ method: 'PUT' }))
    expect(fetchMock).toHaveBeenNthCalledWith(5, '/api/admin/tables/4', expect.objectContaining({ method: 'DELETE' }))
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