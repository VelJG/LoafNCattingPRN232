import { afterEach, describe, expect, it, vi } from 'vitest'
import {
  listNotifications,
  markAllNotificationsRead,
  markNotificationRead,
} from './notificationApi'

describe('notificationApi', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('lists the authenticated customer notifications', async () => {
    const notifications = [{
      notificationId: 3,
      title: 'Bàn đã được xác nhận',
      content: 'Hẹn gặp bạn lúc 18:00.',
      type: 'ReservationConfirmed',
      isRead: false,
      createdAtUtc: '2026-07-22T14:00:00Z',
    }]
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify(notifications), { status: 200 }),
    )
    vi.stubGlobal('fetch', fetchMock)

    const result = await listNotifications('customer-token')

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/notifications',
      expect.objectContaining({
        method: 'GET',
        headers: expect.objectContaining({ Authorization: 'Bearer customer-token' }),
      }),
    )
    expect(result).toEqual(notifications)
  })

  it('marks one notification and then all notifications as read', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(new Response(JSON.stringify({
        notificationId: 3,
        title: 'Bàn đã được xác nhận',
        content: 'Hẹn gặp bạn lúc 18:00.',
        type: 'ReservationConfirmed',
        isRead: true,
        createdAtUtc: '2026-07-22T14:00:00Z',
      }), { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ updatedCount: 2 }), { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)

    await markNotificationRead(3, 'customer-token')
    const allResult = await markAllNotificationsRead('customer-token')

    expect(fetchMock).toHaveBeenNthCalledWith(
      1,
      '/api/notifications/3/read',
      expect.objectContaining({
        method: 'PATCH',
        headers: expect.objectContaining({ Authorization: 'Bearer customer-token' }),
      }),
    )
    expect(fetchMock).toHaveBeenNthCalledWith(
      2,
      '/api/notifications/read-all',
      expect.objectContaining({ method: 'PATCH' }),
    )
    expect(allResult.updatedCount).toBe(2)
  })
})
