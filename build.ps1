# 3D Builder - Build Script (PowerShell)
# Run: .\build.ps1
# Requires: .NET 8 SDK, Python 3.10+, pip

param(
    [switch]$Clean,
    [switch]$PythonOnly,
    [switch]$CSharpOnly
)

$ErrorActionPreference = "Stop"
$BuildDir  = "$PSScriptRoot\dist"
$PyBackend = "$PSScriptRoot\PythonBackend"
$CsProject = "$PSScriptRoot\CSharpUI\ThreeDBuilder.csproj"

Write-Host "=== 3D Builder Build ===" -ForegroundColor Cyan

# ── Clean ────────────────────────────────────────────────────────────────
if ($Clean) {
    Write-Host "Cleaning build artifacts..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force "$BuildDir"  -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force "$PSScriptRoot\CSharpUI\bin"  -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force "$PSScriptRoot\CSharpUI\obj"  -ErrorAction SilentlyContinue
    Write-Host "Clean done." -ForegroundColor Green
}

New-Item -ItemType Directory -Force -Path $BuildDir | Out-Null

# ── Python: install dependencies ─────────────────────────────────────────
if (-not $CSharpOnly) {
    Write-Host "`nInstalling Python dependencies..." -ForegroundColor Yellow
    Push-Location $PyBackend
    try {
        pip install -r requirements.txt
        Write-Host "Python dependencies installed." -ForegroundColor Green
    }
    finally { Pop-Location }
}

# ── C#: build WPF application ────────────────────────────────────────────
if (-not $PythonOnly) {
    Write-Host "`nBuilding C# WPF application..." -ForegroundColor Yellow
    Push-Location "$PSScriptRoot\CSharpUI"
    try {
        dotnet restore
        dotnet build -c Release
        dotnet publish -c Release -o "$BuildDir\app" --self-contained false
        Write-Host "C# build done. Output: $BuildDir\app" -ForegroundColor Green
    }
    finally { Pop-Location }

    # Copy Python backend to output
    Write-Host "`nCopying Python backend to output..." -ForegroundColor Yellow
    $OutPyDir = "$BuildDir\app\PythonBackend"
    New-Item -ItemType Directory -Force -Path $OutPyDir | Out-Null
    Copy-Item "$PyBackend\*" -Destination $OutPyDir -Recurse -Force
    Write-Host "Python backend copied." -ForegroundColor Green
}

Write-Host "`n=== BUILD COMPLETE ===" -ForegroundColor Green
Write-Host "Output directory: $BuildDir\app" -ForegroundColor Cyan
Write-Host ""
Write-Host "To run:"
Write-Host "  .\dist\app\ThreeDBuilder.exe"
Write-Host ""
Write-Host "Requirements on target machine:"
Write-Host "  - .NET 8 Runtime (https://dotnet.microsoft.com/download)"
Write-Host "  - Python 3.10+  (https://python.org)"
Write-Host "  - pip install cadquery"
Write-Host "  - OpenSCAD (optional, for SCAD editor)"
Write-Host ""
Write-Host "Creating Installer..."
Write-Host ""

# Check if WiX is installed
$wixPath = "C:\Program Files (x86)\WiX Toolset v3.11\bin"
if (Test-Path $wixPath) {
    Write-Host "WiX Toolset found. Creating MSI installer..." -ForegroundColor Yellow
    $env:BuildOutputPath = "$(Get-Location)\dist\app\"
    
    # Create installer directory if it doesn't exist
    New-Item -ItemType Directory -Force -Path "$BuildDir\installer" | Out-Null
    
    & "$wixPath\candle.exe" -o "$BuildDir\installer\" "Installer\Product.wxs"
    if ($LASTEXITCODE -eq 0) {
        & "$wixPath\light.exe" -out "$BuildDir\3DBuilderPro.msi" "$BuildDir\installer\Product.wixobj"
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Installer created: $BuildDir\3DBuilderPro.msi" -ForegroundColor Green
        } else {
            Write-Host "Error creating MSI file!" -ForegroundColor Red
        }
    } else {
        Write-Host "Error in candle process!" -ForegroundColor Red
    }
} else {
    Write-Host "WiX Toolset not found. Skipping MSI creation." -ForegroundColor Yellow
    Write-Host "Download from: https://wixtoolset.org/" -ForegroundColor Yellow
}
