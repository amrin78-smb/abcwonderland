using System.Collections.Generic;
using ABCWonderworld.Characters;
using ABCWonderworld.Content;
using ABCWonderworld.UI;
using ABCWonderworld.World;
using UnityEngine;

namespace ABCWonderworld.Screens
{
    /// <summary>
    /// GDD §7.1 main menu: animated world with letter characters, one big Play action, the Alphabet Book,
    /// a star counter, and the Parent Zone behind an adult gate. Minimal text; actions are voiced.
    /// </summary>
    public sealed class HomeScreen : ScreenBase
    {
        private static bool _welcomed;
        public BigButton PlayButton { get; private set; }
        public BigButton BookButton { get; private set; }
        public BigButton MusicButton { get; private set; }
        public ParentZone Parent { get; private set; }

        protected override void Build()
        {
            WorldDefinition meadow = S.Catalog.worlds.Count > 0 ? S.Catalog.worlds[0] : null;
            EnvironmentBuilder.SetupCameraAndLight(meadow, new Vector3(0, 2.6f, -10f), new Vector3(0, 1.6f, 0), 40f);
            EnvironmentBuilder.Build(meadow, true, 11);

            // A parade of characters from every world.
            char[] cast = { 'A', 'B', 'W', 'Z', 'K' };
            var stage = new GameObject("Cast").transform;
            for (int i = 0; i < cast.Length; i++)
            {
                CharacterDefinition cd = S.Catalog.CharacterFor(S.Catalog.Letter(cast[i]));
                if (cd?.prefab == null) continue;
                GameObject go = Instantiate(cd.prefab, stage);
                float x = (i - (cast.Length - 1) * 0.5f) * 2.3f;
                go.transform.localPosition = new Vector3(x, 0, 1.5f + Mathf.Abs(x) * 0.25f);
                go.transform.localScale = Vector3.one * 1.6f;
                go.transform.localRotation = Quaternion.Euler(0, -x * 4f, 0);
                var lc = go.GetComponent<LetterCharacter>();
                Cosmetics.Apply(lc, S);
                LetterDefinition ld = S.Catalog.Letter(cast[i]);
                lc.Tapped += c => { S.Audio.Speak(ld.nameAudioKey); c.Play(CharacterState.Happy); };
                if (i == 2) lc.Play(CharacterState.Greet);
            }

            UiSkin skin = UiSkin.Load();
            RectTransform ui = UiFactory.Canvas("HomeCanvas", 10);
            Text("Title", ui, "ABC Wonderworld", new Vector2(0.5f, 1), new Vector2(0, -110), 96);

            PlayButton = UiFactory.IconButton("Play", ui, skin.iconPlay, skin.success, 300, () => S.Flow.GoWorldMap(), true);
            PlayButton.voiceKey = "vo.lets_play";
            UiFactory.Place((RectTransform)PlayButton.transform, new Vector2(0.5f, 0), new Vector2(0, 230), new Vector2(300, 300));
            PlayButton.gameObject.AddComponent<Attention>();

            BookButton = UiFactory.IconButton("AlphabetBook", ui, skin.iconBook, skin.primary, 190, () => S.Flow.GoAlphabetBook(), true);
            BookButton.voiceKey = "vo.alphabet_book";
            UiFactory.Place((RectTransform)BookButton.transform, new Vector2(0.5f, 0), new Vector2(330, 160), new Vector2(190, 190));

            BigButton music = UiFactory.IconButton("Music", ui, skin.iconMusic, new Color(0.6f, 0.45f, 0.95f), 190, () => S.Flow.GoMusic(), true);
            music.voiceKey = "vo.music_intro";
            UiFactory.Place((RectTransform)music.transform, new Vector2(0.5f, 0), new Vector2(-330, 160), new Vector2(190, 190));
            MusicButton = music;
            if (Cosmetics.HasDecoration(S, "decor_rainbow"))
            {
                SpriteRenderer rb = EnvironmentBuilder.Billboard(stage, UiSkin.Picture("rainbow"), new Vector3(0, 4.2f, 6f), 5f);
                rb.name = "Cosmetic_decor_rainbow";
            }
            StarCounter.Create(ui, S);

            Parent = ParentZone.CreateEntry(ui, S);

            if (!_welcomed)
            {
                _welcomed = true;
                S.Audio.Speak("vo.welcome");
            }
            if (S.Session.TrySuggestBreakAtNaturalPause()) BreakReminder.Show(ui, S);
        }

        public static UnityEngine.UI.Text Text(string name, RectTransform parent, string text, Vector2 anchor, Vector2 pos, int size)
        {
            UnityEngine.UI.Text t = UiFactory.Label(name, parent, text, size, Color.white);
            UiFactory.Place(t.rectTransform, anchor, pos, new Vector2(1400, size * 1.4f));
            var o = t.gameObject.AddComponent<UnityEngine.UI.Outline>();
            o.effectColor = new Color(0.25f, 0.18f, 0.35f, 0.9f);
            o.effectDistance = new Vector2(4, -4);
            return t;
        }
    }

    /// <summary>Wonder Stars display (never decreases; no pressure mechanics).</summary>
    public static class StarCounter
    {
        public static void Create(RectTransform ui, Core.GameServices s)
        {
            UiSkin skin = UiSkin.Load();
            UnityEngine.UI.Image pill = UiFactory.Image("Stars", ui, skin.rounded, new Color(1, 1, 1, 0.85f));
            UiFactory.Place(pill.rectTransform, new Vector2(0, 1), new Vector2(190, -100), new Vector2(260, 120));
            UnityEngine.UI.Image star = UiFactory.Image("Star", pill.transform, skin.star, new Color(1f, 0.78f, 0.2f));
            UiFactory.Place(star.rectTransform, new Vector2(0, 0.5f), new Vector2(65, 0), new Vector2(95, 95));
            UnityEngine.UI.Text t = UiFactory.Label("Count", pill.transform, s.Rewards.Stars.ToString(), 64, skin.ink);
            UiFactory.Place(t.rectTransform, new Vector2(0.62f, 0.5f), Vector2.zero, new Vector2(150, 100));
        }
    }

    /// <summary>Gentle, optional break suggestion shown only at natural pauses (never mid-activity).</summary>
    public static class BreakReminder
    {
        public static void Show(RectTransform ui, Core.GameServices s)
        {
            UiSkin skin = UiSkin.Load();
            RectTransform root = UiFactory.Rect("BreakReminder", ui);
            UiFactory.Stretch(root);
            UnityEngine.UI.Image dim = UiFactory.Image("Dim", root, null, new Color(0.2f, 0.2f, 0.35f, 0.4f), true);
            UiFactory.Stretch(dim.rectTransform);
            UnityEngine.UI.Image card = UiFactory.Image("Card", root, skin.rounded, Color.white);
            UiFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 520));
            UnityEngine.UI.Image moon = UiFactory.Image("Moon", card.transform, UiSkin.Picture("moon"), Color.white);
            UiFactory.Place(moon.rectTransform, new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(220, 220));
            BigButton ok = UiFactory.IconButton("Ok", card.transform, skin.iconCheck, skin.success, 170, () =>
            {
                s.Session.Snooze();
                Object.Destroy(root.gameObject);
            });
            UiFactory.Place((RectTransform)ok.transform, new Vector2(0.5f, 0.2f), Vector2.zero, new Vector2(170, 170));
            s.Audio.Speak("vo.break");
        }
    }
}
