using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ABCWonderworld.Content;
using ABCWonderworld.UI;
using UnityEditor;
using UnityEngine;
using E = ABCWonderworld.EditorTools.EditorAssetUtil;

namespace ABCWonderworld.EditorTools
{
    /// <summary>
    /// Converts Content/alphabet.json and Content/voice_manifest.json into ScriptableObject content
    /// (letters, vocabulary, tracing, adventures, characters, worlds, rewards, audio library, catalog).
    /// Re-runnable: assets are updated in place and keep their GUIDs.
    /// </summary>
    public static class ContentBuilder
    {
        public const string SourcePath = E.Root + "/Content/alphabet.json";
        public const string VoiceManifestPath = E.Root + "/Content/voice_manifest.json";
        public const string VoiceDir = E.Root + "/Audio/Voice/en";
        private const string SO = E.Root + "/ScriptableObjects";

        // ------------------------------------------------------------------ JSON DTOs
        [Serializable] private class Root { public string reviewStatus; public WorldDto[] worlds; public LetterDto[] letters; }
        [Serializable] private class WorldDto { public string id, name, letters, sky, ground, accent, description; }
        [Serializable] private class CharacterDto { public string name, personality, theme, color, accent; }
        [Serializable] private class VocabDto { public string word, emoji, soundNote, artNote, soundPosition; public bool startsWithSound; }
        [Serializable] private class AdventureDto { public string title, archetype, prop, instruction; public int count; }
        [Serializable] private class LetterDto
        {
            public string upper, lower, name, nameSpoken, phoneme, phonemeIpa, soundHint, soundNote;
            public CharacterDto character; public VocabDto[] vocabulary; public AdventureDto adventure;
        }
        [Serializable] private class VoiceManifest { public VoiceLine[] lines; }
        [Serializable] private class VoiceLine { public string key, text, kind; }

        public static ContentCatalog Build(Dictionary<char, GameObject> characterPrefabs = null)
        {
            var root = JsonUtility.FromJson<Root>(File.ReadAllText(SourcePath));
            if (root?.letters == null || root.worlds == null) throw new InvalidDataException("alphabet.json could not be parsed");

            ContentCatalog catalog = E.LoadOrCreateSO<ContentCatalog>(E.Root + "/Resources/ContentCatalog.asset");
            catalog.letters.Clear();
            catalog.characters.Clear();
            catalog.worlds.Clear();
            catalog.audio = BuildAudioLibrary();
            catalog.uiFont = AssetDatabase.LoadAssetAtPath<Font>(ArtPipeline.FontPath);

            var worldOfLetter = new Dictionary<char, string>();
            int order = 0;
            foreach (WorldDto w in root.worlds)
            {
                var wd = E.LoadOrCreateSO<WorldDefinition>($"{SO}/Worlds/{w.id}.asset");
                wd.id = w.id;
                wd.displayName = w.name;
                wd.description = w.description;
                wd.order = order++;
                wd.letterIds = w.letters.Select(LetterDefinition.IdFor).ToList();
                wd.sceneName = Core.SceneNames.World(w.id);
                wd.skyColor = E.Hex(w.sky);
                wd.groundColor = E.Hex(w.ground);
                wd.accentColor = E.Hex(w.accent);
                wd.nameAudioKey = $"world.{w.id}.name";
                wd.environmentStatus = ContentStatus.Placeholder;
                foreach (char c in w.letters) worldOfLetter[c] = w.id;
                EditorUtility.SetDirty(wd);
                catalog.worlds.Add(wd);
            }

            foreach (LetterDto l in root.letters)
            {
                char up = l.upper[0], lo = l.lower[0];
                string lid = LetterDefinition.IdFor(up);
                var letter = E.LoadOrCreateSO<LetterDefinition>($"{SO}/Letters/Letter_{up}.asset");
                letter.id = lid;
                letter.upper = up;
                letter.lower = lo;
                letter.displayName = l.name;
                letter.spokenName = l.nameSpoken;
                letter.phonemeDisplay = l.phoneme;
                letter.phonemeIpa = l.phonemeIpa;
                letter.soundHint = l.soundHint;
                letter.soundNote = l.soundNote;
                letter.characterId = CharacterDefinition.IdFor(up);
                letter.worldId = worldOfLetter.TryGetValue(up, out string wid) ? wid : null;
                letter.reviewStatus = root.reviewStatus;
                letter.nameAudioKey = $"letter.{lo}.name";
                letter.phonemeAudioKey = $"letter.{lo}.sound";
                letter.introAudioKey = $"letter.{lo}.intro";

                letter.vocabulary = new List<VocabularyEntry>();
                foreach (VocabDto v in l.vocabulary)
                {
                    string vid = VocabularyEntry.IdFor(v.word);
                    var ve = E.LoadOrCreateSO<VocabularyEntry>($"{SO}/Vocabulary/{up}/{vid}.asset");
                    ve.id = vid;
                    ve.word = v.word;
                    ve.letterId = lid;
                    ve.startsWithBeginnerSound = v.startsWithSound;
                    ve.soundPosition = string.IsNullOrEmpty(v.soundPosition) ? "start" : v.soundPosition;
                    ve.soundNote = v.soundNote;
                    ve.artNote = v.artNote;
                    ve.picture = LoadPicture(v.emoji);
                    ve.artStatus = ContentStatus.Placeholder;
                    ve.audioKey = $"vocab.{vid}";
                    EditorUtility.SetDirty(ve);
                    letter.vocabulary.Add(ve);
                }

                letter.upperTracing = BuildTracing(up, lid, LetterCase.Upper);
                letter.lowerTracing = BuildTracing(lo, lid, LetterCase.Lower);

                var adv = E.LoadOrCreateSO<AdventureDefinition>($"{SO}/Adventures/Adventure_{up}.asset");
                adv.id = AdventureDefinition.IdFor(up);
                adv.letterId = lid;
                adv.title = l.adventure.title;
                adv.archetype = (AdventureArchetype)Enum.Parse(typeof(AdventureArchetype), l.adventure.archetype);
                adv.propKey = l.adventure.prop;
                adv.propSprite = LoadPicture(l.adventure.prop);
                adv.targetCount = Mathf.Max(1, l.adventure.count);
                adv.instruction = l.adventure.instruction;
                adv.instructionAudioKey = $"adventure.{lo}.instruction";
                adv.rewardStars = 1;
                EditorUtility.SetDirty(adv);
                letter.adventure = adv;
                EditorUtility.SetDirty(letter);
                catalog.letters.Add(letter);

                var ch = E.LoadOrCreateSO<CharacterDefinition>($"{SO}/Characters/Character_{up}.asset");
                ch.id = CharacterDefinition.IdFor(up);
                ch.displayName = l.character.name;
                ch.letterId = lid;
                ch.personality = l.character.personality;
                ch.themeObject = l.character.theme;
                ch.primaryColor = E.Hex(l.character.color);
                ch.accentColor = E.Hex(l.character.accent);
                ch.introAudioKey = letter.introAudioKey;
                ch.portrait = l.vocabulary.Length > 0 ? LoadPicture(ThemePicture(l)) : null;
                if (characterPrefabs != null && characterPrefabs.TryGetValue(up, out GameObject prefab)) ch.prefab = prefab;
                ch.artStatus = ContentStatus.Placeholder;
                EditorUtility.SetDirty(ch);
                catalog.characters.Add(ch);
            }

            catalog.rewards = BuildRewards(catalog);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            catalog.Reindex();
            return catalog;
        }

        /// <summary>Picture key that best represents the character's theme object (first vocab word matching it).</summary>
        private static string ThemePicture(LetterDto l)
        {
            foreach (VocabDto v in l.vocabulary)
                if (l.character.theme.ToLowerInvariant().Contains(v.word.ToLowerInvariant()) || v.word.ToLowerInvariant().Contains(l.character.theme.ToLowerInvariant()))
                    return v.emoji;
            return l.vocabulary[0].emoji;
        }

        public static Sprite LoadPicture(string key)
        {
            string name = ArtKeys.ResourceName(key);
            if (name == null) return null;
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtPipeline.PicturesDir}/{name}.png");
        }

        private static TracingDefinition BuildTracing(char glyph, string letterId, LetterCase c)
        {
            char up = char.ToUpperInvariant(glyph);
            var t = E.LoadOrCreateSO<TracingDefinition>($"{SO}/Tracing/{TracingDefinition.IdFor(up, c)}.asset");
            t.id = TracingDefinition.IdFor(up, c);
            t.letterId = letterId;
            t.letterCase = c;
            t.strokes = LetterStrokeLibrary.Get(glyph).Select(s => new StrokeData { points = s.ToList() }).ToList();
            t.tolerance = 0.16f;
            t.completionThreshold = 0.85f;
            t.status = ContentStatus.Provisional; // stroke order pending expert review
            EditorUtility.SetDirty(t);
            return t;
        }

        private static AudioLibrary BuildAudioLibrary()
        {
            var lib = E.LoadOrCreateSO<AudioLibrary>($"{SO}/AudioLibrary.asset");
            lib.entries.Clear();
            var manifest = JsonUtility.FromJson<VoiceManifest>(File.ReadAllText(VoiceManifestPath));
            foreach (VoiceLine line in manifest.lines)
            {
                string clipPath = $"{VoiceDir}/{line.key}.wav";
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                if (clip != null) AudioSynth.ConfigureClip(clipPath, AudioClipLoadType.DecompressOnLoad);
                lib.entries.Add(new AudioLibrary.Entry
                {
                    key = line.key,
                    text = line.text,
                    clip = clip,
                    status = clip == null ? ContentStatus.Missing : ContentStatus.Provisional
                });
            }
            EditorUtility.SetDirty(lib);
            return lib;
        }

        /// <summary>
        /// Deterministic cosmetic rewards: one sticker per letter (earned by meeting it), plus hats and
        /// glasses for star milestones. Nothing random, nothing purchasable.
        /// </summary>
        private static List<RewardDefinition> BuildRewards(ContentCatalog catalog)
        {
            var list = new List<RewardDefinition>();
            foreach (LetterDefinition l in catalog.letters)
            {
                var r = E.LoadOrCreateSO<RewardDefinition>($"{SO}/Rewards/Sticker_{l.upper}.asset");
                r.id = "sticker_" + char.ToLowerInvariant(l.upper);
                r.displayName = $"{l.upper} sticker";
                r.type = RewardType.Sticker;
                r.condition = UnlockCondition.MeetLetter;
                r.param = l.id;
                r.icon = l.vocabulary.FirstOrDefault()?.picture;
                EditorUtility.SetDirty(r);
                list.Add(r);
            }
            (string id, string name, RewardType type, int stars, string pic)[] extras =
            {
                ("bow_pink", "Pink bow", RewardType.Hat, 5, "1f380"),
                ("glasses_cool", "Sunglasses", RewardType.Glasses, 10, "1f576"),
                ("hat_top", "Top hat", RewardType.Hat, 20, "1f3a9"),
                ("crown_gold", "Golden crown", RewardType.Hat, 35, "1f451"),
                ("dance_wiggle", "Wiggle dance", RewardType.Dance, 50, "1f3b5"),
                ("decor_rainbow", "Rainbow", RewardType.Decoration, 75, "1f308"),
            };
            foreach (var x in extras)
            {
                var r = E.LoadOrCreateSO<RewardDefinition>($"{SO}/Rewards/{x.id}.asset");
                r.id = x.id;
                r.displayName = x.name;
                r.type = x.type;
                r.condition = UnlockCondition.StarTotal;
                r.amount = x.stars;
                r.icon = LoadPicture(x.pic);
                EditorUtility.SetDirty(r);
                list.Add(r);
            }
            return list;
        }
    }
}
