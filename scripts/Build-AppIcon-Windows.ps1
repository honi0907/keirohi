param(
    [string]$SourcePng = "",
    [string]$OutputIco = ""
)

$ErrorActionPreference = "Stop"

function Write-Ico {
    param(
        [string]$Path,
        [System.Collections.Generic.List[System.Drawing.Bitmap]]$Bitmaps
    )

    $stream = [System.IO.File]::Create($Path)
    try {
        $writer = New-Object System.IO.BinaryWriter $stream
        $writer.Write([UInt16]0)
        $writer.Write([UInt16]1)
        $writer.Write([UInt16]$Bitmaps.Count)

        $offset = 6 + (16 * $Bitmaps.Count)
        $pngChunks = New-Object System.Collections.Generic.List[byte[]]
        foreach ($bitmap in $Bitmaps) {
            $pngStream = New-Object System.IO.MemoryStream
            try {
                $bitmap.Save($pngStream, [System.Drawing.Imaging.ImageFormat]::Png)
                $pngChunks.Add($pngStream.ToArray())
            }
            finally {
                $pngStream.Dispose()
            }
        }

        for ($i = 0; $i -lt $Bitmaps.Count; $i++) {
            $bitmap = $Bitmaps[$i]
            $png = $pngChunks[$i]
            $widthByte = if ($bitmap.Width -ge 256) { [byte]0 } else { [byte]$bitmap.Width }
            $heightByte = if ($bitmap.Height -ge 256) { [byte]0 } else { [byte]$bitmap.Height }

            $writer.Write($widthByte)
            $writer.Write($heightByte)
            $writer.Write([byte]0)
            $writer.Write([byte]0)
            $writer.Write([UInt16]1)
            $writer.Write([UInt16]32)
            $writer.Write([UInt32]$png.Length)
            $writer.Write([UInt32]$offset)
            $offset += $png.Length
        }

        foreach ($png in $pngChunks) {
            $writer.Write($png)
        }
    }
    finally {
        $stream.Dispose()
    }
}

$root = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($SourcePng)) {
    $SourcePng = Join-Path $root "Keirohi.Avalonia\Assets\AppIcon-1024.png"
}

if ([string]::IsNullOrWhiteSpace($OutputIco)) {
    $OutputIco = Join-Path $root "Keirohi.Avalonia\Assets\AppIcon.ico"
}

if (-not (Test-Path $SourcePng)) {
    throw "Missing source PNG: $SourcePng"
}

Add-Type -AssemblyName System.Drawing

$sizes = @(16, 32, 48, 256)
$images = New-Object System.Collections.Generic.List[System.Drawing.Bitmap]
$source = $null
try {
    $source = [System.Drawing.Bitmap]::FromFile((Resolve-Path $SourcePng).Path)
    foreach ($size in $sizes) {
        $scaled = New-Object System.Drawing.Bitmap $size, $size
        $graphics = [System.Drawing.Graphics]::FromImage($scaled)
        try {
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.DrawImage($source, 0, 0, $size, $size)
        }
        finally {
            $graphics.Dispose()
        }

        $images.Add($scaled)
    }

    Write-Ico -Path $OutputIco -Bitmaps $images
    Write-Host "Created: $OutputIco"
}
finally {
    if ($source) { $source.Dispose() }
    foreach ($image in $images) {
        $image.Dispose()
    }
}
