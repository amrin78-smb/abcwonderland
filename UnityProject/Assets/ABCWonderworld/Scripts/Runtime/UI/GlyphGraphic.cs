using System.Collections.Generic;
using ABCWonderworld.Content;
using UnityEngine;
using UnityEngine.UI;

namespace ABCWonderworld.UI
{
    /// <summary>
    /// Renders a letter from <see cref="LetterStrokeLibrary"/> (or a TracingDefinition) as thick round-capped
    /// strokes, so on-screen letters always match the forms children trace. Optional partial rendering
    /// (completed strokes + progress of the current one) is used by the tracing studio.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class GlyphGraphic : MaskableGraphic
    {
        [SerializeField] private char glyph = 'A';
        [SerializeField, Range(0.02f, 0.4f)] private float thickness = 0.16f;
        [SerializeField] private float padding = 0.12f;
        [Tooltip("Strokes fully drawn; -1 = all.")]
        [SerializeField] private int strokesShown = -1;
        [SerializeField, Range(0, 1)] private float partialProgress;
        [SerializeField] private bool fitToCapHeight = true;

        private List<Vector2[]> _strokes;
        private const int CapSegments = 10;

        public char Glyph => glyph;
        public float Thickness { get => thickness; set { thickness = value; SetVerticesDirty(); } }

        public void SetGlyph(char c)
        {
            glyph = c;
            _strokes = null;
            SetVerticesDirty();
        }

        public void SetStrokes(List<Vector2[]> strokes)
        {
            _strokes = strokes;
            SetVerticesDirty();
        }

        public void SetProgress(int completedStrokes, float currentProgress)
        {
            strokesShown = completedStrokes;
            partialProgress = Mathf.Clamp01(currentProgress);
            SetVerticesDirty();
        }

        public void ShowAll()
        {
            strokesShown = -1;
            SetVerticesDirty();
        }

        /// <summary>Uniform glyph-space → local rect mapping (shared with the tracing input).</summary>
        public void GetMapping(out Vector2 origin, out float scale)
        {
            Rect r = GetPixelAdjustedRect();
            List<Vector2[]> strokes = Strokes();
            Rect b = LetterStrokeLibrary.Bounds(strokes);
            // Vertical frame: cap/ascender line to descender (so x-height letters sit on a common baseline).
            float top = 1f, bottom = fitToCapHeight ? LetterStrokeLibrary.Descender : b.yMin;
            if (!fitToCapHeight) top = b.yMax;
            float frameH = top - bottom + padding * 2;
            float frameW = Mathf.Max(b.width, 0.4f) + padding * 2;
            scale = Mathf.Min(r.width / frameW, r.height / frameH);
            float cx = r.center.x - (b.center.x) * scale;
            float baseY = r.center.y - ((top + bottom) * 0.5f) * scale;
            origin = new Vector2(cx, baseY);
        }

        public Vector2 GlyphToLocal(Vector2 g)
        {
            GetMapping(out Vector2 o, out float s);
            return o + g * s;
        }

        public Vector2 LocalToGlyph(Vector2 local)
        {
            GetMapping(out Vector2 o, out float s);
            return s <= 0 ? Vector2.zero : (local - o) / s;
        }

        public List<Vector2[]> Strokes() => _strokes ??= LetterStrokeLibrary.Get(glyph);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            List<Vector2[]> strokes = Strokes();
            if (strokes == null || strokes.Count == 0) return;
            GetMapping(out Vector2 o, out float s);
            float half = thickness * s * 0.5f;
            int full = strokesShown < 0 ? strokes.Count : Mathf.Min(strokesShown, strokes.Count);
            for (int i = 0; i < full; i++) AddPolyline(vh, strokes[i], o, s, half, 1f);
            if (strokesShown >= 0 && full < strokes.Count && partialProgress > 0f)
                AddPolyline(vh, strokes[full], o, s, half, partialProgress);
        }

        private void AddPolyline(VertexHelper vh, Vector2[] pts, Vector2 o, float s, float half, float fraction)
        {
            float total = LetterStrokeLibrary.PathLength(pts);
            float remaining = total * fraction;
            Vector2 prev = o + pts[0] * s;
            AddDisc(vh, prev, half);
            for (int i = 1; i < pts.Length && remaining > 0f; i++)
            {
                float seg = Vector2.Distance(pts[i - 1], pts[i]);
                Vector2 a = o + pts[i - 1] * s;
                Vector2 b = o + pts[i] * s;
                if (seg > remaining && seg > 0) b = Vector2.Lerp(a, b, remaining / seg);
                remaining -= seg;
                AddSegment(vh, a, b, half);
                AddDisc(vh, b, half);
            }
        }

        private void AddSegment(VertexHelper vh, Vector2 a, Vector2 b, float half)
        {
            Vector2 d = b - a;
            if (d.sqrMagnitude < 1e-6f) return;
            Vector2 n = new Vector2(-d.y, d.x).normalized * half;
            int i = vh.currentVertCount;
            vh.AddVert(a + n, color, Vector2.zero);
            vh.AddVert(b + n, color, Vector2.zero);
            vh.AddVert(b - n, color, Vector2.zero);
            vh.AddVert(a - n, color, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }

        private void AddDisc(VertexHelper vh, Vector2 c, float r)
        {
            int start = vh.currentVertCount;
            vh.AddVert(c, color, Vector2.zero);
            for (int k = 0; k <= CapSegments * 2; k++)
            {
                float a = k / (float)(CapSegments * 2) * Mathf.PI * 2f;
                vh.AddVert(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, color, Vector2.zero);
                if (k > 0) vh.AddTriangle(start, start + k, start + k + 1);
            }
        }
    }
}
