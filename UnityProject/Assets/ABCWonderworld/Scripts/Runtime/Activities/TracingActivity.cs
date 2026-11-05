using System.Collections;
using System.Collections.Generic;
using ABCWonderworld.Audio;
using ABCWonderworld.Characters;
using ABCWonderworld.Content;
using ABCWonderworld.Learning;
using ABCWonderworld.Tracing;
using ABCWonderworld.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ABCWonderworld.Activities
{
    /// <summary>
    /// GDD §6.2 Letter Tracing Studio. Uppercase then lowercase. Progress follows the expected path
    /// (TracingEvaluator), never pixels. Guidance by level: Guided (path, arrows, start dot, demo hand),
    /// Lightly guided (faint path + start dot), Free (outline only). Imperfect tracing is celebrated.
    /// </summary>
    public sealed class TracingActivity : ActivityBase, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public override ActivityType Type => ActivityType.Tracing;

        private RectTransform _pad;
        private GlyphGraphic _guide, _done;
        private LineGraphic _trail;
        private Image _startDot, _arrow, _hand;
        private TracingEvaluator _eval;
        private TracingDefinition _def;
        private LetterCase _case;
        private TraceMode _mode;
        private bool _inputEnabled;
        private float _lastHint = -10f;
        private float _idle;
        private bool _assisted;      // this form: a demo was shown because the child asked or got stuck
        private bool _anyAssisted;   // whole session (for adaptive difficulty)
        private int _struggle;       // whole session: forms with repeated wrong starts / leaving the path
        private bool _demoRequested; // the running demo was asked for (idle help / Replay), not the routine Guided one
        private int _pointer = NoPointer;
        private const int NoPointer = int.MinValue;
        private Coroutine _demo;
        private readonly List<LetterCase> _forms = new List<LetterCase>();
        private int _formIndex;

        protected override void OnBegin()
        {
            _mode = Ctx.Level <= 0 ? TraceMode.Guided : Ctx.Level == 1 ? TraceMode.LightlyGuided : TraceMode.Free;
            _forms.Add(LetterCase.Upper);
            _forms.Add(LetterCase.Lower);
            if (Ctx.Case == LetterCase.Lower) _forms.Reverse();
            BuildUi();
            StartForm(0);
        }

        private void BuildUi()
        {
            UiSkin skin = UiSkin.Load();
            _pad = UiFactory.Rect("TracePad", Ctx.UiRoot);
            UiFactory.Place(_pad, new Vector2(0.62f, 0.5f), Vector2.zero, new Vector2(860, 900));
            Image card = UiFactory.Image("Card", _pad, skin.rounded, new Color(1, 1, 1, 0.94f), true);
            UiFactory.Stretch(card.rectTransform);
            UiFactory.Shadow(card.transform, 860);
            // Input is received by this component via the card (raycast target) bubbling to the pad.
            var forward = card.gameObject.AddComponent<PointerForwarder>();
            forward.target = this;

            _guide = UiFactory.Glyph("Guide", _pad, 'A', new Color(0.85f, 0.85f, 0.9f), 0.2f);
            UiFactory.Stretch(_guide.rectTransform, 40);
            _done = UiFactory.Glyph("Done", _pad, 'A', LetterColor(Ctx), 0.17f);
            UiFactory.Stretch(_done.rectTransform, 40);

            var trailGo = new GameObject("Trail", typeof(RectTransform), typeof(CanvasRenderer), typeof(LineGraphic));
            trailGo.transform.SetParent(_pad, false);
            _trail = trailGo.GetComponent<LineGraphic>();
            _trail.raycastTarget = false;
            _trail.color = new Color(1f, 0.85f, 0.3f, 0.85f);
            _trail.width = 26;
            UiFactory.Stretch(_trail.rectTransform, 40);

            _startDot = UiFactory.Image("StartDot", _pad, skin.circle, new Color(0.3f, 0.82f, 0.4f));
            _startDot.rectTransform.sizeDelta = new Vector2(90, 90);
            _startDot.gameObject.AddComponent<Attention>().amount = 0.15f;
            _arrow = UiFactory.Image("Arrow", _pad, skin.iconArrow, new Color(0.3f, 0.7f, 0.35f, 0.9f));
            _arrow.rectTransform.sizeDelta = new Vector2(80, 80);
            _hand = UiFactory.Image("DemoHand", _pad, skin.iconHand, Color.white);
            _hand.rectTransform.sizeDelta = new Vector2(150, 150);
            _hand.rectTransform.pivot = new Vector2(0.45f, 0.85f);
            _hand.gameObject.SetActive(false);
        }

        private void StartForm(int index)
        {
            _formIndex = index;
            _case = _forms[index];
            _def = Letter.Tracing(_case);
            char glyph = Letter.Glyph(_case);
            float toleranceScale = S.Settings.Profile == Persistence.LearningProfile.Explorer ? 1.25f : 1f;
            if (S.Settings.SimplifiedInteraction) toleranceScale *= 1.25f;
            if (_def != null && _def.strokes.Count > 0)
            {
                _eval = TracingEvaluator.For(_def, _mode, toleranceScale);
                var strokes = new List<Vector2[]>();
                foreach (StrokeData sd in _def.strokes) strokes.Add(sd.points.ToArray());
                _guide.SetStrokes(strokes);
                _done.SetStrokes(strokes);
            }
            else
            {
                // Content validation reports this; fall back to the built-in library so play continues.
                var strokes = LetterStrokeLibrary.Get(glyph);
                _eval = new TracingEvaluator(strokes.ConvertAll(s => (IReadOnlyList<Vector2>)s), 0.16f * toleranceScale, 0.85f, _mode);
                _guide.SetStrokes(strokes);
                _done.SetStrokes(strokes);
            }
            _guide.SetGlyph(glyph); _guide.SetStrokes(_done.Strokes());
            _guide.color = _mode == TraceMode.Free ? new Color(0.9f, 0.9f, 0.94f) : _mode == TraceMode.LightlyGuided ? new Color(0.87f, 0.87f, 0.93f) : new Color(0.82f, 0.84f, 0.92f);
            _guide.Thickness = _mode == TraceMode.Free ? 0.05f : 0.2f;
            _done.SetProgress(0, 0);
            _trail.Clear();
            _assisted = false;
            _pointer = NoPointer;
            StopDemo();
            UpdateMarkers();
            _inputEnabled = true;
            Say(index == 0 ? "vo.trace_intro" : "vo.trace_now_small");
            // The routine Guided demo is part of the mode, so it does not count as assistance.
            if (_mode == TraceMode.Guided) StartDemo(1.2f, requested: false);
        }

        // ------------------------------------------------------------------ input
        public void OnPointerDown(PointerEventData e)
        {
            if (!_inputEnabled || IsPaused || _eval.IsComplete) return;
            if (_pointer != NoPointer || !ABCWonderworld.Input.PointerGate.TryClaim(e)) return; // one finger traces
            StopDemo();
            _idle = 0;
            Vector2 g = ToGlyph(e);
            TraceFeedback f = _eval.Begin(g);
            if (f == TraceFeedback.Rejected)
            {
                Hint("vo.trace_start_here");
                Tween.Pop(this, _startDot.transform, 0.3f, 0.5f);
                return;
            }
            _pointer = e.pointerId;
            _trail.Clear(); // each touch is its own line; never join separate touches
            _trail.Add(ToLocal(e));
            Handle(f);
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != _pointer) return;
            if (!_inputEnabled || IsPaused || _eval.IsComplete) return;
            _idle = 0;
            TraceFeedback f = _eval.Move(ToGlyph(e));
            if (f != TraceFeedback.None) _trail.Add(ToLocal(e)); // only draw where tracing is actually happening
            Handle(f);
        }

        public void OnPointerUp(PointerEventData e)
        {
            ABCWonderworld.Input.PointerGate.Release(e);
            if (e.pointerId != _pointer) return;
            _pointer = NoPointer;
            if (!_inputEnabled || _eval.IsComplete) return;
            Handle(_eval.End());
        }

        private void Handle(TraceFeedback f)
        {
            switch (f)
            {
                case TraceFeedback.OffPath:
                    Hint("vo.trace_keep_going");
                    break;
                case TraceFeedback.StrokeComplete:
                    Audio.PlaySfx("note_" + Mathf.Min(7, _eval.CurrentStroke + 2));
                    Audio.PlaySfx("sparkle", 0.6f);
                    Character(CharacterState.Happy);
                    _trail.Clear();
                    break;
                case TraceFeedback.LetterComplete:
                    StartCoroutine(FormComplete());
                    break;
            }
            _done.SetProgress(_eval.CurrentStroke, _eval.IsComplete ? 0 : _eval.StrokeProgress);
            UpdateMarkers();
        }

        private void Hint(string key)
        {
            if (Time.time - _lastHint < 4f) return;
            _lastHint = Time.time;
            Say(key, VoiceMode.IfIdle);
        }

        private IEnumerator FormComplete()
        {
            _inputEnabled = false;
            _pointer = NoPointer;
            StopDemo();
            _done.ShowAll();
            _trail.Clear();
            UpdateMarkers();
            _anyAssisted |= _assisted;
            if (_eval.RejectedStarts >= 3 || _eval.OffPathEvents >= 3) _struggle++;
            Progress.RecordTracing(Letter.id, _case, true, _assisted ? Assistance.Demonstrated : Assistance.None);
            Audio.PlaySfx("success");
            Character(CharacterState.Celebrate);
            Tween.Pop(this, _done.transform, 0.15f, 0.6f);
            SaySequence(new[] { "vo.trace_great", _case == LetterCase.Upper ? "vo.big" : "vo.small", NameKey(Letter) });
            yield return WaitWhileSpeaking();
            yield return new WaitForSeconds(0.3f);
            if (_formIndex + 1 < _forms.Count) StartForm(_formIndex + 1);
            else
            {
                S.Difficulty.RecordRound(Type, _struggle, _anyAssisted ? Assistance.Demonstrated : Assistance.None);
                Finish(1);
            }
        }

        // ------------------------------------------------------------------ guidance visuals
        private void UpdateMarkers()
        {
            bool show = !_eval.IsComplete;
            _startDot.gameObject.SetActive(show);
            _arrow.gameObject.SetActive(show && _mode != TraceMode.Free);
            if (!show) return;
            _startDot.rectTransform.anchoredPosition = LocalInPad(_eval.NextPoint);
            IReadOnlyList<Vector2> s = _eval.StrokeSamples(_eval.CurrentStroke);
            int i = Mathf.Min(s.Count - 1, Mathf.RoundToInt(_eval.StrokeProgress * (s.Count - 1)) + 4);
            Vector2 dir = s[i] - _eval.NextPoint;
            if (dir.sqrMagnitude < 1e-6f && s.Count > 1) dir = s[s.Count - 1] - s[0];
            _arrow.rectTransform.anchoredPosition = LocalInPad(_eval.NextPoint) + dir.normalized * 90f;
            _arrow.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        }

        private Vector2 LocalInPad(Vector2 glyph)
        {
            // Guide rect is stretched inside the pad with a 40 px inset; its local origin is the pad centre.
            return _guide.GlyphToLocal(glyph);
        }

        private IEnumerator Demo(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (_eval == null || _eval.IsComplete) { _demo = null; yield break; }
            _hand.gameObject.SetActive(true);
            if (_demoRequested) _assisted = true;
            IReadOnlyList<Vector2> s = _eval.StrokeSamples(_eval.CurrentStroke);
            float dur = Mathf.Clamp(s.Count * 0.025f, 0.8f, 2.2f);
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                int i = Mathf.Min(s.Count - 1, (int)(t / dur * s.Count));
                _hand.rectTransform.anchoredPosition = LocalInPad(s[i]);
                yield return null;
            }
            yield return new WaitForSeconds(0.3f);
            _hand.gameObject.SetActive(false);
            _demo = null;
        }

        private void StartDemo(float delay, bool requested)
        {
            StopDemo();
            _demoRequested = requested;
            _demo = StartCoroutine(Demo(delay));
        }

        private void StopDemo()
        {
            if (_demo != null) StopCoroutine(_demo);
            _demo = null;
            _hand.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_inputEnabled || IsPaused || _eval == null || _eval.IsComplete) return;
            _idle += Time.deltaTime;
            // After a quiet spell, show the way again (a demonstration, never a timer or penalty).
            if (_idle > 6f && _demo == null)
            {
                _idle = 0;
                if (_mode != TraceMode.Free) StartDemo(0f, requested: true);
                Hint("vo.trace_start_here");
            }
        }

        private Vector2 ToLocal(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_guide.rectTransform, e.position, e.pressEventCamera, out Vector2 local);
            return local;
        }

        private Vector2 ToGlyph(PointerEventData e) => _guide.LocalToGlyph(ToLocal(e));

        public override void ReplayInstruction()
        {
            Say(_formIndex == 0 ? "vo.trace_intro" : "vo.trace_now_small");
            if (_mode != TraceMode.Free && _demo == null && _inputEnabled) StartDemo(0.2f, requested: true);
        }

        public override void Abort()
        {
            if (!IsFinished && _eval != null && !_eval.IsComplete && _eval.TotalProgress > 0.05f)
                Progress.RecordTracing(Letter.id, _case, false, Assistance.None);
            base.Abort();
        }

        /// <summary>Follows the path of each stroke through the same evaluator the finger uses.</summary>
        public override IEnumerator AutoPlay()
        {
            for (int form = 0; form < 2 && !IsFinished; form++)
            {
                while (!_inputEnabled) yield return null;
                while (!_eval.IsComplete)
                {
                    IReadOnlyList<Vector2> s = _eval.StrokeSamples(_eval.CurrentStroke);
                    Handle(_eval.Begin(s[0]));
                    int stroke = _eval.CurrentStroke;
                    for (int i = 0; i < s.Count && !_eval.IsComplete && _eval.CurrentStroke == stroke; i += 2)
                    {
                        _trail.Add(_guide.GlyphToLocal(s[i]));
                        Handle(_eval.Move(s[i]));
                        yield return null;
                    }
                    if (!_eval.IsComplete && _eval.CurrentStroke == stroke) Handle(_eval.End());
                    yield return new WaitForSeconds(0.2f);
                }
                float w = 0;
                while (_inputEnabled == false && !IsFinished && w < 12f) { w += Time.deltaTime; yield return null; }
            }
        }
    }

    /// <summary>Forwards pointer events from a raycast-target graphic to a handler on another object.</summary>
    public sealed class PointerForwarder : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IBeginDragHandler
    {
        public MonoBehaviour target;
        public void OnPointerDown(PointerEventData e) => (target as IPointerDownHandler)?.OnPointerDown(e);
        public void OnBeginDrag(PointerEventData e) { }
        public void OnDrag(PointerEventData e) => (target as IDragHandler)?.OnDrag(e);
        public void OnPointerUp(PointerEventData e) => (target as IPointerUpHandler)?.OnPointerUp(e);
    }
}
