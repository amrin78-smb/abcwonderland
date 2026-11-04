#!/usr/bin/env bash
# Regenerates everything (settings, art, audio, characters, content, scenes) and validates content.
# Writes Documentation/CONTENT_COMPLETENESS.md. Exit code 1 on validation errors.
set -uo pipefail
cd "$(dirname "$0")/.."
Tools/unity_batch.sh ABCWonderworld.EditorTools.ProjectSetup.RunBatch setup
CODE=$?
grep -E "\[Setup\]|\[Validate\]" Logs/setup.log | head -40
exit $CODE
