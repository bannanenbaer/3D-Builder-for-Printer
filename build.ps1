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

# Generiert Installer/AppFiles.wxs aus dem publish/-Verzeichnis
function Generate-AppFilesWxs {
    param([string]$PublishDir, [string]$OutputFile)

    $publishRoot = (Resolve-Path $PublishDir).Path.TrimEnd('\', '/')

    # Verzeichnisbaum aufbauen (fuer WiX Directory-Elemente)
    $allFiles = Get-ChildItem -Path $publishRoot -Recurse -File | Sort-Object FullName

    # Hilfsfunktion: Verzeichnis-XML rekursiv erzeugen
    function Build-DirXml($tree, $parentId, $indent) {
        $xml = ''
        foreach ($name in ($tree.Keys | Sort-Object)) {
            $safeId = $parentId + '_' + ($name -replace '[^a-zA-Z0-9]', '_')
            $xml += "$indent<Directory Id=""$safeId"" Name=""$name"">`n"
            if ($tree[$name].Count -gt 0) {
                $xml += Build-DirXml $tree[$name] $safeId ($indent + '  ')
            }
            $xml += "$indent</Directory>`n"
        }
        return $xml
    }

    # Verzeichnisbaum aus Dateipfaden ableiten
    $dirTree = [ordered]@{}
    foreach ($file in $allFiles) {
        $rel = $file.FullName.Substring($publishRoot.Length).TrimStart('\', '/')
        $parts = $rel -split '[\\/]'
        if ($parts.Count -gt 1) {
            $node = $dirTree
            for ($i = 0; $i -lt $parts.Count - 1; $i++) {
                if (-not $node.Contains($parts[$i])) { $node[$parts[$i]] = [ordered]@{} }
                $node = $node[$parts[$i]]
            }
        }
    }

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add('<?xml version="1.0" encoding="UTF-8"?>')
    $lines.Add('<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">')
    $lines.Add('  <Fragment>')

    if ($dirTree.Count -gt 0) {
        $lines.Add('    <DirectoryRef Id="INSTALLFOLDER">')
        $dirXml = (Build-DirXml $dirTree 'dir' '      ').TrimEnd("`n")
        foreach ($l in ($dirXml -split "`n")) { $lines.Add($l) }
        $lines.Add('    </DirectoryRef>')
    }

    $lines.Add('    <ComponentGroup Id="AppFiles">')
    foreach ($file in $allFiles) {
        $rel    = $file.FullName.Substring($publishRoot.Length).TrimStart('\', '/')
        $parts  = $rel -split '[\\/]'
        $compId = 'comp_' + ($rel -replace '[^a-zA-Z0-9]', '_')
        $guid   = [System.Guid]::NewGuid().ToString().ToUpper()
        $src    = $rel -replace '/', '\'

        if ($parts.Count -gt 1) {
            $dirParts = $parts[0..($parts.Count - 2)]
            $dirId = 'dir_' + (($dirParts -join '_') -replace '[^a-zA-Z0-9]', '_')
            $lines.Add("      <Component Id=""$compId"" Directory=""$dirId"" Guid=""{$guid}"">")
        } else {
            $lines.Add("      <Component Id=""$compId"" Directory=""INSTALLFOLDER"" Guid=""{$guid}"">")
        }
        $lines.Add("        <File Source=""..\publish\$src"" KeyPath=""yes"" />")
        $lines.Add("      </Component>")
    }
    $lines.Add('    </ComponentGroup>')
    $lines.Add('  </Fragment>')
    $lines.Add('</Wix>')

    $absOutput = Join-Path (Get-Location).Path $OutputFile
    [System.IO.File]::WriteAllText($absOutput, ($lines -join "`n"), [System.Text.Encoding]::UTF8)
    Write-Success "AppFiles.wxs generiert ($($allFiles.Count) Dateien)"
}

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
        Write-Error-Custom "WiX v4 Toolset nicht gefunden!"
        Write-Host "Installation: dotnet tool install --global wix"
        exit 1
    }

    # WiX-Extensions global installieren (werden von 'wix build -ext ...' benoetigt)
    # Bereits installierte Versionen werden einfach uebersprungen.
    $wixExts = @(
        "WixToolset.UI.wixext/4.0.5",
        "WixToolset.Bal.wixext/4.0.5",
        "WixToolset.Util.wixext/4.0.5"
    )
    foreach ($ext in $wixExts) {
        $extName = $ext.Split('/')[0]
        Write-Host "  Extension sicherstellen: $extName..."
        wix extension add $ext 2>&1 | Out-Null
    }
    Write-Success "WiX-Extensions bereit"
}

# Schritt 2: Abhängigkeiten wiederherstellen
Write-Step "Stelle Abhängigkeiten wieder her" 2 6

Write-Host "Führe 'dotnet restore' aus..."
dotnet restore CSharpUI/ThreeDBuilder.csproj --verbosity minimal
if ($LASTEXITCODE -ne 0) { Write-Error-Custom "Fehler beim Restore"; exit 1 }
Write-Success "Abhängigkeiten wiederhergestellt"

# Schritt 3: Hauptprojekt kompilieren
Write-Step "Kompiliere Hauptprojekt (CSharpUI)" 3 6

Write-Host "Führe 'dotnet build' aus mit Konfiguration: $Configuration..."
dotnet build CSharpUI/ThreeDBuilder.csproj -c $Configuration --verbosity minimal
if ($LASTEXITCODE -ne 0) { Write-Error-Custom "Build fehlgeschlagen – siehe Fehler oben"; exit 1 }
Write-Success "Hauptprojekt erfolgreich kompiliert"

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
    Write-Step "Erstelle Publish-Paket und Installer (WiX v4)" 5 6

    # Schritt 5a: Anwendung publizieren (erzeugt alle DLLs + Python-Backend in publish/)
    Write-Host "Publiziere Anwendung nach publish/..."
    dotnet publish CSharpUI/ThreeDBuilder.csproj -c $Configuration -r win-x64 --self-contained true -o publish/ --verbosity minimal
    if ($LASTEXITCODE -ne 0) { Write-Error-Custom "Publish fehlgeschlagen – siehe Fehler oben"; exit 1 }
    Write-Success "Anwendung erfolgreich publiziert"

    # Schritt 5a.2: AppFiles.wxs aus publish/ generieren (wird von Installer.wixproj eingebunden)
    try {
        Generate-AppFilesWxs -PublishDir "publish" -OutputFile "Installer\AppFiles.wxs"
    } catch {
        Write-Error-Custom "AppFiles.wxs konnte nicht generiert werden: $_"
        exit 1
    }

    # Schritt 5b: WiX v4 MSI bauen (Installer.wixproj)
    # Voraussetzung: dotnet tool install --global wix
    try {
        Write-Host "Baue MSI (Installer.wixproj)..."
        dotnet build Installer/Installer.wixproj -c $Configuration --verbosity minimal
        Write-Success "MSI erfolgreich kompiliert"
    } catch {
        Write-Error-Custom "MSI-Build fehlgeschlagen: $_"
        Write-Warning-Custom "Prüfe ob WiX v4 installiert ist: dotnet tool install --global wix"
        Write-Warning-Custom "Anwendung gebaut, aber Installer konnte nicht erstellt werden"
    }

    # Schritt 5c: Burn-Bundle bauen (wix build → 3DBuilderPro-Setup.exe)
    # Extension-DLLs aus dem NuGet-Cache holen (garantiert vorhanden nach MSI-Build).
    try {
        Write-Host "Baue Burn-Bundle (Bundle.wxs → 3DBuilderPro-Setup.exe)..."

        $nugetPkgs = "$env:USERPROFILE\.nuget\packages"
        $balDll  = Get-ChildItem "$nugetPkgs\wixtoolset.bal.wixext\4.0.5"  -Recurse -Filter "WixToolset.Bal.wixext.dll"  |
                   Where-Object { $_.FullName -notmatch '\\ref\\' } | Select-Object -First 1
        $utilDll = Get-ChildItem "$nugetPkgs\wixtoolset.util.wixext\4.0.5" -Recurse -Filter "WixToolset.Util.wixext.dll" |
                   Where-Object { $_.FullName -notmatch '\\ref\\' } | Select-Object -First 1

        if (-not $balDll)  { throw "WixToolset.Bal.wixext.dll nicht im NuGet-Cache gefunden ($nugetPkgs)" }
        if (-not $utilDll) { throw "WixToolset.Util.wixext.dll nicht im NuGet-Cache gefunden ($nugetPkgs)" }

        Write-Host "  Bal-Extension:  $($balDll.FullName)"
        Write-Host "  Util-Extension: $($utilDll.FullName)"

        # Python-Installer lokal bereitstellen (WiX liest ihn bei Build-Zeit für Hash/Größe;
        # Compressed="no" verhindert das Einbetten – zur Laufzeit wird DownloadUrl genutzt).
        $pythonExe = "Installer\python-3.12.7-amd64.exe"
        if (-not (Test-Path $pythonExe)) {
            Write-Host "  Lade Python 3.12.7 Installer herunter (nur für Build benötigt)..."
            Invoke-WebRequest -Uri "https://www.python.org/ftp/python/3.12.7/python-3.12.7-amd64.exe" `
                              -OutFile $pythonExe -UseBasicParsing
            Write-Success "Python-Installer heruntergeladen"
        } else {
            Write-Success "Python-Installer bereits vorhanden"
        }

        $msiRelPath = "bin\$Configuration\3DBuilderPro.msi"
        $outExe     = "bin\$Configuration\3DBuilderPro-Setup.exe"
        Push-Location Installer
        wix build Bundle.wxs `
            -ext $balDll.FullName `
            -ext $utilDll.FullName `
            -arch x64 `
            -d "MsiPath=$msiRelPath" `
            -b . `
            -o $outExe
        Pop-Location
        if ($LASTEXITCODE -ne 0) {
            throw "wix build fehlgeschlagen (Exit-Code $LASTEXITCODE)"
        }
        Write-Success "Setup.exe erfolgreich erstellt"
    } catch {
        Pop-Location -ErrorAction SilentlyContinue
        Write-Error-Custom "Bundle-Build fehlgeschlagen: $_"
        Write-Warning-Custom "WiX global installiert? dotnet tool install --global wix"
    }
}

# Schritt 6: Output-Verzeichnis anzeigen
Write-Step "Build abgeschlossen" 6 6

$outputDir = "publish"
if (Test-Path $outputDir) {
    Write-Success "Publish-Verzeichnis: $outputDir"
    $exeFile = Get-ChildItem -Path $outputDir -Filter "*.exe" | Select-Object -First 1
    if ($exeFile) {
        Write-Success "Anwendung: $($exeFile.Name)"
        Write-Host "  Größe: $([math]::Round($exeFile.Length / 1MB, 2)) MB"
    }
}

if (-not $BuildOnly) {
    $installerDir = "Installer\bin\$Configuration"
    if (Test-Path $installerDir) {
        # Setup.exe (Burn-Bundle) – das ist die Datei für den Nutzer
        $setupFile = Get-ChildItem -Path $installerDir -Filter "*-Setup.exe" | Select-Object -First 1
        if ($setupFile) {
            Write-Success ">>> Setup (für Nutzer): $($setupFile.Name)"
            Write-Host "  Größe: $([math]::Round($setupFile.Length / 1MB, 2)) MB"
            Write-Host "  Pfad: $($setupFile.FullName)"
        }

        # MSI (intern, wird vom Bundle verwendet)
        $msiFile = Get-ChildItem -Path $installerDir -Filter "*.msi" | Select-Object -First 1
        if ($msiFile) {
            Write-Success "MSI (intern): $($msiFile.Name)"
            Write-Host "  Größe: $([math]::Round($msiFile.Length / 1MB, 2)) MB"
            Write-Host "  Pfad: $($msiFile.FullName)"

            # Angebot zum Testen (Setup.exe bevorzugt)
            $testFile = if ($setupFile) { $setupFile } else { $msiFile }
            Write-Host "`nMöchtest du den Installer jetzt testen? (j/n)" -ForegroundColor $colors.Warning
            $response = Read-Host
            if ($response -eq "j") {
                Write-Host "Starte Installer..."
                & $testFile.FullName
            }
        }
    }
}

Write-Header "Build erfolgreich abgeschlossen! 🎉"

Write-Host "`nNächste Schritte:"
Write-Host "1. Teste 3DBuilderPro-Setup.exe (ein Klick → alles installiert sich still)"
Write-Host "2. Lade 3DBuilderPro-Setup.exe auf GitHub Releases hoch"
Write-Host "3. Teile den Download-Link mit Benutzern"
Write-Host "`nWas der Setup.exe installiert (alles still, kein weiterer Eingriff nötig):"
Write-Host "  • Python 3.12  (falls nicht vorhanden)"
Write-Host "  • cadquery + numpy  (via pip)"
Write-Host "  • 3D Builder Pro Anwendung"
Write-Host "`nDokumentation: https://github.com/bannanenbaer/3D-Builder-for-Printer"
