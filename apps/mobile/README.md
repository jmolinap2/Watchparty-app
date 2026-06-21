# WatchParty Mobile (Expo)

Cliente mobile de WatchParty: autenticacion, salas, player sincronizado, chat en vivo y presencia por SignalR.

## Stack

- Expo SDK 56
- Expo Router
- React Native + TypeScript
- TanStack Query
- Zustand con sesion persistida mediante `expo-secure-store`
- `@microsoft/signalr` para realtime
- `expo-video` para MP4/HLS
- `react-native-youtube-iframe` para YouTube
- `react-native-webview` para Google Drive y MEGA

## Inicio local

```bash
pnpm install
pnpm start
```

Luego usar Expo Go, emulador Android o simulador iOS segun el entorno local.

Este paquete usa pnpm con `node-linker=hoisted` en `.npmrc` para que React Native resuelva modulos nativos correctamente.

## URL de API

La URL base viene de:

```text
app.json -> expo.extra.apiUrl
```

Valor por defecto actual:

```text
http://localhost:5210
```

Tambien se puede sobreescribir por build con:

```text
EXPO_PUBLIC_API_URL
```

En un dispositivo fisico, la API debe ser alcanzable desde el telefono. Normalmente se usa la IP LAN de la maquina, por ejemplo:

```text
http://192.168.1.50:5210
```

Si aplica, agregar ese origen a `Cors:AllowedOrigins` en la API.

## Pantallas

Pantallas observadas:

- Login
- Registro
- Recuperar password
- Home
- Crear sala
- Unirse a sala
- Sala con player, controles, chat, miembros y reportes
- Perfil
- Editar perfil
- Historial
- Configuracion
- Mis reportes

## Sincronizacion de playback

El modelo es el mismo que en web:

- el servidor es autoridad
- el cliente aplica estados versionados
- se calcula posicion objetivo con `serverTimestampUtc`
- se hace `seek` si el desfase supera `0.75` segundos

La logica vive en:

```text
src/lib/realtime/sync.ts
```

Google Drive y MEGA usan WebView/embed y pueden no ser controlables programaticamente para sincronizacion estricta.
