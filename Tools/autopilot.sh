#!/usr/bin/env bash
# Builds the Linux test player (unless SKIP_BUILD=1) and runs the AutoPilot play-through under xvfb.
# Screenshots + report: Logs/AutoPilot/. Exit code 0 = all checks passed.
set -uo pipefail
cd "$(dirname "$0")/.."
if [ "${SKIP_BUILD:-0}" != 1 ]; then
  Tools/unity_batch.sh ABCWonderworld.EditorTools.BuildScript.BuildLinuxBatch linux_build -buildTarget Linux64 || exit 1
  grep "\[Build\]" Logs/linux_build.log | head -5
fi
rm -rf Logs/AutoPilot
timeout 900 xvfb-run -a -s "-screen 0 1920x1080x24" Builds/Linux/ABCWonderworld.x86_64 -force-glcore -screen-fullscreen 0 \
  -screen-width "${W:-1600}" -screen-height "${H:-900}" -abcw-autopilot -abcw-shots "$PWD/Logs/AutoPilot" -logFile "$PWD/Logs/autopilot.log"
CODE=$?
echo "[autopilot] exit=$CODE"
cat Logs/AutoPilot/autopilot_report.txt 2>/dev/null || grep -E "AutoPilot|Exception" Logs/autopilot.log | tail -30
exit $CODE
