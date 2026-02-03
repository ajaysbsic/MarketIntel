#!/usr/bin/env pwsh

# Colors for output
$Green = "`e[32m"
$Yellow = "`e[33m"
$Red = "`e[31m"
$Blue = "`e[34m"
$Reset = "`e[0m"

Write-Host "${Blue}========================================${Reset}"
Write-Host "${Blue}Alfanar Market Intelligence - Full Build${Reset}"
Write-Host "${Blue}========================================${Reset}"
Write-Host ""

$startTime = Get-Date

# ============================================
# 1. Check Prerequisites
# ============================================
Write-Host "${Yellow}[1/6] Checking Prerequisites...${Reset}"

$nodeVersion = & 'C:\Program Files\nodejs\node.exe' --version 2>&1
$npmVersion = & 'C:\Program Files\nodejs\npm.cmd' --version 2>&1
$dotnetVersion = dotnet --version 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "${Red}✗ Prerequisites check failed${Reset}"
    Write-Host "Node.js: $nodeVersion"
    Write-Host "npm: $npmVersion"
    Write-Host ".NET: $dotnetVersion"
    exit 1
}

Write-Host "${Green}✓ Node.js $nodeVersion${Reset}"
Write-Host "${Green}✓ npm $npmVersion${Reset}"
Write-Host "${Green}✓ .NET $dotnetVersion${Reset}"
Write-Host ""

# ============================================
# 2. Install/Update Python Dependencies
# ============================================
Write-Host "${Yellow}[2/6] Setting up Python Environment...${Reset}"

Push-Location "python_watcher"

if (Test-Path "venv") {
    Write-Host "Activating existing virtual environment..."
    & "venv\Scripts\Activate.ps1"
} else {
    Write-Host "Creating virtual environment..."
    python -m venv venv
    & "venv\Scripts\Activate.ps1"
}

Write-Host "Installing Python dependencies..."
pip install -r requirements.txt --quiet

if ($LASTEXITCODE -eq 0) {
    Write-Host "${Green}✓ Python environment ready${Reset}"
} else {
    Write-Host "${Red}✗ Python setup failed${Reset}"
    exit 1
}

Pop-Location
Write-Host ""

# ============================================
# 3. Build .NET API
# ============================================
Write-Host "${Yellow}[3/6] Building .NET API...${Reset}"

Push-Location "Alfanar.MarketIntel.Api"

Write-Host "Restoring .NET packages..."
dotnet restore --nologo --verbosity quiet | Out-Null

Write-Host "Building .NET project..."
dotnet build -c Debug --nologo --verbosity quiet | Out-Null

if ($LASTEXITCODE -eq 0) {
    Write-Host "${Green}✓ .NET API built successfully${Reset}"
} else {
    Write-Host "${Red}✗ .NET build failed${Reset}"
    exit 1
}

Pop-Location
Write-Host ""

# ============================================
# 4. Install Angular Dependencies
# ============================================
Write-Host "${Yellow}[4/6] Installing Angular Dependencies...${Reset}"

Push-Location "Alfanar.MarketIntel.Dashboard"

if (Test-Path "node_modules") {
    Write-Host "node_modules found, updating..."
    & 'C:\Program Files\nodejs\npm.cmd' update --silent 2>&1 | Out-Null
} else {
    Write-Host "Installing Angular packages..."
    & 'C:\Program Files\nodejs\npm.cmd' install --silent 2>&1 | Out-Null
}

if ($LASTEXITCODE -eq 0) {
    Write-Host "${Green}✓ Angular dependencies installed${Reset}"
} else {
    Write-Host "${Red}✗ npm install failed${Reset}"
    exit 1
}

Pop-Location
Write-Host ""

# ============================================
# 5. Build Angular Production Bundle
# ============================================
Write-Host "${Yellow}[5/6] Building Angular Production Bundle...${Reset}"

Push-Location "Alfanar.MarketIntel.Dashboard"

Write-Host "Building production bundle..."
& 'C:\Program Files\nodejs\npm.cmd' run build:prod --silent 2>&1 | Out-Null

if ($LASTEXITCODE -eq 0) {
    Write-Host "${Green}✓ Angular build complete${Reset}"
    Write-Host "  Output: Alfanar.MarketIntel.Dashboard\dist\alfanar-dashboard"
} else {
    Write-Host "${Red}✗ Angular build failed${Reset}"
    exit 1
}

Pop-Location
Write-Host ""

# ============================================
# 6. Verify Configuration
# ============================================
Write-Host "${Yellow}[6/6] Verifying Configuration...${Reset}"

$checks = @(
    @{ Name = "Python requirements.txt"; Path = "python_watcher\requirements.txt" },
    @{ Name = ".NET appsettings.json"; Path = "Alfanar.MarketIntel.Api\appsettings.Development.json" },
    @{ Name = "Angular environment"; Path = "Alfanar.MarketIntel.Dashboard\src\environments\environment.ts" },
    @{ Name = "Angular dist"; Path = "Alfanar.MarketIntel.Dashboard\dist" }
)

$allGood = $true
foreach ($check in $checks) {
    if (Test-Path $check.Path) {
        Write-Host "${Green}✓ $($check.Name)${Reset}"
    } else {
        Write-Host "${Red}✗ $($check.Name) - NOT FOUND${Reset}"
        $allGood = $false
    }
}

Write-Host ""

if ($allGood) {
    Write-Host "${Green}========================================${Reset}"
    Write-Host "${Green}BUILD SUCCESSFUL!${Reset}"
    Write-Host "${Green}========================================${Reset}"
    Write-Host ""
    Write-Host "Next Steps:"
    Write-Host "  1. Start the .NET API:"
    Write-Host "     ${Yellow}dotnet run --project Alfanar.MarketIntel.Api${Reset}"
    Write-Host ""
    Write-Host "  2. Start the Python watcher:"
    Write-Host "     ${Yellow}cd python_watcher${Reset}"
    Write-Host "     ${Yellow}python src/rss_watcher.py${Reset}"
    Write-Host ""
    Write-Host "  3. Start the Angular dev server:"
    Write-Host "     ${Yellow}cd Alfanar.MarketIntel.Dashboard${Reset}"
    Write-Host "     ${Yellow}npm start${Reset}"
    Write-Host ""
    Write-Host "  4. Open browser to ${Yellow}http://localhost:4200${Reset}"
    Write-Host ""
    Write-Host "Build Duration: $([Math]::Round(((Get-Date) - $startTime).TotalSeconds, 2)) seconds"
    Write-Host ""
} else {
    Write-Host "${Red}BUILD FAILED - Some components are missing${Reset}"
    exit 1
}
