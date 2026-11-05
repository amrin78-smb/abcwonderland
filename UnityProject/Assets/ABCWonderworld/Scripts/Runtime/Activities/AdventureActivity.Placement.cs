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
    /// <summary>Placement archetypes. Every drag can also be done by tapping (tap piece → it flies to a valid spot).</summary>
    public sealed partial class AdventureActivity
    {
        private sealed class Slot
        {
            public RectTransform rt;
            public string key;
            public int capacity = 1;
            public int filled;
            public bool Free => filled < capacity;
        }

        private sealed class Piece
        {
            public RectTransform rt;
            public string key;
            public Vector2 home;
            public bool placed;
        }

        private readonly List<Slot> _slots = new List<Slot>();
        private readonly List<Piece> _pieces = new List<Piece>();
        private Piece _selectedPiece;
        private bool _selectThenTap; // MatchPairs: choose piece, then target (no auto-solve on tap)

        private static readonly Color[] PairColors =
            { new Color(1f, 0.5f, 0.6f), new Color(0.5f, 0.8f, 1f), new Color(1f, 0.85f, 0.35f), new Color(0.6f, 0.88f, 0.5f), new Color(0.8f, 0.6f, 1f) };

        private Sprite[] PairMarks => new[] { _skin.star, _skin.circle, _skin.ring, _skin.iconPlay, _skin.iconMusic };

        private Slot AddSlot(string name, Sprite sprite, Vector2 pos, Vector2 size, string key, Color tint, int capacity = 1)
        {
            Image img = UiFactory.Image(name, _area, sprite, tint, true);
            img.rectTransform.sizeDelta = size;
            img.rectTransform.anchoredPosition = pos;
            var slot = new Slot { rt = img.rectTransform, key = key, capacity = capacity };
            var b = img.gameObject.AddComponent<BigButton>();
            b.sfx = null;
            b.onClick.AddListener(() => TapSlot(slot));
            _slots.Add(slot);
            return slot;
        }

        private Piece AddPiece(string name, Sprite sprite, Vector2 pos, float size, string key, Color tint)
        {
            Image img = UiFactory.Image(name, _area, sprite, tint, true);
            img.rectTransform.sizeDelta = new Vector2(size, size);
            img.rectTransform.anchoredPosition = pos;
            img.raycastPadding = new Vector4(-20, -20, -20, -20);
            var piece = new Piece { rt = img.rectTransform, key = key, home = pos };
            var drag = img.gameObject.AddComponent<DragPiece>();
            drag.onTap = () => TapPiece(piece);
            drag.onBegin = () =>
            {
                if (!_ready || piece.placed || IsPaused) return false;
                Audio.PlaySfx("tap");
                piece.rt.SetAsLastSibling();
                return true;
            };
            drag.onDrop = screen => DropPiece(piece, screen);
            img.gameObject.AddComponent<Attention>().amount = 0.04f;
            _pieces.Add(piece);
            return piece;
        }

        private Slot BestSlotFor(Piece p) => _slots.FirstOrDefault(s => s.Free && (s.key == null || s.key == p.key));

        private void TapPiece(Piece p)
        {
            if (!_ready || p.placed || IsPaused) return;
            Audio.PlaySfx("tap");
            if (_selectThenTap)
            {
                Deselect();
                _selectedPiece = p;
                // The idle pulse would overwrite the "picked up" scale every frame, so pause it while selected.
                if (p.rt.TryGetComponent(out Attention at)) at.enabled = false;
                p.rt.localScale = Vector3.one * 1.15f;
                p.rt.SetAsLastSibling();
                return;
            }
            Slot s = BestSlotFor(p);
            if (s != null) Place(p, s);
        }

        private void TapSlot(Slot s)
        {
            if (!_ready || IsPaused) return;
            if (_selectThenTap && _selectedPiece != null && !_selectedPiece.placed)
            {
                Piece p = _selectedPiece;
                if (s.Free && (s.key == null || s.key == p.key)) Place(p, s);
                else Mismatch(p, s);
                return;
            }
            Tween.Pop(this, s.rt, 0.08f, 0.3f);
        }

        private void DropPiece(Piece p, Vector2 screen)
        {
            if (p.placed) return;
            Slot best = null;
            float bestD = float.MaxValue;
            foreach (Slot s in _slots)
            {
                if (!s.Free) continue;
                float d = Vector2.Distance(RectTransformUtility.WorldToScreenPoint(null, s.rt.position), screen);
                if (d < bestD) { bestD = d; best = s; }
            }
            float snap = Mathf.Max(220f, (best != null ? best.rt.rect.width : 200f) * 0.9f) * Ctx.UiRoot.lossyScale.x;
            if (best != null && bestD < snap && (best.key == null || best.key == p.key)) Place(p, best);
            else if (best != null && bestD < snap) Mismatch(p, best);
            else Return(p);
        }

        private void Mismatch(Piece p, Slot s)
        {
            Audio.PlaySfx("gentle");
            Tween.Wiggle(this, s.rt);
            Return(p);
        }

        private void Return(Piece p)
        {
            Vector2 from = p.rt.anchoredPosition;
            if (_selectedPiece == p) Deselect();
            p.rt.localScale = Vector3.one;
            if (!p.placed && p.rt.TryGetComponent(out Attention at)) at.enabled = true;
            Tween.Run(this, 0.3f, k => { if (p.rt != null) p.rt.anchoredPosition = Vector2.Lerp(from, p.home, Tween.EaseOut(k)); });
        }

        private void Deselect()
        {
            Piece prev = _selectedPiece;
            _selectedPiece = null;
            if (prev == null || prev.rt == null) return;
            prev.rt.localScale = Vector3.one;
            if (!prev.placed && prev.rt.TryGetComponent(out Attention at)) at.enabled = true;
        }

        private System.Action<Piece, Slot> _onPlaced;
        /// <summary>Where a placed piece rests relative to its slot centre (e.g. a scoop sits on top of its cone).</summary>
        private System.Func<Piece, Slot, Vector2> _placeOffset;

        private void Place(Piece p, Slot s)
        {
            p.placed = true;
            s.filled++;
            if (_selectedPiece == p) _selectedPiece = null;
            Destroy(p.rt.GetComponent<Attention>());
            Vector2 from = p.rt.anchoredPosition;
            Vector2 to = s.rt.anchoredPosition + (s.capacity > 1 ? new Vector2((s.filled - (s.capacity + 1) * 0.5f) * 70f, 10f) : Vector2.zero)
                         + (_placeOffset?.Invoke(p, s) ?? Vector2.zero);
            p.rt.SetAsLastSibling();
            p.rt.localScale = Vector3.one;
            Audio.PlaySfx("chime", 0.8f);
            Tween.Run(this, 0.35f, k => { if (p.rt != null) p.rt.anchoredPosition = Vector2.Lerp(from, to, Tween.EaseOut(k)) + Vector2.up * Mathf.Sin(k * Mathf.PI) * 60f; },
                () => { if (s.rt != null) Tween.Pop(this, s.rt, 0.1f, 0.3f); });
            _onPlaced?.Invoke(p, s);
            Progressed();
        }

        /// <summary>AutoPlay step for placement archetypes: one tap-path placement (select-then-tap when required).</summary>
        private bool AutoPlace()
        {
            Piece p = _pieces.FirstOrDefault(x => !x.placed);
            if (p == null) return false;
            if (_selectThenTap)
            {
                TapPiece(p);
                Slot s = BestSlotFor(p);
                if (s != null) TapSlot(s);
            }
            else TapPiece(p);
            return true;
        }

        // ------------------------------------------------------------------ DragToTarget (F fish → bubbles, N eggs → nest)
        private void BuildDragToTarget()
        {
            Sprite piece = Prop();
            bool nest = _def != null && _def.propKey == "egg";
            if (nest)
            {
                AddSlot("Nest", UiSkin.Picture("1faba"), new Vector2(330, -120), new Vector2(420, 420), null, Color.white, _goal);
            }
            else
            {
                for (int i = 0; i < _goal; i++)
                {
                    Slot s = AddSlot("Bubble_" + i, UiSkin.Picture("draw:bubble"), new Vector2(360, 260 - i * 260), new Vector2(240, 240), null, Color.white);
                    s.rt.gameObject.AddComponent<Bob2D>();
                }
            }
            for (int i = 0; i < _goal; i++)
                AddPiece("Piece_" + i, piece, new Vector2(-380 + (i % 2) * 120, 260 - i * 250), 190, "any", Color.white);
            _onPlaced = (p, s) => { if (_done == 0) Audio.Speak(Letter.vocabulary.First().audioKey, ABCWonderworld.Audio.VoiceMode.IfIdle); };
            _autoStep = AutoPlace;
            _ready = true;
        }

        // ------------------------------------------------------------------ PlaceParts (H house parts, L lion mane)
        private void BuildPlaceParts()
        {
            if (_def != null && _def.propKey == "mane")
            {
                Image face = UiFactory.Image("LionFace", _area, UiSkin.Picture("draw:lion_face"), Color.white);
                UiFactory.Place(face.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(250, 0), new Vector2(420, 420));
                for (int i = 0; i < _goal; i++)
                {
                    float a = Mathf.PI * 0.5f + (i - (_goal - 1) * 0.5f) * 0.75f;
                    Slot s = AddSlot("ManeSlot_" + i, UiSkin.Picture("draw:mane"), new Vector2(250, 0) + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 230f,
                        new Vector2(170, 170), null, new Color(0.85f, 0.85f, 0.9f, 0.6f));
                    s.rt.SetAsFirstSibling();
                }
                face.transform.SetAsLastSibling();
                for (int i = 0; i < _goal; i++)
                    AddPiece("Mane_" + i, UiSkin.Picture("draw:mane"), new Vector2(-460, 280 - i * 190), 160, "mane", Color.white);
                foreach (Slot s in _slots) s.key = "mane";
            }
            else
            {
                // House: wall is fixed, roof/door/window slots show faint silhouettes.
                Image wall = UiFactory.Image("Wall", _area, UiSkin.Picture("draw:wall"), Color.white);
                UiFactory.Place(wall.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(260, -170), new Vector2(460, 460));
                (string key, Vector2 pos, Vector2 size)[] parts =
                {
                    ("roof", new Vector2(260, 160), new Vector2(520, 360)),
                    ("door", new Vector2(170, -250), new Vector2(330, 330)),
                    ("window", new Vector2(370, -120), new Vector2(230, 230))
                };
                Color ghost = new Color(0.3f, 0.3f, 0.4f, 0.25f);
                foreach (var p in parts.Take(Mathf.Max(1, Mathf.Min(_goal, parts.Length))))
                    AddSlot("Slot_" + p.key, UiSkin.Picture("draw:" + p.key), p.pos, p.size, p.key, ghost);
                Vector2[] homes = { new Vector2(-460, 250), new Vector2(-480, -50), new Vector2(-300, -280) };
                int i = 0;
                foreach (var p in parts.Take(_slots.Count))
                {
                    Piece pc = AddPiece("Part_" + p.key, UiSkin.Picture("draw:" + p.key), homes[i++], 200, p.key, Color.white);
                    pc.rt.sizeDelta = p.size * 0.85f;
                }
                _goal = _slots.Count;
                _onPlaced = (pc, s) => { pc.rt.sizeDelta = s.rt.sizeDelta; s.rt.GetComponent<Image>().color = Color.clear; };
            }
            _autoStep = AutoPlace;
            _ready = true;
        }

        // ------------------------------------------------------------------ MatchPairs (I scoop → cone, Q crown → cushion)
        private void BuildMatchPairs()
        {
            _selectThenTap = Ctx.Level >= 1 && !S.Settings.SimplifiedInteraction;
            bool crowns = _def != null && _def.propKey == "1f451";
            Sprite source = UiSkin.Picture(crowns ? "draw:crown" : "draw:scoop");
            Sprite target = UiSkin.Picture(crowns ? "draw:cushion" : "draw:cone");
            int n = Mathf.Clamp(_goal, 2, 5);
            var order = Enumerable.Range(0, n).OrderBy(_ => Ctx.Rng.Next()).ToList();
            for (int i = 0; i < n; i++)
            {
                int c = order[i];
                // Targets carry their pair colour (the instruction says "same colour"), plus a shape mark.
                Color tint = crowns ? PairColors[c] : Color.Lerp(PairColors[c], Color.white, 0.25f);
                Slot s = AddSlot("Target_" + c, target, new Vector2((i - (n - 1) * 0.5f) * 270, -230), new Vector2(230, 230), "pair" + c, tint);
                Mark(s.rt, c, crowns ? new Vector2(0, -20) : new Vector2(0, 20));
            }
            for (int i = 0; i < n; i++)
            {
                Piece p = AddPiece("Source_" + i, source, new Vector2((i - (n - 1) * 0.5f) * 270, 220), 190, "pair" + i, PairColors[i]);
                Mark(p.rt, i, Vector2.zero);
            }
            _goal = n;
            _placeOffset = (p, s) => new Vector2(0, crowns ? 70 : 110);
            _autoStep = AutoPlace;
            _ready = true;
        }

        /// <summary>Shape mark so pairs never rely on colour alone (GDD §15).</summary>
        private void Mark(RectTransform parent, int i, Vector2 offset)
        {
            Image m = UiFactory.Image("Mark", parent, PairMarks[i % PairMarks.Length], new Color(1, 1, 1, 0.95f));
            UiFactory.Place(m.rectTransform, new Vector2(0.5f, 0.5f), offset, new Vector2(64, 64));
            var o = m.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0.3f, 0.25f, 0.4f, 0.8f);
            o.effectDistance = new Vector2(3, -3);
        }

        // ------------------------------------------------------------------ DragCollect (K kite + stars, U umbrella + raindrops)
        private RectTransform _catcher;
        private readonly List<RectTransform> _collectibles = new List<RectTransform>();
        private bool _horizontalOnly;
        private Vector2? _catcherGoal;

        private void BuildDragCollect()
        {
            _horizontalOnly = _def != null && _def.propKey == "2602";
            Image catcher = UiFactory.Image("Catcher", _area, Prop(), Color.white, true);
            catcher.raycastPadding = new Vector4(-30, -30, -30, -30);
            _catcher = catcher.rectTransform;
            _catcher.sizeDelta = new Vector2(230, 230);
            _catcher.anchoredPosition = _horizontalOnly ? new Vector2(0, -330) : new Vector2(-350, 0);
            var drag = catcher.gameObject.AddComponent<FreeDrag>();
            drag.horizontalOnly = _horizontalOnly;
            drag.onMoved = () => _catcherGoal = null;

            // Tapping anywhere in the play area glides the catcher there (tap alternative to dragging).
            Image pad = UiFactory.Image("TapPad", _area, null, new Color(0, 0, 0, 0), true);
            UiFactory.Stretch(pad.rectTransform);
            pad.transform.SetAsFirstSibling();
            var tap = pad.gameObject.AddComponent<PointerForwarderTap>();
            tap.onTap = e =>
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, e.position, e.pressEventCamera, out Vector2 local);
                _catcherGoal = _horizontalOnly ? new Vector2(local.x, _catcher.anchoredPosition.y) : local;
            };
            Sprite item = UiSkin.Picture(_horizontalOnly ? "draw:raindrop" : "star");
            for (int i = 0; i < 3; i++) SpawnCollectible(item, i);
            _autoStep = () =>
            {
                RectTransform c = _collectibles.FirstOrDefault(x => x != null);
                if (c == null) return false;
                _catcherGoal = _horizontalOnly ? new Vector2(c.anchoredPosition.x, _catcher.anchoredPosition.y) : c.anchoredPosition;
                return true;
            };
            _ready = true;
        }

        private void SpawnCollectible(Sprite s, int i)
        {
            Image img = UiFactory.Image("Collect_" + i, _area, s, _horizontalOnly ? new Color(0.55f, 0.8f, 1f) : Color.white);
            img.rectTransform.sizeDelta = new Vector2(110, 110);
            float x = Ctx.Rng.Next(-520, 520);
            img.rectTransform.anchoredPosition = _horizontalOnly ? new Vector2(x, 460 + i * 260) : new Vector2(Ctx.Rng.Next(-150, 560), Ctx.Rng.Next(-330, 330));
            if (!_horizontalOnly) img.gameObject.AddComponent<Bob2D>();
            _collectibles.Add(img.rectTransform);
        }

        private void UpdateDragCollect()
        {
            if (_catcher == null || !_ready) return;
            if (_catcherGoal.HasValue)
            {
                _catcher.anchoredPosition = Vector2.MoveTowards(_catcher.anchoredPosition, _catcherGoal.Value, 900f * Time.deltaTime);
                if ((_catcher.anchoredPosition - _catcherGoal.Value).sqrMagnitude < 4f) _catcherGoal = null;
            }
            for (int i = 0; i < _collectibles.Count; i++)
            {
                RectTransform c = _collectibles[i];
                if (c == null) continue;
                if (_horizontalOnly)
                {
                    // Gentle rain: slow fall, loops back to the top, never "missed".
                    Vector2 p = c.anchoredPosition + Vector2.down * 110f * Mathf.Max(0.5f, Tween.Motion) * Time.deltaTime;
                    if (p.y < -460) p = new Vector2(Ctx.Rng.Next(-520, 520), 460);
                    c.anchoredPosition = p;
                }
                if (Vector2.Distance(c.anchoredPosition, _catcher.anchoredPosition) < 150f)
                {
                    _collectibles[i] = null;
                    Audio.PlaySfx("note_" + Mathf.Min(7, _done));
                    Audio.PlaySfx("sparkle", 0.5f);
                    RectTransform rt = c;
                    Tween.Run(this, 0.25f, k => { if (rt != null) rt.localScale = Vector3.one * (1 - k); }, () => { if (rt != null) Destroy(rt.gameObject); });
                    Tween.Pop(this, _catcher, 0.1f, 0.3f);
                    Progressed();
                    if (_done + _collectibles.Count(x => x != null) < _goal)
                        SpawnCollectible(UiSkin.Picture(_horizontalOnly ? "draw:raindrop" : "star"), _done + 3);
                }
            }
        }

        private void Update()
        {
            if (IsPaused) return;
            UpdateDragCollect();
        }
    }

    /// <summary>Drag a rect freely (or horizontally) inside its parent.</summary>
    public sealed class FreeDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public bool horizontalOnly;
        public System.Action onMoved;
        private Vector2 _offset;
        private int _pointer = int.MinValue;

        public void OnBeginDrag(PointerEventData e)
        {
            if (!ABCWonderworld.Input.PointerGate.TryClaim(e)) return;
            _pointer = e.pointerId;
            var rt = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rt.parent, e.position, e.pressEventCamera, out Vector2 l);
            _offset = rt.anchoredPosition - l;
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (e.pointerId != _pointer) return;
            _pointer = int.MinValue;
            ABCWonderworld.Input.PointerGate.Release(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != _pointer) return;
            var rt = (RectTransform)transform;
            var parent = (RectTransform)rt.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, e.position, e.pressEventCamera, out Vector2 l);
            Vector2 p = l + _offset;
            Rect r = parent.rect;
            p.x = Mathf.Clamp(p.x, r.xMin + 80, r.xMax - 80);
            p.y = horizontalOnly ? rt.anchoredPosition.y : Mathf.Clamp(p.y, r.yMin + 80, r.yMax - 80);
            rt.anchoredPosition = p;
            onMoved?.Invoke();
        }
    }

    /// <summary>Raises a tap (click without drag) with the event data.</summary>
    public sealed class PointerForwarderTap : MonoBehaviour, IPointerClickHandler
    {
        public System.Action<PointerEventData> onTap;
        public void OnPointerClick(PointerEventData e) { if (!e.dragging) onTap?.Invoke(e); }
    }
}
