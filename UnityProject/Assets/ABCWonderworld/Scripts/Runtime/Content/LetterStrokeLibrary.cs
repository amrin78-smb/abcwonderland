using System.Collections.Generic;
using UnityEngine;

namespace ABCWonderworld.Content
{
    /// <summary>
    /// Ball-and-stick manuscript stroke paths for A–Z upper and lower case, in stroke order.
    /// Glyph space: baseline y = 0, cap/ascender height y = 1, x-height y = 0.62, descender y = -0.38.
    /// Uses the single-storey 'a' and 'g' children are taught to write.
    /// Stroke order and direction follow common US preschool manuscript conventions (top-to-bottom,
    /// left-to-right, circles counter-clockwise from "2 o'clock"); they are PENDING expert review
    /// and are baked into editable TracingDefinition assets by the setup pipeline.
    /// This one source drives tracing paths, on-screen glyph rendering and character body meshes.
    /// </summary>
    public static class LetterStrokeLibrary
    {
        public const float XHeight = 0.62f;
        public const float Descender = -0.38f;
        private const float H = XHeight;

        public static List<Vector2[]> Get(char glyph)
        {
            switch (glyph)
            {
                // ---------------------------------------------------------------- uppercase
                case 'A': return L(Line(0.35f, 1, 0, 0), Line(0.35f, 1, 0.7f, 0), Line(0.13f, 0.36f, 0.57f, 0.36f));
                case 'B': return L(Line(0, 1, 0, 0),
                    Join(Line(0, 1, 0.3f, 1), Arc(0.3f, 0.75f, 0.24f, 0.25f, 90, -90), Line(0.3f, 0.5f, 0, 0.5f)),
                    Join(Line(0, 0.5f, 0.34f, 0.5f), Arc(0.34f, 0.25f, 0.26f, 0.25f, 90, -90), Line(0.34f, 0, 0, 0)));
                case 'C': return L(Arc(0.47f, 0.5f, 0.47f, 0.5f, 45, 315));
                case 'D': return L(Line(0, 1, 0, 0), Join(Line(0, 1, 0.25f, 1), Arc(0.25f, 0.5f, 0.45f, 0.5f, 90, -90), Line(0.25f, 0, 0, 0)));
                case 'E': return L(Line(0, 1, 0, 0), Line(0, 1, 0.58f, 1), Line(0, 0.5f, 0.48f, 0.5f), Line(0, 0, 0.58f, 0));
                case 'F': return L(Line(0, 1, 0, 0), Line(0, 1, 0.58f, 1), Line(0, 0.5f, 0.48f, 0.5f));
                case 'G': return L(Arc(0.48f, 0.5f, 0.48f, 0.5f, 45, 360), Line(0.6f, 0.48f, 0.96f, 0.48f));
                case 'H': return L(Line(0, 1, 0, 0), Line(0.66f, 1, 0.66f, 0), Line(0, 0.5f, 0.66f, 0.5f));
                case 'I': return L(Line(0.22f, 1, 0.22f, 0), Line(0, 1, 0.44f, 1), Line(0, 0, 0.44f, 0));
                case 'J': return L(Join(Line(0.52f, 1, 0.52f, 0.27f), Arc(0.26f, 0.27f, 0.26f, 0.27f, 0, -180)));
                case 'K': return L(Line(0, 1, 0, 0), Line(0.6f, 1, 0, 0.4f), Line(0.16f, 0.56f, 0.62f, 0));
                case 'L': return L(Poly(0, 1, 0, 0, 0.56f, 0));
                case 'M': return L(Line(0, 1, 0, 0), Poly(0, 1, 0.41f, 0.3f, 0.82f, 1), Line(0.82f, 1, 0.82f, 0));
                case 'N': return L(Line(0, 1, 0, 0), Line(0, 1, 0.66f, 0), Line(0.66f, 0, 0.66f, 1));
                case 'O': return L(Arc(0.5f, 0.5f, 0.5f, 0.5f, 90, 450));
                case 'P': return L(Line(0, 1, 0, 0), Join(Line(0, 1, 0.3f, 1), Arc(0.3f, 0.75f, 0.26f, 0.25f, 90, -90), Line(0.3f, 0.5f, 0, 0.5f)));
                case 'Q': return L(Arc(0.5f, 0.5f, 0.5f, 0.5f, 90, 450), Line(0.58f, 0.28f, 0.98f, -0.06f));
                case 'R': return L(Line(0, 1, 0, 0), Join(Line(0, 1, 0.3f, 1), Arc(0.3f, 0.75f, 0.26f, 0.25f, 90, -90), Line(0.3f, 0.5f, 0, 0.5f)),
                    Line(0.26f, 0.5f, 0.6f, 0));
                case 'S': return L(Join(Arc(0.32f, 0.75f, 0.3f, 0.25f, 30, 270), Arc(0.32f, 0.25f, 0.32f, 0.25f, 90, -150)));
                case 'T': return L(Line(0, 1, 0.62f, 1), Line(0.31f, 1, 0.31f, 0));
                case 'U': return L(Join(Line(0, 1, 0, 0.32f), Arc(0.31f, 0.32f, 0.31f, 0.32f, 180, 360), Line(0.62f, 0.32f, 0.62f, 1)));
                case 'V': return L(Poly(0, 1, 0.34f, 0, 0.68f, 1));
                case 'W': return L(Poly(0, 1, 0.21f, 0, 0.46f, 0.72f, 0.71f, 0, 0.92f, 1));
                case 'X': return L(Line(0, 1, 0.62f, 0), Line(0.62f, 1, 0, 0));
                case 'Y': return L(Line(0, 1, 0.33f, 0.5f), Line(0.66f, 1, 0.33f, 0.5f), Line(0.33f, 0.5f, 0.33f, 0));
                case 'Z': return L(Poly(0, 1, 0.62f, 1, 0, 0, 0.62f, 0));

                // ---------------------------------------------------------------- lowercase
                case 'a': return L(Arc(0.26f, H / 2, 0.26f, H / 2, 30, 390), Line(0.53f, H, 0.53f, 0));
                case 'b': return L(Line(0, 1, 0, 0), Arc(0.26f, H / 2, 0.26f, H / 2, 180, -180));
                case 'c': return L(Arc(0.28f, H / 2, 0.28f, H / 2, 45, 315));
                case 'd': return L(Arc(0.26f, H / 2, 0.26f, H / 2, 30, 390), Line(0.53f, 1, 0.53f, 0));
                case 'e': return L(Join(Line(0.02f, H / 2, 0.56f, H / 2), Arc(0.29f, H / 2, 0.27f, H / 2, 0, 320)));
                case 'f': return L(Join(Arc(0.4f, 0.82f, 0.17f, 0.17f, 20, 180), Line(0.23f, 0.82f, 0.23f, 0)), Line(0.04f, H, 0.44f, H));
                case 'g': return L(Arc(0.26f, H / 2, 0.26f, H / 2, 30, 390),
                    Join(Line(0.53f, H, 0.53f, -0.16f), Arc(0.29f, -0.16f, 0.24f, 0.21f, 0, -170)));
                case 'h': return L(Line(0, 1, 0, 0), Join(Arc(0.26f, 0.37f, 0.26f, 0.25f, 180, 0), Line(0.52f, 0.37f, 0.52f, 0)));
                case 'i': return L(Line(0.05f, H, 0.05f, 0), Dot(0.05f, 0.86f));
                case 'j': return L(Join(Line(0.36f, H, 0.36f, -0.16f), Arc(0.15f, -0.16f, 0.21f, 0.21f, 0, -170)), Dot(0.36f, 0.86f));
                case 'k': return L(Line(0, 1, 0, 0), Line(0.46f, H, 0, 0.24f), Line(0.13f, 0.33f, 0.48f, 0));
                case 'l': return L(Line(0, 1, 0, 0));
                case 'm': return L(Line(0, H, 0, 0),
                    Join(Arc(0.21f, 0.4f, 0.21f, 0.22f, 180, 0), Line(0.42f, 0.4f, 0.42f, 0)),
                    Join(Arc(0.63f, 0.4f, 0.21f, 0.22f, 180, 0), Line(0.84f, 0.4f, 0.84f, 0)));
                case 'n': return L(Line(0, H, 0, 0), Join(Arc(0.26f, 0.37f, 0.26f, 0.25f, 180, 0), Line(0.52f, 0.37f, 0.52f, 0)));
                case 'o': return L(Arc(0.28f, H / 2, 0.28f, H / 2, 90, 450));
                case 'p': return L(Line(0, H, 0, Descender), Arc(0.26f, H / 2, 0.26f, H / 2, 180, -180));
                case 'q': return L(Arc(0.26f, H / 2, 0.26f, H / 2, 30, 390), Line(0.53f, H, 0.53f, Descender));
                case 'r': return L(Line(0, H, 0, 0), Arc(0.23f, 0.4f, 0.23f, 0.22f, 180, 30));
                case 's': return L(Join(Arc(0.22f, 0.465f, 0.2f, 0.155f, 30, 270), Arc(0.22f, 0.155f, 0.22f, 0.155f, 90, -150)));
                case 't': return L(Line(0.2f, 0.92f, 0.2f, 0), Line(0, H, 0.42f, H));
                case 'u': return L(Join(Line(0, H, 0, 0.26f), Arc(0.26f, 0.26f, 0.26f, 0.26f, 180, 360), Line(0.52f, 0.26f, 0.52f, H)),
                    Line(0.52f, H, 0.52f, 0));
                case 'v': return L(Poly(0, H, 0.29f, 0, 0.58f, H));
                case 'w': return L(Poly(0, H, 0.19f, 0, 0.39f, 0.46f, 0.59f, 0, 0.78f, H));
                case 'x': return L(Line(0, H, 0.52f, 0), Line(0.52f, H, 0, 0));
                case 'y': return L(Line(0, H, 0.28f, 0), Line(0.56f, H, 0.14f, Descender));
                case 'z': return L(Poly(0, H, 0.52f, H, 0, 0, 0.52f, 0));
                default: return new List<Vector2[]>();
            }
        }

        /// <summary>Strokes shorter than this (in glyph units) are dots: a touch near them completes them.</summary>
        public const float DotLength = 0.06f;

        public static bool IsDot(IReadOnlyList<Vector2> stroke) => PathLength(stroke) < DotLength;

        public static float PathLength(IReadOnlyList<Vector2> pts)
        {
            float len = 0;
            for (int i = 1; i < pts.Count; i++) len += Vector2.Distance(pts[i - 1], pts[i]);
            return len;
        }

        public static Rect Bounds(IEnumerable<IReadOnlyList<Vector2>> strokes)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var s in strokes)
            {
                foreach (Vector2 p in s)
                {
                    minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                    minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
                }
            }
            return minX > maxX ? new Rect(0, 0, 0, 0) : Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        // ------------------------------------------------------------------ builders
        private static List<Vector2[]> L(params Vector2[][] strokes) => new List<Vector2[]>(strokes);

        private static Vector2[] Line(float x0, float y0, float x1, float y1) => new[] { new Vector2(x0, y0), new Vector2(x1, y1) };

        private static Vector2[] Poly(params float[] xy)
        {
            var pts = new Vector2[xy.Length / 2];
            for (int i = 0; i < pts.Length; i++) pts[i] = new Vector2(xy[i * 2], xy[i * 2 + 1]);
            return pts;
        }

        private static Vector2[] Dot(float x, float y) => new[] { new Vector2(x, y + 0.012f), new Vector2(x, y - 0.012f) };

        /// <summary>Elliptical arc from startDeg to endDeg (counter-clockwise when end &gt; start).</summary>
        private static Vector2[] Arc(float cx, float cy, float rx, float ry, float startDeg, float endDeg)
        {
            float sweep = Mathf.Abs(endDeg - startDeg);
            int steps = Mathf.Max(6, Mathf.CeilToInt(sweep / 10f));
            var pts = new Vector2[steps + 1];
            for (int i = 0; i <= steps; i++)
            {
                float a = Mathf.Deg2Rad * Mathf.Lerp(startDeg, endDeg, i / (float)steps);
                pts[i] = new Vector2(cx + Mathf.Cos(a) * rx, cy + Mathf.Sin(a) * ry);
            }
            return pts;
        }

        /// <summary>Concatenates segments into one stroke, dropping duplicate joint points.</summary>
        private static Vector2[] Join(params Vector2[][] parts)
        {
            var list = new List<Vector2>();
            foreach (Vector2[] part in parts)
            {
                foreach (Vector2 p in part)
                {
                    if (list.Count > 0 && (list[list.Count - 1] - p).sqrMagnitude < 1e-6f) continue;
                    list.Add(p);
                }
            }
            return list.ToArray();
        }
    }
}
