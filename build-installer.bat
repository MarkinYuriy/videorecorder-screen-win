@echo off
setlocal

set PROJ=VideoRecorderScreen\VideoRecorderScreen.csproj
set ISCC="C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

echo === Publish x64 ===
dotnet publish %PROJ% /p:PublishProfile=win-x64
if errorlevel 1 ( echo FAILED: publish x64 & exit /b 1 )

echo === Publish ARM64 ===
dotnet publish %PROJ% /p:PublishProfile=win-arm64
if errorlevel 1 ( echo FAILED: publish arm64 & exit /b 1 )

echo === Compile installer ===
if not exist %ISCC% (
  echo Inno Setup not found at %ISCC%
  echo Install from https://jrsoftware.org/isinfo.php
  exit /b 1
)

%ISCC% Installer\setup.iss
if errorlevel 1 ( echo FAILED: ISCC & exit /b 1 )

echo.
echo Done! Installer is in Installer\output\
