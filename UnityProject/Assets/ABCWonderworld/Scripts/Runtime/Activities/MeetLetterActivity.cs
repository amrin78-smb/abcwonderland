using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ABCWonderworld.Audio;
using ABCWonderworld.Characters;
using ABCWonderworld.Content;
using ABCWonderworld.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ABCWonderworld.Activities
{
    /// <summary>
    /// GDD §6.1 Meet the Letters. Voice-led sequence: character greets → letter name → big/small forms →
    /// beginner sound (separate clip from the name) → example word, then free exploration of three or more
    /// vocabulary cards. Every spoken item can be replayed by tapping it. Completion celebrates
    /// participation; tapping every card is not required (a Next arrow appears after the introduction).
    /// </summary>
    public sealed class MeetLetterActivity : ActivityBase
    {
        public override ActivityType Type => ActivityType.MeetLetter;

        private GlyphGraphic _upper, _lower;
        private BigButton _soundButton;
        private readonly List<BigButton> _cards = new List<BigButton>();
        private readonly HashSet<VocabularyEntry> _heard = new HashSet<VocabularyEntry>();
        private BigButton _next;
        private bool _introDone;

        protected override void OnBegin()
        {
            Progress.RecordExposure(Letter.id, Type);
            BuildUi();
            StartCoroutine(Intro());
        }

        private void BuildUi()
        {
            UiSkin skin = UiSkin.Load();
            Color col = LetterColor(Ctx);
            RectTransform panel = UiFactory.Rect("LetterPanel", Ctx.UiRoot);
            UiFactory.Place(panel, new Vector2(0.62f, 0.66f), Vector2.zero, new Vector2(900, 520));
            Image bg = UiFactory.Image("Card", panel, skin.rounded, new Color(1, 1, 1, 0.92f));
            UiFactory.Stretch(bg.rectTransform);
            UiFactory.Shadow(bg.transform, 500);

            _upper = UiFactory.Glyph("Upper", panel, Letter.upper, col);
            UiFactory.Place(_upper.rectTransform, new Vector2(0.3f, 0.52f), Vector2.zero, new Vector2(380, 440));
            var upBtn = _upper.gameObject.AddComponent<BigButton>();
            _upper.raycastTarget = true;
            upBtn.sfx = "tap";
            upBtn.onClick.AddListener(() => SaySequence(new[] { "vo.big", NameKey(Letter) }));

            _lower = UiFactory.Glyph("Lower", panel, Letter.lower, UiFactory.Darken(col, 0.15f));
            UiFactory.Place(_lower.rectTransform, new Vector2(0.68f, 0.52f), Vector2.zero, new Vector2(320, 440));
            var loBtn = _lower.gameObject.AddComponent<BigButton>();
            _lower.raycastTarget = true;
            loBtn.onClick.AddListener(() => SaySequence(new[] { "vo.small", NameKey(Letter) }));

            // Sound button: plays ONLY the phoneme (the letter name is on the glyphs).
            _soundButton = UiFactory.IconButton("Sound", panel, skin.iconSpeaker, skin.secondary, 150, PlaySound);
            UiFactory.Place((RectTransform)_soundButton.transform, new Vector2(0.92f, 0.12f), Vector2.zero, new Vector2(150, 150));

            // Vocabulary cards.
            RectTransform row = UiFactory.Rect("Vocabulary", Ctx.UiRoot);
            UiFactory.Place(row, new Vector2(0.62f, 0.2f), Vector2.zero, new Vector2(1000, 300));
            List<VocabularyEntry> vocab = Letter.vocabulary.Where(v => v != null).ToList();
            for (int i = 0; i < vocab.Count; i++)
            {
                VocabularyEntry v = vocab[i];
                Image card = UiFactory.Image("Card_" + v.id, row, skin.rounded, Color.white, true);
                card.raycastPadding = new Vector4(-12, -12, -12, -12);
                card.rectTransform.sizeDelta = new Vector2(270, 270);
                card.rectTransform.anchoredPosition = new Vector2((i - (vocab.Count - 1) * 0.5f) * 300f - 40f, 0);
                UiFactory.Shadow(card.transform, 270);
                Image pic = UiFactory.Image("Picture", card.transform, v.picture, Color.white);
                UiFactory.Stretch(pic.rectTransform, 28);
                var b = card.gameObject.AddComponent<BigButton>();
                b.sfx = "pop";
                b.onClick.AddListener(() => TapWord(v, card.transform));
                card.gameObject.SetActive(false);
                _cards.Add(b);
            }

            _next = UiFactory.IconButton("Next", Ctx.UiRoot, skin.iconArrow, skin.success, 190, () => Finish(1), true);
            UiFactory.Place((RectTransform)_next.transform, new Vector2(1, 0), new Vector2(-120, 120), new Vector2(170, 170));
            _next.gameObject.SetActive(false);
        }

        private IEnumerator Intro()
        {
            Character(CharacterState.Greet);
            _upperColor = _upper.color; _lowerColor = _lower.color;
            _upper.color = UiFactory.Lighten(_upperColor, 0.75f);
            _lower.color = UiFactory.Lighten(_lowerColor, 0.75f);
            Say(Letter.introAudioKey);
            yield return WaitWhileSpeaking();
            SaySequence(new[] { "vo.this_is_letter", NameKey(Letter) });
            Highlight(_upper, _upperColor);
            yield return WaitWhileSpeaking();
            SaySequence(new[] { "vo.big", NameKey(Letter) });
            yield return WaitWhileSpeaking();
            Highlight(_lower, _lowerColor);
            SaySequence(new[] { "vo.small", NameKey(Letter) });
            yield return WaitWhileSpeaking();
            PlaySound();
            yield return WaitWhileSpeaking();
            ShowCards();
            Say("vo.tap_pictures");
            _introDone = true;
            yield return WaitWhileSpeaking();
            _next.gameObject.SetActive(true);
            Tween.Pop(this, _next.transform);
        }

        /// <summary>"B says /b/, like ball." For X: "X says /ks/, like at the end of fox."</summary>
        private void PlaySound()
        {
            VocabularyEntry example = Letter.vocabulary.FirstOrDefault(v => v != null && v.startsWithBeginnerSound)
                                      ?? Letter.vocabulary.FirstOrDefault(v => v != null && v.soundPosition == "end");
            var keys = new List<string> { NameKey(Letter), "vo.says", Letter.phonemeAudioKey };
            if (example != null)
            {
                keys.Add(example.soundPosition == "end" ? "vo.like_end_of" : "vo.like");
                keys.Add(example.audioKey);
            }
            Tween.Pop(this, _soundButton.transform);
            Character(CharacterState.Happy);
            SaySequence(keys);
        }

        private Color _upperColor, _lowerColor;

        private void Highlight(GlyphGraphic g, Color target)
        {
            Color from = g.color;
            Tween.Run(this, 0.35f, k => g.color = Color.Lerp(from, target, k));
            Tween.Pop(this, g.transform, 0.12f, 0.5f);
        }

        private void ShowCards()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                Transform t = _cards[i].transform;
                t.gameObject.SetActive(true);
                t.localScale = Vector3.zero;
                float delay = i * 0.12f;
                Tween.Run(this, 0.45f + delay, k => t.localScale = Vector3.one * Tween.EaseOutBack(Mathf.Clamp01((k * (0.45f + delay) - delay) / 0.45f)));
            }
        }

        private void TapWord(VocabularyEntry v, Transform card)
        {
            Say(v.audioKey);
            Character(CharacterState.Happy);
            Progress.RecordVocabulary(Letter.id, v.id, Type);
            _heard.Add(v);
            if (_introDone && _heard.Count >= _cards.Count && !IsFinished)
            {
                Audio.Speak(RandomPraise(), VoiceMode.Queue);
                StartCoroutine(FinishAfterVoice());
            }
        }

        private IEnumerator FinishAfterVoice()
        {
            yield return WaitWhileSpeaking();
            Finish(1);
        }

        public override void ReplayInstruction()
        {
            if (_introDone) Say("vo.tap_pictures");
            else PlaySound();
        }

        public override IEnumerator AutoPlay()
        {
            float t = 0;
            while (!_introDone && t < 30f) { t += Time.deltaTime; yield return null; }
            foreach (BigButton c in _cards) { c.Click(); yield return new WaitForSeconds(0.5f); }
            float w = 0;
            while (!IsFinished && w < 10f) { w += Time.deltaTime; yield return null; }
            if (!IsFinished) _next.Click();
        }
    }
}
