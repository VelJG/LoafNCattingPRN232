import { useState, type FormEvent } from 'react'
import { MdSend } from 'react-icons/md'
import { useLocalChat } from '../../features/chat/useLocalChat'

export function ChatPage() {
  const { messages, send } = useLocalChat()
  const [draft, setDraft] = useState('')

  const submit = (event: FormEvent) => {
    event.preventDefault()
    if (send(draft)) setDraft('')
  }

  return (
    <section className="customer-v2-page chat-v2-page">
      <h1>Trò chuyện với quán</h1>
      <div className="chat-v2-messages" aria-live="polite">
        {messages.map((message) => (
          <div className={`chat-v2-line chat-v2-line--${message.sender}`} key={message.id}>
            <div className="chat-v2-bubble">{message.text}</div>
          </div>
        ))}
      </div>
      <form className="chat-v2-composer" onSubmit={submit}>
        <input placeholder="Nhập tin nhắn..." value={draft} onChange={(event) => setDraft(event.target.value)} />
        <button type="submit" aria-label="Gửi tin nhắn"><MdSend aria-hidden="true" /></button>
      </form>
    </section>
  )
}
