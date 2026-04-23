@echo off
:: ============================================================
::  build-for-installer.bat
::  Run this BEFORE opening installer\KSP-Connected.iss
:: ============================================================
::  Compiles the mod plugin and server so Inno Setup can
::  bundle the DLLs into the installer .exe.
::
::  Usage:
::    build-for-installer.bat
::       (auto-detects KSP on all drives)
::
::    build-for-installer.bat "D:\KSP\Kerbal Space Program"
::       (use a specific KSP path)
:: ============================================================

setlocal EnableDelayedExpansion

:: ── 1. Resolve KSP path ──────────────────────────────────────────────────────

set "KSP_PATH=%~1"

if not "%KSP_PATH%"=="" goto have_ksp

echo Searching for KSP installation...

:: Check common fixed locations first
for %%P in (
  "%ProgramFiles(x86)%\Steam\steamapps\common\Kerbal Space Program"
  "%ProgramFiles%\Steam\steamapps\common\Kerbal Space Program"
  "%ProgramFiles(x86)%\GOG Galaxy\Games\Kerbal Space Program"
  "%ProgramFiles%\GOG Galaxy\Games\Kerbal Space Program"
  "%ProgramFiles%\Epic Games\KerbalSpaceProgram"
) do (
  if exist "%%~P\KSP_x64.exe" ( set "KSP_PATH=%%~P" & goto have_ksp )
)

:: Scan all drive letters
for %%D in (C D E F G H I J K L M N O P Q R S T U V W X Y Z) do (
  for %%S in (
    "Kerbal Space Program"
    "KSP\Kerbal Space Program"
    "Games\Kerbal Space Program"
    "Games\KSP"
    "SteamLibrary\steamapps\common\Kerbal Space Program"
    "Steam\steamapps\common\Kerbal Space Program"
    "GOG Games\Kerbal Space Program"
  ) do (
    if exist "%%D:\%%~S\KSP_x64.exe" ( set "KSP_PATH=%%D:\%%~S" & goto have_ksp )
    if exist "%%D:\%%~S\KSP.exe"     ( set "KSP_PATH=%%D:\%%~S" & goto have_ksp )
  )
)

echo.
echo  ERROR: KSP installation not found automatically.
echo  Run this script with your KSP path as an argument, e.g.:
echo    build-for-installer.bat "D:\KSP\Kerbal Space Program"
echo.
exit /b 1

:have_ksp
echo  KSP found: %KSP_PATH%
echo.

:: ── 2. Check .NET SDK ────────────────────────────────────────────────────────

dotnet --version >nul 2>&1
if errorlevel 1 (
  echo  ERROR: .NET 6 SDK not found.
  echo  Download from: https://dotnet.microsoft.com/download/dotnet/6.0
  exit /b 1
)

:: ── 3. Build ─────────────────────────────────────────────────────────────────

echo Building shared library...
dotnet build Shared\KspConnected.Shared.csproj -c Release -nologo -v q
if errorlevel 1 ( echo FAILED: Shared & exit /b 1 )

echo Building KSP plugin...
dotnet build Client\KspConnected.Client.csproj -c Release -nologo -v q ^
  -p:KspPath="%KSP_PATH%"
if errorlevel 1 ( echo FAILED: Client plugin & exit /b 1 )

echo Building server...
dotnet build Server\KspConnected.Server.csproj -c Release -nologo -v q
if errorlevel 1 ( echo FAILED: Server & exit /b 1 )

:: ── 4. Done ──────────────────────────────────────────────────────────────────

echo.
echo  ============================================================
echo   Build complete!
echo   Now open installer\KSP-Connected.iss in Inno Setup
echo   and press Ctrl+F9 (or Build - Compile) to create the .exe
echo  ============================================================
echo.

endlocal
