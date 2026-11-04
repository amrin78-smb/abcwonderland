# Audio and Voice Guidelines

- Channels: Voice (one line at a time; sequences queue), Music (ducks to 30 % under voice), SFX (6-voice pool), Ambient. Independent volume settings; music toggle.
- Voice manifest: `Content/voice_manifest.json` (234 lines, generated from `alphabet.json` + static lines in `Tools/voice/build_voice_manifest.py`). Keys: `letter.<x>.name`, `letter.<x>.sound`, `letter.<x>.intro`, `vocab.<id>`, `adventure.<x>.instruction`, `world.<id>.name`, `vo.*` instructions and feedback.
- **Current clips are PROVISIONAL synthetic speech** (Kokoro TTS, Apache-2.0, General American voices; D-011). Phoneme clips are synthesised from IPA and are the weakest. Every clip must be replaced by a recorded voice actor before release.
- Recording spec for the replacement: General American, warm, consistent narrator, 44.1/48 kHz mono WAV, −1 dBFS peak, 50 ms lead-in, no reverb. Isolated phonemes: continuants held ~0.6 s (/m/, /s/, /f/…), stops clipped with minimal schwa (/b/ not "buh"), short vowels clear. Drop files into `Audio/Voice/en/<key>.wav` and run `Tools/setup.sh`. The status then changes once the AudioLibrary entry is marked Final.
- Feedback language: encouraging, never "wrong"/"no". A wrong choice names the chosen letter and invites another try.
- Phonics review is required (D-007), especially C, G, Q, X and the vowels.
- SFX and music are original synthesised placeholders (`AudioSynth.cs`). The ABC song / sing-along is not implemented yet (M5) and must use the public-domain melody with an original recording.
