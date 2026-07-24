import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr'

export function createMessageHubConnection(token: string) {
  return new HubConnectionBuilder()
    .withUrl('/hubs/messages', {
      accessTokenFactory: () => token,
    })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build()
}

export async function startHubConnection(connection: HubConnection) {
  if (
    connection.state === HubConnectionState.Connected ||
    connection.state === HubConnectionState.Connecting ||
    connection.state === HubConnectionState.Reconnecting
  ) {
    return
  }

  await connection.start()
}
