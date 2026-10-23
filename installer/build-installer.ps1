param(
    [string]$Version = "",
    [string]$DistDir = "",
    [string]$IsccPath = "",
    [ValidateSet("WinUI", "Avalonia")]
    [string]$Product = "WinUI"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$dist = if ([string]::IsNullOrWhiteSpace($DistDir)) { Join-Path $root "dist" } else { $DistDir }

function Get-ProjectVersion {
    param([string]$CsprojPath)
    [xml]$xml = Get-Content $CsprojPath
    $version = $xml.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
    if (-not $version) { throw "Version not found in $CsprojPath" }
    return $version.Trim()
}

function Find-IsccPath {
    param([string]$Override)
    if (-not [string]::IsNullOrWhiteSpace($Override) -and (Test-Path $Override)) {
        return (Resolve-Path $Override).Path
    }

    $candidates = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    )
    foreach ($path in $candidates) {
        if (Test-Path $path) { return (Resolve-Path $path).Path }
    }

    return $null
}

$config = switch ($Product) {
    "WinUI" {
        @{
            Csproj = Join-Path $root "Keirohi.WinUI\Keirohi.App.csproj"
            PublishSubDir = "publish\win-x64"
            PublishExe = "Keirohi.App.exe"
            IssFile = "Keirohi.iss"
            SetupPrefix = "Keirohi"
        }
    }
    "Avalonia" {
        @{
            Csproj = Join-Path $root "Keirohi.Avalonia\Keirohi.Avalonia.csproj"
            PublishSubDir = "publish-avalonia\win-x64"
            PublishExe = "Keirohi.Avalonia.exe"
            IssFile = "Keirohi.Avalonia.iss"
            SetupPrefix = "Keirohi.Avalonia"
        }
    }
    default { throw "Unknown product: $Product" }
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = Get-ProjectVersion -CsprojPath $config.Csproj
}

$publishDir = Join-Path $dist $config.PublishSubDir
$publishExe = Join-Path $publishDir $config.PublishExe
if (-not (Test-Path $publishExe)) {
    throw "Publish output not found: $publishExe"
}

& (Join-Path $root "scripts\Stamp-ExeIcon.ps1") -ExePath $publishExe

$iscc = Find-IsccPath -Override $IsccPath
if (-not $iscc) {
    throw "Inno Setup 6 (ISCC.exe) not found. Install: winget install --id JRSoftware.InnoSetup -e"
}

$issPath = Join-Path $PSScriptRoot $config.IssFile
Get-ChildItem $dist -Filter "$($config.SetupPrefix)-*-x64-Setup.exe" -ErrorAction SilentlyContinue | Remove-Item -Force
& $iscc "/DAppVersion=$Version" "/DPublishDir=$publishDir" "/DOutputDir=$dist" "/DRepoRoot=$root" $issPath
if ($LASTEXITCODE -ne 0) { throw "Inno Setup build failed" }

$setupPath = Join-Path $dist "$($config.SetupPrefix)-$Version-x64-Setup.exe"
if (-not (Test-Path $setupPath)) { throw "Setup was not generated: $setupPath" }
Write-Host "Setup: $setupPath"
