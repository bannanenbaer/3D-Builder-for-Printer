@echo off
rem ============================================================
rem  3D Builder Pro – Stille Python-Paket-Installation
rem  Wird automatisch vom Installer aufgerufen.
rem  Installiert: cadquery, numpy
rem ============================================================

rem PATH aus der Registry neu laden (Python gerade installiert?)
for /f "skip=2 tokens=2*" %%A in (
  'reg query "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment" /v PATH 2^>nul'
) do set "SYS_PATH=%%B"
if defined SYS_PATH set "PATH=%SYS_PATH%;%PATH%"

rem Bekannte Python-Installationspfade ergänzen
set "PATH=%PATH%;%LOCALAPPDATA%\Programs\Python\Python313"
set "PATH=%PATH%;%LOCALAPPDATA%\Programs\Python\Python312"
set "PATH=%PATH%;%LOCALAPPDATA%\Programs\Python\Python311"
set "PATH=%PATH%;%LOCALAPPDATA%\Programs\Python\Python310"
set "PATH=%PATH%;C:\Python313;C:\Python312;C:\Python311;C:\Python310"

rem Versuche py-Launcher (wird bei Systeminstallation in C:\Windows abgelegt)
py -3 -m pip install cadquery numpy --quiet --no-warn-script-location --disable-pip-version-check >nul 2>&1
if %errorlevel% == 0 goto :done

rem Fallback: python
python -m pip install cadquery numpy --quiet --no-warn-script-location --disable-pip-version-check >nul 2>&1
if %errorlevel% == 0 goto :done

rem Fallback: python3
python3 -m pip install cadquery numpy --quiet --no-warn-script-location --disable-pip-version-check >nul 2>&1

:done
rem Immer mit 0 beenden – fehlende Pakete blockieren nicht den Installer.
rem Die Anwendung zeigt eine Warnung beim Start wenn Python fehlt.
exit /b 0
