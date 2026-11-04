# Milestone Report: M0 complete, M1 vertical slice playable (2026-11-04)

## Environment
Unity 6000.3.27f2 (URP 17.3, Input System 1.20.1, Test Framework 1.6), Android Build Support with bundled SDK/NDK/JDK, Linux (Zorin 18.1). Target: Android, package `com.nocvault.abcwonderworld.dev`, version 0.1.0 (1).

## Implemented
- Foundation: project in `UnityProject/`, assemblies, bootstrap/composition root, flow and scene loading, save system, audio, input, UI kit, validation, generators, batch tools, all required documentation.
- Content (all 26 letters, generated from `alphabet.json`): letter data with separate name and sound, 78 vocabulary entries with sound flags, 52 tracing paths, 26 placeholder 3D characters, 5 worlds, 26 adventure configs, 32 rewards, 234 provisional voice clips.
- Screens: Home, World Map, 5 World free-play scenes, Activity, Alphabet Book, Parent Zone (gate, dashboard, settings, reset).
- Activities: Meet the Letters, Letter Tracing (upper + lower, 3 modes), Find the Letter (adaptive 2–6 choices, hint ladder), Match (drag or tap), Adventures (TapCollect, PopTarget, FindHidden, Fill).

## Verification (exact results)
- `Tools/setup.sh`: generated successfully; validation **0 errors**, 136 warnings (all placeholder/provisional/pending-review items).
- `Tools/run_tests.sh EditMode`: **250/250 passed**.
- `Tools/run_tests.sh PlayMode`: **6/6 passed**.
- `Tools/autopilot.sh` (Linux player, 1600×900): **all checks passed, 0 errors/exceptions logged**, 358 s. 36 screenshots in `Logs/AutoPilot/`.
- `Tools/build_android.sh`: **Succeeded**, `Builds/Android/ABCWonderworld-dev.apk` 61.5 MB, arm64-v8a + armeabi-v7a, minSdk 25 / targetSdk 36. aapt: no INTERNET/camera/mic/location/storage permissions.
- **Not verified:** physical Android device (none connected), so there are no FPS/memory measurements yet.

## Bugs found and fixed during verification
Oversized theme badges hid characters; translucent glyph overlap artefacts; celebration overlapping activity UI; the celebration star never appearing (Attention captured scale 0); **B offered against D at beginner level** (confusable groups extended + test); matched pictures hidden behind targets; Unity fake-null `??` on a component (caught by PlayMode tests); screenshot timing in the AutoPilot; Unity-added INTERNET permission.

## Known limitations / placeholders
See `CONTENT_COMPLETENESS.md` and `ASSET_REGISTER.md`. In summary: all art is placeholder (procedural characters and environments, Noto emoji pictures, yak mismatch); all voice is synthetic and provisional; phonics and stroke order are pending expert review; 8 adventure archetypes are pending (13 letters on fallbacks); Puzzles, ABC song and music area, and cosmetic equip UI are not started; no Low/Med/High quality presets; no device profiling. M1's "final-quality art for one character and environment section" needs an art decision.

## Decisions needed from the owner
1. Art production route for final characters and environments.
2. Voice actor / recording plan, and a phonics reviewer.
3. Release package ID (when approaching release).
