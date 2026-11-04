"""Generate PROVISIONAL voice clips offline with Kokoro TTS (Apache-2.0).

Usage: generate_voice.py <voice_manifest.json> <output_dir> <models_dir> [--force] [keys...]
Writes <output_dir>/<key>.wav (16-bit mono 24 kHz): silence trimmed, soft fades, peak-normalised,
short lead-in so the first syllable is not clipped on Android. Existing files are kept unless --force.
"""
import json, os, sys
import numpy as np
import soundfile as sf


def tidy(audio, sr, keep_tail=0.12):
    audio = np.asarray(audio, dtype=np.float32)
    if audio.ndim > 1:
        audio = audio.mean(axis=1)
    thr = 0.01 * float(np.max(np.abs(audio)) or 1.0)
    idx = np.where(np.abs(audio) > thr)[0]
    if len(idx):
        audio = audio[max(0, idx[0] - int(0.03 * sr)): min(len(audio), idx[-1] + int(keep_tail * sr))]
    fade = int(0.012 * sr)
    if len(audio) > 2 * fade:
        audio[:fade] *= np.linspace(0, 1, fade)
        audio[-fade:] *= np.linspace(1, 0, fade)
    peak = float(np.max(np.abs(audio)) or 1.0)
    audio = audio * (0.85 / peak)
    return np.concatenate([np.zeros(int(0.05 * sr), dtype=np.float32), audio])


def main(manifest, out_dir, models, args):
    force = "--force" in args
    only = {a for a in args if not a.startswith("--")}
    from kokoro_onnx import Kokoro
    tts = Kokoro(os.path.join(models, "kokoro-v1.0.onnx"), os.path.join(models, "voices-v1.0.bin"))
    m = json.load(open(manifest, encoding="utf-8"))
    os.makedirs(out_dir, exist_ok=True)
    made = skipped = 0
    for line in m["lines"]:
        key = line["key"]
        if only and key not in only:
            continue
        dst = os.path.join(out_dir, key + ".wav")
        if os.path.exists(dst) and not force:
            skipped += 1
            continue
        voice = line.get("voice", m["narrator"])
        speed = m["speed"]
        if "phonemes" in line:
            audio, sr = tts.create(line["phonemes"], voice=m["narrator"], speed=0.8, lang="en-us", is_phonemes=True)
        else:
            audio, sr = tts.create(line["text"], voice=voice, speed=speed, lang="en-us")
        sf.write(dst, tidy(audio, sr), sr, subtype="PCM_16")
        made += 1
    print(f"voice: {made} generated, {skipped} kept")


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4:])
