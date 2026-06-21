# WatchParty App - Guia de arquitectura

Este documento define la arquitectura tecnica que seguira WatchParty App. Su objetivo es servir como guia para implementar el backend, los clientes, los modulos, las reglas de dependencia y los criterios de diseno del proyecto.
Expo/EAS sirve para compilar apps listas para publicar en Google Play y App Store. Next.js sera el cliente web para PC porque es un framework React orientado a aplicaciones web completas. SignalR sera el nucleo de sincronizacion en tiempo real porque permite que el servidor envie eventos instantaneamente a los clientes.
La arquitectura elegida busca tres cosas:

- Permitir construir rapido una primera version real.
- Mantener el codigo ordenado por dominios de negocio.
- Evitar complejidad prematura, especialmente microservicios antes de necesitarlos.

## 1. Decision arquitectonica principal

WatchParty usara un monolito modular con DDD pragmatico y principios de arquitectura limpia/hexagonal dentro del backend.

Esto significa:

- Habra un solo backend desplegable al inicio.
- El backend estara separado internamente por modulos de negocio.
- El dominio no dependera de frameworks, base de datos, Redis, SignalR ni servicios externos.
- Los casos de uso viviran en la capa de aplicacion.
- La infraestructura implementara interfaces definidas por la aplicacion o el dominio.
- La API y los hubs SignalR actuaran como entrada al sistema, no como lugar de reglas de negocio.

No se usaran microservicios en V0, MVP ni V1. La separacion por modulos debe dejar abierta la posibilidad de extraer partes en el futuro, pero sin pagar ese costo desde el inicio.

## 2. Stack tecnico

| Capa | Tecnologia |
|---|---|
| Mobile | Expo + React Native + TypeScript |
| Web PC | Next.js + React + TypeScript |
| Backend | ASP.NET Core .NET 10 LTS |
| Admin | Blazor Server |
| Tiempo real | SignalR |
| Base de datos | PostgreSQL |
| Cache y estado vivo | Redis |
| ORM | EF Core |
| Infraestructura local | Docker Compose |
| Proxy / HTTPS | Nginx |
| Documentacion API | Swagger / OpenAPI |
| CI/CD | GitHub Actions o equivalente |

## 3. Por que monolito modular

WatchParty tiene varios dominios relacionados entre si: usuarios, salas, reproduccion, chat, presencia, moderacion y administracion. En una etapa inicial, separarlos en servicios independientes agregaria complejidad innecesaria:

- Mas despliegues.
- Mas configuracion.
- Mas observabilidad requerida.
- Mas latencia entre componentes.
- Mas fallos distribuidos.
- Mas trabajo para mantener consistencia.

Un monolito modular permite avanzar con una base clara:

- Un solo deploy principal.
- Transacciones mas simples.
- Debug mas facil.
- Modulos internos bien separados.
- Menor costo operativo.
- Posibilidad de escalar horizontalmente el backend cuando sea necesario.

La regla es simple: primero orden interno, despues distribucion fisica si el producto lo exige.

## 4. Uso pragmatico de arquitectura hexagonal

No se implementara una arquitectura hexagonal excesivamente ceremonial. Se aplicaran sus principios donde aporten valor real.

Reglas:

- Las reglas de negocio no deben conocer detalles tecnicos.
- Las capas internas no deben depender de capas externas.
- Las integraciones externas deben entrar por interfaces.
- PostgreSQL, Redis, SignalR, email, push notifications y storage son detalles de infraestructura.
- Los controladores y hubs deben coordinar peticiones, no contener logica compleja.

Ejemplo:

```text
Controller / Hub
    -> Use Case / Application Service
        -> Domain
        -> Interfaces
            <- Infrastructure implementation
```

Ejemplo concreto:

```text
RoomsController
    -> CreateRoomCommandHandler
        -> Room aggregate
        -> IRoomRepository
        -> IInviteCodeGenerator
```

`CreateRoomCommandHandler` no debe saber si la sala se guarda con EF Core, Dapper o una API externa. Solo usa una interfaz.

## 5. Estructura general del repositorio

Estructura actual observada:

```text
/watchparty-app
|
|-- apps
|   |-- mobile
|   |   `-- Expo React Native
|   |
|   |-- web
|   |   `-- Next.js
|   |
|   `-- admin
|       `-- Blazor Server
|
|-- backend
|   |-- src
|   |   |-- WatchParty.Api
|   |   |-- WatchParty.Application
|   |   |-- WatchParty.Domain
|   |   |-- WatchParty.Infrastructure
|   |   `-- WatchParty.Contracts
|
|-- infra
|   `-- nginx
|
|-- docker-compose.yml
|
`-- docs
    |-- watchparty-app-alcance.md
    |-- alcance-pendiente.md
    |-- architecture.md
    |-- auditing.md
    |-- api.md
    |-- database.md
    |-- realtime-events.md
    `-- deployment.md
```

## 6. Estructura del backend

```text
backend/src
|-- WatchParty.Api
|-- WatchParty.Application
|-- WatchParty.Domain
|-- WatchParty.Infrastructure
`-- WatchParty.Contracts
```

### 6.1 WatchParty.Domain

Contiene el nucleo de negocio.

Puede contener:

- Entidades.
- Agregados.
- Value Objects.
- Enums de dominio.
- Eventos de dominio.
- Reglas de negocio.
- Interfaces de repositorio cuando representen conceptos del dominio.
- Excepciones de dominio.

No debe contener:

- EF Core.
- Atributos de persistencia.
- SignalR.
- ASP.NET Core.
- Redis.
- DTOs de API.
- Logs tecnicos.
- Acceso directo a configuracion.

Ejemplo:

```text
Domain
|-- Rooms
|   |-- Room.cs
|   |-- RoomMember.cs
|   |-- RoomSettings.cs
|   |-- RoomRole.cs
|   `-- Events
|       `-- RoomCreatedDomainEvent.cs
```

### 6.2 WatchParty.Application

Contiene los casos de uso del sistema.

Puede contener:

- Commands.
- Queries.
- Handlers.
- DTOs internos de aplicacion.
- Validadores.
- Interfaces hacia infraestructura.
- Servicios de aplicacion.
- Reglas de autorizacion de casos de uso.
- Mapeos entre dominio y respuestas.

No debe contener:

- SQL directo.
- EF Core DbContext.
- Redis client directo.
- SignalR Hub directo.
- Logica de UI.

Ejemplo:

```text
Application
|-- Rooms
|   |-- CreateRoom
|   |   |-- CreateRoomCommand.cs
|   |   |-- CreateRoomCommandHandler.cs
|   |   `-- CreateRoomValidator.cs
|   |
|   |-- JoinRoom
|   |-- LeaveRoom
|   `-- TransferHost
```

### 6.3 WatchParty.Infrastructure

Contiene detalles tecnicos.

Puede contener:

- EF Core DbContext.
- Configuracion de entidades.
- Migraciones.
- Repositorios.
- Redis stores.
- Implementaciones de email.
- Implementaciones de push notifications.
- Storage de archivos.
- Integraciones externas.
- Implementaciones de reloj, generadores e IDs si aplica.

Ejemplo:

```text
Infrastructure
|-- Persistence
|   |-- WatchPartyDbContext.cs
|   |-- Configurations
|   `-- Repositories
|
|-- Redis
|   |-- RedisPlaybackStateStore.cs
|   `-- RedisPresenceStore.cs
|
`-- Notifications
    `-- PushNotificationSender.cs
```

### 6.4 WatchParty.Api

Contiene las entradas HTTP y realtime.

Puede contener:

- Controllers.
- Minimal API endpoints si se decide usarlos.
- SignalR Hubs.
- Middlewares.
- Filtros.
- Configuracion de auth.
- Swagger.
- Health checks.
- Inyeccion de dependencias.

No debe contener:

- Reglas de negocio complejas.
- Acceso directo a DbContext para casos de uso.
- Acceso directo a Redis para reglas de negocio.
- Validaciones profundas que pertenezcan a Application o Domain.

### 6.5 WatchParty.Contracts

Contiene contratos compartidos.

Puede contener:

- DTOs de solicitud.
- DTOs de respuesta.
- Contratos de eventos SignalR.
- Modelos compartidos entre API, web y mobile si aplica.

Debe cuidarse para no convertirse en una carpeta gigante sin criterio. Los contratos deben estar agrupados por modulo.

## 7. Modulos de negocio

El backend se organizara por estos modulos principales:

| Modulo | Responsabilidad |
|---|---|
| Identity | Registro, login, refresh tokens, recuperacion de contrasena, sesiones |
| Users | Perfil, avatar, privacidad, bloqueos |
| Rooms | Crear sala, entrar, salir, host, miembros, invitaciones |
| Playback | Media actual, play, pause, seek, sincronizacion |
| Chat | Mensajes, historial, typing, reportes de mensaje |
| Presence | Conexiones, usuarios online, miembros activos por sala |
| Reports | Reportes de usuario, sala y mensaje |
| Notifications | Push notifications, preferencias, alertas |
| Admin | Gestion administrativa, metricas, auditoria |

Cada modulo debe tener sus propios casos de uso, entidades y reglas. La comunicacion entre modulos debe ser explicita.

## 8. Reglas de dependencia

Dependencias permitidas:

```text
Api -> Application
Api -> Contracts
Application -> Domain
Application -> Contracts
Infrastructure -> Application
Infrastructure -> Domain
Infrastructure -> Contracts
```

Dependencias no permitidas:

```text
Domain -> Application
Domain -> Infrastructure
Domain -> Api
Application -> Infrastructure
Application -> Api
Contracts -> Infrastructure
Contracts -> Api
```

Regla importante:

`Application` define lo que necesita mediante interfaces. `Infrastructure` implementa esas interfaces.

Ejemplo:

```csharp
public interface IPlaybackStateStore
{
    Task<PlaybackState?> GetAsync(Guid roomId, CancellationToken cancellationToken);
    Task SaveAsync(PlaybackState state, CancellationToken cancellationToken);
}
```

La implementacion Redis vive en `Infrastructure`:

```csharp
public sealed class RedisPlaybackStateStore : IPlaybackStateStore
{
    // Implementacion tecnica con Redis.
}
```

## 9. Principios SOLID

Se aplicara SOLID de forma practica.

### Single Responsibility

Cada clase debe tener una razon clara para cambiar.

Correcto:

- `CreateRoomCommandHandler` crea salas.
- `RoomInviteCodeGenerator` genera codigos.
- `RedisPresenceStore` gestiona presencia en Redis.

Incorrecto:

- Un servicio que crea salas, envia emails, guarda chat y valida tokens.

### Open/Closed

El codigo debe permitir agregar comportamiento sin romper lo existente.

Ejemplo: si luego agregamos otro proveedor de notificaciones, deberia implementarse `INotificationSender` sin modificar todos los casos de uso.

### Liskov Substitution

Las implementaciones de una interfaz deben comportarse de forma compatible.

Ejemplo: `RedisPlaybackStateStore` y una version fake para tests deben respetar el mismo contrato.

### Interface Segregation

Interfaces pequenas y especificas.

Correcto:

```csharp
public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Room room, CancellationToken cancellationToken);
}
```

Evitar interfaces enormes como `IApplicationRepository` con metodos de todos los modulos.

### Dependency Inversion

Las capas internas dependen de abstracciones, no de detalles.

Correcto:

```text
Application -> IRoomRepository
Infrastructure -> EfRoomRepository
```

Incorrecto:

```text
Application -> WatchPartyDbContext
```

## 10. Reglas para casos de uso

Cada accion importante del sistema debe tener un caso de uso claro.

Ejemplos:

- `RegisterUser`
- `LoginUser`
- `CreateRoom`
- `JoinRoom`
- `LeaveRoom`
- `TransferHost`
- `KickRoomMember`
- `ChangeMedia`
- `PlaybackPlay`
- `PlaybackPause`
- `PlaybackSeek`
- `SendChatMessage`
- `ReportMessage`

Un caso de uso debe:

- Recibir un command o query.
- Validar datos de entrada.
- Cargar entidades necesarias.
- Ejecutar reglas de negocio.
- Persistir cambios.
- Emitir eventos si aplica.
- Devolver una respuesta clara.

Un caso de uso no debe:

- Leer directamente de HTTP.
- Depender de `HttpContext`.
- Manipular componentes visuales.
- Conocer detalles internos de Redis o PostgreSQL.

## 11. Reglas para SignalR

SignalR sera el canal realtime, pero no debe contener toda la logica del sistema.

Los hubs deben:

- Autenticar usuario.
- Validar datos basicos.
- Llamar casos de uso.
- Unir o sacar conexiones de grupos.
- Publicar eventos a clientes.

Los hubs no deben:

- Decidir reglas complejas de permisos.
- Manipular directamente entidades.
- Escribir directamente en PostgreSQL.
- Escribir directamente en Redis salvo operaciones puramente tecnicas de conexion si estan encapsuladas.

Ejemplo de flujo:

```text
Client -> PlaybackHub.Play(roomId)
Hub -> PlaybackPlayCommandHandler
Handler -> valida permisos
Handler -> actualiza estado oficial
Handler -> guarda estado en Redis
Handler -> devuelve evento
Hub -> publica PlaybackStateChanged al grupo de sala
```

## 12. Reglas para sincronizacion de video

El servidor sera la autoridad del estado de reproduccion.

Reglas:

- El cliente nunca decide el estado oficial por si solo.
- Todo evento de playback debe tener version.
- Todo evento de playback debe tener timestamp del servidor.
- Los eventos viejos deben ignorarse.
- El estado actual de playback vive en Redis.
- Los eventos relevantes se guardan en PostgreSQL.
- No se guarda progreso segundo a segundo.
- Si el desfase es pequeno, el cliente corrige suavemente.
- Si el desfase es grande, el cliente hace seek.

Estado oficial sugerido:

```json
{
  "roomId": "uuid",
  "mediaId": "uuid",
  "status": "Playing",
  "positionSeconds": 154.3,
  "serverTimestamp": "2026-06-17T20:45:00Z",
  "version": 28,
  "updatedByUserId": "uuid"
}
```

## 13. Persistencia

PostgreSQL sera la fuente de verdad para datos permanentes:

- Usuarios.
- Perfiles.
- Salas.
- Historial.
- Mensajes.
- Reportes.
- Auditoria.
- Configuracion.

Redis sera usado solo para datos temporales o de alta frecuencia:

- Presencia.
- Conexiones activas.
- Estado vivo de sala.
- Estado actual de playback.
- Rate limits.
- Tokens revocados temporalmente.

Regla:

Si el dato no se puede perder, debe estar en PostgreSQL. Si el dato se puede reconstruir o expirar, puede estar en Redis.

## 14. Contratos API y realtime

Los contratos deben ser estables y versionables.

Reglas:

- No exponer entidades de dominio directamente por API.
- Usar request/response DTOs.
- Usar contratos explicitos para eventos SignalR.
- Evitar respuestas anonimas o dinamicas.
- No filtrar propiedades internas.

Ejemplo:

```csharp
public sealed record CreateRoomRequest(
    string Name,
    bool IsPrivate
);

public sealed record RoomResponse(
    Guid Id,
    string Code,
    string Name,
    Guid HostUserId
);
```

## 15. Validacion y errores

La validacion se divide en tres niveles:

| Nivel | Ejemplo |
|---|---|
| Contrato | Campos requeridos, formatos, longitudes |
| Aplicacion | Usuario autenticado, permisos, existencia de recursos |
| Dominio | Reglas que siempre deben cumplirse |

Los errores deben ser claros y consistentes.

Formato sugerido:

```json
{
  "code": "room.not_found",
  "message": "Room was not found.",
  "details": {}
}
```

Los codigos de error deben ser estables para que mobile y web puedan reaccionar correctamente.

## 16. Testing

Se priorizaran pruebas segun riesgo.

### Unit tests

Para:

- Entidades de dominio.
- Value Objects.
- Reglas de permisos.
- Casos de uso.
- Validadores.

### Integration tests

Para:

- Repositorios EF Core.
- Endpoints API.
- Flujos con PostgreSQL.
- Flujos con Redis.

### Realtime tests

Para:

- Conexion SignalR.
- Join room.
- Leave room.
- Playback sync.
- Chat realtime.
- Reconexion.

### E2E tests

Flujos principales:

- Usuario A crea sala.
- Usuario B entra.
- Usuario A reproduce video.
- Usuario B se sincroniza.
- Usuario A pausa.
- Usuario B recibe pausa.
- Usuario B se desconecta.
- Usuario B reconecta y recupera estado.

## 17. Reglas para frontend mobile y web

Los clientes deben compartir conceptos, pero no necesariamente codigo.

Reglas:

- TypeScript obligatorio.
- Cliente API tipado.
- Eventos SignalR tipados.
- Estado remoto con TanStack Query.
- Estado local inmediato con Zustand.
- Tokens guardados de forma segura.
- No duplicar reglas criticas del backend.
- Los clientes pueden validar para UX, pero el backend valida siempre.

Mobile usara:

- Expo.
- React Native.
- Zustand.
- TanStack Query.
- SecureStore.
- MMKV.
- SQLite si se necesita historial local.

Web usara:

- Next.js.
- React.
- Zustand.
- TanStack Query.
- Cliente SignalR.

## 18. Reglas para Admin

El admin sera Blazor Server.

Debe consumir casos de uso del backend o endpoints administrativos. No debe saltarse las reglas del sistema.

Funciones admin deben quedar auditadas:

- Bloquear usuario.
- Desbloquear usuario.
- Cerrar sala.
- Resolver reporte.
- Rechazar reporte.
- Cambiar dominios permitidos.
- Activar mantenimiento.

## 19. Seguridad base

Reglas minimas:

- HTTPS obligatorio en produccion.
- JWT de vida corta.
- Refresh token rotativo.
- Hash seguro de contrasenas.
- Rate limit por IP y usuario.
- CORS restringido.
- Validacion de entrada.
- Sanitizacion de mensajes visibles.
- Auditoria de acciones administrativas.
- Logs de intentos de login.
- Proteccion contra abuso en SignalR.

Ningun cliente debe ser considerado confiable.

## 20. Auditoria transversal

La auditoria sigue el patron de poco boilerplate usado en OmniSuite, adaptado al stack propio de WatchParty.

Reglas:

- Toda request HTTP obtiene `AuditContext` con actor, IP, user agent, ruta y correlation id.
- El `WatchPartyDbContext` genera auditoria de cambios en `SaveChangesAsync`.
- Las entidades con `CreatedAtUtc` y `UpdatedAtUtc` se sellan por convencion.
- Los secretos como passwords, token hashes y security stamps se redactan en detalles de cambios.
- Los endpoints que consultan auditoria no se auditan a si mismos.
- Las acciones funcionales criticas que no se deducen de cambios EF deben crear `AuditLog` manual.
- Eventos de alta frecuencia como ticks de playback, pings y typing no deben saturar PostgreSQL.

La guia operativa vive en `docs/auditing.md`.

## 21. Criterios para no sobrearquitecturar

No crear abstracciones antes de necesitarlas.

Crear una interfaz cuando:

- Hay una dependencia externa.
- Se necesita testear el caso de uso sin infraestructura real.
- Hay mas de una implementacion realista.
- La dependencia pertenece a otra capa.

No crear una interfaz solo porque una clase existe.

No crear microservicios hasta que exista una razon fuerte, como:

- Escalado independiente probado.
- Dominio con ciclo de despliegue claramente separado.
- Carga muy distinta al resto del sistema.
- Equipo separado responsable de ese componente.

## 22. Decision final

La arquitectura oficial de WatchParty es:

```text
Monolito modular
+ DDD pragmatico
+ arquitectura limpia/hexagonal interna
+ SOLID aplicado con criterio
+ SignalR para realtime
+ PostgreSQL como fuente de verdad
+ Redis para estado vivo
```

El primer nucleo a construir sera:

```text
Sala + Video + SignalR + Chat + Web + Mobile
```

Si ese nucleo queda solido, el resto del producto puede crecer encima sin cambiar la base.
