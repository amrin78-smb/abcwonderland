using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ABCWonderworld.Characters;
using ABCWonderworld.Content;
using ABCWonderworld.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ABCWonderworld.Activities
{
    /// <summary>
    /// GDD §6.4 Alphabet Adventures: a reusable mini-game framework. Each letter's AdventureDefinition picks
    /// an archetype and props. No timers, no speed pressure, no fail state; about 1–3 minutes.
    /// All 12 archetypes are implemented (this file + AdventureActivity.Placement.cs + AdventureActivity.Play.cs).
    /// Every drag interaction also has a tap alternative.
    /// </summary>
    public sealed partial class AdventureActivity : ActivityBase
    {
        public override ActivityType Type => ActivityType.Adventure;

        public static readonly AdventureArchetype[] Implemented = (AdventureArchetype[])System.Enum.GetValues(typeof(AdventureArchetype));

        /// <summary>Archetype actually run (identity now that all are implemented; kept for content tooling).</summary>
        public static AdventureArchetype Effective(AdventureArchetype a) => a;

        /// <summary>One AutoPlay step for archetypes not driven by plain BigButtons. Returns false when idle.</summary>
        private System.Func<bool> _autoStep;

        private AdventureDefinition _def;
        private RectTransform _area;
        private int _done;
        private int _goal;
        private readonly List<BigButton> _items = new List<BigButton>();
        private Image _container;
        private RectTransform _fillBar;
        private bool _ready;
        private UiSkin _skin;
        private int _popWrong; // distractor balloons tapped since the last target pop (honest recognition evidence)
        private BigButton _pourButton;

        protected override void OnBegin()
        {
            _def = Letter.adventure;
            _skin = UiSkin.Load();
            _area = UiFactory.Rect("Adventure", Ctx.UiRoot);
            UiFactory.Place(_area, new Vector2(0.6f, 0.48f), Vector2.zero, new Vector2(1250, 880));
            Progress.RecordExposure(Letter.id, Type);
            Progress.RecordPractice(Letter.id, Type, null);
            _goal = Mathf.Clamp(_def != null ? _def.targetCount : 3, 1, 8);
            switch (Effective(_def != null ? _def.archetype : AdventureArchetype.TapCollect))
            {
                case AdventureArchetype.PopTarget: StartCoroutine(PopTarget()); break;
                case AdventureArchetype.FindHidden: BuildFindHidden(); break;
                case AdventureArchetype.Fill: BuildFill(); break;
                case AdventureArchetype.DragToTarget: BuildDragToTarget(); break;
                case AdventureArchetype.PlaceParts: BuildPlaceParts(); break;
                case AdventureArchetype.MatchPairs: BuildMatchPairs(); break;
                case AdventureArchetype.DragCollect: BuildDragCollect(); break;
                case AdventureArchetype.PathSteps: BuildPathSteps(); break;
                case AdventureArchetype.Pattern: BuildPattern(); break;
                case AdventureArchetype.Music: BuildMusic(); break;
                case AdventureArchetype.Swipe: BuildSwipe(); break;
                default: BuildTapCollect(); break;
            }
            ReplayInstruction();
        }

        private Sprite Prop() => _def?.propSprite != null ? _def.propSprite : UiSkin.Picture(_def?.propKey) ?? Letter.vocabulary.FirstOrDefault()?.picture;

        private BigButton Item(string name, Sprite sprite, Vector2 pos, float size, Color tint, System.Action<BigButton> onTap)
        {
            Image img = UiFactory.Image(name, _area, sprite, tint, true);
            img.rectTransform.sizeDelta = new Vector2(size, size);
            img.rectTransform.anchoredPosition = pos;
            img.raycastPadding = new Vector4(-22, -22, -22, -22);
            var b = img.gameObject.AddComponent<BigButton>();
            b.sfx = null;
            b.cooldown = 0.2f;
            b.onClick.AddListener(() => { if (!IsPaused && _ready) onTap(b); });
            _items.Add(b);
            return b;
        }

        // ------------------------------------------------------------------ TapCollect: tap items into a basket
        private void BuildTapCollect()
        {
            Image tree = UiFactory.Image("Scenery", _area, UiSkin.Picture("tree"), Color.white);
            UiFactory.Place(tree.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-120, 120), new Vector2(760, 760));
            _container = UiFactory.Image("Basket", _area, UiSkin.Picture("draw:bucket"), Color.white);
            UiFactory.Place(_container.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(420, -280), new Vector2(300, 300));
            GlyphGraphic label = UiFactory.Glyph("Label", _container.transform, Letter.upper, Color.white, 0.22f);
            UiFactory.Place(label.rectTransform, new Vector2(0.5f, 0.42f), Vector2.zero, new Vector2(120, 120));
            Sprite prop = Prop();
            for (int i = 0; i < _goal; i++)
            {
                float a = i / (float)_goal * Mathf.PI * 2f + 0.4f;
                Vector2 pos = new Vector2(-120, 200) + new Vector2(Mathf.Cos(a) * 230f, Mathf.Sin(a) * 150f);
                BigButton b = Item("Collect_" + i, prop, pos, 150, Color.white, Collect);
                b.gameObject.AddComponent<Attention>().amount = 0.05f;
            }
            _ready = true;
        }

        private void Collect(BigButton b)
        {
            b.Interactable = false;
            var rt = (RectTransform)b.transform;
            Vector2 from = rt.anchoredPosition, to = _container.rectTransform.anchoredPosition + new Vector2(0, 40);
            Audio.PlaySfx("note_" + Mathf.Min(7, _done), 1f);
            Destroy(b.GetComponent<Attention>());
            Tween.Run(this, 0.55f, k =>
            {
                if (rt == null) return;
                rt.anchoredPosition = Vector2.Lerp(from, to, k) + Vector2.up * Mathf.Sin(k * Mathf.PI) * 160f;
                rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.6f, k);
            }, () => { if (rt != null) rt.gameObject.SetActive(false); Tween.Pop(this, _container.transform, 0.12f, 0.3f); Audio.PlaySfx("drop"); });
            Progressed();
        }

        // ------------------------------------------------------------------ PopTarget: gently floating letter balloons
        private IEnumerator PopTarget()
        {
            _ready = true;
            var palette = new[] { new Color(1f, 0.45f, 0.5f), new Color(0.45f, 0.75f, 1f), new Color(1f, 0.8f, 0.3f), new Color(0.55f, 0.85f, 0.45f), new Color(0.8f, 0.6f, 1f) };
            List<char> others = S.Catalog.letters.Where(l => l != Letter && !Learning.ChoiceGenerator.AreConfusable(l.upper, Letter.upper)).Select(l => l.upper).ToList();
            Sprite balloon = _def != null && _def.propKey == "balloon" ? UiSkin.Picture("draw:balloon") : (Prop() ?? UiSkin.Picture("draw:bubble"));
            bool tintable = _def != null && (_def.propKey == "balloon" || _def.propKey == "bubble");
            if (_def != null && _def.propKey == "bubble") balloon = UiSkin.Picture("draw:bubble");
            int spawned = 0;
            while (_done < _goal)
            {
                int alive = _items.Count(i => i != null && i.Interactable);
                int targetsAlive = _items.Count(i => i != null && i.Interactable && i.name.EndsWith("_T"));
                if (alive < 5)
                {
                    bool isTarget = targetsAlive < 2 || Ctx.Rng.NextDouble() < 0.5;
                    char g = isTarget ? Letter.upper : others[Ctx.Rng.Next(others.Count)];
                    if (isTarget && Ctx.Level >= 1 && Ctx.Rng.NextDouble() < 0.5) g = Letter.lower;
                    Color tint = tintable ? palette[spawned % palette.Length] : Color.white;
                    float x = -480 + (spawned * 233 % 960);
                    BigButton b = Item("Balloon_" + spawned + (isTarget ? "_T" : "_N"), balloon, new Vector2(x, -560), 220, tint, Pop);
                    GlyphGraphic glyph = UiFactory.Glyph("Glyph", b.transform, g, Color.white, 0.22f);
                    UiFactory.Place(glyph.rectTransform, new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(110, 120));
                    if (!tintable) glyph.color = UiFactory.Darken(LetterColor(Ctx), 0.2f);
                    b.gameObject.AddComponent<FloatUp>().Init(new Vector2(x, -560), 70f + Ctx.Rng.Next(30), 620f);
                    spawned++;
                }
                yield return new WaitForSeconds(1.1f);
            }
        }

        private void Pop(BigButton b)
        {
            bool target = b.name.EndsWith("_T");
            if (!target)
            {
                // Not a fail: it wiggles and names itself, then keeps floating.
                Audio.PlaySfx("boing", 0.7f);
                Tween.Wiggle(this, b.transform);
                GlyphGraphic g = b.GetComponentInChildren<GlyphGraphic>();
                LetterDefinition l = g != null ? S.Catalog.Letter(char.ToUpperInvariant(g.Glyph)) : null;
                if (l != null) Audio.Speak(l.nameAudioKey);
                _popWrong++;
                return;
            }
            b.Interactable = false;
            Audio.PlaySfx("pop");
            Audio.PlaySfx("sparkle", 0.5f);
            Transform t = b.transform;
            Tween.Run(this, 0.25f, k => { if (t != null) t.localScale = Vector3.one * (1 + k * 0.5f); }, () => { if (t != null) Destroy(t.gameObject); });
            GlyphGraphic popped = b.GetComponentInChildren<GlyphGraphic>();
            LetterCase popCase = popped != null && char.IsLower(popped.Glyph) ? LetterCase.Lower : LetterCase.Upper;
            // Attempts include the distractors tapped first, so tapping everything is not counted as recognition.
            Progress.RecordRecognition(Letter.id, Type, popCase, true, _popWrong + 1, Learning.Assistance.None);
            _popWrong = 0;
            Progressed();
        }

        // ------------------------------------------------------------------ FindHidden: tap hiding places to discover words
        private void BuildFindHidden()
        {
            string hideKey = _def?.propKey switch
            {
                "egg" => "egg", "shell" => "shell", "rock" => "rock", _ => "draw:bush"
            };
            Sprite hide = UiSkin.Picture(hideKey) ?? UiSkin.Picture("draw:bush");
            List<VocabularyEntry> vocab = Letter.vocabulary.Where(v => v != null).ToList();
            for (int i = 0; i < _goal; i++)
            {
                float x = (i - (_goal - 1) * 0.5f) * 360f;
                VocabularyEntry v = vocab[i % vocab.Count];
                BigButton b = Item("Hide_" + i, hide, new Vector2(x, -120 + (i % 2) * 90), 300, Color.white, h => Reveal(h, v));
                b.gameObject.AddComponent<Attention>().amount = 0.04f;
            }
            if (_def?.propKey == "1f41b" || _def?.archetype == AdventureArchetype.FindHidden && _def.propKey.StartsWith("1f41b"))
            {
                Image cat = UiFactory.Image("Caterpillar", _area, UiSkin.Picture("caterpillar"), Color.white);
                UiFactory.Place(cat.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(480, 300), new Vector2(200, 200));
                cat.gameObject.AddComponent<Bob2D>();
            }
            _ready = true;
        }

        private void Reveal(BigButton hide, VocabularyEntry v)
        {
            hide.Interactable = false;
            Destroy(hide.GetComponent<Attention>());
            var rt = (RectTransform)hide.transform;
            Audio.PlaySfx("reveal");
            Image pic = UiFactory.Image("Found_" + v.id, _area, v.picture, Color.white, true);
            pic.rectTransform.sizeDelta = new Vector2(220, 220);
            pic.rectTransform.anchoredPosition = rt.anchoredPosition;
            pic.transform.SetSiblingIndex(rt.GetSiblingIndex());
            var again = pic.gameObject.AddComponent<BigButton>();
            again.sfx = "pop";
            again.onClick.AddListener(() => { Audio.Speak(v.audioKey); Character(CharacterState.Happy); });
            Vector2 start = rt.anchoredPosition;
            Tween.Run(this, 0.6f, k =>
            {
                if (rt == null) return;
                rt.anchoredPosition = start + new Vector2(k * 140f, -k * 60f);
                rt.localScale = Vector3.one * (1 - k * 0.35f);
                pic.rectTransform.anchoredPosition = start + Vector2.up * Tween.EaseOutBack(k) * 170f;
            });
            Audio.Speak(v.audioKey);
            Progress.RecordVocabulary(Letter.id, v.id, Type);
            Character(CharacterState.Surprised);
            Progressed();
        }

        // ------------------------------------------------------------------ Fill: tap to add water to the bucket
        private void BuildFill()
        {
            _container = UiFactory.Image("Bucket", _area, UiSkin.Picture("draw:bucket"), Color.white);
            UiFactory.Place(_container.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(260, -160), new Vector2(380, 380));
            RectTransform barBg = UiFactory.Rect("Level", _container.transform);
            UiFactory.Place(barBg, new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(170, 180));
            Image water = UiFactory.Image("Water", barBg, _skin.rounded, new Color(0.35f, 0.7f, 1f, 0.9f));
            water.rectTransform.anchorMin = new Vector2(0, 0);
            water.rectTransform.anchorMax = new Vector2(1, 0);
            water.rectTransform.pivot = new Vector2(0.5f, 0);
            water.rectTransform.sizeDelta = new Vector2(0, 0);
            _fillBar = water.rectTransform;
            BigButton drop = Item("WaterDrop", UiSkin.Picture("water"), new Vector2(-260, 140), 260, Color.white, Pour);
            drop.gameObject.AddComponent<Attention>();
            _pourButton = drop; // tapping the character (Ellie's trunk, per the spoken instruction) pours too
            _ready = true;
        }

        private void Pour(BigButton b)
        {
            Image d = UiFactory.Image("Drop", _area, UiSkin.Picture("water"), Color.white);
            d.rectTransform.sizeDelta = new Vector2(110, 110);
            Vector2 from = ((RectTransform)b.transform).anchoredPosition, to = _container.rectTransform.anchoredPosition + new Vector2(0, 120);
            Audio.PlaySfx("splash", 0.6f);
            Character(CharacterState.Interact);
            RectTransform rt = d.rectTransform;
            Tween.Run(this, 0.5f, k => { if (rt != null) rt.anchoredPosition = Vector2.Lerp(from, to, k) + Vector2.up * Mathf.Sin(k * Mathf.PI) * 200f; },
                () => { if (rt != null) Destroy(rt.gameObject); Audio.PlaySfx("note_" + Mathf.Min(7, _done)); });
            Progressed();
            float level = _done / (float)_goal;
            Tween.Run(this, 0.4f, k => { if (_fillBar != null) _fillBar.sizeDelta = new Vector2(0, Mathf.Lerp(_fillBar.sizeDelta.y, 180f * level, k)); });
            if (_done >= _goal) b.Interactable = false;
        }

        // ------------------------------------------------------------------ shared
        private void Progressed()
        {
            _done++;
            if (_done == 1 || _done == _goal) Character(CharacterState.Happy);
            if (_done >= _goal) StartCoroutine(Complete());
        }

        private IEnumerator Complete()
        {
            _ready = false;
            yield return new WaitForSeconds(0.7f);
            Audio.PlaySfx("success");
            Character(CharacterState.Celebrate);
            Progress.RecordAdventureCompleted(Letter.id);
            Audio.Speak(RandomPraise());
            yield return WaitWhileSpeaking();
            Finish(_def != null ? _def.rewardStars : 1);
        }

        public override void ReplayInstruction()
        {
            if (_def != null) Say(_def.instructionAudioKey);
        }

        public override bool OnCharacterTapped()
        {
            if (_pourButton == null || !_ready || IsPaused || _done >= _goal || !_pourButton.Interactable) return false;
            Pour(_pourButton);
            return true;
        }

        public override IEnumerator AutoPlay()
        {
            float t = 0;
            while (!IsFinished && t < 90f)
            {
                if (_ready && _autoStep != null) _autoStep();
                else
                {
                    BigButton next = _items.FirstOrDefault(i => i != null && i.Interactable && i.gameObject.activeInHierarchy &&
                                                                (!i.name.StartsWith("Balloon_") || i.name.EndsWith("_T")));
                    if (next != null && _ready) next.Click();
                }
                yield return new WaitForSeconds(0.6f);
                t += 0.6f;
            }
        }
    }

    /// <summary>Slow upward float that loops back to the bottom, so nothing is ever "missed".</summary>
    public sealed class FloatUp : MonoBehaviour
    {
        private Vector2 _start;
        private float _speed, _top, _phase;
        private RectTransform _rt;

        public void Init(Vector2 start, float speed, float top)
        {
            _start = start; _speed = speed; _top = top; _rt = (RectTransform)transform; _phase = Random.value * 6f;
        }

        private void Update()
        {
            if (_rt == null) return;
            Vector2 p = _rt.anchoredPosition;
            p.y += _speed * Time.deltaTime * Mathf.Max(0.5f, Tween.Motion);
            p.x = _start.x + Mathf.Sin(Time.time * 0.8f + _phase) * 30f * Tween.Motion;
            if (p.y > _top) p.y = _start.y;
            _rt.anchoredPosition = p;
        }
    }
}
