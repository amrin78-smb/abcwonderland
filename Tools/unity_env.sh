#!/usr/bin/env bash
# Shared paths for the batch tools. Override UNITY_EDITOR to use another installed editor.
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$ROOT/UnityProject"
LOGS="$ROOT/Logs"
UNITY="${UNITY_EDITOR:-$HOME/Unity/Hub/Editor/6000.3.27f2/Editor/Unity}"
mkdir -p "$LOGS"
if [ ! -x "$UNITY" ]; then echo "Unity editor not found at $UNITY (set UNITY_EDITOR)" >&2; exit 2; fi
# Report compile errors from a log file. Returns 1 if any were found.
check_compile() { if grep -qE "error CS[0-9]+" "$1"; then grep -E "error CS[0-9]+" "$1" | sort -u | head -40; return 1; fi; return 0; }
