# Guia de despliegue WatchParty

Este documento describe lo que existe en el repo para ejecucion local/staging y separa lo que queda pendiente para produccion. No asume proveedores cloud, pipelines de deploy ni servicios que aun no esten configurados.

## Estado actual

Piezas observadas:

- `docker-compose.yml` en la raiz.
- `backend/Dockerfile` para la API.
- `apps/web/Dockerfile` para el cliente Next.js.
- `infra/nginx/nginx.conf` como reverse proxy local/staging.
- `.github/workflows/ci.yml` con build/typecheck de backend, web y mobile.
- `apps/admin` como aplicacion Blazor Server separada.

Piezas no observadas en el repo actual:

- script de deploy a produccion
- configuracion TLS real
- `eas.json` para builds EAS de Expo
- endpoint `/metrics` Prometheus
- servicio SMTP/provider real
- backup automatizado productivo
- orquestacion productiva declarada

## Desarrollo local con Docker Compose

Requisitos:

- Docker y Docker Compose
- archivo `.env` basado en `.env.example`

Preparacion:

```bash
cp .env.example .env
docker-compose up --build
```

Servicios definidos:

| Servicio | Imagen/build | Responsabilidad |
|---|---|---|
| `postgres` | `postgres:16-alpine` | Base de datos |
| `redis` | `redis:7-alpine` | Presencia, playback vivo y metricas operativas |
| `api` | `backend/Dockerfile` | ASP.NET Core API + SignalR |
| `web` | `apps/web/Dockerfile` | Next.js standalone |
| `nginx` | `nginx:1.27-alpine` | Proxy publico local |

Accesos via nginx:

| Ruta | Destino |
|---|---|
| `http://localhost:8080/` | Web Next.js |
| `http://localhost:8080/api/` | API |
| `http://localhost:8080/hubs/room` | SignalR |
| `http://localhost:8080/health` | Health check API |
| `http://localhost:8080/swagger/` | Swagger si la API esta en `Development` |

Parar servicios:

```bash
docker-compose down
```

Parar y borrar volumenes:

```bash
docker-compose down -v
```

## Variables de entorno

Variables usadas por `.env.example` y `docker-compose.yml`:

| Variable | Valor local por defecto | Uso |
|---|---|---|
| `PUBLIC_ORIGIN` | `http://localhost:8080` | Origen publico usado por web/API |
| `PUBLIC_PORT` | `8080` | Puerto expuesto por nginx |
| `ASPNETCORE_ENVIRONMENT` | `Development` | Ambiente API |
| `POSTGRES_USER` | `watchparty` | Usuario PostgreSQL |
| `POSTGRES_PASSWORD` | `watchparty` | Password PostgreSQL |
| `POSTGRES_DB` | `watchparty` | Base PostgreSQL |
| `JWT_KEY` | `CHANGE_ME_USE_A_32_BYTE_MINIMUM_SECRET` | Llave JWT, minimo 32 bytes |
| `SEED_ADMIN_EMAIL` | `admin@aspnetboilerplate.com` | Email del admin inicial |
| `SEED_ADMIN_PASSWORD` | `123qwe` | Password admin inicial |
| `SEED_ADMIN_USERNAME` | `admin` | Usuario corto del admin inicial |

Para cualquier ambiente real, cambiar `JWT_KEY`, credenciales de PostgreSQL y password admin.

## API

Build Docker:

```bash
docker build -f backend/Dockerfile -t watchparty-api:local .
```

El contenedor:

- publica ASP.NET en `http://+:8080`
- instala `curl` para healthcheck
- ejecuta `WatchParty.Api.dll`
- aplica migraciones en startup si `Database:AutoMigrate=true`

Health checks disponibles:

```http
GET /health
GET /health/ready
```

## Web

Build Docker:

```bash
docker build -f apps/web/Dockerfile --build-arg NEXT_PUBLIC_API_URL=http://localhost:8080 -t watchparty-web:local apps/web
```

La imagen usa Next.js standalone:

- `pnpm build`
- `BUILD_STANDALONE=true`
- runtime con `node server.js`
- puerto interno `3000`

## Admin Blazor

`apps/admin` existe como app separada. No esta incluida en `docker-compose.yml` ni en `infra/nginx/nginx.conf`.

Ejecucion local:

```powershell
dotnet run --project apps\admin\WatchParty.Admin.csproj
```

Config relevante:

```json
{
  "BackendApi": {
    "BaseUrl": "https://localhost:44331"
  }
}
```

En `Development`, el panel apunta a la API por IIS Express (`https://localhost:44331`). El login admin acepta usuario o correo; con la semilla local por defecto se puede ingresar como `admin` o `admin@aspnetboilerplate.com` usando `123qwe`. Consume endpoints de metricas, usuarios, salas, reportes, dominios permitidos y auditoria.

Pendiente para despliegue admin:

- decidir si se publica detras de nginx
- agregar Dockerfile/servicio Compose si se quiere contenerizar
- ajustar `BackendApi:BaseUrl` por ambiente

## Mobile Expo

El cliente mobile no esta desplegado por Docker.

Ejecucion local:

```bash
cd apps/mobile
pnpm install
pnpm start
```

La URL de API por defecto vive en `apps/mobile/app.json`:

```json
{
  "expo": {
    "extra": {
      "apiUrl": "http://localhost:5210"
    }
  }
}
```

En dispositivo fisico se debe usar una IP accesible desde el telefono y permitir ese origen en CORS si aplica.

No se observo `eas.json`, por lo que builds EAS/tienda quedan pendientes.

## CI actual

Workflow: `.github/workflows/ci.yml`.

Jobs observados:

| Job | Pasos |
|---|---|
| Backend | `dotnet restore`, `dotnet build` de `WatchParty.Api.csproj` |
| Web | `pnpm install`, `pnpm typecheck`, `pnpm build` |
| Mobile | `pnpm install`, `pnpm exec tsc --noEmit` |

No se observaron pasos de:

- tests automatizados
- publish/push de imagenes Docker
- deploy staging
- deploy produccion
- build Android/iOS

## Produccion: alcance pendiente

Para produccion todavia faltan decisiones e implementacion. Esta lista es deliberadamente concreta y se basa en lo que no aparece configurado en el repo:

- Infraestructura destino: VPS, contenedores administrados, Kubernetes, Railway, Azure, AWS, etc.
- TLS/HTTPS real: certificados, renovacion y redireccion HTTP -> HTTPS.
- Secretos: migrar `JWT_KEY`, passwords y credenciales a un gestor de secretos o variables protegidas.
- Base de datos productiva: backups automaticos, restores probados y politica de retencion.
- Migraciones: decidir si se aplican en startup o en job separado antes de deploy.
- Redis productivo: persistencia, memoria maxima y estrategia si hay multiples instancias API.
- SignalR multi-instancia: sticky sessions o backplane Redis si se escala horizontalmente.
- Email real: reemplazar `LoggingEmailSender` por SMTP/provider.
- Observabilidad: logs centralizados, alertas y metricas externas.
- Rate limiting: no se observo middleware de rate limit.
- Builds mobile: configurar EAS y perfiles Android/iOS.
- Admin: incluir o no en despliegue, proteger acceso y eliminar dependencia de JWT pegado manualmente.

## Rollback pendiente

No hay manifiestos productivos versionados ni pipeline de deploy. Cuando existan imagenes y ambiente de staging/produccion, definir:

- tags inmutables por commit
- forma de volver a la imagen anterior
- compatibilidad de migraciones hacia atras
- verificacion post-deploy con `/health`
- pasos manuales de emergencia
