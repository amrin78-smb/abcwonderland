using System.Collections.Generic;
using ABCWonderworld.Tracing;
using UnityEngine;
using UnityEngine.Rendering;

namespace ABCWonderworld.Characters
{
    /// <summary>
    /// Builds a chunky, rounded 3D letter ("balloon letter") by sweeping a circle along the glyph strokes,
    /// with spheres at ends and corners. Front faces −Z. Units: glyph units (cap height = 1).
    /// </summary>
    public static class GlyphMeshBuilder
    {
        private static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();

        public static Mesh GetCached(char glyph, float radius = 0.13f, float depth = 0.8f)
        {
            string key = $"{glyph}_{radius:0.000}_{depth:0.00}";
            if (Cache.TryGetValue(key, out Mesh m) && m != null) return m;
            m = Build(Content.LetterStrokeLibrary.Get(glyph), radius, depth, 12);
            m.name = "Glyph_" + (char.IsUpper(glyph) ? glyph + "_upper" : glyph + "_lower");
            Cache[key] = m;
            return m;
        }

        public static Mesh Build(IList<Vector2[]> strokes, float radius, float depthScale, int segments)
        {
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var tris = new List<int>();
            foreach (Vector2[] s in strokes)
            {
                if (s == null || s.Length == 0) continue;
                if (Content.LetterStrokeLibrary.PathLength(s) < Content.LetterStrokeLibrary.DotLength)
                {
                    AddSphere(v, n, tris, s[0], radius * 1.15f, depthScale, segments);
                    continue;
                }
                Vector2[] pts = TracingEvaluator.Resample(s, radius * 0.5f);
                AddTube(v, n, tris, pts, radius, depthScale, segments);
                AddSphere(v, n, tris, s[0], radius, depthScale, segments);
                AddSphere(v, n, tris, s[s.Length - 1], radius, depthScale, segments);
                for (int i = 1; i < s.Length - 1; i++)
                {
                    Vector2 a = (s[i] - s[i - 1]).normalized, b = (s[i + 1] - s[i]).normalized;
                    if (Vector2.Angle(a, b) > 20f) AddSphere(v, n, tris, s[i], radius, depthScale, segments);
                }
            }
            var mesh = new Mesh();
            if (v.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddTube(List<Vector3> v, List<Vector3> n, List<int> t, Vector2[] pts, float r, float depth, int seg)
        {
            int start = v.Count;
            for (int i = 0; i < pts.Length; i++)
            {
                Vector2 tan = i == 0 ? pts[1] - pts[0] : i == pts.Length - 1 ? pts[i] - pts[i - 1] : pts[i + 1] - pts[i - 1];
                tan.Normalize();
                Vector3 u = new Vector3(-tan.y, tan.x, 0);
                Vector3 w = Vector3.forward;
                for (int k = 0; k <= seg; k++)
                {
                    float a = k / (float)seg * Mathf.PI * 2f;
                    float c = Mathf.Cos(a), s = Mathf.Sin(a);
                    v.Add((Vector3)pts[i] + (u * c + w * s * depth) * r);
                    n.Add((u * c + w * s / Mathf.Max(0.01f, depth)).normalized);
                }
            }
            int ring = seg + 1;
            for (int i = 0; i < pts.Length - 1; i++)
            {
                for (int k = 0; k < seg; k++)
                {
                    int a = start + i * ring + k, b = a + ring;
                    t.Add(a); t.Add(b); t.Add(a + 1);
                    t.Add(a + 1); t.Add(b); t.Add(b + 1);
                }
            }
        }

        public static void AddSphere(List<Vector3> v, List<Vector3> n, List<int> t, Vector3 c, float r, float depth, int seg)
        {
            int start = v.Count;
            int rings = Mathf.Max(4, seg / 2);
            for (int y = 0; y <= rings; y++)
            {
                float phi = y / (float)rings * Mathf.PI;
                for (int x = 0; x <= seg; x++)
                {
                    float th = x / (float)seg * Mathf.PI * 2f;
                    var dir = new Vector3(Mathf.Sin(phi) * Mathf.Cos(th), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(th));
                    v.Add(c + new Vector3(dir.x, dir.y, dir.z * depth) * r);
                    n.Add(new Vector3(dir.x, dir.y, dir.z / Mathf.Max(0.01f, depth)).normalized);
                }
            }
            for (int y = 0; y < rings; y++)
            {
                for (int x = 0; x < seg; x++)
                {
                    int a = start + y * (seg + 1) + x, b = a + seg + 1;
                    t.Add(a); t.Add(a + 1); t.Add(b);
                    t.Add(a + 1); t.Add(b + 1); t.Add(b);
                }
            }
        }

        /// <summary>A plain sphere/ellipsoid mesh (eyes, cheeks, feet, props).</summary>
        public static Mesh Sphere(float r = 0.5f, int seg = 16)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
            AddSphere(v, n, t, Vector3.zero, r, 1f, seg);
            var m = new Mesh { name = "Sphere" };
            m.SetVertices(v); m.SetNormals(n); m.SetTriangles(t, 0); m.RecalculateBounds();
            return m;
        }
    }
}
