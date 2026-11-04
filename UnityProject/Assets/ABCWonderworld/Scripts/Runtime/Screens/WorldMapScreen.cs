using System.Collections.Generic;
using ABCWonderworld.Content;
using ABCWonderworld.Learning;
using ABCWonderworld.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ABCWonderworld.Screens
{
    /// <summary>
    /// GDD §7.2 World selection: five large portals with environment art, name, letter range and simple
    /// progress dots. Every world is always open; nothing is locked behind performance.
    /// </summary>
    public sealed class WorldMapScreen : ScreenBase
    {
        public readonly List<BigButton> Portals = new List<BigButton>();
        private static readonly string[] PortalArt = { "sunflower", "tropical_fish", "lollipop", "trex", "rainbow" };

        protected override void Build()
        {
            World.EnvironmentBuilder.SetupCameraAndLight(null, new Vector3(0, 6, -12), new Vector3(0, 2, 0), 40f);
            Camera.main.backgroundColor = new Color(0.65f, 0.85f, 1f);
            World.EnvironmentBuilder.Build(S.Catalog.worlds.Count > 4 ? S.Catalog.worlds[4] : null, false, 5);

            UiSkin skin = UiSkin.Load();
            RectTransform ui = UiFactory.Canvas("MapCanvas", 10);
            BigButton home = UiFactory.IconButton("Home", ui, skin.iconHome, skin.secondary, UiFactory.CornerButton, () => S.Flow.GoHome(), true);
            UiFactory.Place((RectTransform)home.transform, new Vector2(0, 1), new Vector2(110, -110), Vector2.one * UiFactory.CornerButton);
            StarCounterRight(ui);

            Vector2[] positions = { new Vector2(-640, 120), new Vector2(-320, -250), new Vector2(0, 120), new Vector2(320, -250), new Vector2(640, 120) };
            for (int i = 0; i < S.Catalog.worlds.Count; i++)
            {
                WorldDefinition w = S.Catalog.worlds[i];
                Portals.Add(Portal(ui, w, positions[i % positions.Length], PortalArt[i % PortalArt.Length], skin));
            }
            S.Audio.Speak("vo.choose_world");
        }

        private void StarCounterRight(RectTransform ui)
        {
            StarCounter.Create(ui, S);
            var stars = ui.Find("Stars") as RectTransform;
            if (stars != null) UiFactory.Place(stars, new Vector2(1, 1), new Vector2(-190, -100), new Vector2(260, 120));
        }

        private BigButton Portal(RectTransform ui, WorldDefinition w, Vector2 pos, string art, UiSkin skin)
        {
            const float size = 330;
            BigButton b = UiFactory.IconButton("Portal_" + w.id, ui, null, w.groundColor, size, () => S.Flow.GoWorld(w.id), true);
            b.voiceKey = w.nameAudioKey;
            var rt = (RectTransform)b.transform;
            UiFactory.Place(rt, new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
            Image ring = UiFactory.Image("Ring", rt, skin.ring, w.accentColor);
            UiFactory.Stretch(ring.rectTransform, -14);
            Image inner = UiFactory.Image("Sky", rt, skin.circle, w.skyColor);
            UiFactory.Stretch(inner.rectTransform, 22);
            Image pic = UiFactory.Image("Art", rt, UiSkin.Picture(art), Color.white);
            UiFactory.Stretch(pic.rectTransform, 60);
            pic.rectTransform.anchoredPosition = new Vector2(0, 26);

            // Letter range, e.g. "A–E", drawn with glyphs.
            List<LetterDefinition> letters = S.Catalog.LettersIn(w);
            RectTransform range = UiFactory.Rect("Range", rt);
            UiFactory.Place(range, new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(260, 110));
            Image pill = UiFactory.Image("Pill", range, skin.rounded, Color.white);
            UiFactory.Stretch(pill.rectTransform);
            if (letters.Count > 0)
            {
                GlyphGraphic a = UiFactory.Glyph("First", range, letters[0].upper, UiFactory.Darken(w.accentColor, 0.35f), 0.2f);
                UiFactory.Place(a.rectTransform, new Vector2(0.27f, 0.5f), Vector2.zero, new Vector2(90, 95));
                Image dash = UiFactory.Image("Dash", range, skin.rounded, UiFactory.Darken(w.accentColor, 0.35f));
                UiFactory.Place(dash.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 12));
                GlyphGraphic z = UiFactory.Glyph("Last", range, letters[letters.Count - 1].upper, UiFactory.Darken(w.accentColor, 0.35f), 0.2f);
                UiFactory.Place(z.rectTransform, new Vector2(0.73f, 0.5f), Vector2.zero, new Vector2(90, 95));
            }
            // Progress dots: one per letter, filled as letters are introduced/practised.
            RectTransform dots = UiFactory.Rect("Progress", rt);
            UiFactory.Place(dots, new Vector2(0.5f, 1), new Vector2(0, 26), new Vector2(260, 40));
            for (int i = 0; i < letters.Count; i++)
            {
                LetterLearningState st = S.Progress.StateOf(letters[i].id);
                Color c = st == LetterLearningState.NotYetSeen ? new Color(1, 1, 1, 0.5f) : st == LetterLearningState.ShowingRecognition ? new Color(1f, 0.8f, 0.2f) : skin.success;
                Image d = UiFactory.Image("Dot", dots, st == LetterLearningState.ShowingRecognition ? skin.star : skin.circle, c);
                UiFactory.Place(d.rectTransform, new Vector2(0.5f, 0.5f), new Vector2((i - (letters.Count - 1) * 0.5f) * 44f, 0), new Vector2(36, 36));
            }
            b.gameObject.AddComponent<Bob2D2>();
            return b;
        }
    }

    /// <summary>Very slow portal float.</summary>
    public sealed class Bob2D2 : MonoBehaviour
    {
        private RectTransform _rt; private Vector2 _p; private float _o; private bool _i;
        private void Update()
        {
            if (!_i) { _rt = (RectTransform)transform; _p = _rt.anchoredPosition; _o = Random.value * 6; _i = true; }
            _rt.anchoredPosition = _p + Vector2.up * Mathf.Sin(Time.time * 0.8f + _o) * 10f * Tween.Motion;
        }
    }
}
