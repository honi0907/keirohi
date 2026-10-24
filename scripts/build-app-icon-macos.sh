#!/usr/bin/env bash
# Build Keirohi.Avalonia/Assets/AppIcon.icns from AppIcon-1024.png (iconset + iconutil).
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
assets="$root/Keirohi.Avalonia/Assets"
base="$assets/AppIcon-1024.png"

if [[ ! -f "$base" ]]; then
  echo "Missing $base" >&2
  exit 1
fi

iconset="$assets/AppIcon.iconset"
rm -rf "$iconset"
mkdir -p "$iconset"

sips -z 16 16 "$base" --out "$iconset/icon_16x16.png" >/dev/null
sips -z 32 32 "$base" --out "$iconset/icon_16x16@2x.png" >/dev/null
sips -z 32 32 "$base" --out "$iconset/icon_32x32.png" >/dev/null
sips -z 64 64 "$base" --out "$iconset/icon_32x32@2x.png" >/dev/null
sips -z 128 128 "$base" --out "$iconset/icon_128x128.png" >/dev/null
sips -z 256 256 "$base" --out "$iconset/icon_128x128@2x.png" >/dev/null
sips -z 256 256 "$base" --out "$iconset/icon_256x256.png" >/dev/null
sips -z 512 512 "$base" --out "$iconset/icon_256x256@2x.png" >/dev/null
sips -z 512 512 "$base" --out "$iconset/icon_512x512.png" >/dev/null
cp "$base" "$iconset/icon_512x512@2x.png"

iconutil -c icns "$iconset" -o "$assets/AppIcon.icns"
rm -rf "$iconset"
sips -z 256 256 "$base" --out "$assets/AppIcon.png" >/dev/null

echo "Created: $assets/AppIcon.icns and AppIcon.png"
