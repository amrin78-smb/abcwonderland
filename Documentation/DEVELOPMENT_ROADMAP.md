# Development Roadmap

| Milestone | Status | Notes |
|---|---|---|
| **M0 Discovery & foundation** | ✅ Done (2026-11-04) | Unity 6 URP project, structure, asmdefs, bootstrap, batch tools, docs, tests, CI-style scripts. |
| **M1 Vertical slice: Happy Meadow A–E** | 🟡 Playable, placeholder art/voice | Menu, world map, Happy Meadow free play, 5 characters, Meet / Trace / Find / Match / Adventure for A–E, local save, parent gate and dashboard, Alphabet Book. Missing for M1 exit: final-quality art for one representative character and one environment section (needs an artist or asset decision); physical-device smoke test. |
| M2 Complete alphabet pipeline | 🟡 Largely done early | All 26 letters' data, tracing, characters, vocabulary, voice placeholders and validation are generated. Pending: real art and voice deliverables. |
| M3 Educational activity suite | ✅ Functionally complete | Tracing, Find, Match, Puzzles (jigsaw + missing letter), adaptive difficulty and evidence for all letters. Pending: phonics/stroke review. |
| M4 Five worlds & adventures | 🟡 Functionally complete | Five themed worlds with free play; all 12 archetypes, 26 adventures verified. Pending: final environment art. |
| M5 Audio, rewards, collection | 🟡 Functionally complete | Stars, stickers, Alphabet Book, wardrobe (hats/glasses/dance/rainbow), ABC sing-along with synced highlighting, music area. Pending: recorded voice and song. |
| M6 Parent experience | 🟡 Mostly done | Gate, dashboard, settings, reminders, reset done. Pending: per-letter detail, a11y review. |
| M7 Hardening & release | 🟡 Started | Quality presets, back-button policy, no network permission. Pending: device profiling (Redmi + S25 Ultra test 2026-11-04 evening), Play Families review, signing, AAB. |

## Next steps (proposed)
1. Owner device test tonight (Redmi + Galaxy S25 Ultra) using `DEVICE_TEST_CHECKLIST.md`; fix findings.
2. Decisions: art production route, voice actor + sung ABC recording, phonics reviewer, release package ID.
3. Remaining GDD gaps: initial-sound and identical-letter matching variants, picture-completion puzzle, per-letter parent drill-down.
