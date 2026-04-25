# ScreenRecorder

WPF screen recorder for Windows. System tray app — records a selected region with audio, saves to MP4.

## Development

**Build & run:**
```bat
dotnet build VideoRecorderScreen/VideoRecorderScreen.csproj
dotnet run --project VideoRecorderScreen/VideoRecorderScreen.csproj
```

**Requirements:** .NET 10 SDK, Windows 10 x64

## Building the installer

**Prerequisites:**
1. [NSIS 3.09+](https://nsis.sourceforge.io/Download)  
   Default install path: `C:\Program Files (x86)\NSIS\`
2. FFmpeg binaries (dynamic build) in `Installer\ffmpeg\x64\`  
   Required files: `ffmpeg.exe` + all `.dll` files alongside it  
   Download: [ffmpeg.org/download.html](https://ffmpeg.org/download.html) → Windows builds (e.g. gyan.dev or BtbN)

**Build installer:**
```bat
build-installer.bat
```

Output: `Installer\output\ScreenRecorder_Setup_1.0.0.exe`

The script publishes the app (`dotnet publish`) then runs `makensis`.
