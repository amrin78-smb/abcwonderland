# Build and Release

## Environment (verified 2026-11-04)
Zorin OS 18.1 (Linux), Unity **6000.3.27f2** at `~/Unity/Hub/Editor/6000.3.27f2` with Android Build Support (bundled SDK/NDK/OpenJDK), xvfb for headless batch runs.

## Development builds
- `Tools/build_android.sh` → `Builds/Android/ABCWonderworld-dev.apk` (IL2CPP, ARM64 + ARMv7, minSdk 25, targetSdk 36, NO INTERNET permission (stripped by `AndroidManifestPrivacy.cs`, verified with aapt), Vulkan + GLES3 with PowerVR → GLES3, debug keystore, package `com.nocvault.abcwonderworld.dev`).
- `Tools/autopilot.sh` → Linux Mono player `Builds/Linux/ABCWonderworld.x86_64` + automated play-through.
- Install on a device: `adb install -r Builds/Android/ABCWonderworld-dev.apk`.

## Release (not yet performed)
1. Confirm the release package ID (replace `ProjectSetup.BundleId`). This changes app identity, so it needs owner approval.
2. Create the upload keystore **outside the repo**. Configure it in Unity Player Settings locally or via environment variables in a private CI. Never commit keystores or passwords (`.gitignore` blocks `*.keystore`, `*.jks`).
3. Switch to an AAB (`EditorUserBuildSettings.buildAppBundle = true`), bump `bundleVersionCode`.
4. Complete the QA plan, the device profiling table and the Google Play Families / Designed for Families review, data-safety form ("no data collected"), content rating and privacy policy.
5. Replace all placeholder/provisional content (CONTENT_COMPLETENESS.md must have no warnings for art/voice/review).
