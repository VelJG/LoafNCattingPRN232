import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { ApiError } from '../../api/httpClient'
import {
  getStoreConversationMessages,
  listStoreConversations,
  markStoreConversationRead,
  sendStoreConversationMessage,
} from './chatApi'
import { createMessageHubConnection, startHubConnection } from './chatSignalR'
import type {
  ChatRealtimeStatus,
  MessageCreatedRealtimeDto,
  MessageDto,
  MessagesReadRealtimeDto,
  StoreConversationDto,
} from './chatTypes'
import { messageRealtimeEvents } from './chatTypes'
import {
  bumpConversationWithMessage,
  clearConversationUnread,
  sortMessages,
  sortStoreConversations,
  upsertMessage,
} from './chatUtils'

export function useAdminChat(token: string) {
  const [conversations, setConversations] = useState<StoreConversationDto[]>([])
  const [activeConversationId, setActiveConversationId] = useState<number | null>(null)
  const [messages, setMessages] = useState<MessageDto[]>([])
  const [loadingConversations, setLoadingConversations] = useState(true)
  const [loadingMessages, setLoadingMessages] = useState(false)
  const [sending, setSending] = useState(false)
  const [conversationError, setConversationError] = useState('')
  const [messageError, setMessageError] = useState('')
  const [realtimeStatus, setRealtimeStatus] = useState<ChatRealtimeStatus>('disconnected')
  const [reloadKey, setReloadKey] = useState(0)
  const activeConversationIdRef = useRef<number | null>(null)
  const conversationsRef = useRef<StoreConversationDto[]>([])

  useEffect(() => {
    activeConversationIdRef.current = activeConversationId
  }, [activeConversationId])

  useEffect(() => {
    conversationsRef.current = conversations
  }, [conversations])

  const activeConversation = useMemo(
    () => conversations.find((conversation) =>
      conversation.conversationId === activeConversationId) ?? null,
    [activeConversationId, conversations],
  )

  const reload = useCallback(() => setReloadKey((value) => value + 1), [])

  const loadConversations = useCallback(async (signal?: AbortSignal) => {
    if (!token) {
      setConversations([])
      setActiveConversationId(null)
      setLoadingConversations(false)
      return
    }

    setLoadingConversations(true)
    setConversationError('')
    try {
      const loaded = sortStoreConversations(
        await listStoreConversations(token, signal),
      )
      setConversations(loaded)
      setActiveConversationId((current) =>
        current && loaded.some((conversation) => conversation.conversationId === current)
          ? current
          : loaded[0]?.conversationId ?? null)
    } catch (caught) {
      if (signal?.aborted) return
      setConversationError(
        caught instanceof ApiError
          ? caught.detail
          : 'Không thể tải danh sách hội thoại.',
      )
      setConversations([])
      setActiveConversationId(null)
    } finally {
      if (!signal?.aborted) setLoadingConversations(false)
    }
  }, [token])

  const loadMessages = useCallback(async (
    conversationId: number,
    signal?: AbortSignal,
  ) => {
    if (!token) return

    setLoadingMessages(true)
    setMessageError('')
    try {
      const loaded = sortMessages(
        await getStoreConversationMessages(token, conversationId, signal),
      )
      setMessages(loaded)
      if (loaded.some((message) => message.senderRole === 'Customer' && !message.isRead)) {
        void markStoreConversationRead(token, conversationId)
          .then(() => {
            setConversations((current) =>
              clearConversationUnread(current, conversationId))
          })
          .catch(() => undefined)
      }
    } catch (caught) {
      if (signal?.aborted) return
      setMessageError(
        caught instanceof ApiError
          ? caught.detail
          : 'Không thể tải tin nhắn của cuộc trò chuyện này.',
      )
      setMessages([])
    } finally {
      if (!signal?.aborted) setLoadingMessages(false)
    }
  }, [token])

  useEffect(() => {
    const controller = new AbortController()
    void loadConversations(controller.signal)
    return () => controller.abort()
  }, [loadConversations, reloadKey])

  useEffect(() => {
    if (!activeConversationId) {
      setMessages([])
      setLoadingMessages(false)
      setMessageError('')
      return
    }

    const controller = new AbortController()
    void loadMessages(activeConversationId, controller.signal)
    return () => controller.abort()
  }, [activeConversationId, loadMessages, reloadKey])

  useEffect(() => {
    if (!token) return undefined

    const connection = createMessageHubConnection(token)

    const handleCreated = (event: MessageCreatedRealtimeDto) => {
      const incoming = event.message
      if (
        !conversationsRef.current.some(
          (conversation) => conversation.conversationId === incoming.conversationId,
        )
      ) {
        void loadConversations()
      } else {
        setConversations((current) =>
          bumpConversationWithMessage(
            current,
            incoming,
            activeConversationIdRef.current,
          ))
      }

      if (incoming.conversationId === activeConversationIdRef.current) {
        setMessages((current) => upsertMessage(current, incoming))
        if (incoming.senderRole === 'Customer') {
          void markStoreConversationRead(token, incoming.conversationId)
            .then(() => {
              setConversations((current) =>
                clearConversationUnread(current, incoming.conversationId))
            })
            .catch(() => undefined)
        }
      }
    }

    const handleRead = (event: MessagesReadRealtimeDto) => {
      if (event.readerRole === 'Customer') return
      setConversations((current) =>
        clearConversationUnread(current, event.conversationId))
      if (event.conversationId === activeConversationIdRef.current) {
        setMessages((current) => current.map((message) =>
          message.conversationId === event.conversationId &&
            message.senderRole === 'Customer'
            ? { ...message, isRead: true }
            : message,
        ))
      }
    }

    connection.on(messageRealtimeEvents.messageCreated, handleCreated)
    connection.on(messageRealtimeEvents.messagesRead, handleRead)
    connection.onreconnecting(() => setRealtimeStatus('connecting'))
    connection.onreconnected(() => {
      setRealtimeStatus('connected')
      void loadConversations()
      if (activeConversationIdRef.current) {
        void loadMessages(activeConversationIdRef.current)
      }
    })
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
  }, [loadConversations, loadMessages, token])

  const selectConversation = useCallback((conversationId: number) => {
    setActiveConversationId(conversationId)
    setConversations((current) => clearConversationUnread(current, conversationId))
    void markStoreConversationRead(token, conversationId).catch(() => undefined)
  }, [token])

  const send = useCallback(async (rawText: string) => {
    const content = rawText.trim()
    if (!content || !token || !activeConversationIdRef.current) return false

    setSending(true)
    setMessageError('')
    try {
      const created = await sendStoreConversationMessage(
        token,
        activeConversationIdRef.current,
        content,
      )
      setMessages((current) => upsertMessage(current, created))
      setConversations((current) =>
        bumpConversationWithMessage(
          current,
          created,
          activeConversationIdRef.current,
        ))
      return true
    } catch (caught) {
      setMessageError(
        caught instanceof ApiError
          ? caught.detail
          : 'Không thể gửi phản hồi cho khách hàng.',
      )
      return false
    } finally {
      setSending(false)
    }
  }, [token])

  return {
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
  }
}
