import type { MessageDto, StoreConversationDto } from './chatTypes'

function toTime(value: string | null | undefined) {
  if (!value) return 0
  const time = Date.parse(value)
  return Number.isNaN(time) ? 0 : time
}

export function sortMessages(messages: MessageDto[]) {
  return [...messages].sort((left, right) =>
    toTime(left.sentAtUtc) - toTime(right.sentAtUtc) ||
    left.messageId - right.messageId)
}

export function upsertMessage(messages: MessageDto[], message: MessageDto) {
  const existing = messages.findIndex((candidate) => candidate.messageId === message.messageId)
  if (existing === -1) {
    return sortMessages([...messages, message])
  }

  const next = [...messages]
  next[existing] = message
  return sortMessages(next)
}

function conversationActivityTime(conversation: StoreConversationDto) {
  return toTime(conversation.lastMessageAtUtc)
    || toTime(conversation.updatedAtUtc)
    || toTime(conversation.createdAtUtc)
}

export function sortStoreConversations(conversations: StoreConversationDto[]) {
  return [...conversations].sort((left, right) =>
    conversationActivityTime(right) - conversationActivityTime(left) ||
    right.conversationId - left.conversationId)
}

export function clearConversationUnread(
  conversations: StoreConversationDto[],
  conversationId: number,
) {
  return conversations.map((conversation) =>
    conversation.conversationId === conversationId
      ? { ...conversation, unreadCustomerMessageCount: 0 }
      : conversation)
}

export function bumpConversationWithMessage(
  conversations: StoreConversationDto[],
  message: MessageDto,
  activeConversationId: number | null,
) {
  const existing = conversations.find(
    (conversation) => conversation.conversationId === message.conversationId,
  )
  if (!existing) return conversations

  const unreadCustomerMessageCount = message.senderRole === 'Customer' &&
    message.conversationId !== activeConversationId
    ? existing.unreadCustomerMessageCount + 1
    : 0

  const updated: StoreConversationDto = {
    ...existing,
    updatedAtUtc: message.sentAtUtc,
    lastMessageAtUtc: message.sentAtUtc,
    lastMessageContent: message.content,
    lastMessageSenderRole: message.senderRole,
    unreadCustomerMessageCount,
  }

  return sortStoreConversations([
    updated,
    ...conversations.filter(
      (conversation) => conversation.conversationId !== updated.conversationId,
    ),
  ])
}
