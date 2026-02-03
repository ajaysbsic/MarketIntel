# Diagnostic Script to List Available Google AI Models

$apiKey = "AIzaSyCqynZZDEObvPPE6Wl_lZA3Ezyx_Hneywo"
$url = "https://generativelanguage.googleapis.com/v1beta/models?key=$apiKey"

Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host "Google AI Models Diagnostic Script" -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host "Querying: $url" -ForegroundColor Yellow

try {
    $response = Invoke-RestMethod -Uri $url -Method Get
    
    if ($response.models) {
        Write-Host "`n--- AVAILABLE MODELS FOR YOUR API KEY ---" -ForegroundColor Green
        
        $generationModels = @()
        
        foreach ($model in $response.models) {
            if ($model.supportedGenerationMethods -contains "generateContent") {
                $modelName = $model.name.Replace("models/", "")
                $generationModels += $modelName
                
                Write-Host "`nModel: $modelName" -ForegroundColor Green
                Write-Host "Display: $($model.displayName)" -ForegroundColor White
            }
        }
        
        if ($generationModels.Count -gt 0) {
            Write-Host "`n======================================================================" -ForegroundColor Green
            Write-Host "RECOMMENDATION - Use this in appsettings.json:" -ForegroundColor Green
            Write-Host "======================================================================" -ForegroundColor Green
            Write-Host $generationModels[0] -ForegroundColor Yellow
            Write-Host "Endpoint: v1beta (NOT v1)" -ForegroundColor Yellow
        }
    }
} catch {
    Write-Host "`nError: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Check API key at https://aistudio.google.com/app/apikeys" -ForegroundColor Yellow
}
