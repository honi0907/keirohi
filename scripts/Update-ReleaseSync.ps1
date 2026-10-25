param(
    [string]$Version = "",
    [ValidateSet("", "WinSetup", "MacDmg", "MainPush")]
    [string]$Set = "",
    [ValidateSet("Synced", "Pending", "Uncertain")]
    [string]$Value = "Synced",
    [string]$Note = "",
    [switch]$EnsureRow,
    [switch]$RefreshTodos
)

$ErrorActionPreference = "Stop"

# ASCII-only script: Japanese status labels as UTF-8 bytes (PowerShell 5 safe).
$Utf8 = [System.Text.Encoding]::UTF8
$LabelSynced = $Utf8.GetString([byte[]](0xE6, 0xB8, 0x88))
$LabelPending = $Utf8.GetString([byte[]](0xE6, 0x9C, 0xAA))
$LabelUncertain = $Utf8.GetString([byte[]](0xE8, 0xA6, 0x81, 0xE7, 0xA2, 0xBA, 0xE8, 0xAA, 0x8D))
$DefaultNote = "Release pending"

function ConvertTo-SyncStatusLabel {
    param([string]$Token)
    switch ($Token) {
        "Synced" { return $LabelSynced }
        "Pending" { return $LabelPending }
        "Uncertain" { return $LabelUncertain }
        default { return $Token }
    }
}

$root = Split-Path -Parent $PSScriptRoot
$docPath = Join-Path $root "docs\RELEASE-SYNC.md"
$csproj = Join-Path $root "Keirohi.Avalonia\Keirohi.Avalonia.csproj"

if (-not (Test-Path $docPath)) {
    throw "Missing $docPath"
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    [xml]$xml = Get-Content $csproj
    $Version = ($xml.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1).Trim()
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    throw "Version not found in csproj"
}

$lines = [System.Collections.Generic.List[string]]::new()
$lines.AddRange([string[]](Get-Content $docPath -Encoding UTF8))

function Get-TableBounds {
    param([System.Collections.Generic.List[string]]$DocLines)
    $header = -1
    $sep = -1
    for ($i = 0; $i -lt $DocLines.Count; $i++) {
        if ($DocLines[$i] -match '^\| Version ') { $header = $i }
        if ($DocLines[$i] -match '^\|-+\|' ) { $sep = $i; break }
    }
    if ($header -lt 0 -or $sep -lt 0) { throw "Release table not found in RELEASE-SYNC.md" }
    return @{ Header = $header; Sep = $sep }
}

function Parse-Row {
    param([string]$Line)
    if ($Line -notmatch '^\|') { return $null }
    $parts = ($Line -split '\|', 0)[1..5]
    if ($parts.Count -lt 5) { return $null }
    return [ordered]@{
        Version = $parts[0].Trim()
        MainPush = $parts[1].Trim()
        WinSetup = $parts[2].Trim()
        MacDmg = $parts[3].Trim()
        Note = $parts[4].Trim()
    }
}

function Format-Row {
    param($Row)
    $v = $Row.Version.PadRight(7)
    $m = $Row.MainPush.PadRight(9)
    $w = $Row.WinSetup.PadRight(9)
    $d = $Row.MacDmg.PadRight(9)
    return ('| {0} | {1} | {2} | {3} | {4} |' -f $v, $m, $w, $d, $Row.Note)
}

$bounds = Get-TableBounds -DocLines $lines
$rowIndex = -1
$rows = [System.Collections.Generic.List[object]]::new()
for ($i = $bounds.Sep + 1; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -notmatch '^\|') { break }
    $parsed = Parse-Row $lines[$i]
    if ($null -eq $parsed) { break }
    $rows.Add($parsed)
    if ($parsed.Version -eq $Version) { $rowIndex = $rows.Count - 1 }
}

if ($EnsureRow -and $rowIndex -lt 0) {
    $mainDefault = $LabelPending
    try {
        Push-Location $root
        $null = git rev-parse --is-inside-work-tree 2>$null
        if ($LASTEXITCODE -eq 0) {
            git fetch origin main 2>$null | Out-Null
            $ahead = (git rev-list --count origin/main..HEAD 2>$null)
            $dirty = git status --porcelain
            if ($ahead -eq "0" -and [string]::IsNullOrWhiteSpace($dirty)) { $mainDefault = $LabelSynced }
        }
    }
    catch { }
    finally { Pop-Location }

    $newRow = [ordered]@{
        Version = $Version
        MainPush = $mainDefault
        WinSetup = $LabelPending
        MacDmg = $LabelPending
        Note = if ($Note) { $Note } else { $DefaultNote }
    }
    $rows.Insert(0, $newRow)
    $rowIndex = 0
}

if ($rowIndex -lt 0) {
    throw "Version $Version not in RELEASE-SYNC table. Re-run with -EnsureRow."
}

$statusLabel = ConvertTo-SyncStatusLabel -Token $Value
$row = $rows[$rowIndex]
if ($Set -eq "WinSetup") { $row.WinSetup = $statusLabel }
if ($Set -eq "MacDmg") { $row.MacDmg = $statusLabel }
if ($Set -eq "MainPush") { $row.MainPush = $statusLabel }
if ($Note) { $row.Note = $Note }
$rows[$rowIndex] = $row

while ($rows.Count -gt 10) { $rows.RemoveAt($rows.Count - 1) }

$newTableRows = for ($r = 0; $r -lt $rows.Count; $r++) { Format-Row $rows[$r] }
$end = $bounds.Sep + 1
while ($end -lt $lines.Count -and $lines[$end] -match '^\|') { $end++ }
$lines.RemoveRange($bounds.Sep + 1, $end - ($bounds.Sep + 1))
for ($i = $newTableRows.Count - 1; $i -ge 0; $i--) {
    $lines.Insert($bounds.Sep + 1, $newTableRows[$i])
}

if ($RefreshTodos -or $Set) {
    $latest = $rows[0]
    $todoStart = -1
    $todoEnd = -1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^## Next steps') { $todoStart = $i + 1 }
        if ($todoStart -ge 0 -and $todoEnd -lt 0 -and $lines[$i] -match '^## ' -and $i -gt $todoStart) {
            $todoEnd = $i
            break
        }
    }
    if ($todoStart -lt 0) { throw "Todo section not found" }
    if ($todoEnd -lt 0) { $todoEnd = $lines.Count }

    $todoLines = [System.Collections.Generic.List[string]]::new()
    $ver = $latest.Version
    $tag = "v$ver"
    if ($latest.MacDmg -ne $LabelSynced) {
        $todoLines.Add("- [ ] **Mac:** ``git pull`` -> DMG build -> ``./scripts/upload-avalonia-macos-release.sh`` (tag **$tag**)")
    }
    if ($latest.WinSetup -ne $LabelSynced) {
        $todoLines.Add("- [ ] **Windows:** ``.\scripts\Build-Release-Avalonia.ps1`` (tag **$tag**)")
    }
    if ($latest.MainPush -ne $LabelSynced) {
        $todoLines.Add("- [ ] **main:** commit and ``git push origin main``")
    }
    if ($todoLines.Count -eq 0) {
        $todoLines.Add("- [x] **v$ver** Win Setup / Mac DMG / main synced per table")
    }
    else {
        $todoLines.Add("- [ ] After the steps above, skim the table (Release scripts update it automatically)")
    }

    $lines.RemoveRange($todoStart, $todoEnd - $todoStart)
    $insertAt = $todoStart
    foreach ($t in $todoLines) {
        $lines.Insert($insertAt, $t)
        $insertAt++
    }

    $lines.Insert($insertAt, "")
}

$lines | Set-Content -Path $docPath -Encoding UTF8
Write-Host "Updated RELEASE-SYNC.md for v$Version$(if ($Set) { " ($Set=$Value)" })"
