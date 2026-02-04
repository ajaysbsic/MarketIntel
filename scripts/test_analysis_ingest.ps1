$apiUrl = "https://market-intel-api-grg6ceczgzd2cwdh.southeastasia-01.azurewebsites.net/api/reports/ingest"

$testPayload = @{
    companyName = "Test Company"
    reportType = "Financial Report"
    title = "Test Report with Analysis"
    sourceUrl = "https://example.com/test"
    downloadUrl = "https://example.com/test.pdf"
    pageCount = 10
    publishedDate = "2025-01-01T00:00:00Z"
    fiscalYear = 2025
    region = "Global"
    sector = "Technology"
    extractedText = "This is a test report text for analysis extraction."
    requiredOcr = $false
    language = "en"
    metadata = @{
        sections = @("Introduction", "Analysis", "Conclusion")
        scrape_date = ([DateTime]::UtcNow).ToString("o")
        watcher_version = "3.0"
        crawler_used = $true
        link_context = "test"
        analysis = @{
            executive_summary = "Test executive summary for AI analysis"
            strategic_initiatives = "Test strategic initiatives"
            market_outlook = "Test market outlook"
            risk_factors = "Test risk factors"
            competitive_position = "Test competitive position"
            investment_thesis = "Test investment thesis"
            sentiment_score = 0.75
            sentiment = "Positive"
            model = "gemini-2.5-flash"
            key_highlights = @("Highlight 1", "Highlight 2")
        }
    }
}

$jsonPayload = $testPayload | ConvertTo-Json -Depth 10

Write-Host "Sending test payload to: $apiUrl"
Write-Host "Payload: $jsonPayload" -ForegroundColor Cyan

try {
    $response = Invoke-RestMethod -Uri $apiUrl `
        -Method Post `
        -ContentType "application/json" `
        -Body $jsonPayload `
        -TimeoutSec 30
    
    Write-Host "Response Status: SUCCESS" -ForegroundColor Green
    Write-Host "Response: $($response | ConvertTo-Json)" -ForegroundColor Green
} catch {
    Write-Host "Response Status: ERROR" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $body = $reader.ReadToEnd()
        Write-Host "Error Body: $body" -ForegroundColor Red
    }
}
