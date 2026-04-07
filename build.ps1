param(
    [ValidateSet("Debug","Release")]
    [string]$Configuration = "Release",
    [switch]$SkipTests,
    [switch]$BuildOnly
)

$ErrorActionPreference = "Continue"

function Step  { param([string]$n,[int]$s,[int]$t) Write-Host "`n[$s/$t] $n" -ForegroundColor Cyan }
function OK    { param([string]$n) Write-Host "OK: $n" -ForegroundColor Green }
function Fail  { param([string]$n) Write-Host "FEHLER: $n" -ForegroundColor Red }
function Warn  { param([string]$n) Write-Host "WARNUNG: $n" -ForegroundColor Yellow }

function Generate-AppFilesWxs {
    param([string]$PublishDir, [string]$OutputFile)

    $publishRoot = (Resolve-Path $PublishDir).Path.TrimEnd('\','/')
    $allFiles    = Get-ChildItem -Path $publishRoot -Recurse -File | Sort-Object FullName

    $sb = New-Object System.Text.StringBuilder
    $null = $sb.AppendLine('<?xml version="1.0" encoding="UTF-8"?>')
    $null = $sb.AppendLine('<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">')
    $null = $sb.AppendLine('  <Fragment>')
    $null = $sb.AppendLine('    <ComponentGroup Id="AppFiles">')

    foreach ($file in $allFiles) {
        $rel    = $file.FullName.Substring($publishRoot.Length).TrimStart('\','/')
        $compId = 'comp_' + ($rel -replace '[^a-zA-Z0-9]','_')
        $fileId = 'fil_'  + ($rel -replace '[^a-zA-Z0-9]','_')
        $guid   = [System.Guid]::NewGuid().ToString().ToUpper()
        $src    = $rel -replace '/','\'
        $subdir = [System.IO.Path]::GetDirectoryName($rel)
        if ([string]::IsNullOrEmpty($subdir)) {
            $null = $sb.AppendLine("      <Component Id=""$compId"" Directory=""INSTALLFOLDER"" Guid=""{$guid}"">")
        } else {
            $subdir = $subdir -replace '/','\'
            $null = $sb.AppendLine("      <Component Id=""$compId"" Directory=""INSTALLFOLDER"" Subdirectory=""$subdir"" Guid=""{$guid}"">")
        }
        $null = $sb.AppendLine("        <File Id=""$fileId"" Source=""..\publish\$src"" KeyPath=""yes"" />")
        $null = $sb.AppendLine("      </Component>")
    }

    $null = $sb.AppendLine('    </ComponentGroup>')
    $null = $sb.AppendLine('  </Fragment>')
    $null = $sb.AppendLine('</Wix>')

    [System.IO.File]::WriteAllText(
        (Join-Path (Get-Location).Path $OutputFile),
        $sb.ToString(),
        [System.Text.Encoding]::UTF8
    )
    OK "AppFiles.wxs generiert ($($allFiles.Count) Dateien)"
}

# =============================================================================
Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "  3D Builder Pro - Build & Installer" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# --- Schritt 1: Voraussetzungen ---
Step "Pruefe Voraussetzungen" 1 6

$dotnetVer = dotnet --version 2>$null
if ($LASTEXITCODE -ne 0) { Fail ".NET SDK nicht gefunden! https://dotnet.microsoft.com/download/dotnet/8.0"; exit 1 }
OK ".NET SDK: $dotnetVer"

if (-not $BuildOnly) {
    $wixVer = wix --version 2>$null
    if ($LASTEXITCODE -ne 0) { Fail "WiX v4 nicht gefunden! dotnet tool install --global wix"; exit 1 }
    OK "WiX Toolset: $wixVer"

    foreach ($ext in @("WixToolset.UI.wixext/4.0.5","WixToolset.Bal.wixext/4.0.5","WixToolset.Util.wixext/4.0.5")) {
        Write-Host "  Extension: $($ext.Split('/')[0])..."
        wix extension add $ext 2>&1 | Out-Null
    }
    OK "WiX-Extensions bereit"
}

# --- Schritt 2: Restore ---
Step "Stelle Abhaengigkeiten wieder her" 2 6

dotnet restore CSharpUI/ThreeDBuilder.csproj --verbosity minimal
if ($LASTEXITCODE -ne 0) { Fail "dotnet restore fehlgeschlagen"; exit 1 }
OK "Abhaengigkeiten wiederhergestellt"

# --- Schritt 3: Build ---
Step "Kompiliere Hauptprojekt (CSharpUI)" 3 6

dotnet build CSharpUI/ThreeDBuilder.csproj -c $Configuration --verbosity minimal
if ($LASTEXITCODE -ne 0) { Fail "dotnet build fehlgeschlagen - siehe Fehler oben"; exit 1 }
OK "Hauptprojekt erfolgreich kompiliert"

# --- Schritt 4: Tests ---
if ($SkipTests) {
    Step "Tests uebersprungen" 4 6
} else {
    Step "Fuehre Tests aus" 4 6
    $testProjects = Get-ChildItem -Recurse -Filter "*.Tests.csproj" -ErrorAction SilentlyContinue
    if ($testProjects.Count -gt 0) {
        foreach ($tp in $testProjects) {
            dotnet test $tp.FullName -c $Configuration --verbosity minimal
        }
        OK "Tests bestanden"
    } else {
        Warn "Keine Test-Projekte gefunden"
    }
}

# --- Schritt 5: Installer ---
if ($BuildOnly) {
    Step "Installer-Build uebersprungen (BuildOnly)" 5 6
} else {
    Step "Erstelle Publish-Paket und Installer" 5 6

    # 5a: Publish
    Write-Host "Publiziere Anwendung..."
    dotnet publish CSharpUI/ThreeDBuilder.csproj -c $Configuration -r win-x64 --self-contained true -o publish/ --verbosity minimal
    if ($LASTEXITCODE -ne 0) { Fail "dotnet publish fehlgeschlagen"; exit 1 }
    OK "Anwendung publiziert"

    # 5b: AppFiles.wxs
    try {
        Generate-AppFilesWxs -PublishDir "publish" -OutputFile "Installer\AppFiles.wxs"
    } catch {
        Fail "AppFiles.wxs konnte nicht generiert werden: $_"
        exit 1
    }

    # 5c: MSI
    Write-Host "Baue MSI..."
    dotnet build Installer/Installer.wixproj -c $Configuration --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        Warn "MSI-Build fehlgeschlagen - WiX installiert? dotnet tool install --global wix"
    } else {
        OK "MSI kompiliert"
    }

    # 5d: Bundle (Setup.exe)
    Write-Host "Baue Setup.exe (Bundle)..."
    $nugetPkgs = "$env:USERPROFILE\.nuget\packages"

    $balDll = Get-ChildItem "$nugetPkgs\wixtoolset.bal.wixext\4.0.5" -Recurse -Filter "WixToolset.Bal.wixext.dll" -ErrorAction SilentlyContinue |
              Where-Object { $_.FullName -notmatch '\\ref\\' } |
              Select-Object -First 1

    $utilDll = Get-ChildItem "$nugetPkgs\wixtoolset.util.wixext\4.0.5" -Recurse -Filter "WixToolset.Util.wixext.dll" -ErrorAction SilentlyContinue |
               Where-Object { $_.FullName -notmatch '\\ref\\' } |
               Select-Object -First 1

    if (-not $balDll -or -not $utilDll) {
        Warn "WiX Extension DLLs nicht gefunden - Bundle-Build wird uebersprungen"
    } else {
        $pythonExe = "Installer\python-3.12.7-amd64.exe"
        if (-not (Test-Path $pythonExe)) {
            Write-Host "  Lade Python 3.12.7 Installer herunter..."
            Invoke-WebRequest -Uri "https://www.python.org/ftp/python/3.12.7/python-3.12.7-amd64.exe" -OutFile $pythonExe -UseBasicParsing
            OK "Python-Installer heruntergeladen"
        } else {
            OK "Python-Installer vorhanden"
        }

        $msiPath = "bin\$Configuration\3DBuilderPro.msi"
        $outExe  = "bin\$Configuration\3DBuilderPro-Setup.exe"

        Push-Location Installer
        wix build Bundle.wxs `
            -ext $balDll.FullName `
            -ext $utilDll.FullName `
            -arch x64 `
            -d "MsiPath=$msiPath" `
            -b . `
            -o $outExe
        $wixExitCode = $LASTEXITCODE
        Pop-Location

        if ($wixExitCode -ne 0) {
            Warn "Bundle-Build fehlgeschlagen (wix build Exit-Code $wixExitCode)"
        } else {
            OK "Setup.exe erfolgreich erstellt"
        }
    }
}

# --- Schritt 6: Ergebnis ---
Step "Build abgeschlossen" 6 6

if (Test-Path "publish") {
    OK "Publish-Verzeichnis: publish\"
    $exe = Get-ChildItem "publish" -Filter "*.exe" | Select-Object -First 1
    if ($exe) { OK "Anwendung: $($exe.Name) ($([math]::Round($exe.Length/1MB,1)) MB)" }
}

if (-not $BuildOnly) {
    $setupExe = Get-ChildItem "Installer\bin\$Configuration" -Filter "*-Setup.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($setupExe) {
        Write-Host ""
        Write-Host ">>> Setup.exe fuer Nutzer: $($setupExe.FullName)" -ForegroundColor Green
        Write-Host "    Groesse: $([math]::Round($setupExe.Length/1MB,1)) MB" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "=========================================" -ForegroundColor Green
Write-Host "  Build erfolgreich abgeschlossen!" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Naechste Schritte:"
Write-Host "  1. Teste Installer\bin\$Configuration\3DBuilderPro-Setup.exe"
Write-Host "  2. Lade 3DBuilderPro-Setup.exe auf GitHub Releases hoch (Tag: latest)"
Write-Host "  3. Die App prueft diesen Release beim Klick auf Update"
