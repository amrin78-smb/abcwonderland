#!/usr/bin/env bash
# Builds the manifest and generates provisional voice clips offline (Kokoro TTS, Apache-2.0).
# First run creates a venv + downloads models (~330 MB) into $ABCW_TTS_HOME (default: reuses ~/.cache/lbw-tts).
# Usage: Tools/voice/generate_voice.sh [--force] [keys...]   then run Tools/setup.sh to attach clips.
set -euo pipefail
cd "$(dirname "$0")/../.."
T="${ABCW_TTS_HOME:-$HOME/.cache/lbw-tts}"
if [ ! -x "$T/venv/bin/python" ]; then
  python3 -m venv "$T/venv"
  "$T/venv/bin/pip" install -q kokoro-onnx==0.6.1 soundfile
fi
mkdir -p "$T/models"
for f in kokoro-v1.0.onnx voices-v1.0.bin; do
  [ -s "$T/models/$f" ] || curl -sSL -o "$T/models/$f" "https://github.com/thewh1teagle/kokoro-onnx/releases/download/model-files-v1.0/$f"
done
python3 -I Tools/voice/build_voice_manifest.py
"$T/venv/bin/python" -I Tools/voice/generate_voice.py UnityProject/Assets/ABCWonderworld/Content/voice_manifest.json \
  UnityProject/Assets/ABCWonderworld/Audio/Voice/en "$T/models" "$@" 2>&1 | grep -v -i warning
