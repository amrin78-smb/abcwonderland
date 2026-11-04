# ABC Wonderworld

A 3D preschool alphabet adventure (ages 2–5) for Android, built with **Unity 6000.3.27f2 + URP**.
Five themed worlds, 26 letter characters, and voice-led play: Meet the Letters, Letter Tracing, Find the Letter, Matching, Alphabet Adventures, Free Play and the Alphabet Book, plus a gated Parent Zone.
Offline, no ads, no accounts, no purchases, no data leaves the device.

> **Status (2026-11-04): all 8 GDD modes playable for A–Z**: Meet, Tracing, Find, Match, Puzzles, 26 Adventures (12 archetypes), Music sing-along + music area, Free Play, Alphabet Book + wardrobe, Parent Zone. All art and voice are placeholders/provisional and phonics are pending expert review; see `Documentation/CONTENT_COMPLETENESS.md`. Test APKs: GitHub Releases. Device checklist: `Documentation/DEVICE_TEST_CHECKLIST.md`.

## Quick start
1. Unity Hub → open `UnityProject/` with Unity **6000.3.27f2** (Android Build Support installed).
2. Open `Assets/ABCWonderworld/Scenes/Boot.unity` → Play. Any other scene also runs on its own.
3. Regenerate generated content after editing `Content/alphabet.json` or generator code: menu **ABC Wonderworld ▸ Setup Project**, or `Tools/setup.sh`.

## Command-line tools (run from the repo root)
| Command | What it does |
|---|---|
| `Tools/setup.sh` | Generate settings, art, audio, characters, ScriptableObjects and scenes, then validate. Writes `Documentation/CONTENT_COMPLETENESS.md`. |
| `Tools/run_tests.sh [EditMode\|PlayMode\|all]` | Unity Test Framework in batch mode. |
| `Tools/autopilot.sh` | Linux test build + automated play-through; screenshots and report in `Logs/AutoPilot/`. |
| `Tools/build_android.sh` | Development APK → `Builds/Android/ABCWonderworld-dev.apk`. |
| `Tools/voice/generate_voice.sh` | Provisional offline TTS voice lines (Kokoro). Run `Tools/setup.sh` afterwards. |
| `python3 -I Tools/art/fetch_noto_emoji.py …` | Re-fetch the placeholder pictures. |

## Layout
```
Documentation/        GDD, master prompt, architecture, specs, decisions, traceability, QA, asset register
Tools/                batch scripts, voice + art pipelines
UnityProject/Assets/ABCWonderworld/
  Content/            alphabet.json (source of truth), voice_manifest.json (generated)
  Scripts/Runtime/    game code (Core, Content, Persistence, Learning, Tracing, Audio, Characters, Activities, Screens, World, UI, Parent, Rewards, Input)
  Scripts/Editor/     generators, validation report, build script
  Tests/              EditMode + PlayMode tests
  Resources/          catalog, UI skin, pictures, SFX, music (generated)
```
