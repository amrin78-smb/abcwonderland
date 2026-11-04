<!-- Converted from Documentation/Source/ABC_Wonderworld_Master_Development_Prompt.docx. The .docx is the original; this Markdown copy is for tooling. -->


ABC WONDERWORLD


CLAUDE CODE MASTER DEVELOPMENT PROMPT


Complete 3D preschool alphabet adventure • Unity 6 • Android


Target age: 2–5 yearsLanguage: English (initial release)Visual direction: expressive 3D cartoon worldScope: complete A–Z educational game


Prepared as an execution brief for Claude Code

# How to use this document


Give this document to Claude Code as the master implementation prompt. Also attach the ABC Wonderworld Game Design Document (GDD) discussed separately. The GDD defines the intended player experience and content; this document defines how Claude Code should inspect, architect, implement, test and deliver the Unity project.


> Important operating ruleDo not ask Claude Code to generate the entire game in one uncontrolled pass. It must first inspect the workspace, report its findings, establish the project foundation, then implement the game in verifiable milestones. It must not claim a feature is complete unless it exists, builds and has been tested.

## Recommended hand-off


Open Claude Code in the intended project workspace.


Attach or place the GDD in Documentation/GAME_DESIGN_DOCUMENT.md (or provide the Word document and ask Claude Code to read it).


Provide this master prompt.


Allow Claude Code to inspect the environment and Unity installation before making changes.


Approve any destructive changes, package installation, asset downloads, licensing decisions or external service use.


Keep the project under Git version control and review each milestone before proceeding.

# MASTER PROMPT — COPY FROM HERE


> Role and missionYou are the lead Unity technical director, senior gameplay engineer, tools engineer and implementation lead for ABC Wonderworld. Your task is to build a complete, polished, production-oriented 3D preschool alphabet learning game for Android using Unity 6 and C#. You are responsible for converting the attached GDD into a working, testable Unity project—not merely writing a plan or producing mock-ups.

## 1. Source of truth and decision hierarchy


Treat the attached ABC Wonderworld GDD as the primary authority for product vision, age range, game modes, worlds, letter content, characters, parent experience and educational intent.


Treat this master prompt as the authority for engineering process, architecture, code quality, project organisation, testing, delivery and acceptance.


If the GDD and this prompt conflict, preserve the GDD's intended player experience while following this prompt's safety, engineering and delivery rules. Document material conflicts and ask before making an irreversible product decision.


Do not silently omit requirements. Maintain a requirements traceability checklist linking each GDD requirement to its implementation, test and status.


Where information is missing, choose the simplest safe, maintainable solution consistent with a toddler-friendly offline-first game. Record assumptions in Documentation/DECISIONS.md.

## 2. Product definition


Build ABC Wonderworld: a complete, English-language, offline-first 3D educational adventure for children aged 2–5. The player explores five colourful themed worlds, meets 26 memorable anthropomorphic alphabet characters, learns uppercase and lowercase letters, hears letter names and phonics, discovers vocabulary, traces letters, matches objects, solves puzzles and plays short letter-themed adventures.


Engine: Unity 6 (use the installed stable Unity 6 editor version; record exact version).


Rendering: Universal Render Pipeline (URP).


Primary language: C#.


Platform: Android phones and tablets, landscape orientation.


Input: Unity Input System; touch-first with mouse support in Editor for testing.


Connectivity: offline-first; all core content and progression must work without network access.


Target: stable 60 FPS where hardware permits, with scalable quality for mid-range Android devices.


Presentation: polished, expressive, friendly 3D cartoon art; large readable letters, rounded forms, clear silhouettes and uncluttered scenes.


No advertising, chat, social features, external links, child-facing purchases or unnecessary personal-data collection.

## 3. Initial workspace inspection — do this before coding


Before creating, modifying or deleting files:


Inspect the current directory, repository status, existing Unity project, Assets tree, Packages/manifest.json, ProjectSettings, scenes, scripts, prefabs and any supplied art/audio archives.


Determine whether a Unity project already exists and whether the installed Unity Editor and Android Build Support are available. Do not assume a Unity executable path.


Identify existing assets supplied by the user. Preserve them. Do not overwrite or rename user assets without explicit approval.


Inspect any supplied ZIP files and list their contents before importing. Check formats, dimensions, rigging, animation clips, licences and whether assets are complete.


Check Git status and branch. Never discard uncommitted user changes, reset the repository, delete unknown files or perform destructive cleanup without permission.


Report the discovered project state, blockers, available tools, asset inventory, Unity version and proposed implementation plan.


If the workspace is empty, create a new Unity 6 URP project using the installed editor or provide exact manual editor steps if project creation cannot be performed safely from the available environment.


If Unity Editor automation is unavailable, do not pretend scenes or builds were generated. Create source files and editor tooling where possible, then clearly state which Unity Editor actions require the user.

## 4. Required project structure


ABC-Wonderworld/


├── CLAUDE.md


├── README.md


├── Documentation/


│   ├── GAME_DESIGN_DOCUMENT.md


│   ├── TECHNICAL_ARCHITECTURE.md


│   ├── ART_AND_ASSET_GUIDELINES.md


│   ├── AUDIO_AND_VOICE_GUIDELINES.md


│   ├── GAMEPLAY_SPECIFICATIONS.md


│   ├── PARENT_DASHBOARD_SPEC.md


│   ├── DEVELOPMENT_ROADMAP.md


│   ├── REQUIREMENTS_TRACEABILITY.md


│   ├── DECISIONS.md


│   ├── BUILD_AND_RELEASE.md


│   └── QA_TEST_PLAN.md


├── UnityProject/


│   ├── Assets/


│   │   ├── ABCWonderworld/


│   │   │   ├── Art/Characters/


│   │   │   ├── Art/Environments/


│   │   │   ├── Art/Props/


│   │   │   ├── Art/UI/


│   │   │   ├── Audio/Music/


│   │   │   ├── Audio/Voice/


│   │   │   ├── Audio/SFX/


│   │   │   ├── Animations/


│   │   │   ├── Materials/


│   │   │   ├── Prefabs/


│   │   │   ├── Scenes/


│   │   │   ├── Scripts/


│   │   │   ├── ScriptableObjects/


│   │   │   ├── Settings/


│   │   │   ├── UI/


│   │   │   └── Tests/


│   │   └── ...


│   ├── Packages/


│   └── ProjectSettings/


├── Builds/


├── Tools/


└── .gitignore


Adapt this structure to an existing project where appropriate. Avoid creating a second nested Unity project if the workspace already contains a valid Unity project.

## 5. Engineering principles


Use clear, maintainable, strongly typed C#; avoid giant manager classes and unnecessary singletons.


Prefer composition, interfaces and data-driven configuration over hard-coded letter-specific logic.


Separate presentation, input, game rules, content data, persistence and analytics/progress recording.


Use ScriptableObjects for authorable static content such as letters, worlds, vocabulary, activities, characters and rewards.


Use dependency injection or explicit bootstrap composition where practical. Avoid hidden execution-order dependencies.


Use events or well-defined interfaces for system communication. Unsubscribe from events correctly.


Use object pooling for frequently spawned effects, collectibles and repeated objects where profiling justifies it.


Avoid allocations and expensive scene-wide searches in per-frame Update methods.


Use asynchronous scene/content loading responsibly; show a friendly loading state and prevent duplicate input.


All gameplay must remain usable with missing optional audio or cosmetic assets; log a clear content validation error rather than crashing.


Do not add packages merely because they are popular. Check compatibility with the installed Unity 6 version, Android support, licence and maintenance status before adding dependencies.


Do not introduce cloud services, analytics SDKs, accounts, ads, purchases or network requirements in the initial release.

## 6. Core architecture and responsibilities


| System | Responsibility |
|---|---|
| ApplicationBootstrapper | Initialises configuration, services, save data, audio and initial scene flow. Handles first-run setup and migration. |
| GameFlowCoordinator | Owns high-level navigation and state transitions. Prevents invalid transitions and duplicate scene loads. |
| SceneLoader | Loads/unloads scenes asynchronously, displays loading UI and handles failures gracefully. |
| WorldCatalog / WorldService | Reads world definitions, unlock/availability state and world selection. |
| AlphabetCatalog / LetterService | Provides validated A–Z definitions, current selection, case mode and learning content. |
| CharacterPresenter | Binds character data to character prefab, animation controller, voice and interaction points. |
| ActivityCoordinator | Starts, pauses, resumes, completes and records activities using a common activity lifecycle. |
| TouchInteractionService | Provides tap, drag, drop and gesture handling with touch and Editor mouse parity. |
| AudioService | Manages voice, music, SFX, mixer groups, ducking, interruption and user volume settings. |
| ProgressService | Records exposure, practice, recognition evidence and activity completion separately. |
| RewardService | Awards deterministic cosmetic rewards and maintains collection state. |
| SaveRepository | Versioned local persistence, validation, atomic writes, backup/recovery and migrations. |
| ParentZoneService | Parental gate, parent settings, reports and explicit reset confirmation. |
| SessionService | Tracks active play session duration locally and supports gentle optional break reminders. |
| ContentValidator | Editor-time and runtime validation of missing data, invalid references and incomplete content. |
| AccessibilitySettings | Reduced motion, audio levels, simplified interaction and other accessible options. |

## 7. Data model and content authoring


Create serializable, validated data models and ScriptableObject authoring assets. Use stable IDs, not display names, as persistent keys.


LetterDefinition: stable ID, uppercase glyph, lowercase glyph, spoken name, phoneme display, phoneme audio reference, character ID, world ID, vocabulary entries, tracing definitions, activity references and artwork references.


VocabularyEntry: stable ID, word, image/model reference, spoken audio, initial phoneme association, optional object interaction and validation status.


CharacterDefinition: stable ID, display name, letter ID, prefab, portrait, palette/material references, animator controller, voice set, idle behaviour and interaction profile.


WorldDefinition: stable ID, name, letter range/list, scene reference, portal artwork, ambient audio, environment configuration and activities.


ActivityDefinition: stable ID, activity type, eligible letters, difficulty bands, instructions, success criteria, hint strategy and reward configuration.


TracingDefinition: glyph/case, ordered strokes, path points or curve representation, tolerance settings, start markers and completion rules.


RewardDefinition: stable ID, reward type, cosmetic asset, unlock condition and display metadata.


All references must be validated. Missing required content must be surfaced in a validation report.

## 8. Alphabet content — A to Z


Implement the complete 26-letter content catalogue from the GDD. Use these approved initial associations unless the supplied GDD contains a more specific finalised mapping:


| Letter | Character / theme | Vocabulary examples |
|---|---|---|
| A | Apple | apple, ant, alligator |
| B | Bear | ball, bear, banana |
| C | Cat | cat, cake, car |
| D | Dinosaur | dog, dinosaur, drum |
| E | Elephant | egg, elephant, envelope |
| F | Fish | fish, flower, frog |
| G | Giraffe | giraffe, goat, grapes |
| H | House | hat, house, horse |
| I | Ice cream | ice cream, igloo, insect |
| J | Jellyfish | jam, juice, jellyfish |
| K | Kite | kite, key, kangaroo |
| L | Lion | lion, leaf, lamp |
| M | Monkey | monkey, moon, milk |
| N | Nest | nest, nose, net |
| O | Octopus | octopus, orange, owl |
| P | Penguin | penguin, pig, pizza |
| Q | Queen | queen, quilt, question mark |
| R | Rabbit | rabbit, rainbow, robot |
| S | Sun | sun, snake, sock |
| T | Turtle | turtle, train, tree |
| U | Umbrella | umbrella, up, unicorn |
| V | Volcano | volcano, van, violin |
| W | Whale | whale, watermelon, window |
| X | Xylophone | xylophone, x-ray, fox (letter x sound/context must be explained carefully) |
| Y | Yo-yo | yo-yo, yellow, yak |
| Z | Zebra | zebra, zipper, zoo |


Educational content rule: phonics must be linguistically accurate and age-appropriate. Do not teach the letter name as if it were the letter sound. Store letter name and phoneme separately. Some letters have multiple common sounds; introduce one clearly selected beginner sound first. X needs special handling because common initial examples are limited; explain the sound in an age-appropriate way and do not imply that every example begins with the same sound. Have the content reviewed by a qualified early-literacy/phonics reviewer before release.

## 9. Gameplay implementation requirements

### Meet the Letters


Build a reusable letter introduction activity driven by LetterDefinition.


Present uppercase and lowercase forms, character introduction, spoken letter name, primary phoneme and at least three vocabulary examples.


Allow replay of each audio item; tapping objects triggers a clear animation and spoken label.


Keep instructions short, sequential and voice-led. Never play several instruction clips over one another.

### Letter Tracing Studio


Implement touch drawing on a dedicated canvas or world-space surface with reliable screen-to-canvas conversion.


Represent strokes as ordered paths; support multiple strokes, start points, direction guides and configurable tolerance.


Track progress along the expected path rather than requiring pixel-perfect drawing.


Offer guided, lightly guided and free-trace modes. Provide a forgiving completion threshold and positive feedback.


Support A–Z uppercase and lowercase. Validate that every tracing asset has a usable path.

### Find the Letter


Generate a target prompt using voice and visual target glyph.


Provide configurable choice counts (two, three/four, five/six) and randomised positions.


Use large targets, generous hit areas and no time limit.


After repeated incorrect attempts, provide progressively stronger hints and optionally demonstrate the correct choice.


Record attempts and recognition evidence without labelling a child as failed.

### Alphabet Adventures


Create a reusable mini-game framework and distinct short activities for all 26 letters, as specified by the GDD.


Activities should be completable in approximately 1–3 minutes but must not force a timer.


Prefer tap, drag, swipe and very simple guided movement. Avoid precision platforming, complex virtual joysticks or fast reactions.


Each mini-game needs clear onboarding, pause/replay, completion feedback and an activity record.

### Match the Objects


Support tap-to-match as well as drag-and-drop so younger players are not blocked by fine motor demands.


Implement letter-to-object, uppercase-to-lowercase, letter-to-vocabulary and initial-sound matching where pedagogically appropriate.


Use generous snapping, clear feedback and no punitive failure state.

### Music and Rhymes


Provide a complete ABC sing-along experience and interactive music area.


Use original or properly licensed music and voice recordings only.


Highlight letters in sync with the song where timing data is available.


Provide pause, replay and independent music/voice volume settings.

### Alphabet Puzzles


Support letter-shape completion, simple jigsaws, missing-letter and sequence activities.


Scale piece count and assistance by profile/difficulty.


Support large pieces, snap zones, tap-to-place alternatives and easy reset.

### Free Play Playground


Provide one explorable environment per world, accessible without completing lessons.


Every prominent tappable object must have a meaningful interaction or be clearly decorative.


Include ambient character behaviours, optional discoveries and safe, non-competitive play.


Ensure free play never blocks the child behind a reading task, timer or progress requirement.

## 10. World and scene construction


Happy Meadow (A–E): Sunny meadow, rolling hills, flowers, trees, river, bridges and friendly animals.


Bubble Bay (F–J): Underwater-inspired world, coral, bubbles, friendly fish and submarine play area.


Candy Kingdom (K–O): Candy-coloured architecture, gumdrop trees, chocolate hills and rainbow bridges.


Dino Jungle (P–T): Tropical foliage, waterfalls, footprints, ruins and friendly dinosaurs.


Rainbow Mountain (U–Z): Floating islands, mountains, clouds, rainbow paths and gentle space-inspired discoveries.


Build environments from modular reusable prefabs. Keep scene hierarchy organised and use additive loading or addressable content where it materially improves memory and load times. Ensure a child can move between worlds without losing progress. World access should not be used to punish or frustrate the player.

## 11. Character and animation pipeline


All 26 alphabet characters must share a consistent art direction, scale conventions, pivot conventions and interaction contract.


Characters should be shaped recognisably around the letter glyph where practical, with a readable uppercase silhouette as the primary identity.


Each character prefab must expose consistent interaction points and an animation interface.


Support at minimum idle, blink/face movement, greet/wave, happy, celebrate, surprised and interact states. Add walk/run/jump/dance only where relevant to the character and activity.


Use Animator Controllers or a clearly justified alternative. Avoid fragile string-based animation calls scattered across gameplay scripts; centralise state hashes/parameters.


Provide fallback idle and feedback behaviour when a model has no custom clip.


Do not create 26 copies of the same character and represent them as finished original characters. Temporary placeholders must be clearly labelled and tracked.

## 12. UI/UX and toddler interaction


Landscape-first responsive layout for common Android phone and tablet aspect ratios, including safe areas and notches.


Use large, well-spaced controls, clear iconography and minimal on-screen text.


Provide voice-led instructions and visual demonstrations. Do not make reading a prerequisite.


Avoid small close buttons, dense grids, hidden gestures, long menus and multi-step forms in child-facing areas.


Use generous hitboxes, touch slop and forgiving drag/drop thresholds.


Debounce taps where necessary and prevent double-triggered actions or accidental scene transitions.


Provide immediate visual/audio feedback without excessive screen shake, flashes or loud effects.


Allow repeat, pause and resume. Do not use countdown timers in core toddler learning activities.


Keep navigation predictable; include a clear home/back affordance that is not easy to trigger accidentally during tracing or dragging.


Use consistent typography and strong contrast; ensure letter glyphs remain readable against backgrounds.

## 13. Audio system and content


Create an AudioService with separate Voice, Music, SFX and Ambient mixer groups.


Support independent volume controls, mute, music toggle, voice priority and audio ducking.


Queue or sequence spoken instructions; never overlap important voice clips unintentionally.


Use audio assets that are local and available offline. Use appropriate compression and import settings for mobile.


Create a content manifest for every required voice clip: letter name, phoneme, vocabulary, instruction, feedback and character introduction.


If a recording is not supplied, create a clearly named placeholder audio event and mark it incomplete. Do not silently substitute robotic or inaccurate speech and call it final.


Use General American English consistently for the first release unless the GDD specifies otherwise.


Check phoneme articulation and pronunciation with an early-literacy reviewer before release.

## 14. Learning evidence and progress model


Do not equate completing a screen with mastering a letter. Record distinct evidence categories and present them honestly to parents.


Exposure: child has encountered/heard/seen a letter.


Practice: child has participated in one or more activities involving the letter.


Recognition evidence: child has correctly selected or matched the letter across multiple attempts or sessions.


Tracing practice: child has attempted/completed tracing; do not claim handwriting mastery.


Vocabulary exposure: child has heard or interacted with associated vocabulary.


Store timestamp, activity type, letter ID, attempt count, outcome and assistance level as appropriate, using local storage only.


Use conservative labels such as Introduced, Practised and Showing Recognition. Do not diagnose learning ability or make developmental claims.


Adaptive difficulty should use transparent, simple rules and must be adjustable/disableable by parents.

## 15. Parent Zone and child safety


Implement an adult-oriented parental gate before settings, reset, purchases (none in initial release) or external actions. Use a hold interaction plus an age-appropriate adult challenge; do not rely on a single trivial arithmetic question.


Parent dashboard should show letters introduced, practised and showing recognition; uppercase/lowercase activity; tracing practice; activity history; approximate session time and favourite modes.


Settings include audio/music volume, music toggle, difficulty/profile, reduced motion, session reminder, and progress reset.


Progress reset requires a second explicit confirmation and clear explanation of what will be erased.


No third-party advertising, behavioural advertising, chat, social sharing, external web links, location, contacts or unnecessary permissions.


Do not implement microphone recording or speech recognition in the initial release. If proposed later, require a separate privacy and consent design.


Keep progress local. Do not add cloud accounts or telemetry without explicit product approval and a child-privacy review.


Use gentle optional break reminders; never abruptly interrupt a child or use guilt-based language.

## 16. Save data and resilience


Define a versioned save schema and a repository interface so JSON or SQLite can be changed without altering gameplay systems.


For the initial release, prefer a robust local JSON save unless structured query needs justify SQLite. Document the choice.


Use atomic writes or a safe temporary-file/replace strategy where supported; validate data on load and recover from corrupt or partial files.


Persist settings, profile, world/letter activity evidence, rewards and collection state.


Provide schema versioning and migrations. Never silently discard an old save when the schema changes.


Test first launch, normal save/load, application termination during play, low-storage/write failure handling and corrupted save recovery.


Do not store sensitive personal data. Avoid collecting child names; use a local generic profile by default.

## 17. Android build and performance


Configure Android as the primary build target and landscape orientation.


Set a unique application identifier only after confirming the desired publisher/package namespace with the user; use a clearly marked development identifier until then.


Use IL2CPP/ARM64 for production where appropriate and validate current Unity 6 Android requirements.


Do not hard-code signing credentials or store keystores/passwords in Git. Use a secure user-controlled release-signing process.


Set sensible texture sizes, compression, mesh budgets, audio compression and shader variants for mobile.


Use baked lighting for static environments where suitable; limit real-time shadows, transparent overdraw and expensive post-processing.


Pool repeated collectibles, particles and transient UI elements where profiling supports it.


Profile on a real Android device, not only in the Editor. Record device model, OS, resolution, frame rate, memory and load times.


Provide Low/Medium/High quality presets, but make sure Low remains visually clear and playable.


Do not claim a performance target has been met without measured device results.

## 18. Testing strategy

### Automated tests


Unit tests for alphabet catalogue completeness, stable IDs, data validation, progress calculations, reward rules and save migrations.


EditMode tests for ScriptableObject validation and content references.


PlayMode tests for scene bootstrap, navigation, activity lifecycle, input interactions, audio settings and save/load.


Tests for all 26 letters and both cases where the feature applies.


Tests for missing audio/art references and invalid world/letter mappings.

### Manual QA


Test touch on real Android phones and tablets, including different aspect ratios and safe areas.


Test a toddler-like interaction pattern: repeated taps, missed taps, accidental drags, rapid taps, pauses and leaving/re-entering activities.


Verify that every visible control works and every interaction produces appropriate feedback.


Verify offline launch and all core learning activities with airplane mode enabled.


Verify save persistence after force-closing and relaunching.


Check volume, voice priority, pause/resume, audio interruptions and mute behaviour.


Check that parental controls cannot be entered accidentally from child gameplay.


Run a full A–Z content audit and verify all vocabulary and phonics.

## 19. Definition of done


> A feature is not complete merely because code exists.It is complete only when it is implemented, connected to the actual game flow, usable on the target platform, tested, documented and free of known blocking defects.


No placeholder buttons or non-functional menu items.


No unhandled exceptions or known critical crashes in the tested path.


No missing required A–Z content references.


All essential gameplay works offline.


All save/progress behaviours pass their tests.


All child-facing interactions meet toddler UX requirements.


All required scenes and prefabs are included in the build.


Android build completes successfully and is smoke-tested on a physical device.


Known limitations and incomplete assets are listed honestly in the milestone report.

## 20. Milestone execution plan

### M0 — Discovery and project foundation


Inspect workspace, Unity version, assets and repository. Create/validate Unity 6 URP project, folder structure, packages, coding conventions, bootstrap, documentation and CI/build notes. Deliver a minimal scene that launches.

### M1 — Vertical slice: Happy Meadow A–E


Create the polished main menu, world selection, one playable meadow, five character slots/characters, letter introduction flow, vocabulary interaction, voice playback, basic tracing, Find the Letter, matching, one mini-adventure per letter, local save and basic parent gate. Use final-quality art for at least one representative character and environment section to validate art direction.

### M2 — Reusable complete alphabet pipeline


Complete data-driven A–Z catalogue, uppercase/lowercase data, vocabulary, character prefab pipeline, validation tools and all letter introduction content. Import/create all approved character assets or clearly track missing art/voice deliverables.

### M3 — Educational activity suite


Complete tracing, Find the Letter, matching, puzzles, activity lifecycle, adaptive difficulty and learning evidence model across A–Z.

### M4 — Five worlds and adventures


Complete all five environments, world navigation, 26 letter adventures and free-play interactions. Ensure modularity and mobile performance.

### M5 — Audio, rewards and collection


Complete original/cleared music and voice assets, audio polish, Wonder Stars, stickers, collection book and cosmetic customisation.

### M6 — Parent experience


Complete dashboard, learning summaries, settings, optional reminders, parental gate, reset flow and accessibility controls.

### M7 — Production hardening and release


Complete QA, device profiling, optimisation, offline validation, privacy/permissions review, release documentation, Android APK/AAB generation and final acceptance report.

## 21. Required milestone reporting


At the end of every milestone, produce a concise report containing:


What was implemented, with links/paths to key scenes, prefabs, scripts and data assets.


What was tested and exact test/build results.


Unity version, target platform and build identifier if applicable.


Screenshots or short recordings of actual running features where available.


Known defects, missing assets, temporary placeholders and technical debt.


Requirements traceability updates.


Next milestone tasks and any decisions required from the user.

## 22. Claude Code working protocol


Work in small, coherent increments. Before a large implementation, outline the files and systems to be changed.


Do not stop after generating a plan. Once discovery is complete and no approval is needed, begin implementing the first safe milestone.


Do not attempt to implement every milestone in a single response or uncontrolled batch. Finish and verify a coherent vertical slice first.


After each meaningful change, run available static checks, Unity batch compilation/tests or other suitable validation. If Unity Editor is unavailable, state this limitation and provide exact validation steps.


Never fabricate successful compilation, tests, screenshots, APKs, asset imports or runtime behaviour.


Never delete user assets, change project identity, alter signing configuration, install paid assets or use external services without approval.


Use original assets or assets with clearly verified licences. Do not copy characters, logos, music or art from copyrighted franchises.


When generating placeholder art procedurally, label it as placeholder and ensure it can be replaced without rewriting gameplay code.


Keep code and documentation synchronised. Update README, architecture, decisions, roadmap and traceability as the implementation evolves.


If blocked by a missing asset or unavailable Unity Editor, continue with independent work that is safe and useful; list exactly what remains blocked.


Ask the user only for decisions that materially affect safety, cost, licensing, irreversible changes or product direction. Make routine engineering decisions and document them.

## 23. First response and immediate actions


Your first response after receiving this prompt must:


Confirm that you have read this prompt and the attached GDD, or explicitly state which document is missing/unreadable.


Summarise the current workspace and Unity project state based on inspection—not assumptions.


List existing assets and identify which are usable, incomplete, placeholders or licence-unknown.


Identify blockers and any approvals required before proceeding.


Present the proposed M0/M1 task sequence and expected deliverables.


Then begin M0 discovery/foundation work where safe. Do not start by writing a large amount of disconnected gameplay code.

## 24. Final instruction


> Build the real game.The goal is a coherent, polished, playable Unity 6 Android game—not a collection of concept screens, mock-ups, disconnected scripts or a design-only prototype. Prioritise a high-quality vertical slice, reusable systems, accurate early-learning content, toddler-safe interactions and verifiable progress. Be precise about what is genuinely implemented and what still requires art, audio, Unity Editor work or QA.

# APPENDIX A — Suggested initial CLAUDE.md


After inspecting the project, create a concise CLAUDE.md in the repository root containing the durable project rules below. Keep it accurate to the actual project and update it when architecture changes.


# ABC Wonderworld — Claude Code Project Rules


- Read Documentation/GAME_DESIGN_DOCUMENT.md and the technical documents before implementing features.


- Unity 6, URP, C#, Android-first, landscape, touch-first, offline-first.


- Follow the architecture and coding conventions in Documentation/TECHNICAL_ARCHITECTURE.md.


- Use data-driven ScriptableObjects for letters, worlds, characters, vocabulary and activities.


- Do not hardcode letter-specific behaviour into shared gameplay systems.


- No ads, chat, social features, child-facing purchases, external links or unnecessary data collection.


- Never claim a feature/build/test is complete unless it has been verified.


- Do not delete or overwrite user assets or uncommitted work without permission.


- Run relevant tests after changes and report exact results.


- Keep Documentation/REQUIREMENTS_TRACEABILITY.md and DECISIONS.md current.


- Use only original or properly licensed assets; record asset provenance/licence.


- Prefer small, testable vertical slices over large unverified changes.

# APPENDIX B — Important production caveats


A comprehensive game of this scope requires real art, rigging, animation, voice recording, sound design, device testing and iterative polish. Claude Code can implement systems and assist with content pipelines, but it cannot make unprovided production assets magically exist.


A complete A–Z release should not be declared content-complete while characters, voice recordings, tracing paths, activity data or environment assets are placeholders.


The phonics examples in the GDD are starting content, not a substitute for expert review. Validate pronunciation, vocabulary suitability and the treatment of irregular letters (especially C, G, Q and X).


Use the current Unity 6 documentation and current Android publishing requirements when configuring packages, target API, signing and release builds. Do not rely on stale tutorials or hard-coded version assumptions.


Before public release, perform a child-directed app privacy and platform-policy review appropriate to the markets in which the game will be distributed.
