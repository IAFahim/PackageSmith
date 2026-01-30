#!/usr/bin/env pwsh

$ErrorActionPreference = "Stop"

function Write-ColorText {
    param([string]$Text, [string]$Color = "White")
    Write-Host $Text -ForegroundColor $Color
}

Write-ColorText "`n========================================" "Cyan"
Write-ColorText "  iupk Uninstaller for Windows" "Cyan"
Write-ColorText "========================================`n" "Cyan"

$shimDir = Join-Path $env:USERPROFILE ".iupk"

if (-not (Test-Path $shimDir)) {
    Write-ColorText "iupk is not installed." "Yellow"
    exit 0
}

Write-ColorText "Removing iupk shim..." "Cyan"

$currentPath = [Environment]::GetEnvironmentVariable("Path", "User")
if ($currentPath -like "*$shimDir*") {
    $newPath = $currentPath -replace [regex]::Escape("$shimDir;?"), ""
    [Environment]::SetEnvironmentVariable("Path", $newPath.Trim(';'), "User")
    Write-ColorText "Removed $shimDir from user PATH" "Green"
    Write-ColorText "`nIMPORTANT: Restart your terminal for PATH changes to take effect!" "Yellow"
}

Remove-Item -Recurse -Force $shimDir
Write-ColorText "Removed: $shimDir" "Green"

Write-ColorText "`n========================================" "Cyan"
Write-ColorText "  Uninstallation Complete!" "Green"
Write-ColorText "========================================`n" "Cyan"
