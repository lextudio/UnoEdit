# UnoEdit — Claude Code Guide

## Project Overview

UnoEdit is a code editor control for Uno Platform and WinUI 3, ported from AvalonEdit. It targets both `net9.0-desktop` (Uno) and `net9.0-windows10.0.19041.0` (WinUI 3 / Windows App SDK).

### Solution layout

```
src/
  UnoEdit/                  Core editor library (Uno.Sdk, dual-target)
  UnoEdit.TextMate/         TextMate grammar support
  UnoEdit.Sample/           Uno desktop sample app (net9.0-desktop)
  UnoEdit.WinUI.Sample/     WinUI 3 sample app (Microsoft.NET.Sdk, net9.0-windows10.0.19041.0)
external/
  propertygrid/             UnoPropertyGrid submodule (also dual-target: net9.0-desktop + net9.0-windows10.0.19041.0)
```

## Dual-platform pattern

Both UnoEdit and UnoPropertyGrid use the same pattern to support Uno and WinUI in a single `.csproj`:

```xml
<TargetFrameworks>net9.0-desktop</TargetFrameworks>
<TargetFrameworks Condition="'$(OS)' == 'Windows_NT'">$(TargetFrameworks);net9.0-windows10.0.19041.0</TargetFrameworks>

<PropertyGroup Condition="'$(TargetFramework)' == 'net9.0-windows10.0.19041.0'">
  <UseWinUI>true</UseWinUI>
  <EnableWindowsTargeting>true</EnableWindowsTargeting>
  <DefineConstants>$(DefineConstants);WINDOWS_APP_SDK</DefineConstants>
  <WindowsSdkPackageVersion>10.0.19041.57</WindowsSdkPackageVersion>
</PropertyGroup>
```

Platform-specific C# uses `#if WINDOWS_APP_SDK` / `#else`. Platform-specific files use `.uno.cs` suffix (excluded from WinUI builds via explicit `<Compile>` ItemGroup on the Windows target).

## Building

### Uno desktop target (both platforms)
```
dotnet build src/UnoEdit/UnoEdit.csproj -f net9.0-desktop
```

### WinUI target — XAML class libraries require MSBuild, not dotnet build (UNOB0008)
```powershell
$msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
    -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe
& $msbuild src/UnoEdit/UnoEdit.csproj /p:TargetFramework=net9.0-windows10.0.19041.0 /t:Build /v:minimal
```

Resolved MSBuild path on this machine: `C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe`

### WinUI.Sample (uses dotnet build — it's an app, not a class library)
```
dotnet build src/UnoEdit.WinUI.Sample/UnoEdit.WinUI.Sample.csproj
```

## Package management

Centralized via `src/Directory.Packages.props`. The `external/propertygrid/src/` directory has its own `Directory.Packages.props` but it inherits and mirrors the same versions.

Key packages: `Uno.Sdk 6.5.31`, `Microsoft.WindowsAppSDK 1.8.260317003`, .NET SDK 10.0.100.
