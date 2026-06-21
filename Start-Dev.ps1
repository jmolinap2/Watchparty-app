<#
.SYNOPSIS
    Inicia el entorno de desarrollo local de WatchParty.

.DESCRIPTION
    1. Levanta Postgres y Redis via Docker (solo infraestructura).
    2. Abre la app web Next.js (apps/web) con pnpm dev en una nueva ventana.
    3. Muestra las URLs de acceso locales, LAN y Dev Tunnel.

.PARAMETER ApiTunnelUrl
    Si tienes un Dev Tunnel activo en VS, pasa la URL publica del API aqui.
    Ejemplo: .\Start-Dev.ps1 -ApiTunnelUrl "https://abc123-44331.devtunnels.ms"
    Esto permite probar desde el celular u otros dispositivos.

.PARAMETER WebTunnelUrl
    URL publica del tunnel de la web. Si se omite y ApiTunnelUrl usa el formato
    de VS Dev Tunnels, se infiere reemplazando el puerto 44331 por 4321.

.PARAMETER TunnelHost
    Host base del Dev Tunnel, sin puerto. Ejemplo: 6rg9ml90.use2.devtunnels.ms.
    Con esto el script imprime https://6rg9ml90-4321.use2.devtunnels.ms
    para la app web y https://6rg9ml90-44331.use2.devtunnels.ms para el API.

.PARAMETER AdminTunnelUrl
    URL publica del tunnel del admin. Si se omite y ApiTunnelUrl usa el formato
    de VS Dev Tunnels, se infiere reemplazando el puerto 44331 por 44306.

.EXAMPLE
    # Desarrollo local normal
    .\Start-Dev.ps1

    # Con Dev Tunnel para cel / otros dispositivos
    .\Start-Dev.ps1 -ApiTunnelUrl "https://abc123-44331.devtunnels.ms"

    # Alias compatible con la version anterior
    .\Start-Dev.ps1 -TunnelUrl "https://abc123-44331.devtunnels.ms"
#>
param(
    [Alias("TunnelUrl")]
    [string]$ApiTunnelUrl = "",

    [string]$WebTunnelUrl = "",

    [string]$TunnelHost = "",

    [string]$AdminTunnelUrl = ""
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$webPort = 4321
$apiHttpsPort = 44331
$adminHttpsPort = 44306

function Normalize-Url {
    param([string]$Url)

    if ([string]::IsNullOrWhiteSpace($Url)) {
        return ""
    }

    return $Url.Trim().TrimEnd("/")
}

function Infer-DevTunnelUrl {
    param(
        [string]$SourceUrl,
        [int]$SourcePort,
        [int]$TargetPort
    )

    if ([string]::IsNullOrWhiteSpace($SourceUrl)) {
        return ""
    }

    try {
        $uri = [Uri]$SourceUrl
        $sourceMarker = "-$SourcePort."
        if (-not $uri.Host.Contains($sourceMarker)) {
            return ""
        }

        $targetHost = $uri.Host.Replace($sourceMarker, "-$TargetPort.")
        return "$($uri.Scheme)://$targetHost"
    } catch {
        return ""
    }
}

function New-DevTunnelUrl {
    param(
        [string]$Host,
        [int]$Port
    )

    if ([string]::IsNullOrWhiteSpace($Host)) {
        return ""
    }

    $cleanHost = $Host.Trim()
    if ($cleanHost.StartsWith("https://", [StringComparison]::OrdinalIgnoreCase)) {
        $cleanHost = $cleanHost.Substring("https://".Length)
    }

    if ($cleanHost.StartsWith("http://", [StringComparison]::OrdinalIgnoreCase)) {
        $cleanHost = $cleanHost.Substring("http://".Length)
    }

    $cleanHost = $cleanHost.TrimEnd("/")

    if ($cleanHost -match "-\d+\.") {
        $cleanHost = $cleanHost -replace "-\d+\.", "."
    }

    $firstDotIndex = $cleanHost.IndexOf(".")
    if ($firstDotIndex -lt 1) {
        return ""
    }

    $prefix = $cleanHost.Substring(0, $firstDotIndex)
    $suffix = $cleanHost.Substring($firstDotIndex)
    return "https://$prefix-$Port$suffix"
}

function Read-WebApiUrlFromEnvLocal {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return ""
    }

    $line = Get-Content -LiteralPath $Path |
        Where-Object { $_ -match "^\s*NEXT_PUBLIC_API_URL\s*=" } |
        Select-Object -First 1

    if ([string]::IsNullOrWhiteSpace($line)) {
        return ""
    }

    return Normalize-Url (($line -split "=", 2)[1].Trim().Trim('"').Trim("'"))
}

function Get-PrimaryLanIp {
    try {
        $ip = Get-NetIPAddress -AddressFamily IPv4 |
            Where-Object {
                $_.AddressState -eq "Preferred" -and
                $_.IPAddress -notlike "127.*" -and
                $_.IPAddress -notlike "169.254.*"
            } |
            Sort-Object -Property InterfaceMetric, InterfaceAlias |
            Select-Object -First 1 -ExpandProperty IPAddress

        if (-not [string]::IsNullOrWhiteSpace($ip)) {
            return $ip
        }
    } catch {
        # Fall back to DNS below.
    }

    try {
        return [System.Net.Dns]::GetHostAddresses($env:COMPUTERNAME) |
            Where-Object {
                $_.AddressFamily -eq [System.Net.Sockets.AddressFamily]::InterNetwork -and
                $_.ToString() -notlike "127.*" -and
                $_.ToString() -notlike "169.254.*"
            } |
            Select-Object -First 1 |
            ForEach-Object { $_.ToString() }
    } catch {
        return ""
    }
}

function Write-Link {
    param(
        [string]$Label,
        [string]$Url,
        [string]$Color = "Green"
    )

    if (-not [string]::IsNullOrWhiteSpace($Url)) {
        Write-Host ("    {0,-24} ->  {1}" -f $Label, $Url) -ForegroundColor $Color
    }
}

$ApiTunnelUrl = Normalize-Url $ApiTunnelUrl
$WebTunnelUrl = Normalize-Url $WebTunnelUrl
$AdminTunnelUrl = Normalize-Url $AdminTunnelUrl
$TunnelHost = Normalize-Url $TunnelHost

$envLocal = "$root\apps\web\.env.local"
$existingWebApiUrl = Read-WebApiUrlFromEnvLocal $envLocal

if ($ApiTunnelUrl -eq "" -and $env:WATCHPARTY_API_TUNNEL_URL) {
    $ApiTunnelUrl = Normalize-Url $env:WATCHPARTY_API_TUNNEL_URL
}

if ($WebTunnelUrl -eq "" -and $env:WATCHPARTY_WEB_TUNNEL_URL) {
    $WebTunnelUrl = Normalize-Url $env:WATCHPARTY_WEB_TUNNEL_URL
}

if ($AdminTunnelUrl -eq "" -and $env:WATCHPARTY_ADMIN_TUNNEL_URL) {
    $AdminTunnelUrl = Normalize-Url $env:WATCHPARTY_ADMIN_TUNNEL_URL
}

if ($TunnelHost -eq "" -and $env:WATCHPARTY_TUNNEL_HOST) {
    $TunnelHost = Normalize-Url $env:WATCHPARTY_TUNNEL_HOST
}

if ($ApiTunnelUrl -eq "" -and $existingWebApiUrl -like "*.devtunnels.ms*") {
    $ApiTunnelUrl = $existingWebApiUrl
}

if ($TunnelHost -ne "") {
    if ($WebTunnelUrl -eq "") {
        $WebTunnelUrl = New-DevTunnelUrl $TunnelHost $webPort
    }

    if ($ApiTunnelUrl -eq "") {
        $ApiTunnelUrl = New-DevTunnelUrl $TunnelHost $apiHttpsPort
    }

    if ($AdminTunnelUrl -eq "") {
        $AdminTunnelUrl = New-DevTunnelUrl $TunnelHost $adminHttpsPort
    }
}

if ($WebTunnelUrl -eq "" -and $ApiTunnelUrl -ne "") {
    $WebTunnelUrl = Infer-DevTunnelUrl $ApiTunnelUrl $apiHttpsPort $webPort
}

if ($AdminTunnelUrl -eq "" -and $ApiTunnelUrl -ne "") {
    $AdminTunnelUrl = Infer-DevTunnelUrl $ApiTunnelUrl $apiHttpsPort $adminHttpsPort
}

if ($ApiTunnelUrl -eq "" -and $AdminTunnelUrl -ne "") {
    $ApiTunnelUrl = Infer-DevTunnelUrl $AdminTunnelUrl $adminHttpsPort $apiHttpsPort
}

if ($WebTunnelUrl -eq "" -and $AdminTunnelUrl -ne "") {
    $WebTunnelUrl = Infer-DevTunnelUrl $AdminTunnelUrl $adminHttpsPort $webPort
}

$lanIp = Get-PrimaryLanIp
$localWebUrl = "http://localhost:$webPort"
$lanWebUrl = if ($lanIp -ne "") { "http://$($lanIp):$webPort" } else { "" }
$apiBaseUrl = if ($ApiTunnelUrl -ne "") { $ApiTunnelUrl } else { "https://localhost:$apiHttpsPort" }
$cellWebUrl = if ($WebTunnelUrl -ne "") { $WebTunnelUrl } else { $lanWebUrl }

Write-Host ""
Write-Host "  WatchParty - Entorno de desarrollo" -ForegroundColor Cyan
Write-Host "  ====================================" -ForegroundColor Cyan
Write-Host ""

# 1. Infraestructura: solo Postgres + Redis
Write-Host "  [1/3] Levantando Postgres + Redis..." -ForegroundColor Yellow
docker compose -f "$root\docker-compose.yml" up postgres redis -d
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ERROR: No se pudo levantar la infraestructura. Docker Desktop esta corriendo?" -ForegroundColor Red
    exit 1
}
Write-Host "  Infraestructura lista." -ForegroundColor Green
Write-Host ""

# 2. Configurar URL del API en la web
Set-Content $envLocal "NEXT_PUBLIC_API_URL=$apiBaseUrl"
if ($ApiTunnelUrl -ne "") {
    Write-Host "  [2/3] Web configurada para usar API publica: $apiBaseUrl" -ForegroundColor Cyan
} else {
    Write-Host "  [2/3] Web configurada para API local ($apiBaseUrl)" -ForegroundColor Yellow
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
Write-Host "  URLs de acceso desde esta PC:" -ForegroundColor White
Write-Link "Web ver peliculas" $localWebUrl "Green"
Write-Link "API Swagger" "https://localhost:$apiHttpsPort/swagger" "Green"
Write-Link "Admin" "https://localhost:$adminHttpsPort" "Green"
if ($WebTunnelUrl -ne "") {
    Write-Link "Tunnel app web" $WebTunnelUrl "Cyan"
} else {
    Write-Host "    Tunnel app web          ->  No detectado. Pasa -WebTunnelUrl o -TunnelHost." -ForegroundColor DarkGray
}

if ($lanWebUrl -ne "") {
    Write-Host ""
    Write-Host "  URL LAN detectada:" -ForegroundColor White
    Write-Link "Web en tu red" $lanWebUrl "Yellow"
    Write-Host "    Si no abre en el cel, usa Dev Tunnel: IIS/Firewall suelen bloquear LAN." -ForegroundColor DarkYellow
}

if ($ApiTunnelUrl -ne "" -or $WebTunnelUrl -ne "" -or $AdminTunnelUrl -ne "") {
    Write-Host ""
    Write-Host "  Dev Tunnels:" -ForegroundColor Cyan
    Write-Link "Web CELULAR" $WebTunnelUrl "Cyan"
    Write-Link "API publica" $ApiTunnelUrl "Cyan"
    if ($ApiTunnelUrl -ne "") {
        Write-Link "API Swagger" "$ApiTunnelUrl/swagger" "Cyan"
    }
    Write-Link "Admin publico" $AdminTunnelUrl "Cyan"
}

Write-Host ""
Write-Host "  Usa en el celular:" -ForegroundColor White
if ($cellWebUrl -ne "") {
    Write-Link "Ver peliculas" $cellWebUrl "Green"
} else {
    Write-Host "    No se pudo detectar una URL para celular. Pasa -WebTunnelUrl o -ApiTunnelUrl." -ForegroundColor Red
}

Write-Host ""
Write-Host "  Para detener Postgres+Redis:  docker compose stop postgres redis" -ForegroundColor DarkGray
Write-Host ""
