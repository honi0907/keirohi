$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$web = Join-Path $root "web"
$dist = Join-Path $root "dist"
$zipPath = Join-Path $dist "keirohi-mobile-web.zip"

$include = @(
    "index.html",
    "version.json",
    "manifest.webmanifest",
    "sw.js",
    "drive-sync.js",
    "google-config.js",
    "google-config.sample.js",
    "OFFLINE.md",
    "icons"
)

$csproj = Join-Path $root "Keirohi.Avalonia\Keirohi.Avalonia.csproj"
if (Test-Path $csproj) {
    $xml = [xml](Get-Content $csproj -Raw)
    $ver = $xml.Project.PropertyGroup.Version | Select-Object -First 1
    if ($ver) {
        $versionJson = Join-Path $web "version.json"
        @{ version = $ver; app = "keirohi-mobile-web" } | ConvertTo-Json | Set-Content $versionJson -Encoding UTF8
        $indexPath = Join-Path $web "index.html"
        if (Test-Path $indexPath) {
            try {
                $html = Get-Content $indexPath -Raw -Encoding UTF8
                $html = [regex]::Replace($html, 'const APP_VERSION = "[^"]+";', "const APP_VERSION = `"$ver`";")
                Set-Content $indexPath $html -Encoding UTF8 -NoNewline
            }
            catch {
                Write-Warning "index.html を更新できませんでした（他プロセスが使用中の可能性）: $_"
            }
        }
    }
}

New-Item -ItemType Directory -Force -Path $dist | Out-Null
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

$staging = Join-Path $env:TEMP "keirohi-mobile-web-pack"
if (Test-Path $staging) {
    Remove-Item $staging -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $staging | Out-Null

foreach ($item in $include) {
    $src = Join-Path $web $item
    if (-not (Test-Path $src)) {
        Write-Warning "Skip missing: $item"
        continue
    }
    Copy-Item $src (Join-Path $staging $item) -Recurse -Force
}

Compress-Archive -Path (Join-Path $staging "*") -DestinationPath $zipPath -Force
Remove-Item $staging -Recurse -Force

Write-Host "Created: $zipPath"
Write-Host "Copy this zip to your phone, unzip, and open index.html in Chrome."
