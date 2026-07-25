import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr'
import type { CustomerNotification } from './notificationApi'

export interface NotificationChangedRealtimeEvent {
  changeType: string
  notification: CustomerNotification | null
  unreadCount: number
}

export const notificationRealtimeEvents = {
  changed: 'NotificationChanged',
} as const

export const notificationRealtimeChangeTypes = {
  created: 'Created',
  read: 'Read',
  readAll: 'ReadAll',
} as const

export function createNotificationHubConnection(token: string) {
  return new HubConnectionBuilder()
    .withUrl('/hubs/notifications', {
      accessTokenFactory: () => token,
    })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build()
}

export async function startNotificationHubConnection(connection: HubConnection) {
  if (
    connection.state === HubConnectionState.Connected ||
    connection.state === HubConnectionState.Connecting ||
    connection.state === HubConnectionState.Reconnecting
  ) {
    return
  }

  await connection.start()
}
