# Alcance pendiente WatchParty

Fecha de corte: 2026-06-19.

Este documento separa lo que esta observado en el repo de lo que todavia queda pendiente. La revision fue estatica sobre archivos del repositorio; no reemplaza una prueba funcional end-to-end.

## Criterio usado

Un punto se marca como pendiente cuando:

- no hay archivo/configuracion que lo implemente
- existe una pantalla o endpoint, pero falta una pieza necesaria para cerrarlo
- aparece como alcance en `docs/watchparty-app-alcance.md`, pero no hay evidencia suficiente para considerarlo terminado
- no se encontro test, pipeline o configuracion que lo valide

Cuando algo solo necesita verificacion manual/automatizada, se marca como "pendiente de validar", no como "no implementado".

## Observado en el repo

### Backend

Hay API ASP.NET Core con controladores para:

- auth
- users
- rooms
- playback
- chat
- reports
- admin users
- admin rooms
- admin reports
- admin allowed domains
- admin metrics
- audit logs

Tambien hay:

- SignalR en `/hubs/room`
- PostgreSQL con migraciones EF
- Redis para presencia/playback/metricas operativas
- auditoria HTTP y de cambios EF
- seed de admin inicial
- seed de dominios permitidos
- validacion de media para Direct, HLS, YouTube, Google Drive y MEGA

### Web

Hay cliente Next.js con:

- login
- registro
- confirmacion de email
- recuperar/resetear password
- home
- crear sala
- unirse a sala
- sala con player, chat, miembros, reportes y controles
- perfil
- historial
- mis reportes
- i18n `es/en`

### Mobile

Hay cliente Expo con:

- login
- registro
- recuperar password
- home
- crear sala
- unirse a sala
- sala con player, chat, miembros, reportes y controles
- perfil
- editar perfil
- historial
- configuracion
- mis reportes
- i18n `es/en`

### Admin

Hay app Blazor Server con un panel principal que consume:

- metricas
- usuarios
- salas
- reportes
- dominios permitidos
- audit logs

El panel pide pegar manualmente un JWT admin.

### Infraestructura local

Hay:

- `docker-compose.yml`
- Dockerfile de API
- Dockerfile de web
- nginx local/staging
- CI con build/typecheck

## Pendiente confirmado

### 1. Pruebas automatizadas

No se encontraron:

- `*.Tests.csproj`
- `playwright.config.*`
- `*.spec.ts`
- `*.test.ts`

Pendiente:

- tests unitarios de dominio y casos de uso
- tests de integracion API/PostgreSQL/Redis
- tests de SignalR
- tests e2e web del flujo sala + video + chat
- tests mobile o al menos validacion automatizada de typecheck/lint mas amplia
- incluir tests en CI

### 2. Validacion end-to-end del MVP

Aunque el codigo existe para el flujo principal, falta validarlo de punta a punta.

Pendiente de validar:

- usuario A se registra/loguea
- usuario B se registra/loguea
- A crea sala
- B entra por codigo
- A carga media permitida
- ambos ven el player
- play/pause/seek se sincronizan
- chat llega en tiempo real
- presencia online cambia correctamente
- reconexion recupera estado
- salir/cerrar sala actualiza ambos clientes

### 3. Link de invitacion web

Observado:

- `RoomHeader` copia un link con forma `/rooms/join?code={room.code}`.
- La pantalla web `rooms/join/page.tsx` no lee `code` desde query string; solo inicia `code` como string vacio.

Pendiente:

- prellenar el codigo desde `?code=`
- opcionalmente auto-ejecutar join si el usuario ya esta autenticado y el codigo es valido
- cubrir el flujo con prueba/manual QA

### 4. Admin

Observado:

- panel principal funcional en `apps/admin/Components/Pages/Home.razor`
- paginas de plantilla Blazor todavia presentes: `Counter.razor` y `Weather.razor`
- nav aun muestra `Counter` y `Weather`
- admin no esta en `docker-compose.yml`
- admin no esta en `infra/nginx/nginx.conf`
- auth admin depende de pegar JWT manualmente

Pendiente:

- remover paginas de plantilla y links del nav
- decidir ruta de despliegue del admin
- agregar Dockerfile/servicio si se va a contenerizar
- reemplazar JWT manual por login/sesion admin
- agregar filtros/paginacion visibles para usuarios, salas, reportes y auditoria
- agregar pantallas de detalle si se quieren usar los endpoints detail existentes

### 5. Produccion y despliegue

No se observo:

- pipeline de deploy
- configuracion TLS real
- gestion de secretos productiva
- backup automatico productivo
- plan de rollback productivo
- orquestacion productiva declarada
- endpoint Prometheus `/metrics`

Pendiente:

- elegir destino de hosting
- definir HTTPS/certificados
- mover secretos fuera de `.env`
- definir migraciones antes/durante deploy
- definir backups y prueba de restore
- definir rollback por tags/imagenes
- agregar observabilidad y alertas

### 6. Mobile release

Observado:

- `app.json` tiene `ios.bundleIdentifier` y `android.package`.
- no se encontro `eas.json`.

Pendiente:

- configurar EAS Build si se publicara con Expo
- perfiles Android/iOS
- variables de ambiente por canal
- pruebas en dispositivo fisico
- build Android
- build iOS
- proceso de beta cerrada

### 7. Email real

Observado:

- `LoggingEmailSender` registra links de confirmacion/reset en logs.
- no se encontro implementacion SMTP/provider.

Pendiente:

- implementar proveedor real de email
- configurar templates o contenido
- configurar `Email:WebBaseUrl` por ambiente
- asegurar que confirmacion/reset funcionen fuera de logs locales

### 8. Seguridad operativa

Pendientes no observados como middleware/configuracion completa:

- rate limiting
- politica de CORS productiva final por dominio real
- headers de seguridad productivos en proxy final
- sanitizacion explicita de mensajes visibles antes de render/persistencia
- politica de bloqueo/abuso mas alla de reportes y bloqueo admin
- proteccion especifica contra spam de eventos SignalR

### 9. Datos y mantenimiento

Pendiente:

- retencion o archivado de `audit_logs`
- limpieza automatica de refresh tokens expirados/revocados
- decidir si se agregan foreign keys fisicas para todas las referencias UUID
- estrategia de backup y restore productiva
- revisar crecimiento de chat/auditoria

### 10. Escalado realtime

Observado:

- SignalR esta configurado en una instancia normal con Redis usado para estado/presencia.
- no se observo backplane SignalR Redis configurado.

Pendiente si se usan multiples instancias API:

- sticky sessions o backplane SignalR
- pruebas de reconexion entre instancias
- limites de conexiones
- metricas operativas de conexiones activas

### 11. Contrato realtime

Observado:

- `RoomHub` implementa `ReportPlaybackError`.
- los clientes TypeScript tienen `RealtimeMethods.ReportPlaybackError`.
- `WatchParty.Contracts.RealtimeMethods` no lista `ReportPlaybackError` como constante.

Pendiente:

- sincronizar la constante C# para que el contrato realtime quede completo

### 12. Coherencia de alcance/documentacion

Observado:

- `docs/watchparty-app-alcance.md` ya referencia i18n basico `es/en`.
- el codigo actual soporta Direct, HLS, YouTube, Google Drive y MEGA.
- la documentacion ahora deja claro que no hay soporte DRM ni bypass de plataformas pagas.

Pendiente:

- mantener el alcance oficial sincronizado si cambia la lista real de proveedores
- decidir si idiomas adicionales mas alla de `es/en` entran en V1 o quedan post V1

## Pendiente de validar, no necesariamente pendiente de implementar

Estos puntos tienen codigo visible, pero necesitan prueba funcional:

- registro/login/refresh/logout
- confirmacion de email con link logueado
- reset password con link logueado
- bloqueo/desbloqueo de usuarios
- privacidad de perfil
- crear/unirse/salir/cerrar sala
- transferir host
- expulsar miembro
- reportar usuario
- reportar mensaje
- resolver/rechazar reportes desde admin
- agregar/deshabilitar dominios permitidos
- carga Direct/HLS/YouTube/GoogleDrive/MEGA
- sincronizacion web-web
- sincronizacion mobile-mobile
- sincronizacion web-mobile
- reconexion SignalR
- auditoria de requests y cambios EF
- metricas admin

## Post V1 declarado, no compromiso actual

Estos puntos aparecen como crecimiento posterior en `docs/watchparty-app-alcance.md`. No se deben tratar como parte del cierre V1 salvo decision nueva:

- amigos
- invitaciones a amigos
- reacciones en chat
- moderadores de sala
- roles avanzados en sala
- notificaciones push
- salas favoritas
- continuar viendo
- suscripciones
- CDN para contenido propio
- escalado horizontal de SignalR
- workers background
- sistema de colas

## Cierre minimo recomendado para beta cerrada

Antes de llamar "beta cerrada" al proyecto, faltaria como minimo:

- probar el flujo sala + video + chat en web y mobile
- arreglar link de invitacion con `?code=`
- remover plantilla Blazor del admin
- agregar pruebas basicas de backend y realtime
- definir correo real o una decision explicita de beta con links en logs
- definir deploy staging con secretos seguros
- configurar builds Android/iOS si la beta incluye mobile nativo
- documentar bugs conocidos despues de una corrida manual
