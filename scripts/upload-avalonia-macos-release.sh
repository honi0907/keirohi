#!/usr/bin/env bash
# Upload Mac .dmg assets to honi0907/keirohi-avalonia GitHub Release (same tag as csproj Version).
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
csproj="$root/Keirohi.Avalonia/Keirohi.Avalonia.csproj"
repo="${KEIROHI_AVALONIA_RELEASE_REPO:-honi0907/keirohi-avalonia}"

version="$(sed -n 's:.*<Version>\([^<]*\)</Version>.*:\1:p' "$csproj" | head -1)"
tag="v${version}"

if ! command -v gh >/dev/null 2>&1; then
  echo "GitHub CLI (gh) が必要です: https://cli.github.com/" >&2
  echo "  brew install gh && gh auth login" >&2
  exit 1
fi

if ! gh auth status >/dev/null 2>&1; then
  echo "gh にログインしてください: gh auth login" >&2
  exit 1
fi

shopt -s nullglob
assets=("$root/dist/Keirohi-${version}"-osx-*.dmg)
if [[ ${#assets[@]} -eq 0 ]]; then
  echo "DMG がありません。先に ./scripts/create-avalonia-macos-dmg.sh を実行してください。" >&2
  exit 1
fi

echo "== Upload to $repo release $tag =="
for dmg in "${assets[@]}"; do
  echo "  $dmg"
  gh release upload "$tag" "$dmg" --repo "$repo" --clobber
done

echo "Done: https://github.com/$repo/releases/tag/$tag"
