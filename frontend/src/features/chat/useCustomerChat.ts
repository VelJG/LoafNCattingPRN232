import { useCallback, useEffect, useState } from 'react'
import { ApiError } from '../../api/httpClient'
import {
  getMyConversation,
  getMyMessages,
  markMyMessagesRead,
  sendMyMessage,
} from './chatApi'
import { createMessageHubConnection, startHubConnection } from './chatSignalR'
import type {
  ChatRealtimeStatus,
  CustomerConversationDto,
  MessageCreatedRealtimeDto,
  MessageDto,
  MessagesReadRealtimeDto,
} from './chatTypes'
import { messageRealtimeEvents } from './chatTypes'
import { sortMessages, upsertMessage } from './chatUtils'

export function useCustomerChat(token: string, userId: number) {
  const [conversation, setConversation] = useState<CustomerConversationDto['conversation']>(null)
  const [messages, setMessages] = useState<MessageDto[]>([])
  const [loading, setLoading] = useState(true)
  const [sending, setSending] = useState(false)
  const [error, setError] = useState('')
  const [realtimeStatus, setRealtimeStatus] = useState<ChatRealtimeStatus>('disconnected')
  const [reloadKey, setReloadKey] = useState(0)

  const appendMessage = useCallback((message: MessageDto) => {
    setMessages((current) => upsertMessage(current, message))
  }, [])

  const reload = useCallback(() => setReloadKey((value) => value + 1), [])

  useEffect(() => {
    if (!token) {
      setConversation(null)
      setMessages([])
      setLoading(false)
      setError('')
      return
    }

    const controller = new AbortController()
    let alive = true

    setLoading(true)
    setError('')

    Promise.all([
      getMyConversation(token, controller.signal),
      getMyMessages(token, controller.signal),
    ])
      .then(([conversationResult, messageResult]) => {
        if (!alive) return
        setConversation(conversationResult.conversation)
        setMessages(sortMessages(messageResult))
        if (messageResult.some((message) =>
          message.senderRole !== 'Customer' && !message.isRead
        )) {
          void markMyMessagesRead(token).catch(() => undefined)
        }
      })
      .catch((caught) => {
        if (!alive) return
        setError(caught instanceof ApiError ? caught.detail : 'Không thể tải cuộc trò chuyện.')
      })
      .finally(() => {
        if (alive) setLoading(false)
      })

    return () => {
      alive = false
      controller.abort()
    }
  }, [reloadKey, token])

  useEffect(() => {
    if (!token || !userId) return undefined

    const connection = createMessageHubConnection(token)

    const handleCreated = (event: MessageCreatedRealtimeDto) => {
      if (event.customerUserId !== userId) return
      setConversation((current) => current ?? {
        conversationId: event.message.conversationId,
        customerUserId: userId,
        customerName: event.message.senderRole === 'Customer'
          ? event.message.senderName
          : 'Bạn',
        createdAtUtc: event.message.sentAtUtc,
        updatedAtUtc: event.message.sentAtUtc,
      })
      appendMessage(event.message)
      if (event.message.senderRole !== 'Customer') {
        void markMyMessagesRead(token).catch(() => undefined)
      }
    }

    const handleRead = (event: MessagesReadRealtimeDto) => {
      if (event.readerRole === 'Customer') return
      setMessages((current) => current.map((message) =>
        message.conversationId === event.conversationId &&
          message.senderRole === 'Customer'
          ? { ...message, isRead: true }
          : message,
      ))
    }

    connection.on(messageRealtimeEvents.messageCreated, handleCreated)
    connection.on(messageRealtimeEvents.messagesRead, handleRead)
    connection.onreconnecting(() => setRealtimeStatus('connecting'))
    connection.onreconnected(() => setRealtimeStatus('connected'))
    connection.onclose(() => setRealtimeStatus('disconnected'))

    setRealtimeStatus('connecting')
    void startHubConnection(connection)
      .then(() => setRealtimeStatus('connected'))
      .catch(() => setRealtimeStatus('disconnected'))

    return () => {
      connection.off(messageRealtimeEvents.messageCreated, handleCreated)
      connection.off(messageRealtimeEvents.messagesRead, handleRead)
      void connection.stop()
    }
  }, [appendMessage, token, userId])

  const send = useCallback(async (rawText: string) => {
    const content = rawText.trim()
    if (!content || !token) return false

    setSending(true)
    setError('')
    try {
      const created = await sendMyMessage(token, content)
      setConversation((current) => current ?? {
        conversationId: created.conversationId,
        customerUserId: userId,
        customerName: created.senderRole === 'Customer' ? created.senderName : 'Bạn',
        createdAtUtc: created.sentAtUtc,
        updatedAtUtc: created.sentAtUtc,
      })
      appendMessage(created)
      return true
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.detail : 'Không thể gửi tin nhắn.')
      return false
    } finally {
      setSending(false)
    }
  }, [appendMessage, token, userId])

  return {
    conversation,
    messages,
    loading,
    sending,
    error,
    realtimeStatus,
    reload,
    send,
  }
}
