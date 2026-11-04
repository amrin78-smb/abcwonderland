using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ABCWonderworld.Characters;
using ABCWonderworld.Content;
using ABCWonderworld.Learning;
using ABCWonderworld.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ABCWonderworld.Activities
{
    /// <summary>
    /// GDD §6.7 Alphabet Puzzles. Round 1: letter-shape jigsaw — the glyph is cut into 2 (Explorer), 3 or 4
    /// pieces that snap into a faint outline (drag, or tap a piece and it flies home). Round 2: missing letter in
    /// a short alphabet sequence (e.g. K _ M) chosen from 2–3 large non-confusable options. Reset is always
    /// possible via Again; nothing can be failed.
    /// </summary>
    public sealed class PuzzleActivity : ActivityBase
    {
        public override ActivityType Type => ActivityType.Puzzle;

        private RectTransform _area;
        private int _round;
        private readonly List<(RectTransform rt, Vector2 home, Vector2 target, bool placed)> _pieces = new List<(RectTransform, Vector2, Vector2, bool)>();
        private readonly List<(BigButton b, char g)> _choices = new List<(BigButton, char)>();
        private char _missing;
        private bool _accepting;
        private int _wrong;
        private const float GlyphSize = 560f;

        protected override void OnBegin()
        {
            _area = UiFactory.Rect("PuzzleArea", Ctx.UiRoot);
            UiFactory.Place(_area, new Vector2(0.6f, 0.47f), Vector2.zero, new Vector2(1250, 880));
            Progress.RecordExposure(Letter.id, Type);
            Progress.RecordPractice(Letter.id, Type, Ctx.Case);
            StartJigsaw();
        }

        // ------------------------------------------------------------------ jigsaw
        private void StartJigsaw()
        {
            _round = 0;
            UiSkin skin = UiSkin.Load();
            Color col = LetterColor(Ctx);
            char glyph = Letter.Glyph(Ctx.Level >= 2 ? LetterCase.Lower : Ctx.Case);
            Vector2 center = new Vector2(220, 0);
            Image board = UiFactory.Image("Board", _area, skin.rounded, new Color(1, 1, 1, 0.9f));
            UiFactory.Place(board.rectTransform, new Vector2(0.5f, 0.5f), center, new Vector2(GlyphSize + 80, GlyphSize + 80));
            GlyphGraphic outline = UiFactory.Glyph("Outline", _area, glyph, new Color(0.86f, 0.86f, 0.92f), 0.2f);
            UiFactory.Place(outline.rectTransform, new Vector2(0.5f, 0.5f), center, new Vector2(GlyphSize, GlyphSize));

            int pieces = S.Settings.SimplifiedInteraction ? 2 : Mathf.Clamp(Ctx.Level + 2, 2, 4);
            // Cut lines: 2 = left/right, 3 = three vertical bands, 4 = quadrants.
            var cuts = new List<Rect>();
            if (pieces == 4) { cuts.Add(new Rect(0, 0.5f, 0.5f, 0.5f)); cuts.Add(new Rect(0.5f, 0.5f, 0.5f, 0.5f)); cuts.Add(new Rect(0, 0, 0.5f, 0.5f)); cuts.Add(new Rect(0.5f, 0, 0.5f, 0.5f)); }
            else for (int i = 0; i < pieces; i++) cuts.Add(new Rect(i / (float)pieces, 0, 1f / pieces, 1));

            Vector2[] homes = { new Vector2(-470, 250), new Vector2(-430, -40), new Vector2(-470, -300), new Vector2(-230, 120) };
            var order = Enumerable.Range(0, cuts.Count).OrderBy(_ => Ctx.Rng.Next()).ToList();
            for (int i = 0; i < cuts.Count; i++)
            {
                Rect c = cuts[i];
                Vector2 size = new Vector2(c.width * GlyphSize, c.height * GlyphSize);
                Vector2 target = center + new Vector2((c.center.x - 0.5f) * GlyphSize, (c.center.y - 0.5f) * GlyphSize);
                // A RectMask2D window shows just this piece of a full-size glyph.
                // Each piece is a white tile (so it reads as a puzzle piece) showing its part of the letter.
                Image hit = UiFactory.Image("Piece_" + i, _area, UiSkin.Load().rounded, new Color(1f, 1f, 1f, 0.97f), true);
                hit.type = Image.Type.Sliced;
                hit.gameObject.AddComponent<UnityEngine.UI.Outline>().effectColor = new Color(0.75f, 0.72f, 0.85f, 0.9f);
                hit.rectTransform.sizeDelta = size;
                hit.gameObject.AddComponent<RectMask2D>();
                GlyphGraphic g = UiFactory.Glyph("Glyph", hit.transform, glyph, col, 0.2f);
                g.rectTransform.sizeDelta = new Vector2(GlyphSize, GlyphSize);
                g.rectTransform.anchoredPosition = new Vector2((0.5f - c.center.x) * GlyphSize, (0.5f - c.center.y) * GlyphSize);
                Vector2 home = homes[order[i] % homes.Length];
                hit.rectTransform.anchoredPosition = home;
                hit.rectTransform.localScale = Vector3.one * 0.8f;
                int idx = i;
                _pieces.Add((hit.rectTransform, home, target, false));
                var drag = hit.gameObject.AddComponent<DragPiece>();
                drag.onTap = () => PlacePiece(idx);
                drag.onBegin = () => !_pieces[idx].placed && !IsPaused;
                drag.onDrop = screen =>
                {
                    Vector2 targetScreen = RectTransformUtility.WorldToScreenPoint(null, _area.TransformPoint(_pieces[idx].target));
                    if (Vector2.Distance(screen, targetScreen) < 260f * Ctx.UiRoot.lossyScale.x) PlacePiece(idx);
                    else ReturnPiece(idx);
                };
            }
            Say("vo.puzzle_intro");
        }

        private void PlacePiece(int i)
        {
            var p = _pieces[i];
            if (p.placed) return;
            _pieces[i] = (p.rt, p.home, p.target, true);
            Vector2 from = p.rt.anchoredPosition;
            Audio.PlaySfx("chime", 0.8f);
            Tween.Run(this, 0.35f, k =>
            {
                if (p.rt == null) return;
                p.rt.anchoredPosition = Vector2.Lerp(from, p.target, Tween.EaseOut(k));
                p.rt.localScale = Vector3.one * Mathf.Lerp(0.8f, 1f, k);
            });
            if (_pieces.All(x => x.placed)) StartCoroutine(JigsawDone());
        }

        private void ReturnPiece(int i)
        {
            var p = _pieces[i];
            Vector2 from = p.rt.anchoredPosition;
            Tween.Run(this, 0.3f, k => { if (p.rt != null) p.rt.anchoredPosition = Vector2.Lerp(from, p.home, Tween.EaseOut(k)); });
        }

        private IEnumerator JigsawDone()
        {
            yield return new WaitForSeconds(0.45f);
            Audio.PlaySfx("success");
            Character(CharacterState.Happy);
            SaySequence(new[] { RandomPraise(), NameKey(Letter) });
            yield return WaitWhileSpeaking();
            foreach (Transform c in _area) Destroy(c.gameObject);
            _pieces.Clear();
            StartMissing();
        }

        // ------------------------------------------------------------------ missing letter
        private void StartMissing()
        {
            _round = 1;
            _wrong = 0;
            UiSkin skin = UiSkin.Load();
            bool lower = Ctx.Level >= 2;
            int idx = Letter.Index;
            int len = Ctx.Level == 0 ? 3 : 4;
            int start = Mathf.Clamp(idx - 1, 0, 26 - len);
            _missing = lower ? Letter.lower : Letter.upper;
            for (int i = 0; i < len; i++)
            {
                char c = (char)((lower ? 'a' : 'A') + start + i);
                Vector2 pos = new Vector2((i - (len - 1) * 0.5f) * 230, 200);
                Image card = UiFactory.Image("Seq_" + i, _area, skin.rounded, c == _missing ? new Color(1f, 0.95f, 0.8f) : Color.white);
                card.rectTransform.sizeDelta = new Vector2(200, 230);
                card.rectTransform.anchoredPosition = pos;
                UiFactory.Shadow(card.transform, 200);
                if (c == _missing)
                {
                    Image q = UiFactory.Image("Gap", card.transform, skin.ring, new Color(1f, 0.7f, 0.3f));
                    UiFactory.Stretch(q.rectTransform, 40);
                    q.gameObject.AddComponent<Attention>().amount = 0.08f;
                    card.name = "Gap";
                }
                else
                {
                    GlyphGraphic g = UiFactory.Glyph("Glyph", card.transform, c, S.Catalog.CharacterFor(S.Catalog.Letter(char.ToUpperInvariant(c)))?.primaryColor ?? skin.ink, 0.2f);
                    UiFactory.Stretch(g.rectTransform, 30);
                }
            }
            int choices = Ctx.Level == 0 || S.Settings.SimplifiedInteraction ? 2 : 3;
            List<char> opts = ChoiceGenerator.Generate(_missing, choices, Ctx.Rng, false);
            for (int i = 0; i < opts.Count; i++)
            {
                char g = opts[i];
                BigButton b = UiFactory.IconButton("Choice_" + g, _area, null, new Color(0.55f, 0.75f, 1f), 230, null);
                UiFactory.Place((RectTransform)b.transform, new Vector2(0.5f, 0.5f), new Vector2((i - (opts.Count - 1) * 0.5f) * 300, -230), new Vector2(230, 230));
                GlyphGraphic gg = UiFactory.Glyph("Glyph", b.transform, g, Color.white, 0.21f);
                UiFactory.Stretch(gg.rectTransform, 40);
                b.sfx = null;
                b.onClick.AddListener(() => Choose(b, g));
                _choices.Add((b, g));
            }
            Say("vo.puzzle_missing");
            _accepting = true;
        }

        private void Choose(BigButton b, char g)
        {
            if (!_accepting) return;
            if (g != _missing)
            {
                _wrong++;
                Audio.PlaySfx("gentle");
                Tween.Wiggle(this, b.transform);
                LetterDefinition chosen = S.Catalog.Letter(char.ToUpperInvariant(g));
                SaySequence(new[] { chosen?.nameAudioKey, "vo.try_another" }.Where(k => k != null));
                if (_wrong >= 2) _choices.First(c => c.g == _missing).b.gameObject.AddComponent<Attention>().amount = 0.12f;
                return;
            }
            _accepting = false;
            StartCoroutine(MissingDone(b));
        }

        private IEnumerator MissingDone(BigButton b)
        {
            Progress.RecordRecognition(Letter.id, Type, char.IsUpper(_missing) ? LetterCase.Upper : LetterCase.Lower, true, _wrong + 1,
                _wrong >= 2 ? Assistance.Hint : Assistance.None);
            S.Difficulty.RecordRound(Type, _wrong, Assistance.None);
            Transform gap = _area.Find("Gap");
            Vector3 from = b.transform.position;
            Audio.PlaySfx("chime");
            if (gap != null)
                yield return Tween.Run(this, 0.4f, k => { if (b != null) b.transform.position = Vector3.Lerp(from, gap.position, Tween.EaseOut(k)); });
            Character(CharacterState.Celebrate);
            SaySequence(new[] { "vo.you_found_it", NameKey(Letter) });
            yield return WaitWhileSpeaking();
            Finish(1);
        }

        public override void ReplayInstruction() => Say(_round == 0 ? "vo.puzzle_intro" : "vo.puzzle_missing");

        public override IEnumerator AutoPlay()
        {
            int guard = 0;
            while (!IsFinished && guard++ < 200)
            {
                if (_round == 0)
                {
                    int i = _pieces.FindIndex(p => !p.placed);
                    if (i >= 0) PlacePiece(i);
                }
                else if (_accepting)
                {
                    var wrong = _choices.FirstOrDefault(c => c.g != _missing);
                    if (_wrong == 0 && wrong.b != null) { wrong.b.Click(); yield return new WaitForSeconds(0.8f); }
                    _choices.First(c => c.g == _missing).b.Click();
                }
                yield return new WaitForSeconds(0.5f);
            }
        }
    }
}
