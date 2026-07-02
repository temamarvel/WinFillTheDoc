param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Version,
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$solution = Join-Path $repoRoot "WinFillTheDoc\WinFillTheDoc.slnx"
$project = Join-Path $repoRoot "WinFillTheDoc\WinFillTheDoc\WinFillTheDoc.csproj"
$publishDir = Join-Path $repoRoot "WinFillTheDoc\release\publish"
$installerDir = Join-Path $repoRoot "WinFillTheDoc\release\installer"
$installerScript = Join-Path $repoRoot "WinFillTheDoc\installer\WinFillTheDoc.iss"
$iscc = Join-Path ${env:ProgramFiles} "Inno Setup 7\ISCC.exe"

if (-not (Test-Path $iscc)) {
    throw "Inno Setup compiler not found: $iscc"
}

function Get-NumericVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RawVersion
    )

    $normalizedVersion = $RawVersion.Trim()
    if ($normalizedVersion.StartsWith("v", [System.StringComparison]::OrdinalIgnoreCase)) {
        $normalizedVersion = $normalizedVersion.Substring(1)
    }

    if ($normalizedVersion -notmatch '^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?:[-+].*)?$') {
        throw "Version must look like 1.2.3 or 1.2.3-beta. Actual value: $RawVersion"
    }

    return "$($Matches.major).$($Matches.minor).$($Matches.patch).0"
}

function Set-ProjectVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath,
        [Parameter(Mandatory = $true)]
        [string]$ReleaseVersion,
        [Parameter(Mandatory = $true)]
        [string]$NumericVersion
    )

    [xml]$projectXml = Get-Content -Path $ProjectPath -Raw
    $propertyGroup = $projectXml.Project.PropertyGroup | Select-Object -First 1

    if ($null -eq $propertyGroup.Version) {
        $propertyGroup.AppendChild($projectXml.CreateElement("Version")) | Out-Null
    }
    if ($null -eq $propertyGroup.AssemblyVersion) {
        $propertyGroup.AppendChild($projectXml.CreateElement("AssemblyVersion")) | Out-Null
    }
    if ($null -eq $propertyGroup.FileVersion) {
        $propertyGroup.AppendChild($projectXml.CreateElement("FileVersion")) | Out-Null
    }
    if ($null -eq $propertyGroup.InformationalVersion) {
        $propertyGroup.AppendChild($projectXml.CreateElement("InformationalVersion")) | Out-Null
    }
    if ($null -eq $propertyGroup.IncludeSourceRevisionInInformationalVersion) {
        $propertyGroup.AppendChild($projectXml.CreateElement("IncludeSourceRevisionInInformationalVersion")) | Out-Null
    }

    $propertyGroup.Version = $ReleaseVersion
    $propertyGroup.AssemblyVersion = $NumericVersion
    $propertyGroup.FileVersion = $NumericVersion
    $propertyGroup.InformationalVersion = $ReleaseVersion
    $propertyGroup.IncludeSourceRevisionInInformationalVersion = "false"
    $projectXml.Save($ProjectPath)
}

function Set-InstallerVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string]$InstallerScriptPath,
        [Parameter(Mandatory = $true)]
        [string]$ReleaseVersion
    )

    $content = Get-Content -Path $InstallerScriptPath -Raw
    $updated = $content -replace '(?m)^#define\s+MyAppVersion\s+".*"$', "#define MyAppVersion `"$ReleaseVersion`""

    if ($updated -eq $content) {
        throw "Could not update MyAppVersion in $InstallerScriptPath"
    }

    Set-Content -Path $InstallerScriptPath -Value $updated -NoNewline
}

$Version = $Version.Trim()
if ([string]::IsNullOrWhiteSpace($Version)) {
    throw "Version is required."
}

$assemblyFileVersion = Get-NumericVersion -RawVersion $Version

Set-ProjectVersion -ProjectPath $project -ReleaseVersion $Version -NumericVersion $assemblyFileVersion
Set-InstallerVersion -InstallerScriptPath $installerScript -ReleaseVersion $Version

Write-Host "Release version: $Version"
Write-Host "Assembly/File version: $assemblyFileVersion"

Remove-Item -LiteralPath $publishDir -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $installerDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null
New-Item -ItemType Directory -Path $installerDir -Force | Out-Null

dotnet restore $solution --verbosity:minimal
dotnet test $solution --configuration $Configuration --no-restore --verbosity:minimal

dotnet publish $project `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    -p:Version=$Version `
    -p:AssemblyVersion=$assemblyFileVersion `
    -p:FileVersion=$assemblyFileVersion `
    -p:InformationalVersion=$Version `
    -p:IncludeSourceRevisionInInformationalVersion=false `
    -p:PublishSingleFile=false `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    --output $publishDir

& $iscc $installerScript

Write-Host "Release installer:"
Get-ChildItem -Path $installerDir -Filter "*.exe" | Select-Object -ExpandProperty FullName
