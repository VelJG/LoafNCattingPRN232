import { requestJson } from '../../api/httpClient'
import type {
  CustomerConversationDto,
  MessageDto,
  StoreConversationDto,
} from './chatTypes'

interface SendMessageInput {
  content: string
}

export const getMyConversation = (token: string, signal?: AbortSignal) =>
  requestJson<CustomerConversationDto>('/conversations/mine', { token, signal })

export const getMyMessages = (token: string, signal?: AbortSignal) =>
  requestJson<MessageDto[]>('/conversations/mine/messages', { token, signal })

export const sendMyMessage = (
  token: string,
  content: string,
) => requestJson<MessageDto>('/conversations/mine/messages', {
  method: 'POST',
  token,
  body: { content } satisfies SendMessageInput,
})

export const markMyMessagesRead = (token: string) =>
  requestJson<void>('/conversations/mine/messages/read', {
    method: 'PATCH',
    token,
  })

export const listStoreConversations = (token: string, signal?: AbortSignal) =>
  requestJson<StoreConversationDto[]>('/store/conversations', {
    token,
    signal,
  })

export const getStoreConversationMessages = (
  token: string,
  conversationId: number,
  signal?: AbortSignal,
) => requestJson<MessageDto[]>(`/store/conversations/${conversationId}/messages`, {
  token,
  signal,
})

export const sendStoreConversationMessage = (
  token: string,
  conversationId: number,
  content: string,
) => requestJson<MessageDto>(`/store/conversations/${conversationId}/messages`, {
  method: 'POST',
  token,
  body: { content } satisfies SendMessageInput,
})

export const markStoreConversationRead = (
  token: string,
  conversationId: number,
) => requestJson<void>(`/store/conversations/${conversationId}/messages/read`, {
  method: 'PATCH',
  token,
})
