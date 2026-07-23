import { act, fireEvent, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AuthContext, type AuthContextValue } from '../../features/auth/AuthProvider'
import * as notificationApi from '../../features/notifications/notificationApi'
import { ChatPage } from './ChatPage'
import { LocationPage } from './LocationPage'
import { NotificationsPage } from './NotificationsPage'

const auth: AuthContextValue = {
  status: 'authenticated',
  session: {
    token: 'customer-token',
    expiresAtUtc: '2030-01-01T00:00:00Z',
    user: {
      userId: 7, name: 'Minh Anh', email: 'minh@example.com', phoneNumber: '0900000001',
      address: null, role: 'Customer', isActive: true, isEmailVerified: true,
    },
  },
  login: vi.fn(), register: vi.fn(), logout: vi.fn(),
}

function renderPath(path: '/notifications' | '/location' | '/chat') {
  return render(
    <AuthContext.Provider value={auth}>
      <MemoryRouter initialEntries={[path]}>
        <Routes>
          <Route path="/notifications" element={<NotificationsPage />} />
          <Route path="/location" element={<LocationPage />} />
          <Route path="/chat" element={<ChatPage />} />
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>,
  )
}

afterEach(() => {
  vi.useRealTimers()
  vi.restoreAllMocks()
})

describe('customer supporting screens', () => {
  it('loads notifications and connects row/read-all actions to the API', async () => {
    const unread = {
      notificationId: 3,
      title: 'Bàn đã được xác nhận',
      content: 'Hẹn gặp bạn lúc 18:00.',
      type: 'ReservationConfirmed',
      isRead: false,
      createdAtUtc: '2026-07-22T14:00:00Z',
    }
    vi.spyOn(notificationApi, 'listNotifications').mockResolvedValue([unread])
    const markOne = vi.spyOn(notificationApi, 'markNotificationRead')
      .mockResolvedValue({ ...unread, isRead: true })
    const markAll = vi.spyOn(notificationApi, 'markAllNotificationsRead')
      .mockResolvedValue({ updatedCount: 1 })
    renderPath('/notifications')

    expect(await screen.findByText('Bàn đã được xác nhận')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: /đánh dấu bàn đã được xác nhận là đã đọc/i }))
    expect(markOne).toHaveBeenCalledWith(3, 'customer-token')

    vi.spyOn(notificationApi, 'listNotifications').mockResolvedValue([{ ...unread, isRead: false }])
    await userEvent.click(screen.getByRole('button', { name: /đánh dấu tất cả đã đọc/i }))
    expect(markAll).toHaveBeenCalledWith('customer-token')
  })

  it('renders the exact cafe details and an external directions target', () => {
    renderPath('/location')

    expect(screen.getByText('128 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /mở chỉ đường/i })).toHaveAttribute(
      'href', expect.stringContaining('google.com/maps'),
    )
  })

  it('adds a trimmed customer message and the prototype reply', async () => {
    vi.useFakeTimers()
    renderPath('/chat')

    expect(screen.getByText('Chào bạn! Quán mình có thể giúp gì cho bạn hôm nay ạ?')).toBeInTheDocument()
    fireEvent.change(screen.getByPlaceholderText('Nhập tin nhắn...'), { target: { value: '  Mình muốn đặt bàn  ' } })
    fireEvent.click(screen.getByRole('button', { name: /gửi tin nhắn/i }))
    expect(screen.getByText('Mình muốn đặt bàn')).toBeInTheDocument()

    await act(async () => { vi.advanceTimersByTime(700) })
    expect(screen.getByText('Cảm ơn bạn đã nhắn tin! Nhân viên sẽ phản hồi ngay ạ. 🐾')).toBeInTheDocument()
  })
})
