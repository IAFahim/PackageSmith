#!/usr/bin/env pwsh

$ErrorActionPreference = "Stop"

function Write-ColorText {
    param([string]$Text, [string]$Color = "White")
    Write-Host $Text -ForegroundColor $Color
}

Write-ColorText "`n========================================" "Cyan"
Write-ColorText "  PackageSmith Installer for Windows" "Cyan"
Write-ColorText "========================================`n" "Cyan"

$MinDotnetVersion = [Version]"8.0.0"

function Test-DotnetInstalled {
    try {
        $versionOutput = dotnet --version 2>&1
        if ($LASTEXITCODE -eq 0) {
            $version = [Version]($versionOutput -replace '\+', '.')
            return $version
        }
    }
    catch {
        return $null
    }
    return $null
}

function Install-Dotnet {
    Write-ColorText ".NET SDK $MinDotnetVersion or higher is required." "Yellow"

    $useWinget = Read-Host "Install via winget? (Y/n)"
    if ($useWinget -ne "n") {
        Write-ColorText "`nInstalling .NET SDK via winget..." "Cyan"
        winget install Microsoft.DotNet.SDK.8

        if ($LASTEXITCODE -eq 0) {
            Write-ColorText "`n.NET SDK installed successfully!" "Green"
            Write-ColorText "Please restart your terminal and run this script again." "Yellow"
            exit 0
        }
    }

    Write-ColorText "`nManual installation required:" "Yellow"
    Write-ColorText "Download from: https://dotnet.microsoft.com/download/dotnet/8.0" "White"
    exit 1
}

function Build-Solution {
    Write-ColorText "`nBuilding PackageSmith..." "Cyan"

    $scriptRoot = $PSScriptRoot
    $slnPath = Join-Path $scriptRoot "PackageSmith.sln"

    if (-not (Test-Path $slnPath)) {
        Write-ColorText "Error: PackageSmith.sln not found at: $slnPath" "Red"
        exit 1
    }

    dotnet build $slnPath -c Release

    if ($LASTEXITCODE -ne 0) {
        Write-ColorText "Build failed!" "Red"
        exit 1
    }

    Write-ColorText "Build successful!" "Green"
}

function Install-Shim {
    Write-ColorText "`nInstalling pksmith shim..." "Cyan"

    $shimDir = Join-Path $env:USERPROFILE ".pksmith"
    $binDir = Join-Path $PSScriptRoot "src\PackageSmith.App\bin\Release\net9.0"
    $exePath = Join-Path $binDir "PackageSmith.App.exe"
    $destDir = Join-Path $shimDir "bin"
    $destExe = Join-Path $destDir "pksmith.exe"
    $shimPath = Join-Path $shimDir "pksmith.cmd"

    if (-not (Test-Path $exePath)) {
        Write-ColorText "Error: Built executable not found at: $exePath" "Red"
        exit 1
    }

    New-Item -ItemType Directory -Path $destDir -Force | Out-Null
    Copy-Item $exePath $destExe -Force

    $shimContent = @"
@echo off
"$destExe" %*
"@
    $shimContent | Out-File -FilePath $shimPath -Encoding ASCII

    $currentPath = [Environment]::GetEnvironmentVariable("Path", "User")
    if ($currentPath -notlike "*$shimDir*") {
        [Environment]::SetEnvironmentVariable("Path", "$currentPath;$shimDir", "User")
        Write-ColorText "Added $shimDir to user PATH" "Green"
        Write-ColorText "`nIMPORTANT: Restart your terminal for PATH changes to take effect!" "Yellow"
    }
    else {
        Write-ColorText "PATH already contains $shimDir" "Green"
    }

    Write-ColorText "`nShim installed to: $shimPath" "Green"
}

function Test-Installation {
    Write-ColorText "`nTesting installation..." "Cyan"

    $shimDir = Join-Path $env:USERPROFILE ".pksmith"
    $shimPath = Join-Path $shimDir "pksmith.cmd"

    if (-not (Test-Path $shimPath)) {
        Write-ColorText "Error: Shim not found at: $shimPath" "Red"
        return $false
    }

    & $shimPath --version
    if ($LASTEXITCODE -eq 0) {
        Write-ColorText "`nInstallation successful!" "Green"
        Write-ColorText "Run 'pksmith' from any directory to use." "Cyan"
        return $true
    }

    return $false
}

$dotnetVersion = Test-DotnetInstalled

if ($null -eq $dotnetVersion -or $dotnetVersion -lt $MinDotnetVersion) {
    Install-Dotnet
}

Build-Solution
Install-Shim
Test-Installation

Write-ColorText "`n========================================" "Cyan"
Write-ColorText "  Installation Complete!" "Green"
Write-ColorText "========================================`n" "Cyan"
