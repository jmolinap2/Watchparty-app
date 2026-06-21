# Esquema de base de datos WatchParty

Este documento resume el esquema observado en las migraciones EF Core actuales:

- `backend/src/WatchParty.Infrastructure/Persistence/Migrations/20260618123947_InitialCreate.cs`
- `backend/src/WatchParty.Infrastructure/Persistence/Migrations/20260618124007_InitialAuditFoundation.cs`
- `backend/src/WatchParty.Infrastructure/Persistence/Migrations/20260621053547_AddUserUsername.cs`

No describe tablas futuras. Si el modelo cambia, este documento debe actualizarse junto con la migracion.

## Motor y ORM

| Pieza | Valor actual |
|---|---|
| Base principal | PostgreSQL |
| Version usada en Docker | `postgres:16-alpine` |
| ORM | EF Core |
| Proyecto de migraciones | `backend/src/WatchParty.Infrastructure` |
| Proyecto ejecutable de migracion | `backend/src/WatchParty.Migrator` |
| Startup para EF | `backend/src/WatchParty.Api` |
| Nombre de BD local | `watchparty` por defecto |

El `DbInitializer` aplica migraciones en startup cuando `Database:AutoMigrate=true` y siembra:

- cuenta admin inicial
- dominios permitidos iniciales

## Migraciones actuales

| Migracion | Responsabilidad |
|---|---|
| `InitialCreate` | Crea tablas base de identidad, salas, miembros, media, chat, reportes y dominios permitidos. |
| `InitialAuditFoundation` | Crea `audit_logs` e indices de auditoria. |
| `AddUserUsername` | Agrega `users.Username` para login por usuario corto, con indice unico filtrado para valores no nulos. |

Observacion importante: en la migracion inicial, muchas columnas de referencia son UUID pero no todas tienen foreign key fisica declarada. La FK explicita observada en `InitialCreate` es `room_members.RoomId -> rooms.Id`. Si se requiere integridad referencial fisica para otros campos, queda pendiente agregarla en una migracion futura.

## Tablas

### `users`

Usuarios de la plataforma.

| Columna | Tipo observado | Nota |
|---|---|---|
| `Id` | `uuid` | PK |
| `Email` | `varchar(256)` | Unico |
| `Username` | `varchar(32)`, nullable | Usuario corto unico para login cuando existe |
| `PasswordHash` | `varchar(512)` | Hash de password |
| `DisplayName` | `varchar(40)` | Nombre visible |
| `AvatarUrl` | `varchar(2048)`, nullable | URL de avatar |
| `IsPrivate` | `boolean` | Privacidad basica |
| `Role` | `varchar(32)` | `User` o `Admin` |
| `EmailConfirmed` | `boolean` | Confirmacion de email |
| `IsBlocked` | `boolean` | Bloqueo plataforma |
| `BlockedReason` | `varchar(1000)`, nullable | Motivo admin |
| `BlockedAtUtc` | timestamp, nullable | Fecha de bloqueo |
| `SecurityStamp` | `uuid` | Invalida sesiones/cambios sensibles |
| `CreatedAtUtc` | timestamp | Alta |
| `UpdatedAtUtc` | timestamp | Ultima actualizacion |
| `LastLoginAtUtc` | timestamp, nullable | Ultimo login |

Indices:

- `IX_users_Email` unico
- `IX_users_Username` unico filtrado (`Username IS NOT NULL`)

### `refresh_tokens`

Tokens rotativos de sesion.

| Columna | Tipo observado | Nota |
|---|---|---|
| `Id` | `uuid` | PK |
| `UserId` | `uuid` | Usuario dueño |
| `TokenHash` | `varchar(512)` | Unico |
| `ExpiresAtUtc` | timestamp | Expiracion |
| `CreatedAtUtc` | timestamp | Creacion |
| `RevokedAtUtc` | timestamp, nullable | Revocacion |
| `ReplacedByTokenHash` | `varchar(512)`, nullable | Rotacion |
| `CreatedByIp` | `varchar(64)`, nullable | IP origen |

Indices:

- `IX_refresh_tokens_TokenHash` unico
- `IX_refresh_tokens_UserId`

### `email_verification_tokens`

Tokens para confirmar email.

| Columna | Tipo observado | Nota |
|---|---|---|
| `Id` | `uuid` | PK |
| `UserId` | `uuid` | Usuario |
| `TokenHash` | `varchar(512)` | Unico |
| `ExpiresAtUtc` | timestamp | Expiracion |
| `CreatedAtUtc` | timestamp | Creacion |
| `ConsumedAtUtc` | timestamp, nullable | Consumo |

Indices:

- `IX_email_verification_tokens_TokenHash` unico
- `IX_email_verification_tokens_UserId`

### `password_reset_tokens`

Tokens para recuperar password.

| Columna | Tipo observado | Nota |
|---|---|---|
| `Id` | `uuid` | PK |
| `UserId` | `uuid` | Usuario |
| `TokenHash` | `varchar(512)` | Unico |
| `ExpiresAtUtc` | timestamp | Expiracion |
| `CreatedAtUtc` | timestamp | Creacion |
| `ConsumedAtUtc` | timestamp, nullable | Consumo |

Indices:

- `IX_password_reset_tokens_TokenHash` unico
- `IX_password_reset_tokens_UserId`

### `user_blocks`

Bloqueos entre usuarios.

| Columna | Tipo observado | Nota |
|---|---|---|
| `Id` | `uuid` | PK |
| `BlockerUserId` | `uuid` | Usuario que bloquea |
| `BlockedUserId` | `uuid` | Usuario bloqueado |
| `CreatedAtUtc` | timestamp | Creacion |

Indices:

- `IX_user_blocks_BlockerUserId_BlockedUserId` unico

### `rooms`

Salas creadas por usuarios.

| Columna | Tipo observado | Nota |
|---|---|---|
| `Id` | `uuid` | PK |
| `Name` | `varchar(60)` | Nombre |
| `Code` | `varchar(6)` | Codigo de invitacion, unico |
| `HostUserId` | `uuid` | Host actual |
| `is_private` | `boolean` | Sala privada |
| `max_members` | `integer` | Maximo de miembros |
| `Status` | `varchar(32)` | Estado de sala |
| `CurrentMediaId` | `uuid`, nullable | Media actual |
| `CreatedAtUtc` | timestamp | Creacion |
| `ClosedAtUtc` | timestamp, nullable | Cierre |

Indices:

- `IX_rooms_Code` unico
- `IX_rooms_HostUserId`

### `room_members`

Participacion de usuarios en salas.

| Columna | Tipo observado | Nota |
|---|---|---|
| `Id` | `uuid` | PK |
| `RoomId` | `uuid` | FK fisica a `rooms.Id` |
| `UserId` | `uuid` | Usuario |
| `Role` | `varchar(32)` | `Host` o `Member` |
| `JoinedAtUtc` | timestamp | Entrada |
| `LeftAtUtc` | timestamp, nullable | Salida |
| `WasKicked` | `boolean` | Expulsion |

Indices:

- `IX_room_members_RoomId_UserId`

### `media_items`

Media cargada en salas.

| Columna | Tipo observado | Nota |
|---|---|---|
| `Id` | `uuid` | PK |
| `RoomId` | `uuid` | Sala |
| `source_kind` | `varchar(32)` | `Direct`, `Hls`, `YouTube`, `GoogleDrive`, `Mega` |
| `source_url` | `varchar(2048)` | URL canonica para reproducir |
| `source_original_url` | `varchar(2048)`, nullable | URL enviada por usuario |
| `source_provider_id` | `varchar(255)`, nullable | ID de proveedor cuando aplica |
| `Title` | `varchar(200)` | Titulo |
| `AddedByUserId` | `uuid` | Usuario que cargo |
| `CreatedAtUtc` | timestamp | Creacion |

Indices:

- `IX_media_items_RoomId`

### `chat_messages`

Mensajes de chat por sala.

| Columna | Tipo observado | Nota |
|---|---|---|
| `Id` | `uuid` | PK |
| `RoomId` | `uuid` | Sala |
| `SenderUserId` | `uuid` | Autor |
| `Content` | `varchar(2000)` | Contenido |
| `CreatedAtUtc` | timestamp | Creacion |
| `IsDeleted` | `boolean` | Borrado logico |
| `DeletedAtUtc` | timestamp, nullable | Fecha de borrado |
| `DeletedByUserId` | `uuid`, nullable | Quien borro |

Indices:

- `IX_chat_messages_RoomId_CreatedAtUtc`

### `reports`

Reportes de usuario o mensaje.

| Columna | Tipo observado | Nota |
|---|---|---|
| `Id` | `uuid` | PK |
| `Type` | `varchar(32)` | Tipo de reporte |
| `ReporterUserId` | `uuid` | Usuario que reporta |
| `TargetUserId` | `uuid`, nullable | Usuario reportado |
| `TargetMessageId` | `uuid`, nullable | Mensaje reportado |
| `RoomId` | `uuid`, nullable | Sala relacionada |
| `Reason` | `varchar(1000)` | Motivo |
| `Status` | `varchar(32)` | Estado |
| `CreatedAtUtc` | timestamp | Creacion |
| `ResolvedByUserId` | `uuid`, nullable | Admin que resolvio |
| `ResolvedAtUtc` | timestamp, nullable | Fecha de resolucion |
| `ResolutionNote` | `varchar(1000)`, nullable | Nota admin |

Indices:

- `IX_reports_Status`
- `IX_reports_TargetMessageId`
- `IX_reports_TargetUserId`

### `allowed_domains`

Dominios permitidos para URLs directas/HLS.

| Columna | Tipo observado | Nota |
|---|---|---|
| `Id` | `uuid` | PK |
| `Host` | `varchar(255)` | Unico |
| `IsEnabled` | `boolean` | Habilitado |
| `AddedByUserId` | `uuid`, nullable | Quien agrego |
| `CreatedAtUtc` | timestamp | Creacion |

Indices:

- `IX_allowed_domains_Host` unico

Hosts sembrados por `DbInitializer`:

- `youtube.com`
- `youtu.be`
- `drive.google.com`
- `docs.google.com`
- `commondatastorage.googleapis.com`
- `storage.googleapis.com`
- `test-streams.mux.dev`

Nota: YouTube, Google Drive y MEGA se reconocen por configuracion/validador. `allowed_domains` se usa para Direct/HLS y visibilidad admin.

### `audit_logs`

Eventos de auditoria HTTP, datos y acciones funcionales relevantes.

| Columna | Tipo observado | Nota |
|---|---|---|
| `Id` | `uuid` | PK |
| `Category` | `varchar(32)` | Categoria |
| `Action` | `varchar(160)` | Accion |
| `ActorUserId` | `uuid`, nullable | Actor |
| `TargetType` | `varchar(120)`, nullable | Tipo de objetivo |
| `TargetId` | `varchar(120)`, nullable | ID objetivo |
| `Details` | `text`, nullable | Detalle JSON/texto |
| `IpAddress` | `varchar(64)`, nullable | IP |
| `Resource` | `varchar(180)`, nullable | Recurso |
| `Operation` | `varchar(180)`, nullable | Operacion |
| `HttpMethod` | `varchar(16)`, nullable | Metodo HTTP |
| `RequestPath` | `varchar(512)`, nullable | Ruta |
| `StatusCode` | `integer`, nullable | Estado HTTP |
| `DurationMs` | `bigint`, nullable | Duracion |
| `UserAgent` | `varchar(512)`, nullable | User agent |
| `CorrelationId` | `varchar(120)`, nullable | Correlation id |
| `Exception` | `text`, nullable | Excepcion |
| `CreatedAtUtc` | timestamp | Creacion |

Indices:

- `IX_audit_logs_ActorUserId`
- `IX_audit_logs_Category_CreatedAtUtc`
- `IX_audit_logs_CorrelationId`
- `IX_audit_logs_CreatedAtUtc`

No se observaron triggers ni stored procedures en la migracion de auditoria actual; la auditoria se genera desde middleware, `WatchPartyDbContext` y casos de uso.

## Comandos EF

La herramienta `dotnet-ef` esta declarada como herramienta local en `backend/.config/dotnet-tools.json`.

Restaurar herramienta:

```powershell
cd backend
dotnet tool restore
```

Listar migraciones sin conectar a la base:

```powershell
cd backend
dotnet ef migrations list -p src\WatchParty.Infrastructure -s src\WatchParty.Api --no-connect
```

Detectar cambios pendientes del modelo:

```powershell
cd backend
dotnet ef migrations has-pending-model-changes -p src\WatchParty.Infrastructure -s src\WatchParty.Api
```

Crear migracion:

```powershell
cd backend
dotnet ef migrations add <NombreMigracion> -p src\WatchParty.Infrastructure -s src\WatchParty.Api -o Persistence\Migrations
```

Aplicar migraciones:

```powershell
cd backend
dotnet ef database update -p src\WatchParty.Infrastructure -s src\WatchParty.Api
```

Ejecutar el migrador, equivalente operativo al proyecto migrator de Holos:

```powershell
cd backend
dotnet run --project src\WatchParty.Migrator -- -q
```

El migrador aplica migraciones pendientes con `DbInitializer` y siembra la cuenta admin/dominios iniciales usando `src\WatchParty.Migrator\appsettings.json` mas variables de entorno.

## Respaldo local

Con el servicio de Docker Compose levantado:

```bash
docker exec watchparty-app-postgres-1 pg_dump -U watchparty watchparty > backup.sql
docker exec -i watchparty-app-postgres-1 psql -U watchparty watchparty < backup.sql
```

El nombre real del contenedor puede variar segun el nombre de carpeta/proyecto de Compose.

## Pendientes de datos

Estos puntos no estan implementados como migraciones en el estado actual del repo:

- politica automatica de retencion/limpieza para `audit_logs`
- job de limpieza de refresh tokens antiguos
- foreign keys fisicas para todas las columnas UUID de referencia, si se decide exigirlas a nivel de base
- estrategia de backup productiva automatizada
- replicas o point-in-time recovery para produccion
