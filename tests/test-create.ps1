#!/usr/bin/env pwsh
# Test script for PackageSmith CLI

$ErrorActionPreference = "Stop"

Write-Host "`n=== PackageSmith CLI Test ===" -ForegroundColor Cyan

# Build the project
Write-Host "`n[1/3] Building..." -ForegroundColor Yellow
dotnet build /mnt/5f79a6c2-0764-4cd7-88b4-12dbd1b39909/packagesmith/PackageSmith.sln

# Check if config exists
$configPath = "$env:APPDATA/PackageSmith/config.json"
if ($IsLinux -or $IsMacOS) {
    $configPath = "$env:HOME/.config/PackageSmith/config.json"
}

if (Test-Path $configPath) {
    Write-Host "`n[2/3] Config exists at: $configPath" -ForegroundColor Green
    Write-Host "Config contents:"
    Get-Content $configPath | Write-Host
} else {
    Write-Host "`n[2/3] No config found - would trigger wizard" -ForegroundColor Yellow
}

# Test create command with pre-filled values (using echo to pipe input)
Write-Host "`n[3/3] Testing create command..." -ForegroundColor Yellow

# Test with explicit options (non-interactive)
Write-Host "`n--- Test: Create with explicit options ---" -ForegroundColor Cyan
dotnet run --project /mnt/5f79a6c2-0764-4cd7-88b4-12dbd1b39909/packagesmith/src/PackageSmith/PackageSmith.csproj -- create com.test.dummypackage -d "Test package for verification" -m Runtime --no-wizard

Write-Host "`n=== Test Complete ===" -ForegroundColor Green
