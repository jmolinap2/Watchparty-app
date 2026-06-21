# WatchParty Web (Next.js)

Cliente web para PC de WatchParty: autenticacion, salas, player sincronizado, chat en vivo y presencia por SignalR.

## Stack

- Next.js 15 con App Router, React 19 y TypeScript
- TanStack Query para estado remoto
- Zustand para sesion de autenticacion
- `@microsoft/signalr` para realtime
- `hls.js` para reproduccion HLS
- Tailwind CSS v4

## Inicio local

```bash
pnpm install
cp .env.example .env.local
pnpm dev
```

La app corre en:

```text
http://localhost:4321
```

La API debe estar corriendo y debe permitir `http://localhost:4321` en `Cors:AllowedOrigins`. La configuracion de desarrollo del backend ya incluye ese origen por defecto y tambien permite Dev Tunnels (`*.devtunnels.ms`) para pruebas desde celular.

Para probar desde un celular, usa el Dev Tunnel de la web, no el del admin. Si el API tunnel es:

```text
https://abc123-44331.use2.devtunnels.ms
```

la web suele quedar en:

```text
https://abc123-4321.use2.devtunnels.ms
```

Tambien puedes iniciar con:

```powershell
.\Start-Dev.ps1 -ApiTunnelUrl "https://abc123-44331.use2.devtunnels.ms"
```

El script configura `NEXT_PUBLIC_API_URL` e imprime la URL recomendada para el celular.

## Ambiente

| Variable | Descripcion | Valor por defecto |
|---|---|---|
| `NEXT_PUBLIC_API_URL` | URL base de la API, sin slash final | `https://localhost:44331` |

## Scripts

| Comando | Uso |
|---|---|
| `pnpm dev` | Servidor de desarrollo |
| `pnpm build` | Build productivo |
| `pnpm start` | Sirve build productivo |
| `pnpm typecheck` | `tsc --noEmit` |

Para la imagen Docker, el build usa `BUILD_STANDALONE=true`.

## Sincronizacion de playback

El servidor define el estado oficial. El hub emite `PlaybackStateChanged` con:

- `status`
- `positionSeconds`
- `serverTimestampUtc`
- `version`

El cliente solo aplica estados con una version mas nueva. Calcula la posicion actual a partir del timestamp del servidor y hace `seek` si el desfase supera `0.75` segundos. La logica vive en:

```text
src/lib/realtime/sync.ts
```

Las acciones locales de play, pause y seek se envian al hub. El hub valida y reemite el estado oficial a todos los clientes.

Google Drive y MEGA usan embed y no exponen una superficie de control completa; por eso pueden mostrarse como fuentes no controlables para sincronizacion estricta.
