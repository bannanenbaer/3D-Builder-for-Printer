#!/usr/bin/env pwsh
<#
.SYNOPSIS
    3D Builder Pro - Build & Installer Script
    
.DESCRIPTION
    Automatisiertes Build-Skript für die Erstellung des Installers
    
.PARAMETER Configuration
    Build-Konfiguration: Debug oder Release (Standard: Release)
    
.PARAMETER SkipTests
    Überspringe Tests (Standard: $false)
    
.PARAMETER BuildOnly
    Baue nur die Anwendung, nicht den Installer (Standard: $false)
    
.EXAMPLE
    .\build.ps1
    .\build.ps1 -Configuration Debug
    .\build.ps1 -BuildOnly
#>

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    
    [switch]$SkipTests,
    [switch]$BuildOnly
)

$ErrorActionPreference = "Stop"

# Farben für Output
$colors = @{
    Success = "Green"
    Error   = "Red"
    Warning = "Yellow"
    Info    = "Cyan"
}

function Write-Header {
    param([string]$Message)
    Write-Host "`n" -NoNewline
    Write-Host "╔" + ("═" * ($Message.Length + 2)) + "╗" -ForegroundColor $colors.Info
    Write-Host "║ $Message ║" -ForegroundColor $colors.Info
    Write-Host "╚" + ("═" * ($Message.Length + 2)) + "╝" -ForegroundColor $colors.Info
}

function Write-Step {
    param([string]$Message, [int]$Step, [int]$Total)
    Write-Host "`n[$Step/$Total] $Message" -ForegroundColor $colors.Info
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor $colors.Success
}

function Write-Error-Custom {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor $colors.Error
}

function Write-Warning-Custom {
    param([string]$Message)
    Write-Host "⚠ $Message" -ForegroundColor $colors.Warning
}

# ═══════════════════════════════════════════════════════════════════════════════

Write-Header "3D Builder Pro - Build & Installer"

# Schritt 1: Voraussetzungen prüfen
Write-Step "Prüfe Voraussetzungen" 1 6

# Prüfe .NET SDK
try {
    $dotnetVersion = dotnet --version
    Write-Success ".NET SDK gefunden: $dotnetVersion"
    
    if (-not $dotnetVersion.StartsWith("8.")) {
        Write-Warning-Custom ".NET 8 wird empfohlen, du hast $dotnetVersion"
    }
} catch {
    Write-Error-Custom ".NET SDK nicht gefunden!"
    Write-Host "Download: https://dotnet.microsoft.com/en-us/download/dotnet/8.0"
    exit 1
}

# Prüfe Visual Studio
$vsPath = "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe"
if (Test-Path $vsPath) {
    Write-Success "Visual Studio 2022 Community gefunden"
} else {
    Write-Warning-Custom "Visual Studio 2022 nicht gefunden (optional)"
}

# Prüfe WiX (nur wenn Installer gebaut werden soll)
if (-not $BuildOnly) {
    try {
        $wixVersion = wix --version 2>$null
        Write-Success "WiX Toolset gefunden: $wixVersion"
    } catch {
        Write-Error-Custom "WiX Toolset nicht gefunden!"
        Write-Host "Download: https://github.com/wixtoolset/wix3/releases"
        Write-Host "Oder: choco install wixtoolset"
        exit 1
    }
}

# Schritt 2: Abhängigkeiten wiederherstellen
Write-Step "Stelle Abhängigkeiten wieder her" 2 6

try {
    Write-Host "Führe 'dotnet restore' aus..."
    dotnet restore CSharpUI/CSharpUI.csproj --verbosity minimal
    Write-Success "Abhängigkeiten wiederhergestellt"
} catch {
    Write-Error-Custom "Fehler beim Restore: $_"
    exit 1
}

# Schritt 3: Hauptprojekt kompilieren
Write-Step "Kompiliere Hauptprojekt (CSharpUI)" 3 6

try {
    Write-Host "Führe 'dotnet build' aus mit Konfiguration: $Configuration..."
    dotnet build CSharpUI/CSharpUI.csproj -c $Configuration --verbosity minimal
    Write-Success "Hauptprojekt erfolgreich kompiliert"
} catch {
    Write-Error-Custom "Build fehlgeschlagen: $_"
    exit 1
}

# Schritt 4: Tests (optional)
if (-not $SkipTests) {
    Write-Step "Führe Tests aus" 4 6
    
    try {
        Write-Host "Suche nach Test-Projekten..."
        $testProjects = Get-ChildItem -Recurse -Filter "*.Tests.csproj"
        
        if ($testProjects.Count -gt 0) {
            Write-Host "Gefundene Test-Projekte: $($testProjects.Count)"
            foreach ($testProject in $testProjects) {
                Write-Host "Führe Tests aus: $($testProject.Name)..."
                dotnet test $testProject.FullName -c $Configuration --verbosity minimal
            }
            Write-Success "Alle Tests bestanden"
        } else {
            Write-Warning-Custom "Keine Test-Projekte gefunden"
        }
    } catch {
        Write-Error-Custom "Tests fehlgeschlagen: $_"
        # Nicht abbrechen, nur warnen
    }
} else {
    Write-Step "Tests übersprungen" 4 6
}

# Schritt 5: Installer kompilieren (optional)
if ($BuildOnly) {
    Write-Step "Installer-Build übersprungen (BuildOnly-Flag gesetzt)" 5 6
} else {
    Write-Step "Kompiliere Installer (WiX)" 5 6
    
    try {
        Write-Host "Führe WiX Build aus..."
        dotnet build Installer/Installer.wixproj -c $Configuration --verbosity minimal
        Write-Success "Installer erfolgreich kompiliert"
    } catch {
        Write-Error-Custom "Installer-Build fehlgeschlagen: $_"
        Write-Warning-Custom "Die Anwendung wurde gebaut, aber der Installer konnte nicht erstellt werden"
    }
}

# Schritt 6: Output-Verzeichnis anzeigen
Write-Step "Build abgeschlossen" 6 6

$outputDir = "CSharpUI\bin\$Configuration"
if (Test-Path $outputDir) {
    Write-Success "Output-Verzeichnis: $outputDir"
    
    $exeFile = Get-ChildItem -Path $outputDir -Filter "*.exe" | Select-Object -First 1
    if ($exeFile) {
        Write-Success "Executable: $($exeFile.Name)"
        Write-Host "  Größe: $([math]::Round($exeFile.Length / 1MB, 2)) MB"
    }
}

if (-not $BuildOnly) {
    $installerDir = "Installer\bin\$Configuration"
    if (Test-Path $installerDir) {
        $msiFile = Get-ChildItem -Path $installerDir -Filter "*.msi" | Select-Object -First 1
        if ($msiFile) {
            Write-Success "Installer: $($msiFile.Name)"
            Write-Host "  Größe: $([math]::Round($msiFile.Length / 1MB, 2)) MB"
            Write-Host "  Pfad: $($msiFile.FullName)"
            
            # Angebot zum Testen
            Write-Host "`nMöchtest du den Installer jetzt testen? (j/n)" -ForegroundColor $colors.Warning
            $response = Read-Host
            if ($response -eq "j") {
                Write-Host "Starte Installer..."
                & $msiFile.FullName
            }
        }
    }
}

Write-Header "Build erfolgreich abgeschlossen! 🎉"

Write-Host "`nNächste Schritte:"
Write-Host "1. Teste die Anwendung"
Write-Host "2. Lade den Installer auf GitHub Releases hoch"
Write-Host "3. Teile den Download-Link mit Benutzern"
Write-Host "`nDokumentation: https://github.com/bannanenbaer/3D-Builder-for-Printer"
