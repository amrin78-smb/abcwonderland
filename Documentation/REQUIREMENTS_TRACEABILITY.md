# Requirements Traceability

Status: ✅ implemented and verified by the listed test/run · 🟡 partial or placeholder content · ⬜ not started.
"AP" = AutoPilot play-through (`Tools/autopilot.sh`), "EM" = EditMode tests, "PM" = PlayMode tests.

| Ref | Requirement | Implementation | Verification | Status |
|---|---|---|---|---|
| GDD 1.2 / MP 2 | All 26 letters, upper + lower | `alphabet.json`, `LetterDefinition`, `LetterStrokeLibrary` | EM CatalogTests, StrokeLibraryTests | ✅ (content pending review) |
| GDD 2.1 | Explorer / Discoverer / Little Learner profiles | `LearningProfile`, Parent settings | EM AdaptiveDifficulty tests | ✅ |
| GDD 2.2 | Adaptive, gradual, reversible, parent-overridable | `AdaptiveDifficulty` (D-017) | EM | ✅ |
| GDD 2.3 / 8.2 | Letter name and sound are separate | separate fields and clips; `MeetLetterActivity` sequence | EM CatalogTests | ✅ |
| GDD 3.1 | Core loop (hub → world → character → activity → feedback → reward → return) | Screens + `ActivityScreen` journey | AP | ✅ |
| GDD 3.2 | Safe save on unexpected exit, no time limits | autosave, pause/quit save | EM save tests, PM | ✅ (device force-close test pending) |
| GDD 4 | Five themed worlds, never punitive gates | `EnvironmentBuilder` themes, WorldMap always open | AP screenshots | 🟡 placeholder art |
| GDD 5 | 26 original letter characters, consistent family | `CharacterFactory` prefabs | EM catalog, AP | 🟡 placeholder art |
| GDD 5.4 / MP 11 | Idle, blink, greet, happy, celebrate, surprised, interact (+dance, talk) | `ProceduralCharacterAnimator`, `ICharacterAnimator` (D-008) | AP visual | ✅ placeholder rigs |
| GDD 6.1 | Meet the Letters | `MeetLetterActivity` | PM, AP | ✅ |
| GDD 6.2 | Tracing A–Z ×2, multi-stroke, 3 guidance modes, path progress | `TracingActivity`, `TracingEvaluator` | EM (52 forms), PM, AP | ✅ (stroke order pending review) |
| GDD 6.3 | Find the Letter, 2 / 3–4 / 5–6 choices, hints, no timer | `FindLetterActivity`, `ChoiceGenerator` | EM, PM, AP | ✅ |
| GDD 6.4 | 26 adventures | `AdventureActivity` (+ `.Placement.cs`, `.Play.cs`), 12 archetypes | PM: all 26 letters complete; AP | ✅ (placeholder art) |
| GDD 6.5 | Matching: picture↔letter, upper↔lower, drag or tap | `MatchActivity` | PM, AP | ✅ (initial-sound and identical-letter variants ⬜) |
| GDD 6.6 | ABC song, sing-along, music area | `MusicScreen`, `AbcSong` (synced highlighting, pause/replay, bells + drum, dancing cast) | PM, EM, AP | 🟡 provisional spoken-letter sing-along, needs a sung recording |
| GDD 6.7 | Puzzles: letter jigsaw + missing letter, scaling pieces, tap alternative | `PuzzleActivity` | PM, AP | ✅ (picture-completion variant ⬜) |
| GDD 6.8 | Free play per world | `WorldScreen`, `Interactable` | AP (5 worlds load) | 🟡 |
| GDD 7.1 | Main menu: animated, Play, Parent Zone, music controls | `HomeScreen` (music controls are in Parent settings) | AP | ✅ |
| GDD 7.2 | Five portals, name, letter range, progress | `WorldMapScreen` | AP | ✅ |
| GDD 7.3 | A–Z selection map, always explorable | `AlphabetBookScreen` | AP | ✅ |
| GDD 7.4 / MP 12 | Large targets, no reading, safe areas, debounce, hold-to-exit | `UiFactory`, `BigButton`, `HoldButton`, `SafeAreaFitter`, guarded taps | AP, PM nav guard | ✅ (device a11y test pending) |
| GDD 8 / MP 13 | Voice/Music/SFX/Ambient, ducking, no overlap, offline | `AudioService` (D-012) | PM/AP no errors | ✅ system; 🟡 provisional voice |
| GDD 9.1 | Wonder Stars, never lost | `RewardService` | AP stars | ✅ |
| GDD 9.2 | My Alphabet Book, 26 pages | `AlphabetBookScreen` | AP | ✅ |
| GDD 9.3 | Cosmetic customisation (hats, glasses, dance, decoration) | `Cosmetics`, `Wardrobe`, `RewardService.Equip` | EM, AP | ✅ |
| GDD 10 / MP 15 | Parent gate, dashboard, settings, reset ×2 | `ParentGate`, `ParentZone` | EM gate, AP | ✅ |
| GDD 10.3 / MP 14 | Cautious evidence labels | `ProgressRules` (D-016) | EM, PM | ✅ |
| GDD 11 | Visible completeness register | `ContentReport` → CONTENT_COMPLETENESS.md | setup | ✅ |
| GDD 13 / MP 2 | Unity 6, URP, Android landscape, touch + mouse | ProjectSetup | dev APK builds (61.5 MB) | ✅ build; ⬜ device test |
| GDD 14 / MP 15 | No ads/chat/links/accounts/mic/location; local data | no such code; INTERNET permission stripped | aapt: no permissions besides AndroidX-internal | ✅ (Play policy review pending) |
| GDD 15 | Accessibility: tap alternatives, reduced motion, audio controls | Match tap mode, `Tween.Motion`, settings | AP | ✅ (contrast/colour-alone audit pending) |
| GDD 16.2 | Asset provenance register | ASSET_REGISTER.md | — | ✅ |
| MP 7 | ScriptableObject data model, stable IDs, validation | `Content/*` | EM | ✅ |
| MP 16 | Versioned save, atomic, recovery, migrations | `Persistence/*` (D-004) | EM SaveRepositoryTests | ✅ |
| MP 17 | Android IL2CPP/ARM64, quality presets, profiling | ProjectSetup (D-018), `QualityService` Auto/Low/Med/High (D-019) | build | 🟡 device profiling pending |
| MP 18 | Unit/EditMode/PlayMode tests | `Tests/` | 259 EM + 29 PM | ✅ |
