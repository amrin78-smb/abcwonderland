using System;
using System.Collections.Generic;
using UnityEngine;

namespace ABCWonderworld.EditorTools
{
    /// <summary>
    /// Minimal anti-aliased vector painter based on signed distance functions. Coordinates are normalised
    /// 0..1 (origin bottom-left). Used to draw placeholder icons and pictures deterministically in code.
    /// </summary>
    public sealed class Painter
    {
        public readonly int Size;
        private readonly Color[] _px;
        private readonly float _pixel;

        public Painter(int size)
        {
            Size = size;
            _px = new Color[size * size];
            _pixel = 1f / size;
        }

        public delegate float Sdf(Vector2 p);

        /// <summary>Fills the region where sdf &lt; 0. <paramref name="outline"/> draws a darker rim first.</summary>
        public Painter Fill(Sdf sdf, Color color, float outline = 0f, Color? outlineColor = null)
        {
            if (outline > 0f) Blend(p => sdf(p) - outline, outlineColor ?? new Color(0.2f, 0.15f, 0.25f, 1f));
            Blend(sdf, color);
            return this;
        }

        public Painter Erase(Sdf sdf)
        {
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    var p = new Vector2((x + 0.5f) * _pixel, (y + 0.5f) * _pixel);
                    float cov = Mathf.Clamp01(0.5f - sdf(p) / _pixel);
                    if (cov <= 0) continue;
                    int i = y * Size + x;
                    _px[i].a *= 1 - cov;
                }
            return this;
        }

        private void Blend(Sdf sdf, Color c)
        {
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    var p = new Vector2((x + 0.5f) * _pixel, (y + 0.5f) * _pixel);
                    float cov = Mathf.Clamp01(0.5f - sdf(p) / _pixel) * c.a;
                    if (cov <= 0) continue;
                    int i = y * Size + x;
                    Color d = _px[i];
                    float a = cov + d.a * (1 - cov);
                    if (a <= 0) continue;
                    _px[i] = new Color((c.r * cov + d.r * d.a * (1 - cov)) / a, (c.g * cov + d.g * d.a * (1 - cov)) / a, (c.b * cov + d.b * d.a * (1 - cov)) / a, a);
                }
        }

        /// <summary>Vertical gradient shading inside a shape (adds light at top, shade at bottom).</summary>
        public Painter Shade(Sdf sdf, float top, float bottom, float strength = 0.25f)
        {
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    var p = new Vector2((x + 0.5f) * _pixel, (y + 0.5f) * _pixel);
                    float cov = Mathf.Clamp01(0.5f - sdf(p) / _pixel);
                    if (cov <= 0) continue;
                    int i = y * Size + x;
                    float k = Mathf.InverseLerp(bottom, top, p.y);
                    Color c = _px[i];
                    float a = c.a;
                    c = k > 0.5f ? Color.Lerp(c, Color.white, (k - 0.5f) * 2f * strength * cov) : Color.Lerp(c, Color.black, (0.5f - k) * 2f * strength * cov);
                    c.a = a;
                    _px[i] = c;
                }
            return this;
        }

        public Texture2D ToTexture()
        {
            var t = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            // Premultiplied edges look dark on transparent backgrounds; keep straight alpha but bleed colour.
            t.SetPixels(_px);
            t.Apply();
            return t;
        }

        // ------------------------------------------------------------------ SDF primitives
        public static Sdf Circle(Vector2 c, float r) => p => (p - c).magnitude - r;

        public static Sdf Ellipse(Vector2 c, Vector2 r) => p =>
        {
            Vector2 q = new Vector2((p.x - c.x) / r.x, (p.y - c.y) / r.y);
            float k = q.magnitude;
            return (k - 1f) * Mathf.Min(r.x, r.y);
        };

        public static Sdf Box(Vector2 c, Vector2 half, float radius = 0f) => p =>
        {
            Vector2 d = new Vector2(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - half + Vector2.one * radius;
            return Vector2.Max(d, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0f) - radius;
        };

        public static Sdf Capsule(Vector2 a, Vector2 b, float r) => p =>
        {
            Vector2 pa = p - a, ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Mathf.Max(1e-6f, Vector2.Dot(ba, ba)));
            return (pa - ba * h).magnitude - r;
        };

        public static Sdf Polygon(params Vector2[] v) => p =>
        {
            float d = Vector2.Dot(p - v[0], p - v[0]);
            float s = 1f;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
            {
                Vector2 e = v[j] - v[i], w = p - v[i];
                Vector2 b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                d = Mathf.Min(d, Vector2.Dot(b, b));
                bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        };

        public static Sdf Rounded(Sdf s, float r) => p => s(p) - r;

        /// <summary>Ring arc between angles (degrees, CCW from +x).</summary>
        public static Sdf Arc(Vector2 c, float r, float width, float fromDeg, float toDeg) => p =>
        {
            Vector2 d = p - c;
            float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            float a0 = fromDeg, a1 = toDeg;
            float t = Mathf.Repeat(ang - a0, 360f);
            if (t <= Mathf.Repeat(a1 - a0, 360f)) return Mathf.Abs(d.magnitude - r) - width;
            Vector2 e0 = c + new Vector2(Mathf.Cos(a0 * Mathf.Deg2Rad), Mathf.Sin(a0 * Mathf.Deg2Rad)) * r;
            Vector2 e1 = c + new Vector2(Mathf.Cos(a1 * Mathf.Deg2Rad), Mathf.Sin(a1 * Mathf.Deg2Rad)) * r;
            return Mathf.Min((p - e0).magnitude, (p - e1).magnitude) - width;
        };

        public static Sdf Star(Vector2 c, float outer, float inner, int points = 5, float rotDeg = 90f)
        {
            var v = new Vector2[points * 2];
            for (int i = 0; i < v.Length; i++)
            {
                float a = (rotDeg + i * 180f / points) * Mathf.Deg2Rad;
                float r = i % 2 == 0 ? outer : inner;
                v[i] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            }
            return Polygon(v);
        }

        public static Sdf Union(params Sdf[] s) => p =>
        {
            float d = float.MaxValue;
            foreach (Sdf f in s) d = Mathf.Min(d, f(p));
            return d;
        };

        public static Sdf Subtract(Sdf a, Sdf b) => p => Mathf.Max(a(p), -b(p));
        public static Sdf Intersect(Sdf a, Sdf b) => p => Mathf.Max(a(p), b(p));

        /// <summary>Strokes from the letter library as capsules, mapped into the given rect.</summary>
        public static Sdf Letter(char glyph, Rect into, float thickness)
        {
            List<Vector2[]> strokes = Content.LetterStrokeLibrary.Get(glyph);
            Rect b = Content.LetterStrokeLibrary.Bounds(strokes);
            float s = Mathf.Min(into.width / Mathf.Max(0.3f, b.width), into.height / Mathf.Max(0.3f, b.height));
            Vector2 o = into.center - b.center * s;
            var caps = new List<Sdf>();
            foreach (Vector2[] st in strokes)
                for (int i = 1; i < st.Length; i++) caps.Add(Capsule(o + st[i - 1] * s, o + st[i] * s, thickness));
            return Union(caps.ToArray());
        }
    }
}
