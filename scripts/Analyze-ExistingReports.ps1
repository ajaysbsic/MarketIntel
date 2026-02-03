# PowerShell script to analyze all existing reports
# Usage: .\Analyze-ExistingReports.ps1
# Compatible with PowerShell 5.1+

param(
    [string]$ApiUrl = "http://localhost:5021",
    [int]$MaxReports = 50,
    [int]$DelaySeconds = 3
)

# Handle SSL/TLS certificate issues for older PowerShell versions
if ($PSVersionTable.PSVersion.Major -lt 6) {
    # PowerShell 5.1 and earlier
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor [System.Net.SecurityProtocolType]::Tls12
    Add-Type @"
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    public class TrustAllCertsPolicy : ICertificatePolicy {
        public bool CheckValidationResult(
            ServicePoint srvPoint, X509Certificate certificate,
            WebRequest request, int certificateProblem) {
            return true;
        }
    }
"@
    [System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
}

Write-Host "===========================================
Market Intelligence - Batch Analysis Tool
===========================================" -ForegroundColor Cyan

Write-Host "`nConfiguration:" -ForegroundColor Yellow
Write-Host "API URL: $ApiUrl"
Write-Host "Max Reports: $MaxReports"
Write-Host "Delay between reports: ${DelaySeconds}s"
Write-Host "PowerShell Version: $($PSVersionTable.PSVersion.Major).$($PSVersionTable.PSVersion.Minor)"

# Step 1: Trigger batch analysis
Write-Host "`n[1/3] Triggering batch analysis..." -ForegroundColor Cyan

try {
    # For PowerShell 6+ use -SkipCertificateCheck, for 5.1 use the cert policy set above
    if ($PSVersionTable.PSVersion.Major -ge 6) {
        $response = Invoke-WebRequest `
            -Uri "$ApiUrl/api/reports/batch-analyze?maxCount=$MaxReports" `
            -Method POST `
            -ContentType "application/json" `
            -SkipCertificateCheck `
            -ErrorAction Stop
    } else {
        $response = Invoke-WebRequest `
            -Uri "$ApiUrl/api/reports/batch-analyze?maxCount=$MaxReports" `
            -Method POST `
            -ContentType "application/json" `
            -ErrorAction Stop
    }

    $result = $response.Content | ConvertFrom-Json
    
    Write-Host "`n? Batch Analysis Triggered!" -ForegroundColor Green
    Write-Host "Total Reports Found: $($result.totalProcessed)"
    Write-Host "Analyzed: $($result.analyzed)"
    Write-Host "Failed: $($result.failed)"
    
    if ($result.errors -and $result.errors.Count -gt 0) {
        Write-Host "`n??  Errors encountered:" -ForegroundColor Yellow
        $result.errors | ForEach-Object { Write-Host "  - $_" }
    }
}
catch {
    Write-Host "`n? Error triggering batch analysis:" -ForegroundColor Red
    Write-Host $_.Exception.Message
    exit 1
}

# Step 2: Monitor progress
Write-Host "`n[2/3] Waiting for analysis to complete..." -ForegroundColor Cyan

$startTime = Get-Date
$maxWaitSeconds = 3600  # Max 1 hour wait
$pollInterval = 5

while ((Get-Date) - $startTime -lt [timespan]::FromSeconds($maxWaitSeconds)) {
    try {
        if ($PSVersionTable.PSVersion.Major -ge 6) {
            $status = Invoke-WebRequest `
                -Uri "$ApiUrl/api/reports/pending?maxCount=5" `
                -Method GET `
                -SkipCertificateCheck `
                -ErrorAction Stop
        } else {
            $status = Invoke-WebRequest `
                -Uri "$ApiUrl/api/reports/pending?maxCount=5" `
                -Method GET `
                -ErrorAction Stop
        }
        
        $pending = $status.Content | ConvertFrom-Json
        
        # PowerShell-compatible null coalescing: check $pending.count first, then fallback
        if ($pending.count) {
            $pendingCount = $pending.count
        }
        elseif ($pending.data) {
            $pendingCount = $pending.data.Count
        }
        else {
            $pendingCount = 0
        }
        
        Write-Host "  Pending reports: $pendingCount" -ForegroundColor Yellow
        
        if ($pendingCount -eq 0) {
            Write-Host "? All reports analyzed!" -ForegroundColor Green
            break
        }
    }
    catch {
        # Silently continue polling
    }
    
    Start-Sleep -Seconds $pollInterval
}

# Step 3: Verify results
Write-Host "`n[3/3] Verifying analysis results..." -ForegroundColor Cyan

try {
    if ($PSVersionTable.PSVersion.Major -ge 6) {
        $reports = Invoke-WebRequest `
            -Uri "$ApiUrl/api/reports/recent?count=5" `
            -Method GET `
            -SkipCertificateCheck `
            -ErrorAction Stop
    } else {
        $reports = Invoke-WebRequest `
            -Uri "$ApiUrl/api/reports/recent?count=5" `
            -Method GET `
            -ErrorAction Stop
    }
    
    $recent = $reports.Content | ConvertFrom-Json
    $withAnalysis = 0
    
    $recent | ForEach-Object {
        if ($_.analysis -and $_.analysis.executiveSummary) {
            $withAnalysis++
            Write-Host "`n? $($_.companyName)" -ForegroundColor Green
            Write-Host "   Title: $($_.title)"
            $summaryPreview = $_.analysis.executiveSummary.Substring(0, [Math]::Min(100, $_.analysis.executiveSummary.Length))
            Write-Host "   Summary: $summaryPreview..."
        }
    }
    
    Write-Host "`n? Batch analysis complete!" -ForegroundColor Green
    Write-Host "Reports with summaries: $withAnalysis / $($recent.Count)"
}
catch {
    Write-Host "`n??  Could not verify results:" -ForegroundColor Yellow
    Write-Host $_.Exception.Message
}

Write-Host "`n===========================================" -ForegroundColor Cyan
Write-Host "Dashboard is now ready with AI summaries!"
Write-Host "Open: http://localhost:5021/alerts.html" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan
