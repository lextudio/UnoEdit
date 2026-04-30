Param(
    [string]$OutDir = ".\dist",
    [string]$Configuration = "Release",
    [string]$DesktopTFM = "net10.0-desktop",
    [string[]]$Projects = @('src\UnoEdit\UnoEdit.csproj','src\UnoEdit.TextMate\UnoEdit.TextMate.csproj','src\LeXtudio.Windows\LeXtudio.Windows.csproj')
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

function Get-AssemblyReferenceNames([string]$AssemblyPath) {
    try {
        Add-Type -AssemblyName System.Reflection.Metadata -ErrorAction Stop

        $stream = [System.IO.File]::OpenRead($AssemblyPath)
        try {
            $peReader = [System.Reflection.PortableExecutable.PEReader]::new($stream)
            try {
                $metadataReader = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($peReader)
                $names = New-Object System.Collections.Generic.List[string]
                foreach ($handle in $metadataReader.AssemblyReferences) {
                    $assemblyRef = $metadataReader.GetAssemblyReference($handle)
                    $names.Add($metadataReader.GetString($assemblyRef.Name))
                }

                return $names
            } finally {
                $peReader.Dispose()
            }
        } finally {
            $stream.Dispose()
        }
    } catch {
        throw "Failed to analyze assembly references for '$AssemblyPath': $($_.Exception.Message)"
    }
}

function Get-TypeDefinitionNames([string]$AssemblyPath) {
    try {
        Add-Type -AssemblyName System.Reflection.Metadata -ErrorAction Stop

        $stream = [System.IO.File]::OpenRead($AssemblyPath)
        try {
            $peReader = [System.Reflection.PortableExecutable.PEReader]::new($stream)
            try {
                $metadataReader = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($peReader)
                $names = New-Object System.Collections.Generic.List[string]
                foreach ($handle in $metadataReader.TypeDefinitions) {
                    $typeDefinition = $metadataReader.GetTypeDefinition($handle)
                    $namespace = $metadataReader.GetString($typeDefinition.Namespace)
                    $name = $metadataReader.GetString($typeDefinition.Name)
                    if ($namespace) {
                        $names.Add("$namespace.$name")
                    } else {
                        $names.Add($name)
                    }
                }

                return $names
            } finally {
                $peReader.Dispose()
            }
        } finally {
            $stream.Dispose()
        }
    } catch {
        throw "Failed to analyze type definitions for '$AssemblyPath': $($_.Exception.Message)"
    }
}

function Test-UnoEditPackageArtifacts([string]$PackagePath) {
    if ((Split-Path -Leaf $PackagePath) -notmatch '^LeXtudio\.UnoEdit\.\d.*\.nupkg$') {
        return
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem

    $extractRoot = Join-Path ([System.IO.Path]::GetTempPath()) "unoedit_pkg_verify_$([guid]::NewGuid())"
    New-Item -ItemType Directory -Path $extractRoot -Force | Out-Null
    try {
        [System.IO.Compression.ZipFile]::ExtractToDirectory($PackagePath, $extractRoot)
        $desktopAssemblies = @(Get-ChildItem -Path (Join-Path $extractRoot 'lib') -Recurse -Filter 'UnoEdit.dll' -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -match 'net[0-9.]+-desktop' })

        if (-not $desktopAssemblies -or $desktopAssemblies.Count -eq 0) {
            throw "Package $PackagePath does not contain a desktop UnoEdit.dll asset."
        }

        foreach ($assembly in $desktopAssemblies) {
            Write-Host "  Inspecting assembly: $($assembly.FullName)"
            $referenceNames = @(Get-AssemblyReferenceNames $assembly.FullName)
            Write-Host "    Reference count: $($referenceNames.Count)"
            if ($referenceNames.Count -gt 0) {
                Write-Host "    References: $($referenceNames -join ', ')"
            } else {
                Write-Host "    (no assembly references found)"
            }

            if ($referenceNames -contains 'Microsoft.WinUI') {
                throw "Desktop asset $($assembly.FullName) references Microsoft.WinUI. Desktop assets must reference Uno.UI instead."
            }

            $typeNames = @(Get-TypeDefinitionNames $assembly.FullName)
            $winUITypes = @($typeNames | Where-Object { $_ -like 'UnoEdit.WinUI.Controls.*' })
            if ($winUITypes.Count -gt 0) {
                throw "Desktop asset $($assembly.FullName) contains WinUI-only control types: $($winUITypes -join ', ')"
            }
        }
    } finally {
        if (Test-Path $extractRoot) {
            Remove-Item -LiteralPath $extractRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

# Create temporary staging directories (MSBuild's OutDir and PackageOutputPath must be isolated)
$repoRoot = $PSScriptRoot
$buildOutDir = Join-Path $repoRoot ".build_out"
$pkgStaging = Join-Path $repoRoot ".pkg_staging"
if (Test-Path $buildOutDir) { Remove-Item -LiteralPath $buildOutDir -Recurse -Force -ErrorAction SilentlyContinue }
if (Test-Path $pkgStaging) { Remove-Item -LiteralPath $pkgStaging -Recurse -Force -ErrorAction SilentlyContinue }
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
        Write-Host "  Build output: $buildOutDir"
        Write-Host "  Package staging: $pkgStaging"
        # NOTE: use BaseOutputPath instead of OutDir so multi-targeted projects keep
        # per-TFM output folders. A shared OutDir lets inner builds overwrite each
        # other, which can put WinUI assemblies into desktop package assets.
        $projectBuildOutDir = Join-Path $buildOutDir ([System.IO.Path]::GetFileNameWithoutExtension($projFull))
        $msbuildArgs = @($projFull, '/t:Restore;Pack', "/p:Configuration=$Configuration", "/p:BaseOutputPath=$projectBuildOutDir\", "/p:PackageOutputPath=$pkgStaging")
        Write-Host "  Starting MSBuild process at $(Get-Date -Format 'HH:mm:ss.fff')..."
        try {
            $proc = Start-Process -FilePath $msbuild -ArgumentList $msbuildArgs -NoNewWindow -PassThru -RedirectStandardOutput ([System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "msbuild_out_$([guid]::NewGuid()).log")) -RedirectStandardError ([System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "msbuild_err_$([guid]::NewGuid()).log"))
            Write-Host "  MSBuild process started (PID: $($proc.Id)). Waiting for completion..."
            $timeoutMs = 600000
            if ($proc.WaitForExit($timeoutMs)) {
                Write-Host "  MSBuild process completed at $(Get-Date -Format 'HH:mm:ss.fff') with exit code: $($proc.ExitCode)" -ForegroundColor Green
                if ($proc.ExitCode -ne 0) { Write-Error "MSBuild pack failed for $projFull (exit $($proc.ExitCode))"; Remove-Item -LiteralPath $buildOutDir -Recurse -Force -ErrorAction SilentlyContinue; Remove-Item -LiteralPath $pkgStaging -Recurse -Force -ErrorAction SilentlyContinue; exit 1 }
            } else {
                Write-Error "MSBuild timed out after 600 seconds for $projFull"
                $proc.Kill()
                Remove-Item -LiteralPath $buildOutDir -Recurse -Force -ErrorAction SilentlyContinue
                Remove-Item -LiteralPath $pkgStaging -Recurse -Force -ErrorAction SilentlyContinue
                exit 1
            }
        } catch {
            Write-Error "Failed to run MSBuild: $_"
            Remove-Item -LiteralPath $buildOutDir -Recurse -Force -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $pkgStaging -Recurse -Force -ErrorAction SilentlyContinue
            exit 1
        }
    }
} else {
    Write-Error "MSBuild not found. Packaging WinUI or multi-TFM packages requires MSBuild/Visual Studio. Aborting."
    Remove-Item -LiteralPath $buildOutDir -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $pkgStaging -Recurse -Force -ErrorAction SilentlyContinue
    exit 1
}

# Move .nupkg and .snupkg files from pkg staging to final OutDir
Write-Host "Extracting package files (.nupkg/.snupkg) from staging at $(Get-Date -Format 'HH:mm:ss.fff')..."
Write-Host "  Staging directory: $pkgStaging"
Write-Host "  Checking for files..."
$packages = @(Get-ChildItem -Path $pkgStaging -File -ErrorAction SilentlyContinue | Where-Object { -not $_.PSIsContainer -and ($_.Extension -eq '.nupkg' -or $_.Extension -eq '.snupkg') })
$allFilesInStaging = @(Get-ChildItem -Path $pkgStaging -File -ErrorAction SilentlyContinue)
Write-Host "  Total files in staging: $($allFilesInStaging.Count)"
if ($allFilesInStaging) {
    $allFilesInStaging | ForEach-Object { Write-Host "    - $($_.Name) ($([math]::Round($_.Length/1MB, 2)) MB)" }
}

if (-not $packages -or $packages.Count -eq 0) {
    Write-Error "No .nupkg or .snupkg files produced in staging: $pkgStaging"
    Remove-Item -LiteralPath $buildOutDir -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $pkgStaging -Recurse -Force -ErrorAction SilentlyContinue
    exit 1
}

Write-Host "Verifying package artifacts..."
foreach ($pkg in $packages | Where-Object { $_.Extension -eq '.nupkg' }) {
    try {
        Test-UnoEditPackageArtifacts $pkg.FullName
    } catch {
        Write-Error $_.Exception.Message
        Remove-Item -LiteralPath $buildOutDir -Recurse -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $pkgStaging -Recurse -Force -ErrorAction SilentlyContinue
        exit 1
    }
}

Write-Host "Moving package files ($($packages.Count) file(s)) to output directory at $(Get-Date -Format 'HH:mm:ss.fff')..."
foreach ($pkg in $packages) {
    Write-Host "  Moving: $($pkg.Name) ($([math]::Round($pkg.Length/1MB, 2)) MB)"
    Move-Item -Path $pkg.FullName -Destination $out -Force
    Write-Host "    Done at $(Get-Date -Format 'HH:mm:ss.fff')"
}

# Clean up staging directories
if (Test-Path $buildOutDir) { Remove-Item -LiteralPath $buildOutDir -Recurse -Force -ErrorAction SilentlyContinue }
if (Test-Path $pkgStaging) { Remove-Item -LiteralPath $pkgStaging -Recurse -Force -ErrorAction SilentlyContinue }

# Validate that ONLY .nupkg and .snupkg files are in the output directory
Write-Host "Validating output directory..."
$allFiles = Get-ChildItem -Path $out -Force -ErrorAction SilentlyContinue
$nonPackages = @($allFiles | Where-Object { -not $_.PSIsContainer -and ($_.Extension -ne '.nupkg' -and $_.Extension -ne '.snupkg') })
if ($nonPackages.Count -gt 0) {
    Write-Error "ERROR: Non-package files found in output directory:"
    foreach ($f in $nonPackages) {
        Write-Error "  - $($f.FullName)"
    }
    exit 1
}

Write-Host "Packing complete. Packages are in: $out" -ForegroundColor Green
Write-Host "Contents:" -ForegroundColor Green
Get-ChildItem -Path $out -File -ErrorAction SilentlyContinue | Where-Object { $_.Extension -eq '.nupkg' -or $_.Extension -eq '.snupkg' } | ForEach-Object { Write-Host "  - $($_.Name)" }
exit 0
