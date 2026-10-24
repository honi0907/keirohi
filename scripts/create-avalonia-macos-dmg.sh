#!/usr/bin/env bash
# Create a drag-to-Applications DMG from dist/Keirohi.app (hdiutil, no create-dmg).
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
csproj="$root/Keirohi.Avalonia/Keirohi.Avalonia.csproj"
app_path="$root/dist/Keirohi.app"

if [[ ! -d "$app_path" ]]; then
  echo "Missing $app_path. Run scripts/bundle-avalonia-macos-app.sh first." >&2
  exit 1
fi

version="$(sed -n 's:.*<Version>\([^<]*\)</Version>.*:\1:p' "$csproj" | head -1)"
runtime="${1:-}"
if [[ -z "$runtime" ]]; then
  case "$(uname -m)" in
    arm64) runtime="osx-arm64" ;;
    x86_64) runtime="osx-x64" ;;
    *) runtime="osx" ;;
  esac
fi

dmg_name="Keirohi-${version}-${runtime}.dmg"
dmg_path="$root/dist/$dmg_name"
staging="$root/dist/dmg-staging-$$"

cleanup() { rm -rf "$staging"; }
trap cleanup EXIT

echo "== DMG $dmg_name =="
rm -f "$dmg_path"
mkdir -p "$staging"
ditto "$app_path" "$staging/Keirohi.app"
ln -s /Applications "$staging/Applications"
cat > "$staging/インストール手順.txt" <<'TXT'
Keirohi（交通費入力）Mac 版

【重要】DMG の中から直接ダブルクリックしないでください。
「壊れている」「ディスクイメージから取り出す」と出ることがあります。

1. このウィンドウの Keirohi.app を「アプリケーション」フォルダにドラッグ
2. 「アプリケーション」フォルダを開き、Keirohi.app を起動
   初回だけ「開けない」と出たら:
   - 右クリック →「開く」→「開く」
   またはターミナルで: xattr -cr /Applications/Keirohi.app
3. 使い終わったら DMG は取り出し（右クリック → 取り出す）

TXT
xattr -cr "$staging/Keirohi.app"

hdiutil create -volname "Keirohi ${version}" -srcfolder "$staging" -ov -format UDZO "$dmg_path"
echo "Created: $dmg_path"
