#!/usr/bin/env bash
# Runs EditMode and/or PlayMode tests. Usage: Tools/run_tests.sh [EditMode|PlayMode|all]
set -uo pipefail
source "$(dirname "$0")/unity_env.sh"
WHICH="${1:-all}"; FAIL=0
for P in EditMode PlayMode; do
  [ "$WHICH" != all ] && [ "$WHICH" != "$P" ] && continue
  L="${P,,}"
  rm -f "$LOGS/$L-results.xml"
  xvfb-run -a -s "-screen 0 1920x1080x24" "$UNITY" -batchmode -force-glcore -projectPath "$PROJECT" \
    -runTests -testPlatform "$P" -testResults "$LOGS/$L-results.xml" -logFile "$LOGS/$L.log"
  CODE=$?
  check_compile "$LOGS/$L.log" || CODE=1
  SUMMARY=$(grep -o -m1 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"[^>]*skipped="[0-9]*"' "$LOGS/$L-results.xml" 2>/dev/null || echo "no results file")
  echo "[run_tests] $P exit=$CODE $SUMMARY"
  if [ -f "$LOGS/$L-results.xml" ]; then
    python3 - "$LOGS/$L-results.xml" <<'PY'
import sys, xml.etree.ElementTree as ET
for tc in ET.parse(sys.argv[1]).iter('test-case'):
    if tc.get('result') == 'Failed':
        msg = tc.find('failure/message')
        print('  FAIL', tc.get('fullname'), '-', (msg.text or '').strip().splitlines()[0][:300] if msg is not None and msg.text else '')
PY
  fi
  [ $CODE -ne 0 ] && FAIL=1
done
exit $FAIL
