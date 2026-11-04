using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ABCWonderworld.UI
{
    /// <summary>A thick, round-capped polyline in local UI space (finger trails, direction guides).</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LineGraphic : MaskableGraphic
    {
        public float width = 24f;
        private readonly List<Vector2> _points = new List<Vector2>();
        public int Count => _points.Count;

        public void Clear() { _points.Clear(); SetVerticesDirty(); }

        public void Add(Vector2 p, float minSpacing = 6f)
        {
            if (_points.Count > 0 && (_points[_points.Count - 1] - p).sqrMagnitude < minSpacing * minSpacing) return;
            _points.Add(p);
            if (_points.Count > 2000) _points.RemoveAt(0);
            SetVerticesDirty();
        }

        public void SetPoints(IEnumerable<Vector2> pts) { _points.Clear(); _points.AddRange(pts); SetVerticesDirty(); }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            float h = width * 0.5f;
            for (int i = 0; i < _points.Count; i++)
            {
                Disc(vh, _points[i], h);
                if (i == 0) continue;
                Vector2 a = _points[i - 1], b = _points[i];
                Vector2 d = b - a;
                if (d.sqrMagnitude < 1e-4f) continue;
                Vector2 n = new Vector2(-d.y, d.x).normalized * h;
                int s = vh.currentVertCount;
                vh.AddVert(a + n, color, Vector2.zero); vh.AddVert(b + n, color, Vector2.zero);
                vh.AddVert(b - n, color, Vector2.zero); vh.AddVert(a - n, color, Vector2.zero);
                vh.AddTriangle(s, s + 1, s + 2); vh.AddTriangle(s, s + 2, s + 3);
            }
        }

        private void Disc(VertexHelper vh, Vector2 c, float r)
        {
            int s = vh.currentVertCount;
            vh.AddVert(c, color, Vector2.zero);
            const int n = 14;
            for (int k = 0; k <= n; k++)
            {
                float a = k / (float)n * Mathf.PI * 2f;
                vh.AddVert(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, color, Vector2.zero);
                if (k > 0) vh.AddTriangle(s, s + k, s + k + 1);
            }
        }
    }
}
