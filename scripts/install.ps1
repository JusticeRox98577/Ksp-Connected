#Requires -Version 5.1
<#
.SYNOPSIS
    KSP-Connected one-click installer for Windows.

.DESCRIPTION
    Automatically detects your KSP 1.12 installation, installs the .NET 6 SDK
    if missing, compiles the mod, copies it into your KSP GameData folder, and
    optionally creates a desktop shortcut for the multiplayer server.

.EXAMPLE
    # Run from PowerShell (may need: Set-ExecutionPolicy RemoteSigned -Scope CurrentUser)
    .\scripts\install.ps1

.EXAMPLE
    # Supply KSP path manually
    .\scripts\install.ps1 -KspPath "D:\Games\KSP"
#>
param(
    [string]$KspPath   = "",
    [switch]$NoServer,       # skip server build
    [switch]$NoShortcut      # skip desktop shortcut creation
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ── helpers ─────────────────────────────────────────────────────────────────

function Write-Header($msg) {
    Write-Host ""
    Write-Host "============================================================" -ForegroundColor Cyan
    Write-Host "  $msg" -ForegroundColor Cyan
    Write-Host "============================================================" -ForegroundColor Cyan
}

function Write-Step($msg)    { Write-Host "  >>> $msg" -ForegroundColor Yellow }
function Write-Ok($msg)      { Write-Host "  [OK] $msg" -ForegroundColor Green }
function Write-Fail($msg)    { Write-Host "  [!!] $msg" -ForegroundColor Red; exit 1 }

# ── banner ───────────────────────────────────────────────────────────────────

Write-Header "KSP-Connected Installer"
Write-Host "  Multiplayer mod for Kerbal Space Program 1.12" -ForegroundColor White
Write-Host ""

# ── locate KSP ───────────────────────────────────────────────────────────────

Write-Step "Locating KSP installation..."

$KspSearchPaths = @(
    $KspPath,
    "C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program",
    "C:\Program Files\Steam\steamapps\common\Kerbal Space Program",
    "$env:ProgramFiles\Steam\steamapps\common\Kerbal Space Program",
    "$env:ProgramFiles(x86)\Steam\steamapps\common\Kerbal Space Program"
)

# Also search Steam library folders from libraryfolders.vdf
$SteamConfig = "$env:ProgramFiles(x86)\Steam\config\libraryfolders.vdf"
if (Test-Path $SteamConfig) {
    $vdf = Get-Content $SteamConfig -Raw
    $drives = [regex]::Matches($vdf, '"path"\s+"([^"]+)"') | ForEach-Object { $_.Groups[1].Value }
    foreach ($d in $drives) {
        $KspSearchPaths += "$d\steamapps\common\Kerbal Space Program"
    }
}

$ResolvedKsp = ""
foreach ($p in $KspSearchPaths) {
    if ($p -ne "" -and (Test-Path "$p\KSP.exe")) {
        $ResolvedKsp = $p
        break
    }
    if ($p -ne "" -and (Test-Path "$p\KSP_x64.exe")) {
        $ResolvedKsp = $p
        break
    }
}

if ($ResolvedKsp -eq "") {
    Write-Host ""
    Write-Host "  KSP installation not found automatically." -ForegroundColor Yellow
    $ResolvedKsp = Read-Host "  Enter full path to your KSP folder (e.g. D:\Games\KSP)"
    if (-not (Test-Path "$ResolvedKsp\KSP_x64.exe") -and -not (Test-Path "$ResolvedKsp\KSP.exe")) {
        Write-Fail "KSP not found at: $ResolvedKsp"
    }
}

Write-Ok "KSP found at: $ResolvedKsp"

# ── check / install .NET 6 SDK ───────────────────────────────────────────────

Write-Step "Checking .NET 6 SDK..."

$dotnetOk = $false
try {
    $ver = & dotnet --version 2>&1
    if ($ver -match "^[6-9]\." -or $ver -match "^[1-9][0-9]\.") {
        $dotnetOk = $true
        Write-Ok ".NET SDK $ver found."
    }
} catch { }

if (-not $dotnetOk) {
    Write-Step ".NET 6 SDK not found. Attempting install via winget..."
    try {
        winget install Microsoft.DotNet.SDK.6 --silent --accept-package-agreements --accept-source-agreements
        $env:PATH = [System.Environment]::GetEnvironmentVariable("PATH","Machine") + ";" +
                    [System.Environment]::GetEnvironmentVariable("PATH","User")
        Write-Ok ".NET 6 SDK installed."
    } catch {
        Write-Fail ".NET 6 SDK install failed. Please install manually from https://dotnet.microsoft.com/download/dotnet/6.0 then re-run this script."
    }
}

# ── build ────────────────────────────────────────────────────────────────────

$ScriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot    = Split-Path -Parent $ScriptDir

Push-Location $RepoRoot

Write-Step "Building shared library..."
& dotnet build Shared\KspConnected.Shared.csproj -c Release -nologo -v q
if ($LASTEXITCODE -ne 0) { Write-Fail "Shared library build failed." }

Write-Step "Building KSP client plugin..."
& dotnet build Client\KspConnected.Client.csproj -c Release -nologo -v q `
    "-p:KspPath=$ResolvedKsp"
if ($LASTEXITCODE -ne 0) { Write-Fail "Client plugin build failed. Make sure KSP 1.12 is installed at: $ResolvedKsp" }
Write-Ok "Plugin DLLs copied to GameData\KspConnected\Plugins\"

if (-not $NoServer) {
    Write-Step "Building server..."
    & dotnet build Server\KspConnected.Server.csproj -c Release -nologo -v q
    if ($LASTEXITCODE -ne 0) { Write-Fail "Server build failed." }
    Write-Ok "Server built."
}

# ── install plugin ────────────────────────────────────────────────────────────

Write-Step "Installing mod into KSP GameData..."

$KspGameData = Join-Path $ResolvedKsp "GameData"
$Dest        = Join-Path $KspGameData "KspConnected"

if (Test-Path $Dest) {
    Write-Host "  Removing previous installation..." -ForegroundColor Gray
    Remove-Item $Dest -Recurse -Force
}

Copy-Item -Path "GameData\KspConnected" -Destination $KspGameData -Recurse
Write-Ok "Mod installed to: $Dest"

# ── server shortcut ──────────────────────────────────────────────────────────

if (-not $NoShortcut) {
    Write-Step "Creating desktop shortcut for the server..."
    try {
        $ServerDll = Resolve-Path "Server\bin\Release\net6.0\KspConnected.Server.dll"
        $Desktop   = [Environment]::GetFolderPath("Desktop")
        $Shortcut  = "$Desktop\KSP-Connected Server.lnk"

        $WshShell = New-Object -ComObject WScript.Shell
        $Link     = $WshShell.CreateShortcut($Shortcut)
        $Link.TargetPath       = "dotnet"
        $Link.Arguments        = "`"$ServerDll`""
        $Link.WorkingDirectory = Split-Path $ServerDll
        $Link.Description      = "KSP-Connected multiplayer server"
        $Link.Save()
        Write-Ok "Server shortcut created on Desktop."
    } catch {
        Write-Host "  (Could not create shortcut: $($_.Exception.Message))" -ForegroundColor Gray
    }
}

Pop-Location

# ── done ─────────────────────────────────────────────────────────────────────

Write-Header "Installation Complete!"
Write-Host ""
Write-Host "  Mod installed to:" -ForegroundColor White
Write-Host "    $Dest" -ForegroundColor Cyan
Write-Host ""
Write-Host "  To host a game, run the server:" -ForegroundColor White
Write-Host "    dotnet `"$RepoRoot\Server\bin\Release\net6.0\KspConnected.Server.dll`"" -ForegroundColor Cyan
Write-Host "    (or double-click the Desktop shortcut)" -ForegroundColor Gray
Write-Host ""
Write-Host "  In KSP, go to the Space Center and use the KSP-Connected window" -ForegroundColor White
Write-Host "  to enter the host IP and connect." -ForegroundColor White
Write-Host ""
