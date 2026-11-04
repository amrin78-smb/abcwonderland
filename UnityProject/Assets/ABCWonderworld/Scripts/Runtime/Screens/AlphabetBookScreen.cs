using System.Collections.Generic;
using System.Linq;
using ABCWonderworld.Content;
using ABCWonderworld.Learning;
using ABCWonderworld.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ABCWonderworld.Screens
{
    /// <summary>
    /// GDD §9.2 My Alphabet Book and §7.3 alphabet selection: a visual A–Z map. Each page shows the
    /// character, upper/lower case, name and sound (replayable), vocabulary, discovery status and sticker.
    /// Every letter is always explorable; pages offer the same activity choices as the worlds.
    /// </summary>
    public sealed class AlphabetBookScreen : ScreenBase
    {
        private RectTransform _ui, _grid, _page;
        public readonly List<BigButton> LetterButtons = new List<BigButton>();
        public BigButton WardrobeButton { get; private set; }

        protected override void Build()
        {
            World.EnvironmentBuilder.SetupCameraAndLight(null, new Vector3(0, 3, -10), new Vector3(0, 1.5f, 0), 40f);
            Camera.main.backgroundColor = new Color(1f, 0.92f, 0.8f);
            UiSkin skin = UiSkin.Load();
            _ui = UiFactory.Canvas("BookCanvas", 10);
            Image paper = UiFactory.Image("Paper", _ui, skin.rounded, new Color(1f, 0.98f, 0.93f));
            UiFactory.Stretch(paper.rectTransform, 30);
            BigButton home = UiFactory.IconButton("Home", _ui, skin.iconHome, skin.secondary, UiFactory.CornerButton, () => S.Flow.GoHome(), true);
            UiFactory.Place((RectTransform)home.transform, new Vector2(0, 1), new Vector2(110, -110), Vector2.one * UiFactory.CornerButton);
            WardrobeButton = UiFactory.IconButton("Wardrobe", _ui, skin.iconStarOutline, new Color(0.95f, 0.6f, 0.2f), UiFactory.CornerButton, () => Wardrobe.Show(_ui, S));
            UiFactory.Place((RectTransform)WardrobeButton.transform, new Vector2(1, 1), new Vector2(-110, -110), Vector2.one * UiFactory.CornerButton);
            WardrobeButton.voiceKey = "vo.sticker";
            BuildGrid(skin);
            S.Audio.Speak("vo.alphabet_book");
        }

        private void BuildGrid(UiSkin skin)
        {
            _grid = UiFactory.Rect("Grid", _ui);
            UiFactory.Place(_grid, new Vector2(0.5f, 0.47f), Vector2.zero, new Vector2(1700, 860));
            for (int i = 0; i < S.Catalog.letters.Count; i++)
            {
                LetterDefinition l = S.Catalog.letters[i];
                CharacterDefinition ch = S.Catalog.CharacterFor(l);
                LetterLearningState st = S.Progress.StateOf(l.id);
                Color col = ch != null ? ch.primaryColor : skin.primary;
                int row = i / 9, colIdx = i % 9;
                int inRow = row < 2 ? 9 : 8;
                BigButton b = UiFactory.IconButton("Letter_" + l.upper, _grid, null, st == LetterLearningState.NotYetSeen ? UiFactory.Lighten(col, 0.55f) : col, 170, () => OpenPage(l), false);
                b.voiceKey = l.nameAudioKey;
                UiFactory.Place((RectTransform)b.transform, new Vector2(0.5f, 0.5f), new Vector2((colIdx - (inRow - 1) * 0.5f) * 185, 280 - row * 280), new Vector2(170, 170));
                GlyphGraphic g = UiFactory.Glyph("Glyph", b.transform, l.upper, Color.white, 0.2f);
                UiFactory.Stretch(g.rectTransform, 30);
                if (S.Rewards.IsUnlocked("sticker_" + l.lower))
                {
                    Image sticker = UiFactory.Image("Sticker", b.transform, l.vocabulary.FirstOrDefault()?.picture, Color.white);
                    UiFactory.Place(sticker.rectTransform, new Vector2(1, 0), new Vector2(-15, 15), new Vector2(80, 80));
                }
                if (st == LetterLearningState.ShowingRecognition)
                {
                    Image star = UiFactory.Image("Star", b.transform, skin.star, new Color(1f, 0.8f, 0.2f));
                    UiFactory.Place(star.rectTransform, new Vector2(1, 1), new Vector2(-15, -15), new Vector2(60, 60));
                }
                LetterButtons.Add(b);
            }
        }

        public void OpenPage(LetterDefinition l)
        {
            if (_page != null) Destroy(_page.gameObject);
            UiSkin skin = UiSkin.Load();
            CharacterDefinition ch = S.Catalog.CharacterFor(l);
            Color col = ch != null ? ch.primaryColor : skin.primary;
            _page = UiFactory.Rect("Page_" + l.upper, _ui);
            UiFactory.Stretch(_page);
            Image dim = UiFactory.Image("Dim", _page, null, new Color(0.2f, 0.15f, 0.3f, 0.35f), true);
            UiFactory.Stretch(dim.rectTransform);
            Image card = UiFactory.Image("Card", _page, skin.rounded, Color.white, true);
            UiFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600, 900));
            S.Progress.RecordExposure(l.id, ActivityType.MeetLetter);

            if (ch?.portrait != null)
            {
                Image portrait = UiFactory.Image("Portrait", card.transform, ch.portrait, Color.white, true);
                UiFactory.Place(portrait.rectTransform, new Vector2(0, 1), new Vector2(230, -220), new Vector2(260, 260));
                var pb = portrait.gameObject.AddComponent<BigButton>();
                pb.voiceKey = l.introAudioKey;
            }
            GlyphGraphic up = UiFactory.Glyph("Upper", card.transform, l.upper, col, 0.18f);
            UiFactory.Place(up.rectTransform, new Vector2(0.5f, 1), new Vector2(-150, -230), new Vector2(300, 340));
            up.raycastTarget = true;
            up.gameObject.AddComponent<BigButton>().onClick.AddListener(() => S.Audio.SpeakSequence(new[] { "vo.big", l.nameAudioKey }));
            GlyphGraphic lo = UiFactory.Glyph("Lower", card.transform, l.lower, UiFactory.Darken(col, 0.15f), 0.18f);
            UiFactory.Place(lo.rectTransform, new Vector2(0.5f, 1), new Vector2(150, -230), new Vector2(260, 340));
            lo.raycastTarget = true;
            lo.gameObject.AddComponent<BigButton>().onClick.AddListener(() => S.Audio.SpeakSequence(new[] { "vo.small", l.nameAudioKey }));
            BigButton sound = UiFactory.IconButton("Sound", card.transform, skin.iconSpeaker, skin.secondary, 150, () => S.Audio.SpeakSequence(new[] { l.nameAudioKey, "vo.says", l.phonemeAudioKey }));
            UiFactory.Place((RectTransform)sound.transform, new Vector2(1, 1), new Vector2(-200, -200), new Vector2(150, 150));
            Text status = UiFactory.Label("Status", card.transform, $"{ch?.displayName}\n{l.soundHint}", 34, new Color(0.4f, 0.38f, 0.5f));
            UiFactory.Place(status.rectTransform, new Vector2(1, 1), new Vector2(-200, -350), new Vector2(380, 120));

            List<VocabularyEntry> vocab = l.vocabulary.Where(v => v != null).ToList();
            for (int i = 0; i < vocab.Count; i++)
            {
                VocabularyEntry v = vocab[i];
                bool heard = S.Progress.Get(l.id)?.vocabularyHeard.Contains(v.id) ?? false;
                Image pic = UiFactory.Image("Word_" + v.id, card.transform, v.picture, heard ? Color.white : new Color(1, 1, 1, 0.75f), true);
                UiFactory.Place(pic.rectTransform, new Vector2(0.5f, 0.5f), new Vector2((i - (vocab.Count - 1) * 0.5f) * 260, -60), new Vector2(200, 200));
                var vb = pic.gameObject.AddComponent<BigButton>();
                vb.sfx = "pop";
                vb.onClick.AddListener(() => { S.Audio.Speak(v.audioKey); S.Progress.RecordVocabulary(l.id, v.id, ActivityType.MeetLetter); pic.color = Color.white; });
            }
            Sprite[] icons = WorldScreen.PickerIcons(skin);
            Color[] colors = WorldScreen.PickerColors(skin);
            for (int i = 0; i < WorldScreen.PickerModes.Length; i++)
            {
                var (type, voice) = WorldScreen.PickerModes[i];
                BigButton b = UiFactory.IconButton("Mode_" + type, card.transform, icons[i], colors[i], 160, () => S.Flow.StartActivity(type, l.id, l.worldId), true);
                b.voiceKey = voice;
                UiFactory.Place((RectTransform)b.transform, new Vector2(0.5f, 0), new Vector2((i - 2.5f) * 190, 130), new Vector2(155, 155));
            }
            BigButton close = UiFactory.IconButton("Close", card.transform, skin.iconBack, skin.secondary, 120, () => { Destroy(_page.gameObject); _page = null; });
            UiFactory.Place((RectTransform)close.transform, new Vector2(0, 0), new Vector2(110, 110), new Vector2(120, 120));
            S.Audio.SpeakSequence(new[] { "vo.this_is_letter", l.nameAudioKey });
        }
    }
}
