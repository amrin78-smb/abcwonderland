using System.Collections.Generic;
using ABCWonderworld.Characters;
using ABCWonderworld.Content;
using ABCWonderworld.Core;
using ABCWonderworld.UI;
using ABCWonderworld.World;
using UnityEngine;
using UnityEngine.UI;

namespace ABCWonderworld.Screens
{
    /// <summary>
    /// A world: free-play playground (GDD §6.8) with the world's letter characters. Tapping a character
    /// greets the child and opens a picker of activities for that letter. No reading, timers or
    /// requirements to explore.
    /// </summary>
    public sealed class WorldScreen : ScreenBase
    {
        [SerializeField] private string worldId;
        public WorldDefinition World { get; private set; }
        public readonly List<LetterCharacter> Characters = new List<LetterCharacter>();
        public RectTransform Picker { get; private set; }
        public readonly List<BigButton> PickerButtons = new List<BigButton>();
        private RectTransform _ui;
        private LetterDefinition _selected;

        public static readonly (ActivityType type, string voice)[] PickerModes =
        {
            (ActivityType.MeetLetter, "vo.act_meet"), (ActivityType.Tracing, "vo.act_trace"), (ActivityType.FindLetter, "vo.act_find"),
            (ActivityType.Match, "vo.act_match"), (ActivityType.Puzzle, "vo.act_puzzle"), (ActivityType.Adventure, "vo.act_adventure")
        };

        public static Sprite[] PickerIcons(UiSkin s) => new[] { s.iconSpeaker, s.iconPencil, s.iconSearch, s.iconMatch, s.iconPuzzle, s.star };
        public static Color[] PickerColors(UiSkin s) => new[] { s.primary, new Color(0.55f, 0.45f, 0.95f), s.secondary, new Color(0.95f, 0.45f, 0.6f), new Color(0.2f, 0.75f, 0.7f), s.success };

        protected override void Build()
        {
            string id = S.Flow.Current.Screen == AppScreen.World ? S.Flow.Current.WorldId : worldId;
            World = S.Catalog.World(id) ?? S.Catalog.World(worldId) ?? S.Catalog.worlds[0];
            EnvironmentBuilder.SetupCameraAndLight(World, new Vector3(0, 3.2f, -10.5f), new Vector3(0, 1.4f, 2f), 42f);
            EnvironmentBuilder.Build(World, true, World.order * 31 + 7);

            List<LetterDefinition> letters = S.Catalog.LettersIn(World);
            var cast = new GameObject("Cast").transform;
            for (int i = 0; i < letters.Count; i++)
            {
                LetterDefinition l = letters[i];
                CharacterDefinition cd = S.Catalog.CharacterFor(l);
                GameObject go = cd?.prefab != null ? Instantiate(cd.prefab, cast) : CharacterFactory.Build(l.upper, Color.gray, Color.white, null, null);
                go.transform.SetParent(cast, false);
                float t = letters.Count == 1 ? 0.5f : i / (float)(letters.Count - 1);
                float x = Mathf.Lerp(-6.2f, 6.2f, t);
                go.transform.localPosition = new Vector3(x, 0, 1.2f + Mathf.Sin(t * Mathf.PI) * 1.6f);
                go.transform.localScale = Vector3.one * 1.7f;
                go.transform.localRotation = Quaternion.Euler(0, -x * 3f, 0);
                var lc = go.GetComponent<LetterCharacter>();
                Cosmetics.Apply(lc, S);
                lc.Tapped += c => Select(l, c);
                Characters.Add(lc);
            }

            UiSkin skin = UiSkin.Load();
            _ui = UiFactory.Canvas("WorldCanvas", 10);
            BigButton back = UiFactory.IconButton("Back", _ui, skin.iconBack, skin.secondary, UiFactory.CornerButton, () => S.Flow.GoWorldMap(), true);
            UiFactory.Place((RectTransform)back.transform, new Vector2(0, 1), new Vector2(110, -110), Vector2.one * UiFactory.CornerButton);
            StarCounter.Create(_ui, S);
            var stars = _ui.Find("Stars") as RectTransform;
            if (stars != null) UiFactory.Place(stars, new Vector2(1, 1), new Vector2(-190, -100), new Vector2(260, 120));

            Progress().RecordActivityStarted(ActivityType.FreePlay);
            S.Audio.SpeakSequence(new[] { World.nameAudioKey, "vo.free_play" });
        }

        private Learning.ProgressService Progress() => S.Progress;

        public void Select(LetterDefinition l, LetterCharacter c)
        {
            _selected = l;
            c.Play(CharacterState.Greet);
            c.SetSpeaking(true);
            S.Audio.SpeakSequence(new[] { l.introAudioKey, "vo.this_is_letter", l.nameAudioKey });
            S.Progress.RecordExposure(l.id, ActivityType.FreePlay);
            foreach (LetterCharacter other in Characters) if (other != c) other.Play(CharacterState.Happy);
            ShowPicker(l);
        }

        private void ShowPicker(LetterDefinition l)
        {
            if (Picker != null) Destroy(Picker.gameObject);
            PickerButtons.Clear();
            UiSkin skin = UiSkin.Load();
            Color col = S.Catalog.CharacterFor(l)?.primaryColor ?? skin.primary;
            Picker = UiFactory.Rect("Picker", _ui);
            UiFactory.Place(Picker, new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(1500, 250));
            Image bg = UiFactory.Image("Bg", Picker, skin.rounded, new Color(1, 1, 1, 0.92f));
            UiFactory.Stretch(bg.rectTransform);
            UiFactory.Shadow(bg.transform, 250);

            GlyphGraphic g = UiFactory.Glyph("Letter", Picker, l.upper, col, 0.2f);
            UiFactory.Place(g.rectTransform, new Vector2(0, 0.5f), new Vector2(110, 0), new Vector2(150, 190));
            GlyphGraphic lo = UiFactory.Glyph("Lower", Picker, l.lower, UiFactory.Darken(col, 0.15f), 0.2f);
            UiFactory.Place(lo.rectTransform, new Vector2(0, 0.5f), new Vector2(230, 0), new Vector2(110, 190));

            Sprite[] icons = PickerIcons(skin);
            Color[] colors = PickerColors(skin);
            for (int i = 0; i < PickerModes.Length; i++)
            {
                var (type, voice) = PickerModes[i];
                BigButton b = UiFactory.IconButton("Mode_" + type, Picker, icons[i], colors[i], 175, () => S.Flow.StartActivity(type, l.id, World.id), true);
                b.voiceKey = voice;
                UiFactory.Place((RectTransform)b.transform, new Vector2(0, 0.5f), new Vector2(410 + i * 200, 0), new Vector2(175, 175));
                PickerButtons.Add(b);
                Transform t = b.transform;
                t.localScale = Vector3.zero;
                float delay = i * 0.06f;
                Tween.Run(this, 0.35f + delay, k => t.localScale = Vector3.one * Tween.EaseOutBack(Mathf.Clamp01((k * (0.35f + delay) - delay) / 0.35f)));
            }
        }

        private void OnDestroy()
        {
            GameServices s = GameServices.Current;
            if (s != null && World != null) s.Progress.RecordActivityFinished(ActivityType.FreePlay, true, Time.timeSinceLevelLoad);
        }
    }
}
