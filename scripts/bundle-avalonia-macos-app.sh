#!/usr/bin/env bash
# Wrap a dotnet publish folder in Keirohi.app (no code signing).
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
csproj="$root/Keirohi.Avalonia/Keirohi.Avalonia.csproj"

runtime="${1:-}"
if [[ -z "$runtime" ]]; then
  case "$(uname -m)" in
    arm64) runtime="osx-arm64" ;;
    x86_64) runtime="osx-x64" ;;
    *) echo "Unsupported arch: $(uname -m)" >&2; exit 1 ;;
  esac
fi

publish_dir="$root/dist/publish-avalonia/$runtime"
exe="$publish_dir/Keirohi.Avalonia"
if [[ ! -x "$exe" ]]; then
  echo "Missing publish output. Run: scripts/build-publish-avalonia-macos.sh $runtime" >&2
  exit 1
fi

version="$(sed -n 's:.*<Version>\([^<]*\)</Version>.*:\1:p' "$csproj" | head -1)"
app_name="Keirohi.app"
app_path="$root/dist/$app_name"
macos_dir="$app_path/Contents/MacOS"
runtime_dir="$macos_dir/publish"
resources_dir="$app_path/Contents/Resources"

echo "== Bundle $app_name (v${version}, $runtime) =="
rm -rf "$app_path"
mkdir -p "$runtime_dir" "$resources_dir"

ditto "$publish_dir" "$runtime_dir"
chmod +x "$runtime_dir/Keirohi.Avalonia"
codesign --remove-signature "$runtime_dir/Keirohi.Avalonia" 2>/dev/null || true

while IFS= read -r -d '' f; do
  if file "$f" | grep -q "Mach-O"; then
    codesign --remove-signature "$f" 2>/dev/null || true
    codesign --force --sign - "$f" 2>/dev/null || true
  fi
done < <(find "$runtime_dir" -type f -print0)

cat > "$macos_dir/Keirohi" <<'LAUNCH'
#!/bin/bash
cd "$(dirname "$0")/publish"
exec ./Keirohi.Avalonia "$@"
LAUNCH
chmod +x "$macos_dir/Keirohi"
codesign --force --sign - "$macos_dir/Keirohi" 2>/dev/null || true

cat > "$app_path/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleDevelopmentRegion</key>
  <string>ja</string>
  <key>CFBundleDisplayName</key>
  <string>通勤経路費</string>
  <key>CFBundleExecutable</key>
  <string>Keirohi</string>
  <key>CFBundleIdentifier</key>
  <string>com.keirohi.avalonia</string>
  <key>CFBundleInfoDictionaryVersion</key>
  <string>6.0</string>
  <key>CFBundleName</key>
  <string>通勤経路費</string>
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>CFBundleShortVersionString</key>
  <string>${version}</string>
  <key>CFBundleVersion</key>
  <string>${version}</string>
  <key>LSMinimumSystemVersion</key>
  <string>12.0</string>
  <key>NSHighResolutionCapable</key>
  <true/>
PLIST

icon_src="$root/Keirohi.Avalonia/Assets/AppIcon.icns"
if [[ -f "$icon_src" ]]; then
  cp "$icon_src" "$resources_dir/AppIcon.icns"
  cat >> "$app_path/Contents/Info.plist" <<'PLIST'
  <key>CFBundleIconFile</key>
  <string>AppIcon</string>
PLIST
fi

cat >> "$app_path/Contents/Info.plist" <<'PLIST'
</dict>
</plist>
PLIST

xattr -cr "$app_path"

echo "Created: $app_path"
echo "Launch: open \"$app_path\""
echo "After download from GitHub: copy to Applications, then xattr -cr /Applications/Keirohi.app"
