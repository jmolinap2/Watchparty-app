<#
.SYNOPSIS
    Detiene el entorno de desarrollo local de WatchParty.

.DESCRIPTION
    1. Detiene la app web Next.js (apps/web) que escucha en el puerto 4321.
    2. Detiene Postgres y Redis de docker compose.

    No detiene Visual Studio, API ni Admin. Esos procesos se manejan desde VS.

.PARAMETER SkipInfrastructure
    No detiene Postgres ni Redis.

.EXAMPLE
    .\Stop-Dev.ps1

.EXAMPLE
    .\Stop-Dev.ps1 -SkipInfrastructure
#>
param(
    [switch]$SkipInfrastructure
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$webPort = 4321

function Write-Step {
    param(
        [string]$Message,
        [string]$Color = "Yellow"
    )

    Write-Host "  $Message" -ForegroundColor $Color
}

function Get-ListeningProcessIds {
    param([int]$Port)

    try {
        return @(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction Stop |
            Select-Object -ExpandProperty OwningProcess -Unique |
            Where-Object { $_ -gt 0 })
    } catch {
        $rows = @(netstat -ano -p tcp | Select-String ":$Port\s+.*LISTENING\s+(\d+)")
        return @($rows | ForEach-Object {
            if ($_.Line -match "LISTENING\s+(\d+)$") {
                [int]$Matches[1]
            }
        } | Select-Object -Unique)
    }
}

function Stop-ProcessTreeById {
    param([int]$ProcessId)

    $children = @(Get-CimInstance Win32_Process -Filter "ParentProcessId = $ProcessId" -ErrorAction SilentlyContinue)
    foreach ($child in $children) {
        Stop-ProcessTreeById -ProcessId $child.ProcessId
    }

    $process = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
    if ($process) {
        Stop-Process -Id $ProcessId -Force -ErrorAction SilentlyContinue
    }
}

function Stop-AppWeb {
    $processIds = @(Get-ListeningProcessIds -Port $webPort)
    if ($processIds.Count -eq 0) {
        Write-Step "App web no esta escuchando en el puerto $webPort." "DarkGray"
        return
    }

    foreach ($processId in $processIds) {
        $processInfo = Get-CimInstance Win32_Process -Filter "ProcessId = $processId" -ErrorAction SilentlyContinue
        $description = if ($processInfo) { $processInfo.CommandLine } else { "PID $processId" }
        Write-Step "Deteniendo app web en puerto $webPort (PID $processId)." "Yellow"
        Write-Host "    $description" -ForegroundColor DarkGray
        Stop-ProcessTreeById -ProcessId $processId
    }
}

function Stop-AppWebTerminals {
    $webPath = Join-Path $root "apps\web"
    $escapedWebPath = $webPath.Replace("\", "\\")
    $terminalProcesses = @(Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
        Where-Object {
            $_.CommandLine -and
            $_.CommandLine -match [regex]::Escape($webPath) -and
            $_.CommandLine -match "pnpm\s+dev"
        })

    foreach ($terminal in $terminalProcesses) {
        Write-Step "Cerrando terminal de app web (PID $($terminal.ProcessId))." "Yellow"
        Stop-ProcessTreeById -ProcessId $terminal.ProcessId
    }
}

Write-Host ""
Write-Host "  WatchParty - Detener desarrollo local" -ForegroundColor Cyan
Write-Host "  ======================================" -ForegroundColor Cyan
Write-Host ""

Stop-AppWeb
Stop-AppWebTerminals

if (-not $SkipInfrastructure) {
    Write-Host ""
    Write-Step "Deteniendo Postgres + Redis..." "Yellow"
    docker compose -f "$root\docker-compose.yml" stop postgres redis
    if ($LASTEXITCODE -ne 0) {
        Write-Step "No se pudo detener docker compose. Revisa Docker Desktop." "Red"
        exit 1
    }

    Write-Step "Infraestructura detenida." "Green"
} else {
    Write-Step "Infraestructura omitida por -SkipInfrastructure." "DarkGray"
}

Write-Host ""
Write-Step "Listo. Visual Studio/API/Admin no fueron detenidos." "Green"
Write-Host ""
