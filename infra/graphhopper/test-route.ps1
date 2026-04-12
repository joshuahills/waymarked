# Test GraphHopper routing API with a sample Edinburgh route
# Route: Edinburgh city centre (Waverley) to Arthur's Seat viewpoint

$ErrorActionPreference = "Stop"

# API endpoint
$GraphHopperUrl = "http://localhost:8989"

# Route coordinates (lat,lon)
$Start = "55.9533,-3.1883"  # Edinburgh Waverley Station
$End = "55.9444,-3.1618"    # Arthur's Seat viewpoint

# Profile to test
$Profile = "hike"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "GraphHopper Routing Test" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Route: Edinburgh Waverley → Arthur's Seat"
Write-Host "Profile: $Profile"
Write-Host ""

# Check if GraphHopper is running
Write-Host "Checking GraphHopper health..."
try {
    $null = Invoke-WebRequest -Uri "$GraphHopperUrl/health" -UseBasicParsing -TimeoutSec 5
    Write-Host "✅ GraphHopper is running" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "❌ GraphHopper is not responding at $GraphHopperUrl" -ForegroundColor Red
    Write-Host "   Start it with: docker-compose up -d" -ForegroundColor Yellow
    Write-Host "   Check logs: docker-compose logs graphhopper" -ForegroundColor Yellow
    exit 1
}

# Make routing request
Write-Host "Requesting route..."
$RouteUrl = "$GraphHopperUrl/route?point=$Start&point=$End&profile=$Profile&points_encoded=false&elevation=true&instructions=true&locale=en-GB"

try {
    $Response = Invoke-RestMethod -Uri $RouteUrl -Method Get
} catch {
    Write-Host "❌ Routing request failed: $_" -ForegroundColor Red
    exit 1
}

# Check for errors in response
if ($Response.message) {
    Write-Host "❌ Routing failed: $($Response.message)" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Route found!" -ForegroundColor Green
Write-Host ""

# Extract key metrics
$Path = $Response.paths[0]
$DistanceKm = [math]::Round($Path.distance / 1000, 2)
$TimeMin = [math]::Round($Path.time / 60000, 1)
$Ascent = if ($Path.ascend) { [math]::Round($Path.ascend, 0) } else { 0 }
$Descent = if ($Path.descend) { [math]::Round($Path.descend, 0) } else { 0 }

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Route Summary" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Distance:  $DistanceKm km"
Write-Host "Time:      $TimeMin minutes"
Write-Host "Ascent:    $Ascent m"
Write-Host "Descent:   $Descent m"
Write-Host ""

# Show first few instructions
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Turn-by-Turn Instructions (first 5)" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

$Instructions = $Path.instructions | Select-Object -First 5
foreach ($Instruction in $Instructions) {
    $DistanceM = [math]::Round($Instruction.distance, 0)
    Write-Host "$DistanceM m - $($Instruction.text)"
}
Write-Host ""

# Optionally save full response
$OutputFile = "test-route-response.json"
$Response | ConvertTo-Json -Depth 10 | Out-File -FilePath $OutputFile -Encoding UTF8
Write-Host "Full response saved to: $OutputFile"
Write-Host ""

# Show coordinate count (useful for route complexity)
$CoordCount = $Path.points.coordinates.Count
Write-Host "Route has $CoordCount coordinate points"

Write-Host ""
Write-Host "✅ Test complete!" -ForegroundColor Green
