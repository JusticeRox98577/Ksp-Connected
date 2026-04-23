@echo off
REM ============================================================
REM  KSP-Connected build script (Windows)
REM ============================================================
REM  Requirements: .NET 6 SDK
REM  Set KSP_PATH to your KSP 1.12 installation directory
REM ============================================================

setlocal

IF "%KSP_PATH%"=="" SET KSP_PATH=D:\KSP\Kerbal Space Program
SET TARGET=%1
IF "%TARGET%"=="" SET TARGET=all

echo ============================================
echo  KSP-Connected Build (Windows)
echo ============================================

IF "%TARGET%"=="server" GOTO build_server
IF "%TARGET%"=="client" GOTO build_client
IF "%TARGET%"=="all"    GOTO build_all

echo Unknown target: %TARGET%
echo Usage: build.bat [all^|client^|server]
EXIT /B 1

:build_all
CALL :build_server_sub
CALL :build_client_sub
GOTO done

:build_server
CALL :build_server_sub
GOTO done

:build_client
CALL :build_client_sub
GOTO done

:build_server_sub
echo.
echo ^>^>^> Building Server...
dotnet build Server\KspConnected.Server.csproj -c Release
EXIT /B %ERRORLEVEL%

:build_client_sub
echo.
echo ^>^>^> Building Client plugin...
echo     KSP_PATH = %KSP_PATH%
IF EXIST "%KSP_PATH%\" GOTO ksp_found
echo ERROR: KSP not found. Set the KSP_PATH environment variable.
EXIT /B 1
:ksp_found
dotnet build Client\KspConnected.Client.csproj -c Release -p:KspPath="%KSP_PATH%"
IF ERRORLEVEL 1 EXIT /B %ERRORLEVEL%

echo.
echo ^>^>^> Copying plugin DLLs to KSP GameData...
set "PLUGIN_DIR=%KSP_PATH%\GameData\KspConnected\Plugins"
IF NOT EXIST "%PLUGIN_DIR%" mkdir "%PLUGIN_DIR%"
copy /Y "Client\bin\Release\net472\KspConnected.Client.dll" "%PLUGIN_DIR%\" >nul
copy /Y "Client\bin\Release\net472\KspConnected.Shared.dll" "%PLUGIN_DIR%\" >nul
echo     Copied to: %PLUGIN_DIR%
EXIT /B 0

:done
echo.
echo ============================================
echo  Build complete!
echo ============================================
echo.
echo Next steps:
echo   Server: dotnet Server\bin\Release\net6.0\KspConnected.Server.dll
echo   Client DLLs copied automatically to %KSP_PATH%\GameData\KspConnected\Plugins\
endlocal
