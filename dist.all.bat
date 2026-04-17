@echo off
REM dist.all.bat - Pack UnoEdit projects and sign resulting NuGet packages
REM Ensures ONLY .nupkg files exist in dist folder upon successful completion
SETLOCAL EnableExtensions EnableDelayedExpansion
echo on

REM Configuration
set CONFIG=Release
set OUTDIR=%~dp0dist
set TIMESTAMPER=http://timestamp.digicert.com

echo.
echo ============================================================
echo Pre-cleanup: Removing dist folder completely
echo ============================================================
if exist "%OUTDIR%" (
    echo Removing: %OUTDIR%
    rmdir /s /q "%OUTDIR%"
    if errorlevel 1 (
        echo Attempting aggressive cleanup...
        for /d %%D in ("%OUTDIR%\*") do rmdir /s /q "%%D" 2>nul
        for %%F in ("%OUTDIR%\*") do del /q /f "%%F" 2>nul
    )
)
mkdir "%OUTDIR%"

echo.
echo ============================================================
echo Step 1: Packing NuGet packages
echo ============================================================
echo Invoking pack.ps1 to produce nupkgs in "%OUTDIR%" ...
pwsh -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%~dp0pack.ps1" -OutDir "%OUTDIR%" -Configuration "%CONFIG%"
if errorlevel 1 goto :error_pack

echo.
echo ============================================================
echo Verification: Checking that only .nupkg and .snupkg files exist
echo ============================================================
pwsh -NoProfile -NonInteractive -ExecutionPolicy Bypass -Command ^
  "$outdir = '%OUTDIR%'; $files = @(Get-ChildItem -Path $outdir -File -ErrorAction SilentlyContinue | Where-Object { ($_.Extension -ne '.nupkg') -and ($_.Extension -ne '.snupkg') }); if ($files.Count -gt 0) { Write-Error 'ERROR: Non-package files found in dist folder:'; $files | ForEach-Object { Write-Error ('  - ' + $_.FullName) }; exit 1 } else { Write-Host 'SUCCESS: Only .nupkg/.snupkg files in dist folder'; exit 0 }"
if errorlevel 1 goto :error_verify

echo.
echo ============================================================
echo Step 2: Signing packages
echo ============================================================
echo Invoking sign.ps1 to sign packages in "%OUTDIR%" ...
pwsh -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%~dp0sign.ps1" -PackageDir "%OUTDIR%" -TimestampServer "%TIMESTAMPER%" -Overwrite
if errorlevel 1 goto :error_sign

echo.
echo ============================================================
echo Final verification: Checking that only .nupkg and .snupkg files exist
echo ============================================================
pwsh -NoProfile -NonInteractive -ExecutionPolicy Bypass -Command ^
  "$outdir = '%OUTDIR%'; $files = @(Get-ChildItem -Path $outdir -File -ErrorAction SilentlyContinue | Where-Object { ($_.Extension -ne '.nupkg') -and ($_.Extension -ne '.snupkg') }); if ($files.Count -gt 0) { Write-Error 'ERROR: Non-package files found in dist folder after signing:'; $files | ForEach-Object { Write-Error ('  - ' + $_.FullName) }; exit 1 } else { Write-Host 'SUCCESS: Only .nupkg/.snupkg files in dist folder'; exit 0 }"
if errorlevel 1 goto :error_final_verify

echo.
echo All packages processed successfully and available in "%OUTDIR%"
exit /b 0

:error_pack
echo ERROR: NuGet package creation failed.
exit /b 1

:error_verify
echo ERROR: Post-pack verification failed. Non-.nupkg files found in dist folder.
exit /b 1

:error_sign
echo ERROR: Package signing failed.
exit /b 1

:error_final_verify
echo ERROR: Final verification failed. Non-.nupkg files found in dist folder after signing.
exit /b 1
