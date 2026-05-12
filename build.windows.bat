@echo off
rem build.windows.bat - Locate MSBuild via vswhere and build UnoEdit.slnx
setlocal EnableExtensions EnableDelayedExpansion

echo Locating MSBuild via vswhere/where...
set "MSBUILD_PATH="

rem Prefer explicit vswhere locations (ProgramFiles(x86) then ProgramFiles)
set "VSWHERE1=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
set "VSWHERE2=%ProgramFiles%\Microsoft Visual Studio\Installer\vswhere.exe"
set "VSWHERE="
if exist "%VSWHERE1%" (set "VSWHERE=%VSWHERE1%") else if exist "%VSWHERE2%" (set "VSWHERE=%VSWHERE2%")

rem If we have vswhere, try to get the installationPath then the MSBuild.exe path
if defined VSWHERE (
	for /f "usebackq delims=" %%I in (`"%VSWHERE%" -latest -products * -requires Microsoft.Component.MSBuild -property installationPath 2^>nul`) do (
		if exist "%%I\MSBuild\Current\Bin\MSBuild.exe" (
			set "MSBUILD_PATH=%%I\MSBuild\Current\Bin\MSBuild.exe"
			goto :found_msbuild
		)
	)
	rem fallback: ask vswhere to find the msbuild.exe directly
	for /f "usebackq delims=" %%I in (`"%VSWHERE%" -latest -products * -requires Microsoft.Component.MSBuild -find "MSBuild\\**\\Bin\\MSBuild.exe" 2^>nul`) do (
		if exist "%%I" (
			set "MSBUILD_PATH=%%I"
			goto :found_msbuild
		)
	)
) else (
	rem try vswhere on PATH if not in the usual locations
	for /f "delims=" %%I in ('vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath 2^>nul') do (
		if exist "%%I\MSBuild\Current\Bin\MSBuild.exe" (
			set "MSBUILD_PATH=%%I\MSBuild\Current\Bin\MSBuild.exe"
			goto :found_msbuild
		)
	)
	for /f "delims=" %%I in ('vswhere -latest -products * -requires Microsoft.Component.MSBuild -find "MSBuild\\**\\Bin\\MSBuild.exe" 2^>nul') do (
		if exist "%%I" (
			set "MSBUILD_PATH=%%I"
			goto :found_msbuild
		)
	)
)

rem Fallbacks: where.exe then search Program Files
for /f "delims=" %%I in ('where msbuild 2^>nul') do (
	if exist "%%I" ( set "MSBUILD_PATH=%%I" & goto :found_msbuild )
)
for /f "delims=" %%I in ('dir /b /s "%ProgramFiles%\Microsoft Visual Studio\*\MSBuild\Current\Bin\MSBuild.exe" 2^>nul') do (
	if exist "%%~I" ( set "MSBUILD_PATH=%%~I" & goto :found_msbuild )
)
for /f "delims=" %%I in ('dir /b /s "%ProgramFiles(x86)%\Microsoft Visual Studio\*\MSBuild\Current\Bin\MSBuild.exe" 2^>nul') do (
	if exist "%%~I" ( set "MSBUILD_PATH=%%~I" & goto :found_msbuild )
)

:found_msbuild

if defined MSBUILD_PATH (
	echo Found MSBuild: "%MSBUILD_PATH%"
) else (
	echo MSBuild not found. Will fallback to 'dotnet msbuild'.
)

rem Determine solution path relative to script location
set "SCRIPT_DIR=%~dp0/src/"
set "SOLUTION=%SCRIPT_DIR%UnoEdit.slnx"
if not exist "%SOLUTION%" (
	set "SOLUTION=UnoEdit.slnx"
	if not exist "%SOLUTION%" (
		echo Solution file UnoEdit.slnx not found in script folder or cwd.
		endlocal & exit /b 1
	)
)

rem Determine build configuration (default: Debug, override with first argument)
set "CONFIG=Debug"
if not "%~1"=="" (
	set "CONFIG=%~1"
)

rem Build the solution and capture exit code properly using delayed expansion
if defined MSBUILD_PATH (
	echo Running MSBuild with Configuration=!CONFIG!...
	"%MSBUILD_PATH%" "%SOLUTION%" /t:restore /p:Configuration=!CONFIG! /m /verbosity:minimal /nologo
	"%MSBUILD_PATH%" "%SOLUTION%" /p:Configuration=!CONFIG! /m /verbosity:minimal /nologo
	set "RC=!ERRORLEVEL!"
) else (
	echo No MSBuild found. Failed.
	exit /b 1
)

if !RC! neq 0 (
	echo Build failed with exit code !RC!.
) else (
	echo Build succeeded.
)

endlocal & exit /b !RC!

