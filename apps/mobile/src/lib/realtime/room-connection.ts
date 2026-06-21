import {
  HubConnection,
  HubConnectionBuilder,
  HttpTransportType,
  LogLevel,
} from "@microsoft/signalr";
import { HUB_URL } from "@/lib/env";
import { authStore } from "@/lib/store/auth-store";

/**
 * Builds (but does not start) a SignalR connection to the room hub. React Native
 * supports WebSockets; the access token is supplied per-negotiation and forwarded
 * as the `access_token` query parameter the API reads for `/hubs/room`.
 */
export function createRoomConnection(): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(HUB_URL, {
      accessTokenFactory: () => authStore.getState().accessToken ?? "",
      transport: HttpTransportType.WebSockets | HttpTransportType.LongPolling,
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 15000])
    .configureLogging(LogLevel.Warning)
    .build();
}
