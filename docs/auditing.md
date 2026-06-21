# Auditoria y trazabilidad

WatchParty usa auditoria transversal inspirada en el patron de OmniSuite: las pantallas y casos de uso no deben llenar el codigo de llamadas manuales para dejar rastro. La mayor parte del registro ocurre en infraestructura.

## Objetivos

- Registrar quien hizo una accion, desde que IP y con que correlation id.
- Registrar requests HTTP con metodo, ruta, estado, duracion y excepcion.
- Registrar cambios persistidos en entidades EF durante `SaveChangesAsync`.
- Mantener un historial consultable desde admin con filtros y paginacion.
- Redactar secretos como passwords, token hashes y security stamps.
- Evitar auditar en exceso eventos de alta frecuencia como ticks de playback.

## Piezas principales

| Pieza | Ubicacion | Responsabilidad |
|---|---|---|
| `AuditLog` | `WatchParty.Domain/Admin` | Registro inmutable de auditoria. |
| `AuditCategory` | `WatchParty.Domain/Admin` | Clasifica eventos: `Admin`, `Security`, `Data`, `Http`, `Realtime`, `System`. |
| `AuditContext` | `Application/Abstractions/Auditing` | Actor, IP, user agent, correlation id, metodo y ruta del request actual. |
| `RequestAuditMiddleware` | `WatchParty.Api/Auditing` | Crea auditoria HTTP por request. |
| `WatchPartyDbContext` | `Infrastructure/Persistence` | Sella timestamps y crea auditoria de cambios por convencion. |
| `IAuditLogReader` | `Application/Abstractions/Admin` | Consulta paginada para admin. |
| `AuditLogsController` | `WatchParty.Api/Controllers/Admin` | Endpoints `GET /api/admin/audit-logs`. |

## Que sale gratis

Cuando un caso de uso modifica entidades mediante repositorios EF y llama `IUnitOfWork.SaveChangesAsync`, el `WatchPartyDbContext`:

1. Lee el `AuditContext` del request actual.
2. Actualiza `CreatedAtUtc` en altas si esta vacio.
3. Actualiza `UpdatedAtUtc` en modificaciones si la entidad tiene esa propiedad.
4. Recorre entidades agregadas, modificadas o eliminadas.
5. Crea un `AuditLog` categoria `Data` con:
   - `ActorUserId`
   - `IpAddress`
   - `CorrelationId`
   - `TargetType`
   - `TargetId`
   - detalle JSON de cambios

Eso permite que una pantalla nueva quede auditada sin boilerplate siempre que use el flujo normal:

```text
Controller / Hub
    -> Application use case
        -> Repository
        -> IUnitOfWork.SaveChangesAsync()
            -> AuditLog automatico
```

## Auditoria HTTP

`RequestAuditMiddleware` registra cada request con categoria `Http`, salvo endpoints marcados con `[DisableRequestAuditing]`.

Se captura:

- Metodo y ruta.
- Endpoint/resource.
- Status code.
- Duracion en milisegundos.
- Excepcion si ocurrio.
- IP, user agent y correlation id.

Los endpoints de lectura de auditoria usan `[DisableRequestAuditing]` para evitar ruido y recursividad, igual que OmniSuite deshabilita auditoria sobre las consultas del audit log.

## Auditoria manual

La auditoria automatica cubre cambios de datos, pero algunas acciones merecen un registro funcional explicito:

- Login exitoso o fallido.
- Refresh token reutilizado o revocado por sospecha.
- Bloqueo/desbloqueo admin.
- Cierre forzado de sala.
- Resolucion/rechazo de reportes.
- Cambio de dominios permitidos.
- Errores relevantes de integraciones.

Para esos casos se usa `IAuditLogRepository` dentro del caso de uso o `IAuditLogWriter` desde infraestructura:

```csharp
await auditLogRepository.AddAsync(
    AuditLog.Security(
        "Identity.LoginFailed",
        actorUserId: null,
        details: "Invalid credentials",
        ipAddress: ipAddress),
    cancellationToken);
```

Luego se persiste en la misma unidad de trabajo, dejando el evento funcional junto con el resto de cambios.

## Consulta admin

Endpoint:

```http
GET /api/admin/audit-logs
GET /api/admin/audit-logs/{id}
```

Filtros soportados:

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

Ejemplo:

```http
GET /api/admin/audit-logs?Category=Data&TargetType=Room&Page=1&PageSize=50
```

## Migraciones EF

WatchParty sigue la misma idea de OmniSuite: las migraciones se generan desde el proyecto central de infraestructura y usando el proyecto API como startup.

La herramienta `dotnet-ef` esta declarada como herramienta local en `backend/.config/dotnet-tools.json`, no se asume instalacion global.

Setup:

```powershell
cd backend
dotnet tool restore
```

El estado inicial se mantiene en dos migraciones generadas por EF:

1. `InitialCreate`: tablas base del sistema, sin `audit_logs`.
2. `InitialAuditFoundation`: tabla `audit_logs` e indices de auditoria.

Crear una nueva migracion:

```powershell
cd backend
dotnet ef migrations add <NombreMigracion> -p src\WatchParty.Infrastructure -s src\WatchParty.Api -o Persistence\Migrations
```

Verificar migraciones y cambios pendientes del modelo:

```powershell
cd backend
dotnet ef migrations list -p src\WatchParty.Infrastructure -s src\WatchParty.Api --no-connect
dotnet ef migrations has-pending-model-changes -p src\WatchParty.Infrastructure -s src\WatchParty.Api
```

Aplicar migraciones:

```powershell
cd backend
dotnet ef database update -p src\WatchParty.Infrastructure -s src\WatchParty.Api
```

## Redaccion de datos sensibles

La auditoria de cambios redacta propiedades cuyo nombre contiene:

- `password`
- `tokenhash`
- `replacedbytokenhash`
- `securitystamp`
- `secret`
- `credential`

Los detalles guardan `***REDACTED***` en vez del valor real.

## Alta frecuencia y realtime

No todo evento realtime debe ir a PostgreSQL. Playback y presencia pueden generar demasiado volumen.

Regla:

- Guardar en auditoria persistente acciones discretas y relevantes: cambiar media, pausar/play manual del host, kick, transferir host, cerrar sala.
- Mantener fuera de auditoria persistente los ticks, pings, typing y sincronizaciones repetitivas.
- Usar logs tecnicos/metricas para volumen operativo que no necesita reconstruccion historica legal o funcional.

## Checklist para nuevas pantallas

1. Usar repositorios + `IUnitOfWork.SaveChangesAsync`.
2. Evitar acceso directo a `DbContext` desde API o UI admin.
3. Agregar auditoria manual solo para eventos funcionales que no se deducen de cambios EF.
4. No incluir tokens ni passwords en DTOs, query strings o `details`.
5. Si el endpoint solo lee auditoria, marcarlo con `[DisableRequestAuditing]`.
