#!/usr/bin/env pwsh

# Quick development start script

$Yellow = "`e[33m"
$Green = "`e[32m"
$Reset = "`e[0m"

Write-Host "${Green}"
Write-Host "========================================${Reset}"
Write-Host "Alfanar Market Intelligence - Dev Startup${Reset}"
Write-Host "========================================${Reset}"
Write-Host ""

# Start 1: .NET API
Write-Host "${Yellow}Starting .NET API (http://localhost:5000)...${Reset}"
Start-Process powershell -ArgumentList '-NoExit', '-Command', 'cd Alfanar.MarketIntel.Api; dotnet run'
Start-Sleep -Seconds 3

# Start 2: Angular Dev Server
Write-Host "${Yellow}Starting Angular Dev Server (http://localhost:4200)...${Reset}"
Push-Location "Alfanar.MarketIntel.Dashboard"
& 'C:\Program Files\nodejs\npm.cmd' start
Pop-Location
