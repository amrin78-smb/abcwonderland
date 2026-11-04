#!/usr/bin/env bash
# Runs a Unity Editor static method in batch mode inside a virtual X server.
# Usage: Tools/unity_batch.sh <Namespace.Class.Method> [log-name] [extra Unity args...]
# With no method, just opens the project (imports + compiles) and quits.
set -uo pipefail
source "$(dirname "$0")/unity_env.sh"
METHOD="${1:-}"; LOG="${2:-batch}"; shift $(( $# >= 2 ? 2 : $# ))
ARGS=(); [ -n "$METHOD" ] && ARGS=(-executeMethod "$METHOD")
xvfb-run -a -s "-screen 0 1920x1080x24" "$UNITY" -batchmode -quit -force-glcore \
  -projectPath "$PROJECT" "${ARGS[@]}" -logFile "$LOGS/$LOG.log" "$@"
CODE=$?
check_compile "$LOGS/$LOG.log" || CODE=1
echo "[unity_batch] $LOG exit=$CODE (log: Logs/$LOG.log)"
exit $CODE
