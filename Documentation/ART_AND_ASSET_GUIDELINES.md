# Art and Asset Guidelines

- Style: premium, soft 3D cartoon; rounded forms; saturated but calm palette per world; clear silhouettes; nothing frightening.
- Characters: the uppercase letter is the body and dominant silhouette. Face on the upper-middle of the glyph (big eyes with pupils and shine, smile, cheeks), feet at the lowest points, theme badge beside the head (~0.38 cap-heights). Shared scale: cap height = 1 unit (shown at 1.6–2.1×). Pivot at the baseline centre. Prefab contract: `LetterCharacter` (anchors: face, above-head, hands, feet), an `ICharacterAnimator` implementation, and a collider.
- Replacing a placeholder: build a prefab honouring that contract, assign it to `CharacterDefinition.prefab`, and set `artStatus` to Final. No gameplay changes are needed. Rigged models should implement `ICharacterAnimator` with an Animator Controller (D-008).
- Vocabulary pictures: square sprites, transparent background, ≥256 px, centred subject. Referenced by key in `alphabet.json` (`emoji` field: Noto code or `draw:name`). Replace by dropping a sprite into `Resources/Pictures/` with the same name, or point the key at a new name.
- Environments: modular; keep the central stage (|x| < 5 m, z < 4 m) clear for characters and activities; the foreground must stay readable behind UI.
- Mobile budgets (targets): character ≤ 8k tris, world scene ≤ 150k tris visible, textures ≤ 1024 (UI/pictures 256–512), one realtime directional light.
- Every asset needs an `ASSET_REGISTER.md` row (source, licence, status).
