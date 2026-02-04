$body = @{
    companyName = "Test Company"
    reportType = "Financial Report"
    title = "Q4 2024 Blob Storage Test"
    sourceUrl = "https://www.example.com"
    downloadUrl = "https://www.example.com/test.pdf"
    fiscalYear = 2024
    fiscalQuarter = "Q4"
    fileContent = "JVBERi0xLjQKJeLjz9MKMyAwIG9iago8PC9UeXBlIC9QYWdlCi9QYXJlbnQgMSAwIFIKL1Jlc291cmNlcyAyIDAgUgovTWVkaWFCb3ggWzAgMCA1OTUuMjggODQxLjg5XQovQ29udGVudHMgNCAwIFIKPj4KZW5kb2JqCjQgMCBvYmoKPDwvTGVuZ3RoIDQ0Pj4Kc3RyZWFtCkJUCi9GMSAxOCBUZgowIDAgVGQKKFRlc3QgRmluYW5jaWFsIFJlcG9ydCkgVGoKRVQKZW5kc3RyZWFtCmVuZG9iago1IDAgb2JqCjw8L1R5cGUgL0ZvbnQKL1N1YnR5cGUgL1R5cGUxCi9CYXNlRm9udCAvSGVsdmV0aWNhCj4+CmVuZG9iagoyIDAgb2JqCjw8L0ZvbnQgPDwvRjEgNSAwIFI+Pgo+PgplbmRvYmoKMSAwIG9iago8PC9UeXBlIC9QYWdlcwovQ291bnQgMQovS2lkcyBbMyAwIFJdCj4+CmVuZG9iagp4cmVmCjAgNgowMDAwMDAwMDAwIDY1NTM1IGYgCjAwMDAwMDAyODggMDAwMDAgbiAKMDAwMDAwMDI0MCAwMDAwMCBuIAAwMDAwMDAwMDA5IDAwMDAwIG4gCjAwMDAwMDAxMTggMDAwMDAgbiAKMDAwMDAwMDE5MCAwMDAwMCBuIAp0cmFpbGVyCjw8L1NpemUgNgovUm9vdCA2IDAgUgo+PgpzdGFydHhyZWYKMzQ1CiUlRU9G"
    fileName = "blob-test.pdf"
}

Write-Host "Sending request..." -ForegroundColor Yellow
Write-Host "Body: " -NoNewline
$body | ConvertTo-Json | Write-Host

try {
    $response = Invoke-WebRequest `
        -Uri "http://localhost:5021/api/reports/ingest" `
        -Method POST `
        -ContentType "application/json" `
        -Body ($body | ConvertTo-Json) `
        -UseBasicParsing `
        -ErrorAction Stop
    
    Write-Host "SUCCESS!" -ForegroundColor Green
    $response.Content | ConvertFrom-Json | ConvertTo-Json -Depth 5
    
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    Write-Host "Response:" -ForegroundColor Red
    
    $result = $_.Exception.Response.GetResponseStream()
    $reader = New-Object System.IO.StreamReader($result)
    $responseBody = $reader.ReadToEnd()
    Write-Host $responseBody -ForegroundColor Yellow
}
