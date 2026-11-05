using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ABCWonderworld.Audio;
using ABCWonderworld.Characters;
using ABCWonderworld.Content;
using ABCWonderworld.Learning;
using ABCWonderworld.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ABCWonderworld.Activities
{
    /// <summary>
    /// GDD §6.3 Find the Letter. Voice + visual target, large bubbles in random positions, no time limit.
    /// Wrong taps get progressively stronger help: (1) gentle wiggle + "try another", (2) the target glows,
    /// (3) a distractor floats away, (4) the hand shows the answer. Nothing ever labels the child as failing.
    /// </summary>
    public sealed class FindLetterActivity : ActivityBase
    {
        public override ActivityType Type => ActivityType.FindLetter;
        public const int Rounds = 3;

        private RectTransform _area;
        private GlyphGraphic _targetCue;
        private readonly List<(BigButton button, char glyph)> _choices = new List<(BigButton, char)>();
        private int _round;
        private int _wrong;
        private Assistance _assist;
        private char _target;
        private LetterCase _roundCase;
        private bool _accepting;
        private Image _hand;

        protected override void OnBegin()
        {
            UiSkin skin = UiSkin.Load();
            RectTransform bubble = UiFactory.Rect("TargetCue", Ctx.UiRoot);
            UiFactory.Place(bubble, new Vector2(0.24f, 0.8f), Vector2.zero, new Vector2(230, 230));
            Image bg = UiFactory.Image("Bubble", bubble, skin.circle, Color.white, true);
            UiFactory.Stretch(bg.rectTransform);
            UiFactory.Shadow(bg.transform, 230);
            _targetCue = UiFactory.Glyph("Glyph", bubble, 'A', LetterColor(Ctx), 0.2f);
            UiFactory.Stretch(_targetCue.rectTransform, 30);
            var cueBtn = bg.gameObject.AddComponent<BigButton>();
            cueBtn.onClick.AddListener(ReplayInstruction);

            _area = UiFactory.Rect("Choices", Ctx.UiRoot);
            UiFactory.Place(_area, new Vector2(0.62f, 0.45f), Vector2.zero, new Vector2(1150, 760));
            _hand = UiFactory.Image("Hand", Ctx.UiRoot, skin.iconHand, Color.white);
            _hand.rectTransform.sizeDelta = new Vector2(170, 170);
            _hand.rectTransform.pivot = new Vector2(0.45f, 0.9f);
            _hand.gameObject.SetActive(false);
            Progress.RecordExposure(Letter.id, Type);
            StartCoroutine(NextRound());
        }

        private IEnumerator NextRound()
        {
            foreach (var c in _choices) if (c.button != null) Destroy(c.button.gameObject);
            _choices.Clear();
            _wrong = 0;
            _assist = Assistance.None;
            _hand.gameObject.SetActive(false);

            int level = S.Difficulty.LevelFor(Type);
            // Case by level: beginners see uppercase; then alternate; advanced mixes lowercase more often.
            _roundCase = level == 0 ? Ctx.Case : (_round % 2 == 0 ? LetterCase.Upper : LetterCase.Lower);
            if (level >= 2 && _round == Rounds - 1) _roundCase = LetterCase.Lower;
            _target = Letter.Glyph(_roundCase);
            int count = AdaptiveDifficulty.ChoiceCount(level, Ctx.Rng);
            if (S.Settings.SimplifiedInteraction) count = Mathf.Min(count, 3);
            IEnumerable<char> pool = Ctx.World != null ? Ctx.World.letterIds.Select(id => S.Catalog.Letter(id)?.upper ?? 'A') : null;
            List<char> letters = ChoiceGenerator.Generate(_target, count, Ctx.Rng, level >= 2, pool);

            _targetCue.SetGlyph(_target);
            _targetCue.transform.parent.gameObject.SetActive(level < 2 || _round == 0);
            Progress.RecordPractice(Letter.id, Type, _roundCase);

            UiSkin skin = UiSkin.Load();
            List<Vector2> slots = Slots(letters.Count);
            Color[] tints = { new Color(1f, 0.6f, 0.6f), new Color(0.6f, 0.8f, 1f), new Color(1f, 0.85f, 0.45f), new Color(0.65f, 0.9f, 0.6f), new Color(0.85f, 0.7f, 1f), new Color(1f, 0.72f, 0.5f) };
            for (int i = 0; i < letters.Count; i++)
            {
                char g = letters[i];
                float size = letters.Count <= 2 ? 330 : letters.Count <= 4 ? 280 : 240;
                Image b = UiFactory.Image("Choice_" + g, _area, skin.circle, tints[i % tints.Length], true);
                b.raycastPadding = new Vector4(-20, -20, -20, -20);
                b.rectTransform.sizeDelta = new Vector2(size, size);
                b.rectTransform.anchoredPosition = slots[i];
                UiFactory.Shadow(b.transform, size);
                Image inner = UiFactory.Image("Inner", b.transform, skin.circle, new Color(1, 1, 1, 0.9f));
                UiFactory.Stretch(inner.rectTransform, size * 0.07f);
                GlyphGraphic glyph = UiFactory.Glyph("Glyph", b.transform, g, UiFactory.Darken(tints[i % tints.Length], 0.45f), 0.2f);
                UiFactory.Stretch(glyph.rectTransform, size * 0.16f);
                var btn = b.gameObject.AddComponent<BigButton>();
                btn.sfx = null;
                btn.cooldown = 0.25f;
                btn.onClick.AddListener(() => Choose(btn, g));
                b.gameObject.AddComponent<Bob2D>();
                b.transform.localScale = Vector3.zero;
                Transform t = b.transform;
                Tween.Run(this, 0.4f, k => { if (t != null) t.localScale = Vector3.one * Tween.EaseOutBack(k); });
                _choices.Add((btn, g));
            }
            yield return new WaitForSeconds(0.3f);
            ReplayInstruction();
            _accepting = true;
        }

        private static readonly Vector2[][] Layouts =
        {
            new Vector2[0], new Vector2[0],
            new[] { new Vector2(-260, 0), new Vector2(260, 0) },
            new[] { new Vector2(-380, 40), new Vector2(0, -60), new Vector2(380, 40) },
            new[] { new Vector2(-420, 150), new Vector2(-140, -150), new Vector2(140, 150), new Vector2(420, -150) },
            new[] { new Vector2(-440, 180), new Vector2(-220, -170), new Vector2(0, 180), new Vector2(220, -170), new Vector2(440, 180) },
            new[] { new Vector2(-400, 190), new Vector2(0, 190), new Vector2(400, 190), new Vector2(-400, -190), new Vector2(0, -190), new Vector2(400, -190) },
        };

        private List<Vector2> Slots(int n)
        {
            var list = Layouts[Mathf.Clamp(n, 2, 6)].ToList();
            for (int i = 0; i < list.Count; i++) list[i] += new Vector2(Ctx.Rng.Next(-30, 30), Ctx.Rng.Next(-25, 25));
            return list;
        }

        private void Choose(BigButton b, char glyph)
        {
            if (!_accepting || IsPaused) return;
            if (glyph == _target)
            {
                _accepting = false;
                StartCoroutine(Correct(b));
                return;
            }
            _wrong++;
            Audio.PlaySfx("gentle");
            Tween.Wiggle(this, b.transform);
            // Say the name of what they chose: informative, not corrective.
            LetterDefinition chosen = S.Catalog.Letter(char.ToUpperInvariant(glyph));
            var keys = new List<string>();
            if (chosen != null) keys.Add(chosen.nameAudioKey);
            keys.Add(_wrong >= 3 ? "vo.here_it_is" : "vo.try_another");
            SaySequence(keys);
            Character(CharacterState.Encourage);
            if (_wrong == 2)
            {
                _assist = Assistance.Hint;
                BigButton t = TargetButton();
                if (t != null) Attention.Ensure(t.gameObject, 0.12f);
            }
            if (_wrong == 3) RemoveADistractor();
            if (_wrong >= 4)
            {
                _assist = Assistance.Demonstrated;
                BigButton t = TargetButton();
                if (t != null)
                {
                    _hand.gameObject.SetActive(true);
                    _hand.transform.position = t.transform.position + new Vector3(30, -40, 0);
                    Attention.Ensure(_hand.gameObject, 0.08f);
                }
            }
        }

        private BigButton TargetButton() => _choices.FirstOrDefault(c => c.glyph == _target && c.button != null).button;

        private void RemoveADistractor()
        {
            var d = _choices.FirstOrDefault(c => c.glyph != _target && c.button != null && c.button.gameObject.activeSelf);
            if (d.button == null || _choices.Count(c => c.button != null && c.button.gameObject.activeSelf) <= 2) return;
            d.button.Interactable = false;
            Transform t = d.button.transform;
            Tween.Run(this, 0.5f, k => { if (t != null) t.localScale = Vector3.one * (1 - k); }, () => { if (t != null) t.gameObject.SetActive(false); });
        }

        private IEnumerator Correct(BigButton b)
        {
            bool firstTry = _wrong == 0;
            Progress.RecordRecognition(Letter.id, Type, _roundCase, true, _wrong + 1, _assist);
            S.Difficulty.RecordRound(Type, _wrong, _assist);
            Audio.PlaySfx("chime");
            Audio.PlaySfx("sparkle", 0.6f);
            Character(CharacterState.Happy);
            SaySequence(new[] { "vo.you_found_it", NameKey(Letter) });
            Tween.Pop(this, b.transform, 0.25f, 0.6f);
            _hand.gameObject.SetActive(false);
            foreach (var c in _choices)
            {
                if (c.button == null || c.button == b) continue;
                Transform t = c.button.transform;
                Tween.Run(this, 0.4f, k => { if (t != null) t.localScale = Vector3.one * (1 - k); });
            }
            yield return WaitWhileSpeaking();
            yield return new WaitForSeconds(firstTry ? 0.2f : 0.4f);
            _round++;
            if (_round >= Rounds) Finish(1);
            else StartCoroutine(NextRound());
        }

        public override void ReplayInstruction()
        {
            Tween.Pop(this, _targetCue.transform.parent, 0.12f, 0.4f);
            SaySequence(new[] { _roundCase == LetterCase.Upper ? "vo.find_big" : "vo.find_small", NameKey(Letter) });
        }

        public override void Abort()
        {
            if (!IsFinished && _accepting && _wrong > 0)
                Progress.RecordRecognition(Letter.id, Type, _roundCase, false, _wrong, _assist);
            base.Abort();
        }

        public override IEnumerator AutoPlay()
        {
            int guard = 0;
            while (!IsFinished && guard++ < 2000)
            {
                if (_accepting)
                {
                    // One deliberate wrong answer on the first round exercises the hint path.
                    if (_round == 0 && _wrong == 0)
                    {
                        var wrong = _choices.FirstOrDefault(c => c.glyph != _target);
                        if (wrong.button != null) { wrong.button.Click(); yield return new WaitForSeconds(1.2f); }
                    }
                    TargetButton()?.Click();
                }
                yield return new WaitForSeconds(0.3f);
            }
        }
    }

    /// <summary>Gentle floating motion for UI bubbles.</summary>
    public sealed class Bob2D : MonoBehaviour
    {
        private RectTransform _rt;
        private Vector2 _base;
        private float _o;
        private bool _init;
        private void Update()
        {
            if (!_init) { _rt = (RectTransform)transform; _base = _rt.anchoredPosition; _o = Random.value * 6f; _init = true; }
            _rt.anchoredPosition = _base + new Vector2(Mathf.Sin(Time.time * 0.9f + _o) * 8f, Mathf.Sin(Time.time * 1.3f + _o) * 12f) * Tween.Motion;
        }
    }
}
