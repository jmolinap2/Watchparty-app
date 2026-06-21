# WatchParty App - Alcance del producto

Este documento define que construiremos, que queda fuera y en que orden avanzaremos.

La arquitectura tecnica oficial vive en:

```text
docs/architecture.md
```

El alcance pendiente observado en el repo vive en:

```text
docs/alcance-pendiente.md
```

Este archivo no debe duplicar decisiones tecnicas profundas. Su funcion es mantener claro el alcance del producto.

## 1. Vision

WatchParty App sera una aplicacion para ver videos sincronizados con otras personas desde movil y PC.

El producto permitira crear una sala, compartir una invitacion, reproducir un video permitido, sincronizar play/pause/seek y conversar en tiempo real.

El objetivo de la primera version es tener una beta cerrada funcional, distribuible y moderable.

## 2. Stack definido

| Capa | Tecnologia |
|---|---|
| Mobile | Expo + React Native + TypeScript |
| Web PC | Next.js + React + TypeScript |
| Backend | ASP.NET Core .NET 10 LTS |
| Admin | Blazor Server |
| Tiempo real | SignalR |
| Base de datos | PostgreSQL |
| Estado vivo/cache | Redis |
| Infraestructura local | Docker Compose |

Detalles de arquitectura, capas, SOLID, DDD, dependencias y reglas tecnicas estan en `docs/architecture.md`.

## 3. Alcance funcional principal

La aplicacion permitira:

- Registrar usuarios.
- Iniciar sesion.
- Crear salas.
- Entrar a salas por codigo o link.
- Salir de salas.
- Reproducir videos permitidos.
- Sincronizar play, pause y seek.
- Chatear dentro de una sala.
- Ver usuarios conectados en una sala.
- Tener un host de sala.
- Transferir el host.
- Expulsar usuarios de una sala.
- Reportar usuarios.
- Reportar mensajes.
- Guardar historial basico.
- Administrar usuarios y salas desde un panel admin.
- Ver logs y metricas basicas.

## 4. Fuentes de video permitidas

La V1 soportara contenido legalmente permitido. En el codigo actual se observan estos tipos de media:

- URLs directas permitidas, por ejemplo MP4.
- URLs HLS/m3u8 permitidas.
- YouTube mediante reproductor embebido.
- Google Drive mediante embed/WebView.
- MEGA mediante embed/WebView.
- Videos propios o alojados por el usuario en fuentes autorizadas.

La aplicacion validara:

- Protocolo HTTPS.
- Dominio permitido.
- Formato soportado o proveedor reconocido.
- URL segura.

## 5. Fuera de alcance

No se implementara en V1:

- Netflix.
- Disney+.
- Prime Video.
- HBO Max / Max.
- Reproduccion de contenido con DRM.
- Descarga de peliculas.
- Bypass de restricciones.
- Pirateria.
- Marketplace de contenido.
- Monetizacion.
- Amigos/red social completa.
- Moderacion automatica avanzada.
- A/B testing.
- Feature flags.
- Multilenguaje completo mas alla del i18n basico `es/en` ya observado en web/mobile.

Motivo: la V1 debe validar el nucleo del producto sin depender de licencias, DRM, acuerdos comerciales o sistemas sociales complejos.

## 6. Versiones

### V0 - Fundacion tecnica

Objetivo: levantar el esqueleto funcional del sistema.

Entregables:

- Monorepo.
- Backend ASP.NET Core.
- App mobile Expo.
- Web Next.js.
- Admin Blazor.
- PostgreSQL local.
- Redis local.
- Docker Compose.
- Health checks.
- Swagger.
- Configuracion de ambientes.
- CI inicial.

Resultado esperado: sistema vacio, pero tecnicamente conectado.

### MVP - Nucleo usable

Objetivo: que dos personas puedan ver un video sincronizado y chatear.

Entregables:

- Registro.
- Login.
- Crear sala.
- Entrar a sala por codigo.
- Entrar a sala por link.
- Salir de sala.
- Cargar video MP4/HLS permitido.
- Play sincronizado.
- Pause sincronizado.
- Seek sincronizado.
- Chat basico.
- Lista de usuarios conectados.
- Host de sala.
- Estado vivo en Redis.
- Persistencia basica en PostgreSQL.
- Cliente mobile funcional.
- Cliente web funcional.

Resultado esperado: app funcional para uso privado.

### V1 - Beta cerrada distribuible

Objetivo: publicar una beta cerrada con moderacion, admin y operacion basica.

Entregables:

- Perfiles.
- Avatar.
- Historial de salas.
- Historial de videos.
- Transferencia de host.
- Expulsion de usuarios.
- Reporte de usuario.
- Reporte de mensaje.
- Salas privadas.
- Recuperacion de contrasena.
- Confirmacion de email.
- Panel admin.
- Gestion de usuarios.
- Gestion de salas.
- Gestion de reportes.
- Logs.
- Metricas basicas.
- Respaldos.
- Seguridad base.
- Deploy staging.
- Deploy produccion.
- Build Android.
- Build iOS.

Resultado esperado: producto listo para beta cerrada.

### Post V1

Despues de validar la beta, se evaluaran estas lineas de crecimiento:

- Amigos.
- Invitaciones a amigos.
- Reacciones en chat.
- Moderadores de sala.
- Roles avanzados en sala.
- Notificaciones push.
- Salas favoritas.
- Continuar viendo.
- Suscripciones.
- CDN para contenido propio.
- Escalado horizontal de SignalR.
- Workers background.
- Sistema de colas.

Estas funciones no forman parte del compromiso de V1.

## 7. Modulos funcionales

### Identity

Responsable de:

- Registro.
- Login.
- Refresh token.
- Logout.
- Recuperacion de contrasena.
- Confirmacion de email.

### Users

Responsable de:

- Perfil.
- Nombre visible.
- Avatar.
- Privacidad basica.
- Usuarios bloqueados.

### Rooms

Responsable de:

- Crear sala.
- Unirse a sala.
- Salir de sala.
- Cerrar sala.
- Transferir host.
- Expulsar usuario.
- Generar codigo de invitacion.
- Validar permisos de sala.

### Playback

Responsable de:

- Cargar video.
- Play.
- Pause.
- Seek.
- Cambiar video.
- Sincronizar estado entre usuarios.
- Mantener estado oficial de reproduccion.

### Chat

Responsable de:

- Enviar mensaje.
- Recibir mensaje.
- Consultar historial.
- Eliminar mensaje propio o moderado.
- Reportar mensaje.
- Estado typing en fases posteriores.

### Presence

Responsable de:

- Usuarios conectados.
- Usuarios activos por sala.
- Reconexion.
- Limpieza de conexiones muertas.

### Reports

Responsable de:

- Reportar usuario.
- Reportar mensaje.
- Revisar reporte.
- Resolver reporte.
- Rechazar reporte.

### Admin

Responsable de:

- Gestion de usuarios.
- Gestion de salas.
- Gestion de reportes.
- Logs.
- Metricas basicas.
- Configuracion de dominios permitidos.

## 8. Pantallas principales

### Mobile

Pantallas V1:

- Splash.
- Login.
- Registro.
- Recuperar contrasena.
- Home.
- Crear sala.
- Unirse a sala.
- Sala.
- Player.
- Chat.
- Perfil.
- Editar perfil.
- Historial.
- Reportar usuario.
- Reportar mensaje.
- Configuracion.

### Web PC

Pantallas V1:

- Login.
- Registro.
- Home.
- Crear sala.
- Unirse a sala.
- Sala con player y chat lateral.
- Perfil.
- Historial.
- Reportes.

### Admin

Pantallas V1:

- Dashboard.
- Usuarios.
- Detalle de usuario.
- Salas activas.
- Detalle de sala.
- Reportes.
- Detalle de reporte.
- Dominios permitidos.
- Logs.
- Metricas basicas.

## 9. Reglas de producto

- El servidor define el estado oficial del video.
- El cliente no decide por si solo el estado final de reproduccion.
- Solo contenido permitido puede reproducirse.
- Toda sala tiene un host.
- El host puede transferir su rol.
- El host puede expulsar usuarios.
- Los usuarios pueden reportar abuso.
- Las acciones administrativas importantes quedan auditadas.
- Redis guarda estado temporal.
- PostgreSQL guarda informacion permanente.

## 10. Seguridad minima de V1

La V1 debe incluir:

- HTTPS en produccion.
- JWT de vida corta.
- Refresh token rotativo.
- Hash seguro de contrasena.
- Rate limit basico.
- Validacion de entrada.
- Sanitizacion de mensajes visibles.
- CORS restringido.
- Logs de seguridad.
- Auditoria admin.
- Revocacion de sesion.

## 11. Metricas minimas de V1

El admin debe poder ver:

- Usuarios registrados.
- Usuarios activos.
- Salas creadas.
- Salas activas.
- Mensajes enviados.
- Reportes abiertos.
- Reportes resueltos.
- Errores de playback.
- Reconexiones SignalR.

## 12. Orden de desarrollo

### Fase 1 - Fundacion

- Crear estructura del repo.
- Crear backend.
- Crear mobile.
- Crear web.
- Crear admin.
- Crear Docker Compose.
- Conectar PostgreSQL.
- Conectar Redis.
- Agregar Swagger.
- Agregar health checks.

### Fase 2 - Identidad

- Registro.
- Login.
- JWT.
- Refresh token.
- Logout.
- Perfil basico.

### Fase 3 - Salas

- Crear sala.
- Generar codigo.
- Entrar a sala.
- Salir de sala.
- Listar miembros.
- Cerrar sala.

### Fase 4 - Tiempo real

- Configurar SignalR.
- Join room.
- Leave room.
- Presence.
- Reconexion.
- Estado temporal en Redis.

### Fase 5 - Player

- Reproducir MP4.
- Reproducir HLS.
- Play.
- Pause.
- Seek.
- Manejo de errores.

### Fase 6 - Sincronizacion

- Estado oficial servidor.
- Play sincronizado.
- Pause sincronizado.
- Seek sincronizado.
- Versionado de eventos.
- Recuperacion de estado al reconectar.

### Fase 7 - Chat

- Enviar mensaje.
- Recibir mensaje.
- Historial.
- Eliminar mensaje.
- Reportar mensaje.

### Fase 8 - Moderacion y admin

- Transferir host.
- Expulsar usuario.
- Reportar usuario.
- Resolver reportes.
- Gestionar usuarios.
- Gestionar salas.
- Ver logs y metricas.

### Fase 9 - Produccion

- Deploy staging.
- Pruebas reales.
- Correccion de bugs.
- Deploy produccion.
- Build Android.
- Build iOS.
- Beta cerrada.

## 13. Criterio de cierre de V1

La V1 se considera completa cuando:

- Un usuario puede registrarse e iniciar sesion.
- Un usuario puede crear una sala.
- Otro usuario puede entrar por link o codigo.
- Ambos pueden ver un MP4/HLS permitido.
- Play, pause y seek se sincronizan.
- Ambos pueden chatear.
- El sistema muestra usuarios conectados.
- El host puede transferir host.
- El host puede expulsar usuarios.
- Un usuario puede reportar abuso.
- El admin puede revisar usuarios, salas y reportes.
- Existen logs y metricas basicas.
- Hay deploy de produccion.
- Hay build Android.
- Hay build iOS.

## 14. Nucleo del producto

El nucleo que no debe perder prioridad es:

```text
Sala + Video + SignalR + Chat + Web + Mobile
```

Todo lo demas debe construirse despues de que ese flujo funcione de punta a punta.
