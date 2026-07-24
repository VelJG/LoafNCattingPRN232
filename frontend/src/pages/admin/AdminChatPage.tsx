import { useEffect, useRef, useState, type FormEvent } from 'react'
import { MdChatBubbleOutline, MdSend } from 'react-icons/md'
import { AdminFeedback } from '../../features/admin/components/AdminFeedback'
import { useAuth } from '../../features/auth/useAuth'
import { useAdminChat } from '../../features/chat/useAdminChat'
import type { ChatRealtimeStatus, MessageDto } from '../../features/chat/chatTypes'

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

function senderClass(message: MessageDto, currentUserId: number) {
  return message.senderUserId === currentUserId
    ? 'admin'
    : 'customer'
}

export function AdminChatPage() {
  const session = useAuth().session
  const token = session?.token ?? ''
  const currentUserId = session?.user.userId ?? 0
  const {
    conversations,
    activeConversation,
    activeConversationId,
    messages,
    loadingConversations,
    loadingMessages,
    sending,
    conversationError,
    messageError,
    realtimeStatus,
    reload,
    selectConversation,
    send,
  } = useAdminChat(token)
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
    <section className="admin-page admin-chat-page">
      <div className="admin-chat-shell">
        <aside className="admin-dashboard-panel admin-chat-sidebar">
          <header>
            <h2>Hộp thư khách hàng</h2>
            <span className={`admin-chat-status admin-chat-status--${realtimeStatus}`}>
              {statusLabel(realtimeStatus)}
            </span>
          </header>
          {loadingConversations ? (
            <AdminFeedback state="loading" />
          ) : conversationError ? (
            <AdminFeedback
              state="error"
              title="Không thể tải hội thoại"
              message={conversationError}
              onRetry={reload}
            />
          ) : conversations.length === 0 ? (
            <AdminFeedback
              state="empty"
              title="Chưa có cuộc trò chuyện nào"
              message="Tin nhắn mới từ khách hàng sẽ xuất hiện tại đây."
            />
          ) : (
            <div className="admin-chat-conversations" role="list" aria-label="Danh sách hội thoại">
              {conversations.map((conversation) => (
                <button
                  key={conversation.conversationId}
                  className={conversation.conversationId === activeConversationId
                    ? 'admin-chat-conversation admin-chat-conversation--active'
                    : 'admin-chat-conversation'}
                  type="button"
                  onClick={() => selectConversation(conversation.conversationId)}
                >
                  <span className="admin-chat-conversation__avatar">
                    {conversation.customerName.slice(0, 1).toUpperCase()}
                  </span>
                  <span className="admin-chat-conversation__copy">
                    <strong>{conversation.customerName}</strong>
                    <span>{conversation.lastMessageContent ?? 'Khách hàng chưa nhắn tin.'}</span>
                  </span>
                  <span className="admin-chat-conversation__meta">
                    {conversation.lastMessageAtUtc && (
                      <time>{formatTimestamp(conversation.lastMessageAtUtc)}</time>
                    )}
                    {conversation.unreadCustomerMessageCount > 0 && (
                      <b>{conversation.unreadCustomerMessageCount}</b>
                    )}
                  </span>
                </button>
              ))}
            </div>
          )}
        </aside>

        <section className="admin-dashboard-panel admin-chat-thread">
          <header>
            <h2>{activeConversation?.customerName ?? 'Chọn hội thoại'}</h2>
            {activeConversation && (
              <span>
                #{activeConversation.conversationId} · {activeConversation.customerName}
              </span>
            )}
          </header>

          {!activeConversation && !loadingConversations ? (
            <AdminFeedback
              state="empty"
              title="Chưa chọn hội thoại"
              message="Hãy chọn một khách hàng ở cột bên trái để đọc và trả lời tin nhắn."
            />
          ) : loadingMessages ? (
            <AdminFeedback state="loading" />
          ) : messageError && messages.length === 0 ? (
            <AdminFeedback
              state="error"
              title="Không thể tải tin nhắn"
              message={messageError}
              onRetry={reload}
            />
          ) : (
            <>
              <div className="admin-chat-messages" aria-live="polite">
                {messages.length === 0 ? (
                  <div className="admin-chat-empty-thread">
                    <MdChatBubbleOutline aria-hidden="true" />
                    <div>
                      <h3>Chưa có nội dung trao đổi</h3>
                      <p>Khách hàng sẽ xuất hiện ở đây ngay khi cuộc trò chuyện bắt đầu.</p>
                    </div>
                  </div>
                ) : messages.map((message) => (
                  <div
                    className={`admin-chat-line admin-chat-line--${senderClass(message, currentUserId)}`}
                    key={message.messageId}
                  >
                    <div className="admin-chat-bubble">
                      <small>{message.senderName} · {formatTimestamp(message.sentAtUtc)}</small>
                      <p>{message.content}</p>
                    </div>
                  </div>
                ))}
                <div ref={bottomRef} />
              </div>
              <form className="admin-chat-composer" onSubmit={(event) => void submit(event)}>
                <input
                  placeholder={activeConversation
                    ? `Nhắn ${activeConversation.customerName}...`
                    : 'Chọn hội thoại để trả lời'}
                  value={draft}
                  onChange={(event) => setDraft(event.target.value)}
                  disabled={!activeConversation || sending}
                />
                <button
                  type="submit"
                  aria-label="Gửi phản hồi"
                  disabled={!activeConversation || sending}
                >
                  <MdSend aria-hidden="true" />
                </button>
              </form>
              {messageError && (
                <p className="admin-chat-error" role="alert">{messageError}</p>
              )}
            </>
          )}
        </section>
      </div>
    </section>
  )
}
