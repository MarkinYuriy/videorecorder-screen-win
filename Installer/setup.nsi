; ─────────────────────────────────────────────────────────────
;  ScreenRecorder — NSIS installer
;  Requires NSIS 3.09+ (https://nsis.sourceforge.io)
;
;  Before building, place ffmpeg files at:
;    Installer\ffmpeg\x64\ffmpeg.exe  (+ avcodec-*.dll etc.)
;
;  Build via: build-installer.bat
; ─────────────────────────────────────────────────────────────

!include "MUI2.nsh"
!include "x64.nsh"
!include "LogicLib.nsh"

; ── Defines ───────────────────────────────────────────────────
!define APP_NAME    "ScreenRecorder"
!define APP_VER     "1.0.0"
!define APP_EXE     "VideoRecorderScreen.exe"
!define APP_GUID    "B7C4A3F2-1E9D-4F8A-BC3D-5A6E7F8B9C0D"
!define UNINST_KEY  "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_GUID}"
!define RUN_KEY     "Software\Microsoft\Windows\CurrentVersion\Run"

; ── General ───────────────────────────────────────────────────
Name            "${APP_NAME} ${APP_VER}"
OutFile         "output\ScreenRecorder_Setup_${APP_VER}.exe"
Unicode         True
RequestExecutionLevel admin
SetCompressor   /SOLID lzma

InstallDir      "$PROGRAMFILES64\${APP_NAME}"
InstallDirRegKey HKLM "${UNINST_KEY}" "InstallLocation"

; ── MUI ───────────────────────────────────────────────────────
!define MUI_ABORTWARNING

; Finish page: run checkbox
!define MUI_FINISHPAGE_RUN          "$INSTDIR\${APP_EXE}"
!define MUI_FINISHPAGE_RUN_TEXT     "Launch ${APP_NAME}"

; Finish page: "Launch with Windows" checkbox via SHOWREADME hook
!define MUI_FINISHPAGE_SHOWREADME   ""
!define MUI_FINISHPAGE_SHOWREADME_TEXT    "Launch ${APP_NAME} with Windows"
!define MUI_FINISHPAGE_SHOWREADME_FUNCTION  Func_WriteRunKey

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"
!insertmacro MUI_LANGUAGE "Russian"

; ── Install ───────────────────────────────────────────────────
Section "Main" SecMain

  ; Kill any running instance
  nsExec::Exec 'taskkill /f /im "${APP_EXE}"'

  SetOutPath "$INSTDIR"

  ; ── x64 binaries (ARM64 support deferred — no test hardware) ──
  File /r "..\publish\win-x64\*"
  File /r "ffmpeg\x64\*"

  ; ── Uncomment when ARM64 is ready: ────────────────────────────
  ; ${If} ${IsNativeARM64}
  ;   File /r "..\publish\win-arm64\*"
  ;   File /r "ffmpeg\arm64\*"
  ; ${Else}
  ;   File /r "..\publish\win-x64\*"
  ;   File /r "ffmpeg\x64\*"
  ; ${EndIf}

  ; Start menu shortcuts
  CreateDirectory "$SMPROGRAMS\${APP_NAME}"
  CreateShortcut  "$SMPROGRAMS\${APP_NAME}\${APP_NAME}.lnk"  "$INSTDIR\${APP_EXE}"
  CreateShortcut  "$SMPROGRAMS\${APP_NAME}\Uninstall.lnk"    "$INSTDIR\Uninstall.exe"

  ; Add/Remove Programs entry
  WriteRegStr   HKLM "${UNINST_KEY}" "DisplayName"     "${APP_NAME}"
  WriteRegStr   HKLM "${UNINST_KEY}" "DisplayVersion"  "${APP_VER}"
  WriteRegStr   HKLM "${UNINST_KEY}" "Publisher"       "Markin Yuriy"
  WriteRegStr   HKLM "${UNINST_KEY}" "InstallLocation" "$INSTDIR"
  WriteRegStr   HKLM "${UNINST_KEY}" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegDWORD HKLM "${UNINST_KEY}" "NoModify"        1
  WriteRegDWORD HKLM "${UNINST_KEY}" "NoRepair"        1

  WriteUninstaller "$INSTDIR\Uninstall.exe"

SectionEnd

; Called if user checks "Launch with Windows" on Finish page
Function Func_WriteRunKey
  WriteRegStr HKCU "${RUN_KEY}" "${APP_NAME}" '"$INSTDIR\${APP_EXE}"'
FunctionEnd

; ── Uninstall ─────────────────────────────────────────────────
Section "Uninstall"

  nsExec::Exec 'taskkill /f /im "${APP_EXE}"'

  ; Remove shortcuts
  Delete "$SMPROGRAMS\${APP_NAME}\${APP_NAME}.lnk"
  Delete "$SMPROGRAMS\${APP_NAME}\Uninstall.lnk"
  RMDir  "$SMPROGRAMS\${APP_NAME}"

  ; Remove installed files
  RMDir /r "$INSTDIR"

  ; Remove registry entries
  DeleteRegKey   HKLM "${UNINST_KEY}"
  DeleteRegValue HKCU "${RUN_KEY}" "${APP_NAME}"

  ; Remove user settings
  RMDir /r "$APPDATA\ScreenRecorder"

SectionEnd
