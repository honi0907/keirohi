param(
    [ValidateSet("osx-arm64", "osx-x64")]
    [string]$Runtime = "osx-arm64"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$csproj = Join-Path $root "Keirohi.Avalonia\Keirohi.Avalonia.csproj"
[xml]$xml = Get-Content $csproj
$version = ($xml.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1).Trim()
$publishDir = Join-Path $root "dist\publish-avalonia\$Runtime"

Write-Host "== Publish Avalonia v$version ($Runtime, net8.0) =="
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
dotnet publish $csproj -c Release -f net8.0 -r $Runtime --self-contained true `
    -p:PublishTrimmed=false `
    -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

Write-Host "Published: $publishDir/Keirohi.Avalonia"
Write-Host "On macOS: chmod +x Keirohi.Avalonia && open . or wrap in .app / .dmg"
