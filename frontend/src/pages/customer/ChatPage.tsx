import { useEffect, useRef, useState, type FormEvent } from 'react'
import {
  MdChatBubbleOutline,
  MdRefresh,
  MdSend,
} from 'react-icons/md'
import { useAuth } from '../../features/auth/useAuth'
import type {
  ChatRealtimeStatus,
  MessageDto,
} from '../../features/chat/chatTypes'
import { useCustomerChat } from '../../features/chat/useCustomerChat'

function formatTimestamp(value: string) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return new Intl.DateTimeFormat('vi-VN', {
    hour: '2-digit',
    minute: '2-digit',
    day: '2-digit',
    month: '2-digit',
  }).format(date)
}

function statusLabel(value: ChatRealtimeStatus) {
  if (value === 'connected') return 'Trực tuyến'
  if (value === 'connecting') return 'Đang kết nối'
  return 'Mất kết nối'
}

function senderClass(message: MessageDto) {
  return message.senderRole === 'Customer' ? 'customer' : 'staff'
}

export function ChatPage() {
  const session = useAuth().session
  const token = session?.token ?? ''
  const userId = session?.user.userId ?? 0
  const {
    messages,
    loading,
    sending,
    error,
    realtimeStatus,
    reload,
    send,
  } = useCustomerChat(token, userId)
  const [draft, setDraft] = useState('')
  const bottomRef = useRef<HTMLDivElement | null>(null)

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ block: 'end' })
  }, [messages])

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    if (await send(draft)) setDraft('')
  }

  return (
    <section className="customer-v2-page chat-v2-page">
      <div className="chat-v2-heading">
        <h1>Trò chuyện với quán</h1>
        <span className={`chat-v2-status chat-v2-status--${realtimeStatus}`}>
          {statusLabel(realtimeStatus)}
        </span>
      </div>
      {loading ? (
        <div
          className="chat-v2-messages chat-v2-messages--loading"
          aria-label="Đang tải trò chuyện"
        />
      ) : error && messages.length === 0 ? (
        <div className="customer-v2-feedback" role="alert">
          <MdChatBubbleOutline />
          <div>
            <h2>Không thể tải cuộc trò chuyện</h2>
            <p>{error}</p>
          </div>
          <button type="button" onClick={reload}>
            <MdRefresh aria-hidden="true" />
            THỬ LẠI
          </button>
        </div>
      ) : (
        <>
          <div className="chat-v2-messages" aria-live="polite">
            {messages.length === 0 ? (
              <div className="chat-v2-empty">
                <MdChatBubbleOutline aria-hidden="true" />
                <div>
                  <h2>Bắt đầu trò chuyện</h2>
                  <p>
                    Nhắn quán để hỏi chỗ ngồi, lịch đặt hoặc bất kỳ điều gì bạn
                    cần.
                  </p>
                </div>
              </div>
            ) : (
              messages.map((message) => (
                <div
                  className={`chat-v2-line chat-v2-line--${senderClass(message)}`}
                  key={message.messageId}
                >
                  <div className="chat-v2-bubble">
                    <small>
                      {message.senderName} · {formatTimestamp(message.sentAtUtc)}
                    </small>
                    <p>{message.content}</p>
                  </div>
                </div>
              ))
            )}
            <div ref={bottomRef} />
          </div>
          <form
            className="chat-v2-composer"
            onSubmit={(event) => void submit(event)}
          >
            <input
              placeholder="Nhập tin nhắn..."
              value={draft}
              onChange={(event) => setDraft(event.target.value)}
              disabled={sending}
            />
            <button
              type="submit"
              aria-label="Gửi tin nhắn"
              disabled={sending}
            >
              <MdSend aria-hidden="true" />
            </button>
          </form>
          {error && (
            <p className="chat-v2-error" role="alert">
              {error}
            </p>
          )}
        </>
      )}
    </section>
  )
}
