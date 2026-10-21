param(
    [string]$Version = "",
    [string]$DistDir = "",
    [string]$IsccPath = ""
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$dist = if ([string]::IsNullOrWhiteSpace($DistDir)) { Join-Path $root "dist" } else { $DistDir }

function Get-ProjectVersion {
    $csproj = Join-Path $root "Keirohi.WinUI\Keirohi.App.csproj"
    [xml]$xml = Get-Content $csproj
    $version = $xml.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
    if (-not $version) { throw "Version not found in Keirohi.App.csproj" }
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

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = Get-ProjectVersion
}

$publishDir = Join-Path $dist "publish\win-x64"
$publishExe = Join-Path $publishDir "Keirohi.App.exe"
if (-not (Test-Path $publishExe)) {
    throw "Publish output not found: $publishDir"
}

# ショートカットは exe 内のアイコンを使う。公開物へ AppIcon.ico を明示して埋め込む。
& (Join-Path $root "scripts\Stamp-ExeIcon.ps1") -ExePath $publishExe

$iscc = Find-IsccPath -Override $IsccPath
if (-not $iscc) {
    throw "Inno Setup 6 (ISCC.exe) not found. Install: winget install --id JRSoftware.InnoSetup -e"
}

$issPath = Join-Path $PSScriptRoot "Keirohi.iss"
Get-ChildItem $dist -Filter "Keirohi-*-x64-Setup.exe" -ErrorAction SilentlyContinue | Remove-Item -Force
& $iscc "/DAppVersion=$Version" "/DPublishDir=$publishDir" "/DOutputDir=$dist" "/DRepoRoot=$root" $issPath
if ($LASTEXITCODE -ne 0) { throw "Inno Setup build failed" }

$setupPath = Join-Path $dist "Keirohi-$Version-x64-Setup.exe"
if (-not (Test-Path $setupPath)) { throw "Setup was not generated: $setupPath" }
Write-Host "Setup: $setupPath"
