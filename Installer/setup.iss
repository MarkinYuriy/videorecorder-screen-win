#define AppName      "ScreenRecorder"
#define AppVersion   "1.0.0"
#define AppPublisher "Markin Yuriy"
#define AppExeName   "VideoRecorderScreen.exe"
#define PublishX64   "..\publish\win-x64"
#define PublishARM64 "..\publish\win-arm64"
; Place ffmpeg builds next to this script (all files including DLLs):
;   Installer\ffmpeg\x64\ffmpeg.exe  + avcodec-*.dll etc.
;   Installer\ffmpeg\arm64\ffmpeg.exe + avcodec-*.dll etc.
; Use a shared/dynamic build from https://github.com/BtbN/FFmpeg-Builds/releases
#define FfmpegX64    "ffmpeg\x64"
#define FfmpegARM64  "ffmpeg\arm64"

[Setup]
AppId={{B7C4A3F2-1E9D-4F8A-BC3D-5A6E7F8B9C0D}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL=https://github.com/MarkinYuriy/videorecorder-screen-win
AppSupportURL=https://github.com/MarkinYuriy/videorecorder-screen-win/issues
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir=output
OutputBaseFilename=ScreenRecorder_Setup_{#AppVersion}
SetupIconFile=
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible arm64
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName}
CloseApplications=yes
CloseApplicationsFilter=*{#AppExeName}*

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "launchonstartup"; \
  Description: "Launch {#AppName} with Windows"; \
  GroupDescription: "{cm:AdditionalIcons}"; \
  Flags: unchecked

[Files]
; ── x64 binaries ──────────────────────────────────────────────
Source: "{#PublishX64}\*"; \
  DestDir: "{app}"; \
  Flags: ignoreversion recursesubdirs createallsubdirs; \
  Check: not IsARM64

Source: "{#FfmpegX64}\*"; \
  DestDir: "{app}"; \
  Flags: ignoreversion recursesubdirs createallsubdirs; \
  Check: not IsARM64

; ── ARM64 binaries ────────────────────────────────────────────
Source: "{#PublishARM64}\*"; \
  DestDir: "{app}"; \
  Flags: ignoreversion recursesubdirs createallsubdirs; \
  Check: IsARM64

Source: "{#FfmpegARM64}\*"; \
  DestDir: "{app}"; \
  Flags: ignoreversion recursesubdirs createallsubdirs; \
  Check: IsARM64

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autostartup}\{#AppName}";  Filename: "{app}\{#AppExeName}"; Tasks: launchonstartup

[Registry]
; Remove Run entry on uninstall regardless of task selection
Root: HKCU; \
  Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
  ValueName: "{#AppName}"; \
  Flags: uninsdeletevalue dontcreatekey

[Run]
; Kill any running instance before install (belt & suspenders after CloseApplications)
Filename: "taskkill"; \
  Parameters: "/f /im {#AppExeName}"; \
  Flags: runhidden skipifdoesntexist; \
  BeforeInstall: True

; Launch after install
Filename: "{app}\{#AppExeName}"; \
  Description: "Launch {#AppName}"; \
  Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "taskkill"; \
  Parameters: "/f /im {#AppExeName}"; \
  Flags: runhidden skipifdoesntexist

[UninstallDelete]
; Clean up settings left by the app
Type: filesandordirs; Name: "{userappdata}\ScreenRecorder"

[Code]
// After install: write Run key if user chose launchonstartup
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    if IsTaskSelected('launchonstartup') then
      RegWriteStringValue(HKCU,
        'Software\Microsoft\Windows\CurrentVersion\Run',
        '{#AppName}',
        ExpandConstant('"{app}\{#AppExeName}"'));
  end;
end;
