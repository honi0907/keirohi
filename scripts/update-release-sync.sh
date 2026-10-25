#!/usr/bin/env bash
# Update docs/RELEASE-SYNC.md (Mac DMG / shared). Prefer PowerShell implementation when available.
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
ps1="$root/scripts/Update-ReleaseSync.ps1"

usage() {
  echo "Usage: $0 mac|win|main [-Note text]" >&2
  exit 1
}

platform="${1:-}"
shift || true
note=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    -Note) note="${2:-}"; shift 2 ;;
    *) echo "Unknown arg: $1" >&2; usage ;;
  esac
done

[[ -n "$platform" ]] || usage

set_arg=""
case "$platform" in
  mac) set_arg="MacDmg" ;;
  win) set_arg="WinSetup" ;;
  main) set_arg="MainPush" ;;
  *) usage ;;
esac

args=(-Set $set_arg -Value Synced -RefreshTodos -EnsureRow)
[[ -n "$note" ]] && args+=(-Note "$note")

if command -v pwsh >/dev/null 2>&1; then
  exec pwsh -NoProfile -File "$ps1" @args
fi

echo "pwsh not found; install PowerShell or run on Windows: Update-ReleaseSync.ps1" >&2
exit 1
