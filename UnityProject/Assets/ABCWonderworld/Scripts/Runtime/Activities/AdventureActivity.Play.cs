using System.Collections.Generic;
using System.Linq;
using ABCWonderworld.Characters;
using ABCWonderworld.Content;
using ABCWonderworld.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ABCWonderworld.Activities
{
    public sealed partial class AdventureActivity
    {
        // ------------------------------------------------------------------ PathSteps (P: Poppy slides along the ice)
        private RectTransform _walker;
        private readonly List<BigButton> _steps = new List<BigButton>();
        private int _stepIndex;

        private void BuildPathSteps()
        {
            int n = _goal;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                Vector2 pos = new Vector2(Mathf.Lerp(-480, 480, t), Mathf.Sin(t * Mathf.PI * 1.5f) * 160f - 120f);
                int idx = i;
                BigButton b = Item("Ice_" + i, UiSkin.Picture("draw:ice"), pos, 210, Color.white, _ => Step(idx));
                b.transform.SetAsFirstSibling();
                _steps.Add(b);
            }
            Image p = UiFactory.Image("Walker", _area, Prop(), Color.white);
            _walker = p.rectTransform;
            _walker.sizeDelta = new Vector2(190, 190);
            _walker.anchoredPosition = ((RectTransform)_steps[0].transform).anchoredPosition + new Vector2(0, 80);
            _stepIndex = 0;
            Glow(1);
            _autoStep = () => { if (_stepIndex + 1 < _steps.Count) { _steps[_stepIndex + 1].Click(); return true; } return false; };
            _ready = true;
        }

        private void Glow(int i)
        {
            foreach (BigButton b in _steps) { var a = b.GetComponent<Attention>(); if (a != null) a.enabled = false; }
            if (i < _steps.Count) Attention.Ensure(_steps[i].gameObject, 0.1f);
        }

        private void Step(int idx)
        {
            if (idx != _stepIndex + 1)
            {
                // Not the next tile: point the way gently instead of moving far.
                if (idx > _stepIndex + 1) Tween.Pop(this, _steps[_stepIndex + 1].transform, 0.15f, 0.4f);
                return;
            }
            _stepIndex = idx;
            Vector2 from = _walker.anchoredPosition;
            Vector2 to = ((RectTransform)_steps[idx].transform).anchoredPosition + new Vector2(0, 80);
            Audio.PlaySfx("whoosh", 0.6f);
            Tween.Run(this, 0.6f, k => { if (_walker != null) _walker.anchoredPosition = Vector2.Lerp(from, to, Tween.EaseInOut(k)) + Vector2.up * Mathf.Sin(k * Mathf.PI) * 50f; });
            Glow(idx + 1);
            // Every other tile reveals a P word.
            List<VocabularyEntry> vocab = Letter.vocabulary.Where(v => v != null).ToList();
            if (idx % 2 == 1 && vocab.Count > 0)
            {
                VocabularyEntry v = vocab[(idx / 2) % vocab.Count];
                Image pic = UiFactory.Image("Found_" + v.id, _area, v.picture, Color.white);
                pic.rectTransform.sizeDelta = new Vector2(160, 160);
                pic.rectTransform.anchoredPosition = to + new Vector2(0, 230);
                pic.transform.localScale = Vector3.zero;
                Transform t = pic.transform;
                Tween.Run(this, 0.4f, k => { if (t != null) t.localScale = Vector3.one * Tween.EaseOutBack(k); });
                Audio.Speak(v.audioKey, ABCWonderworld.Audio.VoiceMode.Queue);
                Progress.RecordVocabulary(Letter.id, v.id, Type);
            }
            Progressed();
        }

        // ------------------------------------------------------------------ Pattern (T: AB colours on the shell; Z: paint the stripes)
        private readonly List<Image> _spots = new List<Image>();
        private Color[] _patternColors;
        private int _nextSpot;
        private bool _stripes;

        private void BuildPattern()
        {
            _stripes = _def != null && _def.propKey == "stripe";
            if (_stripes)
            {
                Image zebra = UiFactory.Image("Zebra", _area, UiSkin.Picture("draw:zebra_plain"), Color.white);
                UiFactory.Place(zebra.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(60, 0), new Vector2(760, 760));
                for (int i = 0; i < _goal; i++)
                {
                    Image s = UiFactory.Image("Stripe_" + i, _area, _skin.rounded, new Color(0.85f, 0.85f, 0.88f), true);
                    s.rectTransform.sizeDelta = new Vector2(34, 200);
                    s.rectTransform.anchoredPosition = new Vector2(60, 0) + new Vector2(-150 + i * 72, -30);
                    s.rectTransform.localRotation = Quaternion.Euler(0, 0, 12);
                    s.raycastPadding = new Vector4(-30, -20, -30, -20);
                    int idx = i;
                    var b = s.gameObject.AddComponent<BigButton>();
                    b.sfx = null;
                    b.onClick.AddListener(() => PaintStripe(idx));
                    _spots.Add(s);
                }
                _patternColors = new[] { new Color(0.15f, 0.15f, 0.2f) };
                _autoStep = () => { int i = _spots.FindIndex(x => x.color != _patternColors[0]); if (i < 0) return false; PaintStripe(i); return true; };
            }
            else
            {
                Image turtle = UiFactory.Image("Turtle", _area, UiSkin.Picture("draw:turtle_plain"), Color.white);
                UiFactory.Place(turtle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(60, 60), new Vector2(760, 760));
                _patternColors = new[] { new Color(1f, 0.55f, 0.3f), new Color(0.4f, 0.7f, 1f) };
                int total = _goal + 2; // two filled examples show the AB pattern
                for (int i = 0; i < total; i++)
                {
                    float t = (i + 0.5f) / total;
                    // Spots as big and as far apart as the shell allows; any unpainted spot counts as "the next one".
                    Vector2 pos = new Vector2(60, 60) + new Vector2(Mathf.Lerp(-250, 250, t), 10 + Mathf.Sin(t * Mathf.PI) * 70f);
                    Image s = UiFactory.Image("Spot_" + i, _area, _skin.circle, i < 2 ? _patternColors[i % 2] : new Color(0.75f, 0.85f, 0.7f), true);
                    s.rectTransform.sizeDelta = new Vector2(104, 104);
                    s.raycastPadding = new Vector4(-6, -30, -6, -30);
                    s.rectTransform.anchoredPosition = pos;
                    int idx = i;
                    var b = s.gameObject.AddComponent<BigButton>();
                    b.sfx = null;
                    b.onClick.AddListener(() => TapSpot(idx));
                    _spots.Add(s);
                }
                _nextSpot = 2;
                // Palette: from Discoverer level the child chooses the colour; Explorers just tap the glowing spot.
                if (Ctx.Level >= 1 && !S.Settings.SimplifiedInteraction)
                {
                    for (int c = 0; c < 2; c++)
                    {
                        int ci = c;
                        BigButton pb = UiFactory.IconButton("Paint_" + c, _area, c == 0 ? _skin.star : _skin.circle, _patternColors[c], 170, () => PaintNext(ci));
                        UiFactory.Place((RectTransform)pb.transform, new Vector2(0.5f, 0.5f), new Vector2(-520, 120 - c * 230), new Vector2(170, 170));
                        Attention.Ensure(pb.gameObject, 0.05f);
                        _paints.Add(pb);
                    }
                }
                GlowSpot();
                _autoStep = () => { if (_nextSpot >= _spots.Count) return false; if (Ctx.Level >= 1 && !S.Settings.SimplifiedInteraction) PaintNext(_nextSpot % 2); else TapSpot(_nextSpot); return true; };
            }
            _ready = true;
        }

        private void PaintStripe(int i)
        {
            if (!_ready || _spots[i].color == _patternColors[0]) return;
            _spots[i].color = _patternColors[0];
            Audio.PlaySfx("note_" + Mathf.Min(7, i));
            Tween.Pop(this, _spots[i].transform, 0.2f, 0.35f);
            Progressed();
        }

        private void GlowSpot()
        {
            foreach (Image s in _spots) { var a = s.GetComponent<Attention>(); if (a != null) a.enabled = false; }
            if (_nextSpot < _spots.Count) Attention.Ensure(_spots[_nextSpot].gameObject, 0.12f);
        }

        private readonly List<BigButton> _paints = new List<BigButton>();

        private void TapSpot(int i)
        {
            // Painted spots and the examples do nothing; any unpainted spot means "paint the next one".
            if (!_ready || i < _nextSpot || _nextSpot >= _spots.Count) return;
            if (_paints.Count == 2)
            {
                // Choosing the colour is the child's job here: point at the right paint pot, never "wrong".
                Audio.PlaySfx("tap");
                Tween.Pop(this, _paints[_nextSpot % 2].transform, 0.25f, 0.5f);
                Tween.Pop(this, _spots[_nextSpot].transform, 0.15f, 0.35f);
                return;
            }
            PaintNext(_nextSpot % 2);
        }

        private void PaintNext(int colorIndex)
        {
            if (!_ready || _nextSpot >= _spots.Count) return;
            int expected = _nextSpot % 2;
            if (colorIndex != expected)
            {
                Audio.PlaySfx("gentle");
                Tween.Wiggle(this, _spots[_nextSpot].transform);
                return;
            }
            Image s = _spots[_nextSpot];
            s.color = _patternColors[expected];
            Audio.PlaySfx("note_" + Mathf.Min(7, _nextSpot));
            Tween.Pop(this, s.transform, 0.2f, 0.35f);
            _nextSpot++;
            GlowSpot();
            Progressed();
        }

        // ------------------------------------------------------------------ Music (X: follow the glowing bar)
        private readonly List<Image> _bars = new List<Image>();
        private int[] _melody;
        private int _melodyIndex;
        private static readonly Color[] BarColors =
        {
            new Color(0.95f, 0.3f, 0.3f), new Color(1f, 0.6f, 0.2f), new Color(1f, 0.85f, 0.25f), new Color(0.4f, 0.8f, 0.4f),
            new Color(0.3f, 0.75f, 0.85f), new Color(0.3f, 0.55f, 0.95f), new Color(0.6f, 0.45f, 0.9f), new Color(0.9f, 0.45f, 0.75f)
        };

        private void BuildMusic()
        {
            Image frame = UiFactory.Image("Frame", _area, _skin.rounded, new Color(0.6f, 0.42f, 0.28f));
            UiFactory.Place(frame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(80, -60), new Vector2(1080, 90));
            for (int i = 0; i < 8; i++)
            {
                Image bar = UiFactory.Image("Bar_" + i, _area, _skin.rounded, BarColors[i], true);
                bar.rectTransform.sizeDelta = new Vector2(105, 520 - i * 36);
                bar.rectTransform.anchoredPosition = new Vector2(-380 + i * 130 + 80, -60);
                bar.raycastPadding = new Vector4(-10, -30, -10, -30);
                int idx = i;
                var b = bar.gameObject.AddComponent<BigButton>();
                b.sfx = null;
                b.cooldown = 0.08f;
                b.onClick.AddListener(() => PlayBar(idx));
                _bars.Add(bar);
                _items.Add(b);
            }
            int[] tune = { 0, 2, 4, 2, 4, 5, 7, 4 };
            _melody = tune.Take(Mathf.Clamp(_goal, 3, tune.Length)).ToArray();
            _melodyIndex = 0;
            GlowBar();
            _autoStep = () => { if (_melodyIndex >= _melody.Length) return false; PlayBar(_melody[_melodyIndex]); return true; };
            _ready = true;
        }

        private void GlowBar()
        {
            foreach (Image b in _bars) { var a = b.GetComponent<Attention>(); if (a != null) a.enabled = false; }
            if (_melodyIndex < _melody.Length) Attention.Ensure(_bars[_melody[_melodyIndex]].gameObject, 0.07f);
        }

        private void PlayBar(int i)
        {
            if (!_ready) return;
            Audio.PlaySfx("note_" + i);
            Tween.Pop(this, _bars[i].transform, 0.1f, 0.25f);
            Ctx.CharacterInstance?.Play(CharacterState.Dance);
            // Every tap makes music; following the glowing bar moves the tune along.
            if (_melodyIndex < _melody.Length && i == _melody[_melodyIndex])
            {
                _melodyIndex++;
                GlowBar();
                Progressed();
            }
        }

        // ------------------------------------------------------------------ Swipe (Y: yo-yo)
        private RectTransform _yoyo;
        private bool _swinging;

        private void BuildSwipe()
        {
            Image hand = UiFactory.Image("Holder", _area, _skin.rounded, new Color(0.55f, 0.4f, 0.3f));
            UiFactory.Place(hand.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(150, 380), new Vector2(220, 40));
            Image str = UiFactory.Image("String", _area, _skin.rounded, new Color(0.4f, 0.4f, 0.45f));
            str.rectTransform.pivot = new Vector2(0.5f, 1f);
            UiFactory.Place(str.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(150, 370), new Vector2(8, 120));
            str.rectTransform.pivot = new Vector2(0.5f, 1f);
            str.rectTransform.anchoredPosition = new Vector2(150, 370);
            Image yo = UiFactory.Image("YoYo", _area, Prop(), Color.white, true);
            yo.raycastPadding = new Vector4(-40, -40, -40, -40);
            _yoyo = yo.rectTransform;
            _yoyo.sizeDelta = new Vector2(200, 200);
            _yoyo.anchoredPosition = new Vector2(150, 220);
            var swipe = yo.gameObject.AddComponent<SwipeOrTap>();
            swipe.onGesture = () => Swing(str.rectTransform);
            yo.gameObject.AddComponent<Attention>().amount = 0.05f;
            // Visual hint: an arrow pointing down (no hidden gestures: a tap works too).
            Image arrow = UiFactory.Image("Hint", _area, _skin.iconArrow, new Color(1f, 1f, 1f, 0.9f));
            UiFactory.Place(arrow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-40, 150), new Vector2(110, 110));
            arrow.rectTransform.localRotation = Quaternion.Euler(0, 0, -90);
            arrow.gameObject.AddComponent<Bob2D>();
            _autoStep = () => { if (_swinging) return true; Swing(str.rectTransform); return true; };
            _ready = true;
        }

        private void Swing(RectTransform str)
        {
            if (!_ready || _swinging || IsPaused) return;
            _swinging = true;
            Audio.PlaySfx("whoosh", 0.7f);
            int n = _done;
            Tween.Run(this, 0.9f, k =>
            {
                float d = Mathf.Sin(k * Mathf.PI) * 520f;
                if (_yoyo != null) { _yoyo.anchoredPosition = new Vector2(150, 220 - d); _yoyo.localRotation = Quaternion.Euler(0, 0, -k * 720f); }
                if (str != null) str.sizeDelta = new Vector2(8, 120 + d);
            }, () =>
            {
                _swinging = false;
                Audio.PlaySfx("note_" + Mathf.Min(7, n + 2));
                List<VocabularyEntry> vocab = Letter.vocabulary.Where(v => v != null).ToList();
                if (vocab.Count > 0 && n % 2 == 0)
                {
                    VocabularyEntry v = vocab[(n / 2) % vocab.Count];
                    Image pic = UiFactory.Image("Found_" + v.id, _area, v.picture, Color.white);
                    pic.rectTransform.sizeDelta = new Vector2(170, 170);
                    pic.rectTransform.anchoredPosition = new Vector2(-460 + (n / 2) * 230, -300);
                    Audio.Speak(v.audioKey, ABCWonderworld.Audio.VoiceMode.IfIdle);
                    Progress.RecordVocabulary(Letter.id, v.id, Type);
                }
                Progressed();
            });
        }
    }

    /// <summary>Downward swipe or tap: both trigger (no hidden gestures).</summary>
    public sealed class SwipeOrTap : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public System.Action onGesture;
        private Vector2 _start;
        public void OnPointerClick(PointerEventData e) { if (!e.dragging) onGesture?.Invoke(); }
        public void OnBeginDrag(PointerEventData e) => _start = e.position;
        public void OnDrag(PointerEventData e) { }
        public void OnEndDrag(PointerEventData e) { if (_start.y - e.position.y > 40f || (e.position - _start).magnitude > 60f) onGesture?.Invoke(); }
    }
}
