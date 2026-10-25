# 通勤経路費 (Keirohi.Avalonia) が起動中なら終了する。ビルド前の DLL ロック回避用。
$ErrorActionPreference = "SilentlyContinue"

$stopped = @()

foreach ($p in Get-Process) {
    $name = $p.ProcessName
    $title = $p.MainWindowTitle
    $path = $p.Path

    $isApp = $name -eq "Keirohi.Avalonia" `
        -or $name -eq "通勤経路費" `
        -or ($title -and $title.Contains("通勤経路費")) `
        -or ($path -and $path -match "Keirohi\.Avalonia(\.exe)?$")

    if (-not $isApp) {
        continue
    }

    Stop-Process -Id $p.Id -Force
    $stopped += "$name ($($p.Id))"
}

if ($stopped.Count -gt 0) {
    Write-Host "Stopped: $($stopped -join ', ')"
    Start-Sleep -Milliseconds 600
}
