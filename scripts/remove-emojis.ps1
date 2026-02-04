# Script to remove all emoji characters from C# files
$ErrorActionPreference = 'Stop'

Write-Host "=== Removing Emoji Characters from Code ===" -ForegroundColor Cyan

$workspacePath = "d:\Storage Market Intel\Alfanar.MarketIntel"
$affectedFiles = @()

# Define emoji replacements (emoji -> replacement text)
$emojiReplacements = @{
    "✓" = ""
    "✅" = ""
    "❌" = ""
    "⚠️" = ""
    "💾" = ""
    "ℹ️" = ""
    "🔄" = ""
    "📦" = ""
    "📄" = ""
    "📝" = ""
    "🧪" = ""
    "🔍" = ""
}

# Find all C# files
$csFiles = Get-ChildItem -Path $workspacePath -Filter "*.cs" -Recurse | Where-Object { 
    $_.FullName -notlike "*\obj\*" -and 
    $_.FullName -notlike "*\bin\*" 
}

Write-Host "Found $($csFiles.Count) C# files to scan" -ForegroundColor Yellow

foreach ($file in $csFiles) {
    $content = Get-Content -Path $file.FullName -Raw -Encoding UTF8
    $originalContent = $content
    $modified = $false
    
    # Remove each emoji
    foreach ($emoji in $emojiReplacements.Keys) {
        if ($content.Contains($emoji)) {
            $content = $content.Replace($emoji, $emojiReplacements[$emoji])
            $modified = $true
        }
    }
    
    if ($modified) {
        Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -NoNewline
        $affectedFiles += $file.FullName
        Write-Host "  Cleaned: $($file.FullName)" -ForegroundColor Green
    }
}

Write-Host "`nEmoji Cleanup Complete!" -ForegroundColor Green
Write-Host "  Modified files: $($affectedFiles.Count)" -ForegroundColor Cyan
Write-Host "`nModified files:" -ForegroundColor Yellow
$affectedFiles | ForEach-Object { Write-Host "  - $_" }
