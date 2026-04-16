Param(
    [string]$OutDir = ".\dist",
    [string]$Configuration = "Release",
    [string]$DesktopTFM = "net10.0-desktop",
    [string[]]$Projects = @('src\UnoEdit\UnoEdit.csproj','src\UnoEdit.TextMate\UnoEdit.TextMate.csproj')
)

Set-StrictMode -Version Latest

function Find-MSBuild {
    $programFilesX86 = [System.Environment]::GetEnvironmentVariable('ProgramFiles(x86)')
    $vswhere = if ($programFilesX86) { Join-Path $programFilesX86 'Microsoft Visual Studio\Installer\vswhere.exe' } else { $null }
    if ($vswhere -and (Test-Path $vswhere)) {
        try { $instPath = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath 2>$null } catch { $instPath = $null }
        if ($instPath) {
            $c1 = Join-Path $instPath 'MSBuild\Current\Bin\MSBuild.exe'
            $c2 = Join-Path $instPath 'MSBuild\15.0\Bin\MSBuild.exe'
            if (Test-Path $c1) { return (Resolve-Path $c1).Path }
            if (Test-Path $c2) { return (Resolve-Path $c2).Path }
        }
    }
    $ms = Get-Command msbuild -ErrorAction SilentlyContinue
    if ($ms) { return $ms.Path }
    return $null
}

function Resolve-ProjectPath([string]$proj) {
    if (Test-Path $proj) { return (Resolve-Path $proj).Path }
    $candidate = Join-Path $PSScriptRoot $proj
    if (Test-Path $candidate) { return (Resolve-Path $candidate).Path }
    throw "Project file not found: $proj"
}

# Create temporary staging directories (MSBuild's OutDir and PackageOutputPath must be isolated)
$buildOutDir = Join-Path ([System.IO.Path]::GetTempPath()) ("unoedit_build_" + [Guid]::NewGuid().ToString("N").Substring(0,8))
$pkgStaging = Join-Path ([System.IO.Path]::GetTempPath()) ("unoedit_pkg_" + [Guid]::NewGuid().ToString("N").Substring(0,8))
New-Item -ItemType Directory -Path $buildOutDir -Force | Out-Null
New-Item -ItemType Directory -Path $pkgStaging -Force | Out-Null

if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir -Force | Out-Null }
$out = (Resolve-Path $OutDir).Path

# Completely clean the destination directory
Write-Host "Preparing output directory: $out"
if (Test-Path $out) {
    try {
        Get-ChildItem -Path $out -Force | Remove-Item -Recurse -Force -ErrorAction Stop
        Write-Host "Cleaned existing output directory"
    } catch {
        Write-Error "Failed to clean output directory: $_"
        Remove-Item -LiteralPath $buildOutDir -Recurse -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $pkgStaging -Recurse -Force -ErrorAction SilentlyContinue
        exit 1
    }
}

$msbuild = Find-MSBuild
if ($msbuild) {
    Write-Host "MSBuild found: $msbuild" -ForegroundColor Green
    foreach ($p in $Projects) {
        try { $projFull = Resolve-ProjectPath $p } catch { Write-Error $_.Exception.Message; Remove-Item -LiteralPath $buildOutDir -Recurse -Force -ErrorAction SilentlyContinue; Remove-Item -LiteralPath $pkgStaging -Recurse -Force -ErrorAction SilentlyContinue; exit 1 }
        Write-Host "Packing $projFull with MSBuild..."
        # NOTE: OutDir must be a temp directory to prevent DLL/PDB/XML files from going to dist
        $args = @($projFull, '/t:Restore;Pack', "/p:Configuration=$Configuration", "/p:OutDir=$buildOutDir", "/p:PackageOutputPath=$pkgStaging")
        $proc = Start-Process -FilePath $msbuild -ArgumentList $args -NoNewWindow -Wait -PassThru
        if ($proc.ExitCode -ne 0) { Write-Error "MSBuild pack failed for $projFull (exit $($proc.ExitCode))"; Remove-Item -LiteralPath $buildOutDir -Recurse -Force -ErrorAction SilentlyContinue; Remove-Item -LiteralPath $pkgStaging -Recurse -Force -ErrorAction SilentlyContinue; exit 1 }
    }
} else {
    Write-Error "MSBuild not found. Packaging WinUI or multi-TFM packages requires MSBuild/Visual Studio. Aborting."
    Remove-Item -LiteralPath $buildOutDir -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $pkgStaging -Recurse -Force -ErrorAction SilentlyContinue
    exit 1
}

# Move only .nupkg files from pkg staging to final OutDir
Write-Host "Extracting .nupkg files from staging..."
$nupkgs = Get-ChildItem -Path $pkgStaging -Filter *.nupkg -File -ErrorAction SilentlyContinue
if (-not $nupkgs) {
    Write-Error "No .nupkg files produced in staging: $pkgStaging"
    Remove-Item -LiteralPath $buildOutDir -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $pkgStaging -Recurse -Force -ErrorAction SilentlyContinue
    exit 1
}

Write-Host "Moving .nupkg files to output directory..."
foreach ($pkg in $nupkgs) {
    Write-Host "  Moving: $($pkg.Name)"
    Move-Item -Path $pkg.FullName -Destination $out -Force
}

# Clean up staging directories
Remove-Item -LiteralPath $buildOutDir -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $pkgStaging -Recurse -Force -ErrorAction SilentlyContinue

# Validate that ONLY .nupkg files are in the output directory
Write-Host "Validating output directory..."
$allFiles = Get-ChildItem -Path $out -Force -ErrorAction SilentlyContinue
$nonNupkgs = @($allFiles | Where-Object { -not $_.PSIsContainer -and $_.Extension -ne '.nupkg' })
if ($nonNupkgs.Count -gt 0) {
    Write-Error "ERROR: Non-.nupkg files found in output directory:"
    foreach ($f in $nonNupkgs) {
        Write-Error "  - $($f.FullName)"
    }
    exit 1
}

Write-Host "Packing complete. Packages are in: $out" -ForegroundColor Green
Write-Host "Contents:" -ForegroundColor Green
Get-ChildItem -Path $out -Filter *.nupkg | ForEach-Object { Write-Host "  - $($_.Name)" }
exit 0
