# test-ci-config.ps1
# Simulates the CI graph pre-build step locally.
# Run from the repo root:  .\infra\graphhopper\test-ci-config.ps1
#
# What it does:
#   1. Copies config-ci.yml over config.yml (in a temp dir - doesn't touch your local config)
#   2. Downloads the IoW OSM PBF if not already in the temp dir
#   3. Builds the graphhopper-ci Docker image
#   4. Runs GraphHopper with the CI config and waits for it to become ready
#   5. Hits the route endpoint to confirm routing works
#   6. Cleans up

param(
    [string]$TempDir = "$PSScriptRoot\ci-test-data",
    [int]$TimeoutSeconds = 300,
    [string]$ContainerEngine = ""   # auto-detected: podman > docker
)

$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

# Auto-detect container engine (prefer podman, fall back to docker)
if (-not $ContainerEngine) {
    if (Get-Command podman -ErrorAction SilentlyContinue) { $ContainerEngine = "podman" }
    elseif (Get-Command docker -ErrorAction SilentlyContinue) { $ContainerEngine = "docker" }
    else { Write-Error "Neither podman nor docker found in PATH."; exit 1 }
}
Write-Host "==> Using container engine: $ContainerEngine" -ForegroundColor Cyan

Write-Host "==> CI config test" -ForegroundColor Cyan
Write-Host "    Temp data dir: $TempDir"

# 1. Prepare temp data dir with CI config
New-Item -ItemType Directory -Force -Path $TempDir | Out-Null
Copy-Item "$PSScriptRoot\config-ci.yml" "$TempDir\config.yml" -Force
Write-Host "==> Copied config-ci.yml -> $TempDir\config.yml" -ForegroundColor Green

# 2. Download IoW OSM PBF if missing
$pbf = "$TempDir\map.osm.pbf"
if (-not (Test-Path $pbf) -or (Get-Item $pbf).Length -lt 1MB) {
    if (Test-Path $pbf) { Remove-Item $pbf }
    $url = "https://download.geofabrik.de/europe/great-britain/england/isle-of-wight-latest.osm.pbf"
    Write-Host "==> Downloading Isle of Wight OSM (~8 MB)..." -ForegroundColor Yellow
    Write-Host "    Source: $url"
    try {
        # BITS behaves like a browser download — no User-Agent issues
        Start-BitsTransfer -Source $url -Destination $pbf -Description "IoW OSM PBF"
    } catch {
        Write-Host "    BITS failed ($_), trying curl.exe with browser UA..." -ForegroundColor Yellow
        curl.exe -L -A "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120.0 Safari/537.36" `
            -e "https://download.geofabrik.de/europe/great-britain/england.html" `
            -o $pbf $url
    }
    if (-not (Test-Path $pbf) -or (Get-Item $pbf).Length -lt 1MB) {
        Write-Host ""
        Write-Host "ERROR: Automated download failed." -ForegroundColor Red
        Write-Host "Please manually download the file from your browser:" -ForegroundColor Yellow
        Write-Host "  $url" -ForegroundColor Cyan
        Write-Host "Save it to: $pbf" -ForegroundColor Cyan
        Write-Host "Then re-run this script."
        exit 1
    }
    Write-Host "    Downloaded: $([math]::Round((Get-Item $pbf).Length/1MB,1)) MB" -ForegroundColor Green
} else {
    $sizeMB = [math]::Round((Get-Item $pbf).Length/1MB,1)
    Write-Host "==> IoW PBF already present ($sizeMB MB) — skipping download" -ForegroundColor Green
}

# 3. Build the container image
Write-Host "==> Building graphhopper-ci image..." -ForegroundColor Yellow
& $ContainerEngine build -t graphhopper-ci "$PSScriptRoot"
Write-Host "==> Image built" -ForegroundColor Green

# 4. Remove any stale container
& $ContainerEngine rm -f gh-ci-test 2>&1 | Out-Null

# 5. Start GraphHopper with CI config
Write-Host "==> Starting GraphHopper with CI config (no elevation)..." -ForegroundColor Yellow
& $ContainerEngine run -d --name gh-ci-test `
    -v "${TempDir}:/data" `
    -v "${TempDir}/config.yml:/data/config.yml:ro" `
    -p 8989:8989 `
    graphhopper-ci

Write-Host "==> Waiting up to ${TimeoutSeconds}s for GraphHopper to become ready..."
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
$ready = $false

while ((Get-Date) -lt $deadline) {
    try {
        $resp = Invoke-WebRequest `
            -Uri "http://localhost:8989/route?profile=foot&point=50.7017,-1.2986&point=50.72,-1.16" `
            -UseBasicParsing `
            -TimeoutSec 5 `
            -ErrorAction SilentlyContinue
        if ($resp.StatusCode -eq 200) {
            $ready = $true
            break
        }
    } catch { }
    Write-Host "    ... still building ($(([int]($deadline - (Get-Date)).TotalSeconds))s remaining)"
    Start-Sleep -Seconds 5
}

if (-not $ready) {
    Write-Host ""
    Write-Host "ERROR: GraphHopper did not become ready within ${TimeoutSeconds}s" -ForegroundColor Red
    Write-Host "--- Container logs ---" -ForegroundColor Red
    & $ContainerEngine logs gh-ci-test
    & $ContainerEngine rm -f gh-ci-test | Out-Null
    exit 1
}

Write-Host "==> GraphHopper is ready!" -ForegroundColor Green

# 6. Test the route
Write-Host "==> Testing route: Newport IoW -> Ryde IoW..." -ForegroundColor Yellow
$route = Invoke-RestMethod `
    -Uri "http://localhost:8989/route?profile=foot&point=50.7017,-1.2986&point=50.7274,-1.1616" `
    -UseBasicParsing
$distKm = [math]::Round($route.paths[0].distance / 1000, 1)
$timeMin = [math]::Round($route.paths[0].time / 60000, 0)
Write-Host "    Route found: ${distKm} km, ~${timeMin} min" -ForegroundColor Green

# 7. Cleanup
Write-Host "==> Stopping and removing container..." -ForegroundColor Yellow
& $ContainerEngine stop gh-ci-test | Out-Null
& $ContainerEngine rm gh-ci-test | Out-Null

Write-Host ""
Write-Host "SUCCESS — CI config works correctly." -ForegroundColor Green
Write-Host "The graph will be at: $TempDir\graph-cache"
Write-Host "(ci-test-data\ is git-ignored — safe to leave or delete)"
