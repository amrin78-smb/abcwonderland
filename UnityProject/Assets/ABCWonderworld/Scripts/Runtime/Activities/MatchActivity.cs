using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ABCWonderworld.Characters;
using ABCWonderworld.Content;
using ABCWonderworld.Learning;
using ABCWonderworld.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ABCWonderworld.Activities
{
    /// <summary>
    /// GDD §6.5 Match the Objects. Round 1: picture → letter (vocabulary association). Round 2: uppercase →
    /// lowercase. Pieces can be dragged onto a target OR tapped and then the target tapped, so younger
    /// children are not blocked by fine-motor demands. Generous snap zones; a mismatch just floats back.
    /// </summary>
    public sealed class MatchActivity : ActivityBase
    {
        public override ActivityType Type => ActivityType.Match;

        private enum Mode { PictureToLetter, UpperToLower }

        private sealed class Piece
        {
            public RectTransform rt;
            public string key;
            public Vector2 home;
            public bool matched;
            public LetterDefinition letter;
        }

        private readonly List<Piece> _sources = new List<Piece>();
        private readonly List<Piece> _targets = new List<Piece>();
        private Piece _selected;
        private RectTransform _area;
        private Mode _mode;
        private int _round;
        private int _wrongThisRound;
        private bool _busy;

        protected override void OnBegin()
        {
            _area = UiFactory.Rect("MatchArea", Ctx.UiRoot);
            UiFactory.Place(_area, new Vector2(0.62f, 0.47f), Vector2.zero, new Vector2(1150, 860));
            Progress.RecordExposure(Letter.id, Type);
            // Picture round only for letters with a true beginner-sound word (X has none: "fox" ends in x).
            StartRound(HasBeginnerWord(Letter) ? 0 : 1);
        }

        private static bool HasBeginnerWord(LetterDefinition l) =>
            l != null && l.vocabulary != null && l.vocabulary.Any(v => v != null && v.startsWithBeginnerSound && v.picture != null);

        private void StartRound(int round)
        {
            _round = round;
            _wrongThisRound = 0;
            foreach (Piece p in _sources.Concat(_targets)) if (p.rt != null) Destroy(p.rt.gameObject);
            _sources.Clear();
            _targets.Clear();
            _selected = null;
            _mode = round == 0 ? Mode.PictureToLetter : Mode.UpperToLower;
            int level = S.Difficulty.LevelFor(Type);
            int pairs = Mathf.Clamp(2 + level, 2, 4);
            if (S.Settings.SimplifiedInteraction) pairs = 2;

            // Letters: the focus letter plus visually distinct neighbours from the same world (or the alphabet).
            bool pictures = _mode == Mode.PictureToLetter;
            var pool = (Ctx.World != null ? S.Catalog.LettersIn(Ctx.World) : S.Catalog.letters)
                .Where(l => l != Letter && !ChoiceGenerator.AreConfusable(l.upper, Letter.upper) && (!pictures || HasBeginnerWord(l))).ToList();
            if (pool.Count < pairs - 1) pool = S.Catalog.letters.Where(l => l != Letter && (!pictures || HasBeginnerWord(l))).ToList();
            var letters = new List<LetterDefinition> { Letter };
            letters.AddRange(pool.OrderBy(_ => Ctx.Rng.Next()).Take(pairs - 1));
            Progress.RecordPractice(Letter.id, Type, _mode == Mode.UpperToLower ? LetterCase.Lower : (LetterCase?)null);

            UiSkin skin = UiSkin.Load();
            var srcOrder = letters.OrderBy(_ => Ctx.Rng.Next()).ToList();
            var tgtOrder = letters.OrderBy(_ => Ctx.Rng.Next()).ToList();
            float spacing = 820f / Mathf.Max(1, pairs - 1);
            for (int i = 0; i < pairs; i++)
            {
                float y = pairs == 1 ? 0 : 410 - i * spacing;
                _sources.Add(MakeSource(srcOrder[i], new Vector2(-330, y * 0.85f), skin));
                _targets.Add(MakeTarget(tgtOrder[i], new Vector2(330, y * 0.85f), skin));
            }
            ReplayInstruction();
        }

        private Piece MakeSource(LetterDefinition l, Vector2 pos, UiSkin skin)
        {
            float size = 230;
            Image card = UiFactory.Image("Source_" + l.upper, _area, skin.rounded, Color.white, true);
            card.rectTransform.sizeDelta = new Vector2(size, size);
            card.rectTransform.anchoredPosition = pos;
            card.raycastPadding = new Vector4(-14, -14, -14, -14);
            UiFactory.Shadow(card.transform, size);
            if (_mode == Mode.PictureToLetter)
            {
                // Never fall back to a non-beginner word: the picture is presented as starting with this letter.
                VocabularyEntry v = l.vocabulary.FirstOrDefault(x => x != null && x.startsWithBeginnerSound && x.picture != null);
                Image pic = UiFactory.Image("Picture", card.transform, v?.picture, Color.white);
                UiFactory.Stretch(pic.rectTransform, 26);
                var piece = new Piece { rt = card.rectTransform, key = l.id, home = pos, letter = l };
                AddDrag(card.gameObject, piece, v?.audioKey);
                return piece;
            }
            GlyphGraphic g = UiFactory.Glyph("Glyph", card.transform, l.upper, LetterColor(l), 0.19f);
            UiFactory.Stretch(g.rectTransform, 30);
            var p2 = new Piece { rt = card.rectTransform, key = l.id, home = pos, letter = l };
            AddDrag(card.gameObject, p2, l.nameAudioKey);
            return p2;
        }

        private Piece MakeTarget(LetterDefinition l, Vector2 pos, UiSkin skin)
        {
            float size = 250;
            Image slot = UiFactory.Image("Target_" + l.upper, _area, skin.rounded, new Color(1f, 0.97f, 0.88f), true);
            slot.rectTransform.sizeDelta = new Vector2(size, size);
            slot.rectTransform.anchoredPosition = pos;
            slot.raycastPadding = new Vector4(-14, -14, -14, -14);
            Image ring = UiFactory.Image("Dash", slot.transform, skin.rounded, new Color(0.75f, 0.7f, 0.85f, 0.6f));
            UiFactory.Stretch(ring.rectTransform, -6);
            ring.transform.SetAsFirstSibling();
            char glyph = _mode == Mode.UpperToLower ? l.lower : l.upper;
            GlyphGraphic g = UiFactory.Glyph("Glyph", slot.transform, glyph, UiFactory.Darken(LetterColor(l), 0.1f), 0.19f);
            UiFactory.Stretch(g.rectTransform, 34);
            var piece = new Piece { rt = slot.rectTransform, key = l.id, home = pos, letter = l };
            var b = slot.gameObject.AddComponent<BigButton>();
            b.sfx = null;
            b.onClick.AddListener(() => TapTarget(piece));
            return piece;
        }

        private Color LetterColor(LetterDefinition l) => S.Catalog.CharacterFor(l)?.primaryColor ?? new Color(1f, 0.5f, 0.3f);

        private void AddDrag(GameObject go, Piece p, string audioKey)
        {
            var d = go.AddComponent<DragPiece>();
            d.onTap = () => SelectSource(p, audioKey);
            d.onBegin = () => { if (_busy || p.matched) return false; Select(p); Audio.Speak(audioKey); return true; };
            d.onDrop = screen => DropSource(p, screen);
        }

        private void SelectSource(Piece p, string audioKey)
        {
            if (_busy || p.matched || IsPaused) return;
            Audio.PlaySfx("tap");
            Audio.Speak(audioKey);
            Select(p);
        }

        private void Select(Piece p)
        {
            if (_selected != null && _selected.rt != null) _selected.rt.localScale = Vector3.one;
            _selected = p;
            p.rt.localScale = Vector3.one * 1.1f;
            p.rt.SetAsLastSibling();
        }

        private void TapTarget(Piece target)
        {
            if (_busy || IsPaused) return;
            if (_selected == null || _selected.matched)
            {
                Audio.Speak(_mode == Mode.UpperToLower ? target.letter.nameAudioKey : "vo.match_tip");
                Tween.Pop(this, target.rt, 0.1f, 0.3f);
                return;
            }
            StartCoroutine(TryMatch(_selected, target));
        }

        private void DropSource(Piece p, Vector2 screenPos)
        {
            if (p.matched) return; // already matched (e.g. by a tap while this drag was in flight)
            if (_busy) { ReturnHome(p); return; }
            Piece best = null;
            float bestD = float.MaxValue;
            foreach (Piece t in _targets)
            {
                if (t.matched) continue;
                float d = Vector2.Distance(RectTransformUtility.WorldToScreenPoint(null, t.rt.position), screenPos);
                if (d < bestD) { bestD = d; best = t; }
            }
            // Generous snap: anywhere within ~1.3 card widths of a target counts as aiming at it.
            float snap = 1.3f * 250f * Ctx.UiRoot.lossyScale.x;
            if (best != null && bestD < snap) StartCoroutine(TryMatch(p, best));
            else ReturnHome(p);
        }

        private IEnumerator TryMatch(Piece src, Piece target)
        {
            _busy = true;
            if (src.key == target.key && !target.matched)
            {
                src.matched = target.matched = true;
                _selected = null;
                Vector2 from = src.rt.anchoredPosition;
                Vector2 to = target.rt.anchoredPosition;
                yield return Tween.Run(this, 0.25f, k => src.rt.anchoredPosition = Vector2.Lerp(from, to, Tween.EaseOut(k)));
                src.rt.SetAsLastSibling();
                src.rt.localScale = Vector3.one * 0.55f;
                src.rt.anchoredPosition = to + new Vector2(95, 95);
                Audio.PlaySfx("chime");
                Character(CharacterState.Happy);
                Audio.Speak(src.letter.nameAudioKey);
                if (src.letter == Letter)
                    Progress.RecordRecognition(Letter.id, Type, _mode == Mode.UpperToLower ? LetterCase.Lower : LetterCase.Upper, true, _wrongThisRound + 1, Assistance.None);
                if (_mode == Mode.PictureToLetter)
                {
                    VocabularyEntry v = src.letter.vocabulary.FirstOrDefault(x => x != null && x.startsWithBeginnerSound);
                    if (v != null && src.letter == Letter) Progress.RecordVocabulary(Letter.id, v.id, Type);
                }
                Tween.Pop(this, target.rt, 0.15f, 0.4f);
                if (_targets.All(t => t.matched))
                {
                    S.Difficulty.RecordRound(Type, _wrongThisRound, Assistance.None);
                    yield return new WaitForSeconds(0.6f);
                    Audio.Speak(RandomPraise());
                    yield return WaitWhileSpeaking();
                    if (_round == 0) StartRound(1);
                    else Finish(1);
                }
            }
            else
            {
                _wrongThisRound++;
                Audio.PlaySfx("gentle");
                Tween.Wiggle(this, target.rt);
                ReturnHome(src);
                if (_wrongThisRound % 3 == 0) Audio.Speak("vo.try_another");
            }
            _busy = false;
        }

        private void ReturnHome(Piece p)
        {
            Vector2 from = p.rt.anchoredPosition;
            Tween.Run(this, 0.3f, k => { if (p.rt != null) p.rt.anchoredPosition = Vector2.Lerp(from, p.home, Tween.EaseOut(k)); });
            p.rt.localScale = Vector3.one;
            if (_selected == p) _selected = null;
        }

        public override void ReplayInstruction()
        {
            SaySequence(new[] { _mode == Mode.UpperToLower ? "vo.match_case" : "vo.match_pictures", "vo.match_tip" });
        }

        public override IEnumerator AutoPlay()
        {
            int guard = 0;
            while (!IsFinished && guard++ < 50)
            {
                while (_busy) yield return null;
                Piece src = _sources.FirstOrDefault(s => !s.matched);
                if (src == null) { yield return new WaitForSeconds(0.3f); continue; }
                Piece wrong = _targets.FirstOrDefault(t => !t.matched && t.key != src.key);
                Piece right = _targets.First(t => t.key == src.key);
                if (_round == 0 && guard == 1 && wrong != null)
                {
                    SelectSource(src, null);
                    TapTarget(wrong); // tap-to-match path, deliberately mismatched once
                    yield return new WaitForSeconds(0.6f);
                }
                SelectSource(src, null);
                TapTarget(right);
                yield return new WaitForSeconds(0.7f);
            }
        }
    }

    /// <summary>Drag with tap fallback. A press without movement is a tap.</summary>
    public sealed class DragPiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public System.Action onTap;
        public System.Func<bool> onBegin;
        public System.Action<Vector2> onDrop;
        private bool _dragging;
        private int _pointer;
        private Vector2 _grabOffset;

        public void OnPointerClick(PointerEventData e)
        {
            if (e.dragging || !ABCWonderworld.Input.PointerGate.Allows(e)) return;
            onTap?.Invoke();
        }

        public void OnBeginDrag(PointerEventData e)
        {
            if (_dragging || !ABCWonderworld.Input.PointerGate.TryClaim(e)) return; // one piece, one finger
            _dragging = onBegin == null || onBegin();
            if (!_dragging) { ABCWonderworld.Input.PointerGate.Release(e); return; }
            _pointer = e.pointerId;
            var rt = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rt.parent, e.position, e.pressEventCamera, out Vector2 local);
            _grabOffset = rt.anchoredPosition - local;
        }

        public void OnDrag(PointerEventData e)
        {
            if (!_dragging || e.pointerId != _pointer) return;
            var rt = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rt.parent, e.position, e.pressEventCamera, out Vector2 local);
            rt.anchoredPosition = local + _grabOffset;
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (!_dragging || e.pointerId != _pointer) return;
            _dragging = false;
            ABCWonderworld.Input.PointerGate.Release(e);
            onDrop?.Invoke(e.position);
        }
    }
}
