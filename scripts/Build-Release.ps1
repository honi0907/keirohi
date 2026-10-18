param(
    [switch]$SkipGitHub
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$csproj = Join-Path $root "Keirohi.WinUI\Keirohi.App.csproj"
[xml]$xml = Get-Content $csproj
$version = ($xml.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1).Trim()
$publishDir = Join-Path $root "dist\publish\win-x64"
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }

Write-Host "== Publish v$version =="
dotnet publish $csproj -c Release -r win-x64 --self-contained true `
    -p:PublishTrimmed=false `
    -p:WindowsAppSDKSelfContained=true `
    -p:WindowsPackageType=None `
    -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

Write-Host "== Installer =="
& (Join-Path $root "installer\build-installer.ps1") -Version $version
if ($LASTEXITCODE -ne 0) { throw "installer failed" }

$setup = Join-Path $root "dist\Keirohi-$version-x64-Setup.exe"
if (-not (Test-Path $setup)) { throw "Missing setup: $setup" }

if ($SkipGitHub) {
    Write-Host "SkipGitHub: $setup"
    exit 0
}

$tag = "v$version"
Write-Host "== GitHub Release $tag =="
gh release view $tag 2>$null | Out-Null
if ($LASTEXITCODE -eq 0) {
    throw "Release $tag already exists. Bump Version in Keirohi.App.csproj first."
}

$notes = "Keirohi v$version setup. Install, then use Settings or the startup prompt to update. The app restarts after an update."
gh release create $tag $setup --title "Keirohi v$version" --notes $notes

Write-Host "Release created: $tag"
