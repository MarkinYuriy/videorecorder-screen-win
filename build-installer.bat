@echo off
setlocal

set PROJ=VideoRecorderScreen\VideoRecorderScreen.csproj
set MAKENSIS="C:\Program Files (x86)\NSIS\makensis.exe"

echo === Publish x64 ===
dotnet publish %PROJ% /p:PublishProfile=win-x64
if errorlevel 1 ( echo FAILED: publish x64 & exit /b 1 )

echo === Compile installer ===
if not exist %MAKENSIS% (
  echo NSIS not found at %MAKENSIS%
  echo Download from https://nsis.sourceforge.io
  exit /b 1
)

if not exist Installer\output mkdir Installer\output

%MAKENSIS% Installer\setup.nsi
if errorlevel 1 ( echo FAILED: makensis & exit /b 1 )

echo.
echo Done! Installer is in Installer\output\
