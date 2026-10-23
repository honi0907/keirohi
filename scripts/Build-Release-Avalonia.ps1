param(
    [switch]$SkipGitHub
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$csproj = Join-Path $root "Keirohi.Avalonia\Keirohi.Avalonia.csproj"
[xml]$xml = Get-Content $csproj
$version = ($xml.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1).Trim()
$publishDir = Join-Path $root "dist\publish-avalonia\win-x64"
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }

Write-Host "== Publish Avalonia v$version =="
dotnet publish $csproj -c Release -r win-x64 --self-contained true `
    -p:PublishTrimmed=false `
    -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

Write-Host "== Installer =="
& (Join-Path $root "installer\build-installer.ps1") -Version $version -Product Avalonia
if ($LASTEXITCODE -ne 0) { throw "installer failed" }

$setup = Join-Path $root "dist\Keirohi.Avalonia-$version-x64-Setup.exe"
if (-not (Test-Path $setup)) { throw "Missing setup: $setup" }

if ($SkipGitHub) {
    Write-Host "SkipGitHub: $setup"
    exit 0
}

$repo = "honi0907/keirohi-avalonia"
$tag = "v$version"
Write-Host "== GitHub Release $repo $tag =="
$previousErrorAction = $ErrorActionPreference
$ErrorActionPreference = "Continue"
gh release view $tag --repo $repo 2>$null | Out-Null
$viewCode = $LASTEXITCODE
$ErrorActionPreference = $previousErrorAction
if ($viewCode -eq 0) {
    throw "Release $tag already exists on $repo. Bump Version in Keirohi.Avalonia.csproj first."
}

$notes = "Keirohi Avalonia v$version setup. Install to ``Program Files\Keirohi.Avalonia``. Updates use GitHub Release on $repo."
gh release create $tag $setup --repo $repo --title "Keirohi Avalonia v$version" --notes $notes

Write-Host "Release created: $repo $tag"
