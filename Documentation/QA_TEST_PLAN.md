# QA Test Plan

## Automated (run before every commit)
| Suite | Command | Covers |
|---|---|---|
| EditMode (250) | `Tools/run_tests.sh EditMode` | 52 letter forms: stroke validity, metrics, tracing completes with wobble (guided + free), O cannot be skipped, scribble does not complete, lift and continue, dots, off-path; recognition/progress rules; adaptive difficulty; GDD choice counts; confusable exclusion; parent gate; session reminder; reset; save round-trip, backup, corruption recovery, interrupted write, write failure, future version, v0 migration, sanitising; generated catalogue (26 letters, ≥78 words, worlds A–E…U–Z, irregular-sound flags, adventure archetypes, voice clips, missing-reference reporting). |
| PlayMode (6) | `Tools/run_tests.sh PlayMode` | Real Activity scene: Meet, Tracing (both cases), Find (honest recognition), Match, Adventures A–E complete and record evidence; navigation guard during transitions; settings persist to disk. |
| AutoPilot | `Tools/autopilot.sh` | Full flow in a desktop player: Home, Parent gate and dashboard, world map, Happy Meadow, all five modes for A, adventures B–E, other four worlds, Alphabet Book, save round-trip, zero logged errors. Screenshots in `Logs/AutoPilot/`. |
| Content | `Tools/setup.sh` | Validator (errors fail the run), completeness register. |

## Manual (device) — not yet executed
- [ ] Install the dev APK on a physical phone and tablet; record model, OS, resolution, FPS, memory, load times.
- [ ] Toddler patterns: rapid repeated taps, palm on screen, accidental drags, leaving mid-activity, pause/resume via the home button.
- [ ] Every visible control responds; the exit hold cannot be triggered by tracing near the corner.
- [ ] Airplane mode: launch and play every mode.
- [ ] Force-close during an activity → relaunch → progress intact.
- [ ] Volume sliders, music toggle, voice ducking, interruption by a phone call/notification.
- [ ] Parent Zone cannot be entered by a child tapping or briefly holding the lock.
- [ ] Safe areas: notch and punch-hole devices, 4:3 tablets, 21:9 phones.
- [ ] Full A–Z content audit with the phonics reviewer.
