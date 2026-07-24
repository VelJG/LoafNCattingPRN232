export type MessageSenderRole = 'Customer' | 'Staff' | 'Admin'

export interface ConversationDto {
  conversationId: number
  customerUserId: number
  customerName: string
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface CustomerConversationDto {
  conversation: ConversationDto | null
}

export interface StoreConversationDto extends ConversationDto {
  lastMessageContent: string | null
  lastMessageAtUtc: string | null
  lastMessageSenderRole: MessageSenderRole | null
  unreadCustomerMessageCount: number
}

export interface MessageDto {
  messageId: number
  conversationId: number
  senderUserId: number
  senderName: string
  senderRole: MessageSenderRole
  content: string
  sentAtUtc: string
  isRead: boolean
}

export interface MessageCreatedRealtimeDto {
  customerUserId: number
  message: MessageDto
}

export interface MessagesReadRealtimeDto {
  conversationId: number
  readerUserId: number
  readerRole: MessageSenderRole
  updatedCount: number
}

export const messageRealtimeEvents = {
  messageCreated: 'MessageCreated',
  messagesRead: 'MessagesRead',
} as const

export type ChatRealtimeStatus = 'connecting' | 'connected' | 'disconnected'
