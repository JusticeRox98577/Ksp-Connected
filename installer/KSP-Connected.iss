; ============================================================
;  KSP-Connected Inno Setup Script
;  Version 1.0.1-alpha
; ============================================================
;
;  HOW TO BUILD THE INSTALLER
;  --------------------------
;  1. Run build-for-installer.bat  (builds the DLLs from source)
;  2. Open this file in Inno Setup 6 and press Ctrl+F9  (or run ISCC.exe)
;  3. The finished installer appears in: dist\installer\
;
;  Requirements:
;    - Inno Setup 6   https://jrsoftware.org/isinfo.php
;    - .NET 6 SDK     https://dotnet.microsoft.com/download/dotnet/6.0
;    - KSP 1.12 installed locally (needed to compile the plugin DLL)
; ============================================================

#define AppName      "KSP-Connected"
#define AppVersion   "1.0.1-alpha"
#define AppVerShort  "1.0.1"
#define AppPublisher "KSP-Connected Contributors"
#define AppURL       "https://github.com/JusticeRox98577/Ksp-Connected"
#define ServerDll    "KspConnected.Server.dll"

; ── [Setup] ──────────────────────────────────────────────────────────────────

[Setup]
AppId={{B7C4A1D2-3E5F-4A8B-9C2D-1E6F7A8B9C0D}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}

; Server goes to Program Files\KSP-Connected\
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes

; Splash / wizard images (place your own 164x314 and 55x55 bitmaps next to this
; file and uncomment these lines for a branded look)
;WizardImageFile=wizard-side.bmp
;WizardSmallImageFile=wizard-top.bmp

OutputDir=..\dist\installer
OutputBaseFilename=KSP-Connected-v{#AppVersion}-Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern

; Run without admin if possible; elevate only if writing to protected folders
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

; ── [Languages] ──────────────────────────────────────────────────────────────

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

; ── [Types] & [Components] ───────────────────────────────────────────────────

[Types]
Name: "full";    Description: "Full installation (mod + server)"
Name: "modonly"; Description: "Mod only  (no server)"
Name: "custom";  Description: "Custom";  Flags: iscustom

[Components]
Name: "mod";    Description: "KSP mod plugin (GameData files)";  Types: full modonly custom; Flags: fixed
Name: "server"; Description: "Multiplayer server executable";    Types: full custom

; ── [Tasks] ──────────────────────────────────────────────────────────────────

[Tasks]
Name: "desktopicon"; \
  Description: "Create a &Desktop shortcut for the server"; \
  GroupDescription: "Additional icons:"; \
  Components: server; \
  Flags: unchecked

; ── [Files] ──────────────────────────────────────────────────────────────────

[Files]
; ── mod DLLs → copied into KSP\GameData\KspConnected\
Source: "..\GameData\KspConnected\*"; \
  DestDir: "{code:GetKspDir}\GameData\KspConnected"; \
  Flags: ignoreversion recursesubdirs createallsubdirs; \
  Components: mod

; ── server binaries → Program Files\KSP-Connected\Server\
Source: "..\Server\bin\Release\net6.0\*"; \
  DestDir: "{app}\Server"; \
  Flags: ignoreversion recursesubdirs createallsubdirs; \
  Components: server

; ── server config  (preserve existing config on upgrades)
Source: "..\server.json"; \
  DestDir: "{app}\Server"; \
  Flags: onlyifdoesntexist; \
  Components: server

; ── readme always installed
Source: "..\README.md"; \
  DestDir: "{app}"; \
  Flags: ignoreversion

; ── [Icons] ──────────────────────────────────────────────────────────────────

[Icons]
; Start Menu → run server (direct mode)
Name: "{group}\KSP-Connected Server"; \
  FileName: "dotnet"; \
  Parameters: """{app}\Server\{#ServerDll}"""; \
  WorkingDir: "{app}\Server"; \
  Comment: "Start the KSP-Connected multiplayer server"; \
  Components: server

; Start Menu → run server (relay mode — no port forwarding)
Name: "{group}\KSP-Connected Server (Relay mode)"; \
  FileName: "dotnet"; \
  Parameters: """{app}\Server\{#ServerDll}"" --relay"; \
  WorkingDir: "{app}\Server"; \
  Comment: "Start in relay mode — players join with a room code, no port forwarding needed"; \
  Components: server

; Start Menu → README
Name: "{group}\README"; FileName: "{app}\README.md"

; Start Menu → Uninstall
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; FileName: "{uninstallexe}"

; Desktop shortcut (opt-in task)
Name: "{autodesktop}\KSP-Connected Server"; \
  FileName: "dotnet"; \
  Parameters: """{app}\Server\{#ServerDll}"""; \
  WorkingDir: "{app}\Server"; \
  Comment: "Start the KSP-Connected multiplayer server"; \
  Tasks: desktopicon; \
  Components: server

; ── [Run] ─────────────────────────────────────────────────────────────────────

[Run]
; Optional: open README after install
Filename: "{app}\README.md"; \
  Description: "View README (getting started guide)"; \
  Flags: postinstall shellexec skipifsilent unchecked

; ── [UninstallDelete] ────────────────────────────────────────────────────────

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

; ── [Code] ───────────────────────────────────────────────────────────────────

[Code]

var
  KspDirPage : TInputDirWizardPage;

{ ── KSP path detection ──────────────────────────────────────────── }

function IsKspDir(Path: String): Boolean;
begin
  Result := DirExists(Path) and
            ( FileExists(Path + '\KSP_x64.exe') or
              FileExists(Path + '\KSP.exe') );
end;

function FindKspPath(): String;
var
  I, J : Integer;
  Drive, P : String;
begin
  Result := '';

  { Common fixed locations }
  if IsKspDir(ExpandConstant('{pf32}\Steam\steamapps\common\Kerbal Space Program')) then
    begin Result := ExpandConstant('{pf32}\Steam\steamapps\common\Kerbal Space Program'); Exit; end;
  if IsKspDir(ExpandConstant('{pf}\Steam\steamapps\common\Kerbal Space Program')) then
    begin Result := ExpandConstant('{pf}\Steam\steamapps\common\Kerbal Space Program'); Exit; end;
  if IsKspDir(ExpandConstant('{pf32}\GOG Galaxy\Games\Kerbal Space Program')) then
    begin Result := ExpandConstant('{pf32}\GOG Galaxy\Games\Kerbal Space Program'); Exit; end;
  if IsKspDir(ExpandConstant('{pf}\GOG Galaxy\Games\Kerbal Space Program')) then
    begin Result := ExpandConstant('{pf}\GOG Galaxy\Games\Kerbal Space Program'); Exit; end;
  if IsKspDir(ExpandConstant('{pf}\Epic Games\KerbalSpaceProgram')) then
    begin Result := ExpandConstant('{pf}\Epic Games\KerbalSpaceProgram'); Exit; end;

  { Scan drive letters C–Z for common sub-paths }
  for I := 67 to 90 do  { ASCII 'C' = 67, 'Z' = 90 }
  begin
    Drive := Chr(I) + ':\';
    if not DirExists(Drive) then Continue;

    P := Drive + 'Kerbal Space Program';
    if IsKspDir(P) then begin Result := P; Exit; end;

    P := Drive + 'KSP\Kerbal Space Program';
    if IsKspDir(P) then begin Result := P; Exit; end;

    P := Drive + 'Games\Kerbal Space Program';
    if IsKspDir(P) then begin Result := P; Exit; end;

    P := Drive + 'Games\KSP';
    if IsKspDir(P) then begin Result := P; Exit; end;

    P := Drive + 'SteamLibrary\steamapps\common\Kerbal Space Program';
    if IsKspDir(P) then begin Result := P; Exit; end;

    P := Drive + 'Steam\steamapps\common\Kerbal Space Program';
    if IsKspDir(P) then begin Result := P; Exit; end;

    P := Drive + 'GOG Games\Kerbal Space Program';
    if IsKspDir(P) then begin Result := P; Exit; end;
  end;
end;

{ ── Wizard initialisation ───────────────────────────────────────── }

procedure InitializeWizard();
var
  Detected: String;
begin
  { Add a custom page right after "Select Destination Location" }
  KspDirPage := CreateInputDirPage(
    wpSelectDir,
    'KSP Installation Folder',
    'Where is Kerbal Space Program 1.12 installed?',
    'The mod DLLs will be copied into the GameData folder inside this directory. ' +
    'Click Browse if the path below is wrong.',
    False, '');
  KspDirPage.Add('KSP installation folder:');

  { Auto-detect and pre-fill }
  Detected := FindKspPath();
  if Detected <> '' then
    KspDirPage.Values[0] := Detected
  else
    KspDirPage.Values[0] :=
      'C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program';
end;

{ ── Validation on Next click ────────────────────────────────────── }

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;

  if CurPageID = KspDirPage.ID then
  begin
    if not DirExists(KspDirPage.Values[0] + '\GameData') then
    begin
      MsgBox(
        'The selected folder does not appear to be a valid KSP installation ' +
        '(no GameData sub-folder found).' + #13#10 + #13#10 +
        'Please select the folder that contains KSP_x64.exe.',
        mbError, MB_OK);
      Result := False;
    end;
  end;
end;

{ ── Helper used by [Files] / [Icons] DestDir ────────────────────── }

function GetKspDir(Param: String): String;
begin
  Result := KspDirPage.Values[0];
end;
