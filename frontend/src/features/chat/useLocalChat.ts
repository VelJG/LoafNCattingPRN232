import { useEffect, useRef, useState } from 'react'

export interface ChatMessage {
  id: number
  sender: 'customer' | 'staff'
  text: string
}

const initialMessages: ChatMessage[] = [
  { id: 1, sender: 'staff', text: 'Chào bạn! Quán mình có thể giúp gì cho bạn hôm nay ạ?' },
  { id: 2, sender: 'customer', text: 'Quán mấy giờ mở cửa vậy?' },
  { id: 3, sender: 'staff', text: 'Quán mở cửa 08:00 - 22:00 tất cả các ngày trong tuần nhé!' },
]

export function useLocalChat() {
  const [messages, setMessages] = useState<ChatMessage[]>(initialMessages)
  const timer = useRef<number | undefined>(undefined)

  useEffect(() => () => window.clearTimeout(timer.current), [])

  const send = (rawText: string) => {
    const text = rawText.trim()
    if (!text) return false
    setMessages((current) => [...current, { id: Date.now(), sender: 'customer', text }])
    window.clearTimeout(timer.current)
    timer.current = window.setTimeout(() => {
      setMessages((current) => [...current, {
        id: Date.now() + 1,
        sender: 'staff',
        text: 'Cảm ơn bạn đã nhắn tin! Nhân viên sẽ phản hồi ngay ạ. 🐾',
      }])
    }, 700)
    return true
  }

  return { messages, send }
}
