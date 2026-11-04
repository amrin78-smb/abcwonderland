# Gameplay Specifications (as implemented)

General rules for every child-facing activity: voice-led instruction (replay with the speaker button), no timers, no lives, no fail screens, unlimited retries. Exit is a 0.8 s press-and-hold (ring fills). Completion shows a short celebration (stars, any new sticker), then large Again / Next / Back choices. Reduced motion scales every animation.

## Meet the Letters (`MeetLetterActivity`)
Sequence: character intro ("Hi! I'm Bouncy Bear!") → "This is the letter B" → "Big B" (upper highlights) → "Small b" (lower highlights) → "B says /b/ like ball" (name, "says", phoneme clip, "like", example word). The example is the first word with `startsWithBeginnerSound`. For X: "X says /ks/ like at the end of fox". Then three vocabulary cards pop in ("Tap the pictures…"). Tapping the upper or lower glyph replays "big/small + name". The speaker plays the sound sentence. Completion: all cards tapped, or the Next arrow (shown after the intro). Records: exposure, vocabulary heard.

## Letter Tracing Studio (`TracingActivity`, `TracingEvaluator`)
Upper case, then lower case. Mode by level: 0 Guided (thick path, green start dot, direction arrow, demo hand at start and after 6 s idle), 1 Lightly guided (fainter path), 2 Free (outline only; may start anywhere in the first quarter of a stroke). Tolerance 0.16 cap-heights (×1.25 for the Explorer profile, ×1.25 with simplified interaction). Progress moves only forward within a 0.3-unit look-ahead window, so a finger cannot skip around an O. Lifting keeps progress. A stroke completes at the end of the path or on lift at ≥85 %. Dots complete on a nearby tap. Off-path → throttled "Keep going!". Starting in the wrong place → "Start at the green dot" plus the dot pulses. Records tracing attempt/completion per case (assisted if a demo was shown).

## Find the Letter (`FindLetterActivity`, `ChoiceGenerator`)
3 rounds. Choices: level 0 → 2, level 1 → 3–4, level 2 → 5–6 (GDD §6.3). Below level 2, visually confusable letters are excluded (b/d/p/q, B/D/P/R, M/W/N, E/F, O/Q/C/G…). Distractors prefer letters from the current world. Positions are randomised. Case: level 0 uses the requested case; then rounds alternate; level 2 ends on lower case. The target glyph is shown in a bubble (tap to replay). Wrong choices escalate: 1 wrong → wiggle + its name + "try another"; 2 → target glows; 3 → one distractor floats away; 4 → pointing hand + "Look! Here it is!". Records recognition (unassisted only if first try with no hint) and an adaptive round.

## Match the Objects (`MatchActivity`)
Round 1: picture → letter (each letter's first initial-sound word). Round 2: upper → lower. 2–4 pairs by level (2 with simplified interaction). Drag and drop, with a generous snap (1.3 card widths), or tap a piece then tap a target. A mismatch floats back with a soft sound. Records practice and recognition for the focus letter.

## Alphabet Adventures (`AdventureActivity`)
Archetypes are configured by `AdventureDefinition` (archetype, prop, count, instruction).
- **TapCollect** (A apples, M, R): tap items, which arc into a basket labelled with the letter; rising notes.
- **PopTarget** (B balloons, G, J, S, W): slowly floating balloons/bubbles carrying letters. Popping the target counts. Others wiggle and say their name (no penalty). Items loop instead of being missed.
- **FindHidden** (C bushes, D eggs, O, V): tap hiding places to reveal the letter's vocabulary (word spoken).
- **Fill** (E): tap the water drop to fill Ellie's bucket.
- **DragToTarget** (F fish → bubbles, N eggs → nest): drag, or tap a piece to send it home.
- **PlaceParts** (H roof/door/window into silhouettes, L mane around Leo's face).
- **MatchPairs** (I scoops → cones, Q crowns → cushions): colour + shape mark; tap auto-matches for Explorers, select-then-tap from Discoverer.
- **DragCollect** (K kite catches drifting stars, U umbrella catches slow looping rain): drag, or tap where the catcher should glide.
- **PathSteps** (P): tap the glowing ice tile; Poppy slides; P words pop up along the way.
- **Pattern** (T): AB colour pattern on the shell — Explorers tap the glowing spot, older profiles pick the right colour; (Z): tap stripes to paint them.
- **Music** (X): every bar plays a note; following the glowing bar plays a short tune.
- **Swipe** (Y): swipe down OR tap the yo-yo; Y words appear.

## Free Play (`WorldScreen` + `EnvironmentBuilder`)
Each world is explorable with no prerequisites. Characters greet when tapped and open a 5-mode picker for that letter. Scenery (trees, flowers, coral, lollipops, crystals, critters) reacts with wobble, grow, spin, hop, fly-away, pop or reveal (shows a hidden word picture).

## Learning evidence (see D-016, D-017)
Introduced = any exposure. Practised = took part in a practice activity. Showing Recognition = ≥3 unassisted first-try correct selections across ≥2 sessions, with unassisted ≥ incorrect. Adaptive levels: profile base; +1 after 4 easy rounds; −1 after 2 struggling rounds; parents can disable.

## Alphabet Puzzles (`PuzzleActivity`)
Round 1: the letter (lower case at Little Learner level) is cut into 2/3/4 pieces (by level; 2 with simplified interaction) that snap into a faint outline — drag or tap. Round 2: missing letter in a 3–4 letter alphabet sequence with 2–3 non-confusable choices; second wrong choice makes the answer glow. Records practice + recognition.

## Music and Rhymes (`MusicScreen`)
ABC sing-along (play/pause/replay), all 26 letters highlight in sync; the closing lines play melody only. Free play: 8 bells and a drum; the cast dances. Finishing the song awards a star.

## Wardrobe (`Wardrobe`, from the Alphabet Book)
Earned items toggle on/off (one hat and one pair of glasses at a time); locked items show the star total still needed. Equipped items appear on every character; the dance makes characters dance when tapped; the rainbow decorates Home.
