# Referencia de API WatchParty

Este documento describe la API observada en:

- `backend/src/WatchParty.Api/Controllers`
- `backend/src/WatchParty.Contracts`
- clientes HTTP en `apps/web/src/lib/api`

No inventa endpoints futuros. Para validar el contrato exacto en ejecucion, usar Swagger cuando la API corre en ambiente `Development`.

## URLs base

| Contexto | URL |
|---|---|
| API directa local | `http://localhost:5210` |
| Docker Compose via nginx | `http://localhost:8080` |
| Prefijo REST | `/api` |
| Hub SignalR | `/hubs/room` |
| Health check | `/health` y `/health/ready` |
| Swagger en Development | `/swagger` |

## Autenticacion

Los endpoints privados requieren JWT en el header:

```http
Authorization: Bearer <access_token>
```

La API tambien acepta el token por query string solo para SignalR:

```text
/hubs/room?access_token=<access_token>
```

### Respuesta de autenticacion

`register`, `login` y `refresh` devuelven `AuthResponse`:

```json
{
  "accessToken": "jwt",
  "accessTokenExpiresAtUtc": "2026-06-18T12:15:00Z",
  "refreshToken": "token",
  "refreshTokenExpiresAtUtc": "2026-07-18T12:00:00Z",
  "user": {
    "id": "uuid",
    "email": "user@example.com",
    "displayName": "User Name",
    "avatarUrl": null,
    "isPrivate": false,
    "role": "User",
    "emailConfirmed": false,
    "createdAtUtc": "2026-06-18T12:00:00Z"
  }
}
```

## Errores

Los errores usan el sobre `ApiErrorResponse`:

```json
{
  "code": "room.not_found",
  "message": "Room was not found.",
  "details": {
    "field": ["Validation message"]
  }
}
```

`details` solo aparece cuando hay errores de validacion por campo.

## Auth

Ruta base: `/api/auth`. Estos endpoints son anonimos.

| Metodo | Ruta | Body | Respuesta |
|---|---|---|---|
| `POST` | `/register` | `{ "email", "password", "displayName" }` | `200 OK`, `AuthResponse` |
| `POST` | `/login` | `{ "email", "password" }` | `200 OK`, `AuthResponse` |
| `POST` | `/refresh` | `{ "refreshToken" }` | `200 OK`, `AuthResponse` |
| `POST` | `/logout` | `{ "refreshToken" }` | `200 OK` |
| `POST` | `/confirm-email` | `{ "token" }` | `200 OK` |
| `POST` | `/resend-confirmation` | `{ "email" }` | `200 OK` |
| `POST` | `/forgot-password` | `{ "email" }` | `200 OK` |
| `POST` | `/reset-password` | `{ "token", "newPassword" }` | `200 OK` |

Notas observadas:

- El registro genera un token de confirmacion de email.
- El sender actual es `LoggingEmailSender`: deja los links en logs, no envia correo real.
- El refresh token es rotativo y se guarda hasheado.

## Users

Ruta base: `/api/users`. Requiere JWT.

| Metodo | Ruta | Body/query | Respuesta |
|---|---|---|---|
| `GET` | `/me` | - | `UserProfileDto` |
| `PUT` | `/me` | `{ "displayName", "isPrivate" }` | `UserProfileDto` |
| `PUT` | `/me/avatar` | `{ "avatarUrl": string | null }` | `UserProfileDto` |
| `PUT` | `/me/password` | `{ "currentPassword", "newPassword" }` | `200 OK` |
| `GET` | `/me/blocked` | - | `PublicUserDto[]` |
| `POST` | `/blocks` | `{ "userId" }` | `200 OK` |
| `DELETE` | `/blocks/{userId}` | - | `200 OK` |

## Rooms

Ruta base: `/api/rooms`. Requiere JWT.

| Metodo | Ruta | Body/query | Respuesta |
|---|---|---|---|
| `POST` | `/` | `{ "name", "isPrivate", "maxMembers" }` | `201 Created`, `RoomDto` |
| `GET` | `/history` | - | `RoomHistoryItemDto[]` |
| `GET` | `/by-code/{code}` | - | `RoomDto` |
| `POST` | `/join` | `{ "code" }` | `RoomDetailDto` |
| `GET` | `/{id}` | - | `RoomDetailDto` |
| `POST` | `/{id}/leave` | - | `200 OK` |
| `POST` | `/{id}/close` | - | `200 OK` |
| `POST` | `/{id}/transfer-host` | `{ "toUserId" }` | `200 OK` |
| `POST` | `/{id}/kick` | `{ "userId" }` | `200 OK` |

`RoomDetailDto` contiene:

- `room`
- `members`
- `currentMedia`
- `playback`

## Playback

Ruta base: `/api/rooms/{roomId}/playback`. Requiere JWT.

Estos endpoints son espejo REST de los mismos casos de uso que normalmente se invocan por SignalR.

| Metodo | Ruta | Body | Respuesta |
|---|---|---|---|
| `GET` | `/state` | - | `PlaybackStateDto` |
| `POST` | `/media` | `{ "url", "title": string | null }` | `PlaybackStateDto` |
| `POST` | `/play` | - | `PlaybackStateDto` |
| `POST` | `/pause` | - | `PlaybackStateDto` |
| `POST` | `/seek` | `{ "positionSeconds" }` | `PlaybackStateDto` |

Fuentes de media observadas en dominio/validador:

- `Direct`
- `Hls`
- `YouTube`
- `GoogleDrive`
- `Mega`

Reglas observadas:

- Solo HTTPS.
- YouTube, Google Drive y MEGA se validan como proveedores reconocidos.
- Direct/HLS deben venir de dominios habilitados en `allowed_domains`.
- Direct/HLS se resuelven por extension configurada.

## Chat

Ruta base: `/api/rooms/{roomId}/chat`. Requiere JWT.

| Metodo | Ruta | Body/query | Respuesta |
|---|---|---|---|
| `GET` | `/messages` | `before`, `limit` | `ChatMessageDto[]` |
| `POST` | `/messages` | `{ "content" }` | `ChatMessageDto` |
| `DELETE` | `/messages/{messageId}` | - | `200 OK` |

Validacion observada:

- `content` no puede estar vacio.
- Longitud maxima definida por `ChatMessage.MaxLength`.

## Reports

Ruta base: `/api/reports`. Requiere JWT.

| Metodo | Ruta | Body/query | Respuesta |
|---|---|---|---|
| `POST` | `/users` | `{ "targetUserId", "roomId": string | null, "reason" }` | `201 Created`, `ReportDto` |
| `POST` | `/messages` | `{ "messageId", "reason" }` | `201 Created`, `ReportDto` |
| `GET` | `/mine` | - | `ReportDto[]` |

## Admin

Los endpoints admin requieren JWT con rol `Admin`.

### Metricas

| Metodo | Ruta | Respuesta |
|---|---|---|
| `GET` | `/api/admin/metrics` | `MetricsDto` |

Metricas observadas:

- `registeredUsers`
- `activeUsers`
- `roomsCreated`
- `activeRooms`
- `messagesSent`
- `openReports`
- `resolvedReports`
- `playbackErrors`
- `signalrReconnections`

### Usuarios

Ruta base: `/api/admin/users`.

| Metodo | Ruta | Body/query | Respuesta |
|---|---|---|---|
| `GET` | `/` | `search`, `page`, `pageSize` | `PagedResult<AdminUserDto>` |
| `GET` | `/{id}` | - | detalle admin de usuario |
| `POST` | `/{id}/block` | `{ "reason": string | null }` | `200 OK` |
| `POST` | `/{id}/unblock` | - | `200 OK` |
| `PUT` | `/{id}/role` | `{ "role" }` | `200 OK` |

### Salas

Ruta base: `/api/admin/rooms`.

| Metodo | Ruta | Body/query | Respuesta |
|---|---|---|---|
| `GET` | `/` | `status`, `page`, `pageSize` | `PagedResult<AdminRoomDto>` |
| `GET` | `/{id}` | - | detalle admin de sala |
| `POST` | `/{id}/close` | - | `200 OK` |

### Reportes

Ruta base: `/api/admin/reports`.

| Metodo | Ruta | Body/query | Respuesta |
|---|---|---|---|
| `GET` | `/` | `status`, `page`, `pageSize` | `PagedResult<ReportDto>` |
| `GET` | `/{id}` | - | `ReportDto` |
| `POST` | `/{id}/resolve` | `{ "note": string | null }` | `200 OK` |
| `POST` | `/{id}/reject` | `{ "note": string | null }` | `200 OK` |

### Dominios permitidos

Ruta base: `/api/admin/allowed-domains`.

| Metodo | Ruta | Body | Respuesta |
|---|---|---|---|
| `GET` | `/` | - | `AllowedDomainDto[]` |
| `POST` | `/` | `{ "host" }` | `201 Created`, `AllowedDomainDto` |
| `POST` | `/{id}/enable` | - | `200 OK` |
| `POST` | `/{id}/disable` | - | `200 OK` |

### Auditoria

Ruta base: `/api/admin/audit-logs`.

| Metodo | Ruta | Body/query | Respuesta |
|---|---|---|---|
| `GET` | `/` | filtros de `AuditLogSearchRequest` | `PagedResult<AuditLogDto>` |
| `GET` | `/{id}` | - | `AuditLogDto` o `404` |

Filtros soportados por contrato:

- `StartDateUtc`
- `EndDateUtc`
- `Category`
- `ActorUserId`
- `Action`
- `TargetType`
- `Resource`
- `Operation`
- `HasException`
- `Search`
- `Page`
- `PageSize`

## Realtime

La referencia completa de SignalR vive en `docs/realtime-events.md`.

Resumen:

- Hub: `/hubs/room`
- El cliente inicia con REST (`GET /api/rooms/{id}` y chat history).
- Luego invoca `JoinRoom(roomId)`.
- Los cambios de playback/chat/presencia se propagan por eventos server-to-client.
