#!/usr/bin/env bash
# Builds the development APK (IL2CPP, ARM64+ARMv7, dev package id, debug signing) to Builds/Android/.
# Release signing is deliberately NOT automated: see Documentation/BUILD_AND_RELEASE.md.
set -uo pipefail
cd "$(dirname "$0")/.."
source Tools/unity_env.sh
"$UNITY" -batchmode -nographics -quit -buildTarget Android -projectPath "$PROJECT" \
  -executeMethod ABCWonderworld.EditorTools.BuildScript.BuildAndroidBatch -logFile "$LOGS/android_build.log"
CODE=$?
check_compile "$LOGS/android_build.log" || CODE=1
grep "\[Build\]" "$LOGS/android_build.log" | head -10
echo "[build_android] exit=$CODE"
exit $CODE
