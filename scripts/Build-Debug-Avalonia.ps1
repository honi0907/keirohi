# Debug ビルド（Windows）。起動中のアプリがあれば先に終了する。
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

& (Join-Path $root "scripts\Stop-KeirohiAvaloniaIfRunning.ps1")

$csproj = Join-Path $root "Keirohi.Avalonia\Keirohi.Avalonia.csproj"
dotnet build $csproj -f net8.0-windows
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
