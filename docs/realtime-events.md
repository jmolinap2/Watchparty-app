# Eventos realtime WatchParty

WatchParty usa SignalR para sincronizar sala, presencia, chat y playback. Este documento refleja los nombres observados en:

- `backend/src/WatchParty.Api/Realtime/RoomHub.cs`
- `backend/src/WatchParty.Contracts/Realtime/RealtimeEvents.cs`
- `backend/src/WatchParty.Contracts/Realtime/ServerEvents.cs`
- `apps/web/src/lib/realtime/events.ts`
- `apps/mobile/src/lib/realtime/events.ts`

## Hub

| Campo | Valor |
|---|---|
| Endpoint directo local | `http://localhost:5210/hubs/room` |
| Endpoint via Docker/nginx | `http://localhost:8080/hubs/room` |
| Grupo por sala | `room-{roomId}` |
| Autenticacion | JWT Bearer via `access_token` query string |

Ejemplo web/mobile:

```typescript
const connection = new HubConnectionBuilder()
  .withUrl(`${apiUrl}/hubs/room`, {
    accessTokenFactory: () => accessToken,
  })
  .withAutomaticReconnect()
  .build();

await connection.start();
await connection.invoke("JoinRoom", roomId);
```

## Flujo de entrada a sala

Flujo observado en clientes:

1. El usuario entra por REST: `POST /api/rooms/join` o navega a una sala existente.
2. El cliente carga detalle por REST: `GET /api/rooms/{id}`.
3. El cliente carga historial de chat: `GET /api/rooms/{id}/chat/messages`.
4. El cliente abre SignalR.
5. El cliente invoca `JoinRoom(roomId)`.
6. El servidor agrega la conexion al grupo `room-{roomId}`.
7. El servidor actualiza presencia en Redis.
8. El servidor emite eventos de miembro/presencia al grupo.

## Metodos cliente -> servidor

Estos metodos existen en `RoomHub`.

| Metodo | Argumentos | Efecto |
|---|---|---|
| `JoinRoom` | `roomId: Guid` | Une la conexion al grupo de sala si el usuario es miembro activo. |
| `LeaveRoom` | - | Saca la conexion de la sala actual. |
| `Heartbeat` | - | Refresca presencia de la conexion. |
| `Play` | `roomId: Guid` | Ejecuta play sobre el estado oficial. |
| `Pause` | `roomId: Guid` | Ejecuta pause sobre el estado oficial. |
| `Seek` | `roomId: Guid`, `positionSeconds: double` | Cambia posicion oficial. |
| `ChangeMedia` | `roomId: Guid`, `url: string`, `title?: string` | Valida y cambia media. |
| `SendMessage` | `roomId: Guid`, `content: string` | Envia mensaje de chat. |
| `DeleteMessage` | `messageId: Guid` | Borra mensaje si el usuario tiene permiso. |
| `ReportPlaybackError` | - | Incrementa metrica operativa de errores de playback. |

Ejemplos:

```typescript
await connection.invoke("Play", roomId);
await connection.invoke("Pause", roomId);
await connection.invoke("Seek", roomId, 120.5);
await connection.invoke("ChangeMedia", roomId, "https://example.com/video.mp4", "Titulo");
await connection.invoke("SendMessage", roomId, "Hola");
await connection.invoke("DeleteMessage", messageId);
await connection.invoke("Heartbeat");
```

Nota: `ReportPlaybackError` existe como metodo en `RoomHub` y en los clientes TypeScript. En `WatchParty.Contracts.RealtimeMethods` del backend no aparece como constante al momento de esta revision; si se quiere que el contrato C# quede completo, hay que agregarlo.

## Eventos servidor -> cliente

Nombres canonicos observados:

| Evento | Payload | Cuando ocurre |
|---|---|---|
| `PlaybackStateChanged` | `PlaybackStateDto` | Play, pause o seek aceptado por servidor. |
| `MediaChanged` | `MediaChangedEvent` | Se carga/cambia media. |
| `MemberJoined` | `MemberJoinedEvent` | Un miembro se conecta al hub de la sala. |
| `MemberLeft` | `MemberLeftEvent` | Un miembro deja de estar online en la sala. |
| `PresenceUpdated` | `PresenceUpdatedEvent` | Cambia la lista de usuarios online. |
| `HostTransferred` | `HostTransferredEvent` | El host transfiere su rol. |
| `MemberKicked` | `MemberKickedEvent` | Un miembro fue expulsado. |
| `YouWereKicked` | sin payload observado | El usuario actual fue expulsado. |
| `RoomClosed` | `RoomClosedEvent` | La sala fue cerrada. |
| `ChatMessageReceived` | `ChatMessageDto` | Llega mensaje nuevo. |
| `ChatMessageDeleted` | `ChatMessageDeletedEvent` | Se borra un mensaje. |
| `HubError` | `HubErrorEvent` | Error funcional del hub. |

No se observaron eventos llamados `RoomStateUpdated`, `MemberOnlineStatusChanged`, `KickedFromRoom`, `PlayRequested`, `PauseRequested`, `SeekRequested`, `MessageSent`, `MessageDeleted` o `PresenceUpdate` en el contrato actual.

## Payloads principales

### `PlaybackStateDto`

```json
{
  "roomId": "uuid",
  "mediaId": "uuid",
  "status": "Playing",
  "positionSeconds": 154.3,
  "serverTimestampUtc": "2026-06-18T12:00:00Z",
  "version": 28,
  "updatedByUserId": "uuid"
}
```

`version` es monotonico. Los clientes ignoran estados viejos.

### `MediaChangedEvent`

```json
{
  "roomId": "uuid",
  "media": {
    "id": "uuid",
    "kind": "Direct",
    "url": "https://example.com/video.mp4",
    "providerId": null,
    "title": "Video",
    "addedByUserId": "uuid",
    "createdAtUtc": "2026-06-18T12:00:00Z"
  },
  "playback": {
    "roomId": "uuid",
    "mediaId": "uuid",
    "status": "Paused",
    "positionSeconds": 0,
    "serverTimestampUtc": "2026-06-18T12:00:00Z",
    "version": 1,
    "updatedByUserId": "uuid"
  }
}
```

### `MemberJoinedEvent`

```json
{
  "roomId": "uuid",
  "member": {
    "userId": "uuid",
    "displayName": "User Name",
    "avatarUrl": null,
    "role": "Member",
    "isOnline": true,
    "joinedAtUtc": "2026-06-18T12:00:00Z"
  },
  "onlineCount": 2
}
```

### `PresenceUpdatedEvent`

```json
{
  "roomId": "uuid",
  "onlineUserIds": ["uuid-1", "uuid-2"]
}
```

### `HostTransferredEvent`

```json
{
  "roomId": "uuid",
  "fromUserId": "uuid",
  "toUserId": "uuid"
}
```

### `MemberKickedEvent`

```json
{
  "roomId": "uuid",
  "userId": "uuid"
}
```

### `RoomClosedEvent`

```json
{
  "roomId": "uuid"
}
```

### `ChatMessageDeletedEvent`

```json
{
  "roomId": "uuid",
  "messageId": "uuid",
  "deletedByUserId": "uuid"
}
```

### `HubErrorEvent`

```json
{
  "code": "rooms.not_member",
  "message": "User is not an active room member."
}
```

## Sincronizacion de playback

El servidor es autoridad. Los clientes aplican solo estados con version nueva.

La funcion `applyPlaybackState` existe en:

- `apps/web/src/lib/realtime/sync.ts`
- `apps/mobile/src/lib/realtime/sync.ts`

Regla observada:

1. Si el estado no esta en `Playing`, la posicion objetivo es `positionSeconds`.
2. Si esta en `Playing`, la posicion objetivo es `positionSeconds + tiempo transcurrido desde serverTimestampUtc`.
3. Si el desfase local supera `0.75` segundos, el cliente hace `seek`.
4. Luego aplica `play` o `pause`.
5. Si el player no esta listo o no es controlable, no se aplica control programatico.

Google Drive y MEGA se muestran mediante embed/WebView y pueden no exponer una superficie de control programatico completa; los clientes marcan esas fuentes como no controlables para sincronizacion estricta de playback.

## Presencia

Presencia se guarda en Redis mediante `IPresenceStore`.

Comportamiento observado:

- `OnConnectedAsync` incrementa la metrica `signalr_reconnections`.
- `JoinRoom` agrega la conexion a la sala y a Redis.
- `Heartbeat` refresca la conexion.
- `OnDisconnectedAsync` elimina la conexion.
- Si el usuario ya no tiene conexiones activas en esa sala, se emiten `MemberLeft` y `PresenceUpdated`.
- `PresenceSweeper` existe como hosted service para limpiar conexiones muertas.

## Errores del hub

Cuando un caso de uso falla, el hub no lanza directamente una excepcion al cliente. Envia:

```typescript
connection.on("HubError", (error) => {
  console.log(error.code, error.message);
});
```

Los errores usan codigos estables del dominio.
