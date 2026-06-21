<#
.SYNOPSIS
    Inicia el entorno de desarrollo local de WatchParty.

.DESCRIPTION
    1. Levanta Postgres y Redis via Docker (solo infraestructura).
    2. Abre la app web Next.js (apps/web) con pnpm dev en una nueva ventana.
    3. Muestra las URLs de acceso.

    El API y el Admin los arrancas desde Visual Studio con el perfil "WP".

.PARAMETER TunnelUrl
    Si tienes un Dev Tunnel activo en VS, pasa la URL pública del API aquí.
    Ejemplo: .\Start-Dev.ps1 -TunnelUrl "https://abc123-44331.devtunnels.ms"
    Esto permite probar desde el celular u otros dispositivos.

.EXAMPLE
    # Desarrollo local normal
    .\Start-Dev.ps1

    # Con Dev Tunnel para cel / otros dispositivos
    .\Start-Dev.ps1 -TunnelUrl "https://abc123-44331.devtunnels.ms"
#>
param(
    [string]$TunnelUrl = ""
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

Write-Host ""
Write-Host "  WatchParty — Entorno de desarrollo" -ForegroundColor Cyan
Write-Host "  ====================================" -ForegroundColor Cyan
Write-Host ""

# 1. Infraestructura: solo Postgres + Redis
Write-Host "  [1/3] Levantando Postgres + Redis..." -ForegroundColor Yellow
docker compose -f "$root\docker-compose.yml" up postgres redis -d
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ERROR: No se pudo levantar la infraestructura. ¿Docker Desktop está corriendo?" -ForegroundColor Red
    exit 1
}
Write-Host "  Infraestructura lista." -ForegroundColor Green
Write-Host ""

# 2. Configurar URL del API en la web
$envLocal = "$root\apps\web\.env.local"
if ($TunnelUrl -ne "") {
    $cleanUrl = $TunnelUrl.TrimEnd("/")
    Set-Content $envLocal "NEXT_PUBLIC_API_URL=$cleanUrl"
    Write-Host "  [2/3] Web configurada para usar Dev Tunnel: $cleanUrl" -ForegroundColor Cyan
} else {
    Set-Content $envLocal "NEXT_PUBLIC_API_URL=https://localhost:44331"
    Write-Host "  [2/3] Web configurada para API local (https://localhost:44331)" -ForegroundColor Yellow
}
Write-Host ""

# 3. Iniciar la web en una nueva ventana de terminal
Write-Host "  [3/3] Iniciando la app web (pnpm dev)..." -ForegroundColor Yellow
$webPath = "$root\apps\web"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$webPath'; pnpm dev"
Write-Host ""

# Resumen
Write-Host "  ========================================" -ForegroundColor Cyan
Write-Host "  Todo listo. Arranca el API desde VS con el perfil 'WP'." -ForegroundColor Green
Write-Host ""
Write-Host "  URLs de acceso:" -ForegroundColor White
Write-Host "    Web (ver peliculas)  ->  http://localhost:4321" -ForegroundColor Green
Write-Host "    API Swagger          ->  https://localhost:44331/swagger" -ForegroundColor Green
Write-Host "    Admin                ->  https://localhost:44306" -ForegroundColor Green

if ($TunnelUrl -ne "") {
    Write-Host ""
    Write-Host "  Dev Tunnel activo:" -ForegroundColor Cyan
    Write-Host "    API publica          ->  $TunnelUrl" -ForegroundColor Cyan
    Write-Host "    (la web usa este URL para el cel y otros dispositivos)" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "  Para detener Postgres+Redis:  docker compose stop postgres redis" -ForegroundColor DarkGray
Write-Host ""
