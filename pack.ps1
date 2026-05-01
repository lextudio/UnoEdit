Param(
    [string]$OutDir = ".\dist",
    [string]$Configuration = "Release",
    [string[]]$Projects = @(
        "src\LeXtudio.Windows\LeXtudio.Windows.csproj",
        "src\UnoEdit\UnoEdit.csproj",
        "src\UnoEdit.TextMate\UnoEdit.TextMate.csproj"
    )
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = $PSScriptRoot
$BuildRoot = Join-Path $RepoRoot ".build_out"
$PackageStaging = Join-Path $RepoRoot ".pkg_staging"

function Find-MSBuild {
    $programFilesX86 = [Environment]::GetEnvironmentVariable("ProgramFiles(x86)")
    $vswhere = if ($programFilesX86) {
        Join-Path $programFilesX86 "Microsoft Visual Studio\Installer\vswhere.exe"
    }

    if ($vswhere -and (Test-Path $vswhere)) {
        $installPath = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath 2>$null
        if ($installPath) {
            $candidate = Join-Path $installPath "MSBuild\Current\Bin\MSBuild.exe"
            if (Test-Path $candidate) {
                return (Resolve-Path $candidate).Path
            }
        }
    }

    $command = Get-Command msbuild -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Path
    }

    throw "MSBuild was not found. Install Visual Studio/MSBuild to pack the WinUI target."
}

function Resolve-ProjectPath([string]$Project) {
    if (Test-Path $Project) {
        return (Resolve-Path $Project).Path
    }

    $candidate = Join-Path $RepoRoot $Project
    if (Test-Path $candidate) {
        return (Resolve-Path $candidate).Path
    }

    throw "Project file not found: $Project"
}

function Reset-Directory([string]$Path) {
    if (Test-Path $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }

    New-Item -ItemType Directory -Path $Path -Force | Out-Null
}

function Get-AssemblyReferenceNames([string]$AssemblyPath) {
    Add-Type -AssemblyName System.Reflection.Metadata -ErrorAction Stop

    $stream = [IO.File]::OpenRead($AssemblyPath)
    try {
        $peReader = [Reflection.PortableExecutable.PEReader]::new($stream)
        try {
            $metadata = [Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($peReader)
            foreach ($handle in $metadata.AssemblyReferences) {
                $assemblyRef = $metadata.GetAssemblyReference($handle)
                $metadata.GetString($assemblyRef.Name)
            }
        } finally {
            $peReader.Dispose()
        }
    } finally {
        $stream.Dispose()
    }
}

function Get-TypeDefinitionNames([string]$AssemblyPath) {
    Add-Type -AssemblyName System.Reflection.Metadata -ErrorAction Stop

    $stream = [IO.File]::OpenRead($AssemblyPath)
    try {
        $peReader = [Reflection.PortableExecutable.PEReader]::new($stream)
        try {
            $metadata = [Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($peReader)
            foreach ($handle in $metadata.TypeDefinitions) {
                $type = $metadata.GetTypeDefinition($handle)
                $namespace = $metadata.GetString($type.Namespace)
                $name = $metadata.GetString($type.Name)
                if ($namespace) {
                    "$namespace.$name"
                } else {
                    $name
                }
            }
        } finally {
            $peReader.Dispose()
        }
    } finally {
        $stream.Dispose()
    }
}

function Test-UnoEditPackage([string]$PackagePath) {
    if ((Split-Path -Leaf $PackagePath) -notmatch "^LeXtudio\.UnoEdit\.\d.*\.nupkg$") {
        return
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction Stop

    $extractRoot = Join-Path ([IO.Path]::GetTempPath()) "unoedit_pkg_verify_$([Guid]::NewGuid())"
    Reset-Directory $extractRoot
    try {
        [IO.Compression.ZipFile]::ExtractToDirectory($PackagePath, $extractRoot)
        $desktopAssemblies = @(
            Get-ChildItem -Path (Join-Path $extractRoot "lib") -Recurse -Filter "UnoEdit.dll" |
                Where-Object { $_.FullName -match "net[0-9.]+-desktop" }
        )

        if ($desktopAssemblies.Count -eq 0) {
            throw "Package '$PackagePath' does not contain a desktop UnoEdit.dll asset."
        }

        foreach ($assembly in $desktopAssemblies) {
            $references = @(Get-AssemblyReferenceNames $assembly.FullName)
            Write-Host "  Verified desktop asset: $($assembly.FullName)"
            Write-Host "    References: $($references -join ', ')"

            if ($references -contains "Microsoft.WinUI") {
                throw "Desktop asset '$($assembly.FullName)' references Microsoft.WinUI. Desktop assets must reference Uno.UI instead."
            }

            $winUITypes = @(
                Get-TypeDefinitionNames $assembly.FullName |
                    Where-Object { $_ -like "UnoEdit.WinUI.Controls.*" }
            )
            if ($winUITypes.Count -gt 0) {
                throw "Desktop asset '$($assembly.FullName)' contains WinUI-only types: $($winUITypes -join ', ')"
            }
        }
    } finally {
        if (Test-Path $extractRoot) {
            Remove-Item -LiteralPath $extractRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

function Invoke-PackProject([string]$MSBuild, [string]$ProjectPath) {
    $projectName = [IO.Path]::GetFileNameWithoutExtension($ProjectPath)
    $projectOutput = Join-Path $BuildRoot $projectName
    Reset-Directory $projectOutput

    $arguments = @(
        $ProjectPath,
        "/restore",
        "/t:Pack",
        "/p:Configuration=$Configuration",
        "/p:BaseOutputPath=$projectOutput\",
        "/p:PackageOutputPath=$PackageStaging",
        "/v:minimal",
        "/nologo"
    )

    Write-Host ""
    Write-Host "Packing $ProjectPath"
    Write-Host "  Output:   $projectOutput"
    Write-Host "  Packages: $PackageStaging"

    # MSBuild imports environment variables as properties. Clear OutDir so callers
    # cannot accidentally force all target frameworks into one shared folder.
    $previousOutDir = [Environment]::GetEnvironmentVariable("OutDir", "Process")
    [Environment]::SetEnvironmentVariable("OutDir", $null, "Process")
    try {
        & $MSBuild @arguments
        if ($LASTEXITCODE -ne 0) {
            throw "MSBuild pack failed for '$ProjectPath' with exit code $LASTEXITCODE."
        }
    } finally {
        [Environment]::SetEnvironmentVariable("OutDir", $previousOutDir, "Process")
    }
}

function Copy-PackagesToOutput([string]$Source, [string]$Destination) {
    $packages = @(
        Get-ChildItem -Path $Source -File |
            Where-Object { $_.Extension -eq ".nupkg" -or $_.Extension -eq ".snupkg" } |
            Sort-Object Name
    )

    if ($packages.Count -eq 0) {
        throw "No .nupkg or .snupkg files were produced in '$Source'."
    }

    Write-Host ""
    Write-Host "Verifying packages"
    foreach ($package in $packages | Where-Object { $_.Extension -eq ".nupkg" }) {
        Test-UnoEditPackage $package.FullName
    }

    Write-Host ""
    Write-Host "Copying packages to $Destination"
    foreach ($package in $packages) {
        Copy-Item -LiteralPath $package.FullName -Destination $Destination -Force
        Write-Host "  $($package.Name)"
    }

    $nonPackages = @(
        Get-ChildItem -Path $Destination -File |
            Where-Object { $_.Extension -ne ".nupkg" -and $_.Extension -ne ".snupkg" }
    )
    if ($nonPackages.Count -gt 0) {
        throw "Non-package files found in output directory: $($nonPackages.FullName -join ', ')"
    }
}

try {
    $msbuild = Find-MSBuild
    $outputPath = if ([IO.Path]::IsPathRooted($OutDir)) { $OutDir } else { Join-Path (Get-Location) $OutDir }
    $outputPath = [IO.Path]::GetFullPath($outputPath)

    Write-Host "MSBuild: $msbuild"
    Write-Host "Configuration: $Configuration"
    Write-Host "Output directory: $outputPath"

    Reset-Directory $BuildRoot
    Reset-Directory $PackageStaging
    Reset-Directory $outputPath

    foreach ($project in $Projects) {
        Invoke-PackProject $msbuild (Resolve-ProjectPath $project)
    }

    Copy-PackagesToOutput $PackageStaging $outputPath

    Write-Host ""
    Write-Host "Packing complete." -ForegroundColor Green
    Get-ChildItem -Path $outputPath -File |
        Sort-Object Name |
        ForEach-Object { Write-Host "  $($_.Name)" -ForegroundColor Green }
} finally {
    if (Test-Path $BuildRoot) {
        Remove-Item -LiteralPath $BuildRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
    if (Test-Path $PackageStaging) {
        Remove-Item -LiteralPath $PackageStaging -Recurse -Force -ErrorAction SilentlyContinue
    }
}
