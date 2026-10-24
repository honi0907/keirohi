#!/usr/bin/env bash
# Publish Keirohi.Avalonia for macOS (self-contained, net8.0 only).
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
version="$(sed -n 's:.*<Version>\([^<]*\)</Version>.*:\1:p' "$csproj" | head -1)"

echo "== Publish Keirohi.Avalonia v${version} (${runtime}, net8.0) =="
rm -rf "$publish_dir"
dotnet publish "$csproj" -c Release -f net8.0 -r "$runtime" --self-contained true \
  -p:PublishTrimmed=false \
  -p:TargetFrameworks=net8.0 \
  -o "$publish_dir"

chmod +x "$publish_dir/Keirohi.Avalonia"
echo "Published: $publish_dir/Keirohi.Avalonia"
