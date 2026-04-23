; KSP-Connected Inno Setup Installer Script
; ===========================================
; Produces a Windows .exe installer.
;
; Prerequisites:
;   1. Inno Setup 6  — https://jrsoftware.org/isinfo.php
;   2. Build the mod first: build.bat
;   3. Open this file in Inno Setup IDE and click Compile, or run:
;      ISCC.exe installer\KSP-Connected.iss
;
; The installer will:
;   - Ask for KSP installation directory
;   - Copy GameData\KspConnected\ into it
;   - Install the server to Program Files\KSP-Connected\
;   - Create a Start Menu entry and optional Desktop shortcut for the server

#define AppName      "KSP-Connected"
#define AppVersion   "1.0.0"
#define AppPublisher "KSP-Connected Contributors"
#define AppURL       "https://github.com/JusticeRox98577/Ksp-Connected"
#define ServerExe    "KspConnected.Server.exe"

[Setup]
AppId={{B7C4A1D2-3E5F-4A8B-9C2D-1E6F7A8B9C0D}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes
LicenseFile=..\README.md
OutputDir=..\dist\installer
OutputBaseFilename=KSP-Connected-v{#AppVersion}-Setup
SetupIconFile=
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
KspDirLabel=KSP Installation Directory:
KspDirDescription=Select your Kerbal Space Program 1.12 installation folder
NoDotNet=The .NET 6 Runtime is required to run the server.%n%nDownload it from: https://dotnet.microsoft.com/download/dotnet/6.0%nThen re-run this installer.

[Types]
Name: "full";    Description: "Full installation (mod + server)"
Name: "modonly"; Description: "Mod only (no server)"
Name: "server";  Description: "Server only"

[Components]
Name: "mod";    Description: "KSP GameData mod files"; Types: full modonly; Flags: fixed
Name: "server"; Description: "Multiplayer server";     Types: full server

[Code]
var
  KspDirPage: TInputDirWizardPage;

procedure InitializeWizard();
begin
  KspDirPage := CreateInputDirPage(
    wpSelectDir,
    'KSP Installation Directory',
    'Where is Kerbal Space Program installed?',
    'The mod files will be copied into the GameData folder here.',
    False, '');
  KspDirPage.Add('');

  // Try to detect KSP from common Steam paths
  if DirExists(ExpandConstant('{pf32}\Steam\steamapps\common\Kerbal Space Program')) then
    KspDirPage.Values[0] := ExpandConstant('{pf32}\Steam\steamapps\common\Kerbal Space Program')
  else if DirExists(ExpandConstant('{pf}\Steam\steamapps\common\Kerbal Space Program')) then
    KspDirPage.Values[0] := ExpandConstant('{pf}\Steam\steamapps\common\Kerbal Space Program')
  else
    KspDirPage.Values[0] := 'C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program';
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = KspDirPage.ID then
  begin
    if not DirExists(KspDirPage.Values[0] + '\GameData') then
    begin
      MsgBox('The selected folder does not contain a GameData directory. Please select your KSP installation folder.', mbError, MB_OK);
      Result := False;
    end;
  end;
end;

function GetKspDir(Param: String): String;
begin
  Result := KspDirPage.Values[0];
end;

[Files]
; Mod DLLs — copied directly into KSP GameData
Source: "..\GameData\KspConnected\*"; \
  DestDir: "{code:GetKspDir}\GameData\KspConnected"; \
  Flags: ignoreversion recursesubdirs createallsubdirs; \
  Components: mod

; Server — installed to Program Files\KSP-Connected\
Source: "..\Server\bin\Release\net6.0\*"; \
  DestDir: "{app}\Server"; \
  Flags: ignoreversion recursesubdirs createallsubdirs; \
  Components: server

; Server config
Source: "..\server.json"; \
  DestDir: "{app}\Server"; \
  Flags: onlyifdoesntexist; \
  Components: server

; Install scripts (for developers who want to rebuild)
Source: "..\scripts\install.ps1"; \
  DestDir: "{app}"; \
  Flags: ignoreversion

Source: "..\README.md"; \
  DestDir: "{app}"; \
  Flags: ignoreversion

[Icons]
; Start Menu
Name: "{group}\KSP-Connected Server";          FileName: "dotnet"; Parameters: """{app}\Server\KspConnected.Server.dll"""; WorkingDir: "{app}\Server"; Comment: "Start the KSP multiplayer server"; Components: server
Name: "{group}\KSP-Connected Server (Custom Port)"; FileName: "dotnet"; Parameters: """{app}\Server\KspConnected.Server.dll"" --port 7654"; WorkingDir: "{app}\Server"; Components: server
Name: "{group}\README";                        FileName: "{app}\README.md"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; FileName: "{uninstallexe}"

; Desktop shortcut (optional)
Name: "{autodesktop}\KSP-Connected Server";    FileName: "dotnet"; Parameters: """{app}\Server\KspConnected.Server.dll"""; WorkingDir: "{app}\Server"; Comment: "Start the KSP multiplayer server"; Tasks: desktopicon; Components: server

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Run]
Filename: "{app}\README.md"; Description: "View README"; Flags: postinstall shellexec skipifsilent unchecked

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
