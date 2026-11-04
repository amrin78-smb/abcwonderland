using System;
using System.Collections.Generic;
using ABCWonderworld.Content;
using UnityEngine;

namespace ABCWonderworld.Characters
{
    /// <summary>
    /// Builds a placeholder letter character from its glyph: puffy letter body, googly eyes, smile, cheeks
    /// and feet, plus a floating theme badge. Used by the setup pipeline to bake prefabs and at runtime as a
    /// fallback when a prefab is missing. Real production models replace the prefab, not this contract.
    /// </summary>
    public static class CharacterFactory
    {
        public const float BodyRadius = 0.15f;
        public const float BodyDepth = 0.85f;

        public sealed class Parts
        {
            public Mesh body, sphere, smile;
        }

        public static GameObject Build(char upper, Color primary, Color accent, Sprite badge, Func<Color, int, Material> mat, Parts meshes = null)
        {
            mat ??= MaterialLibrary.Get;
            meshes ??= new Parts();
            List<Vector2[]> strokes = LetterStrokeLibrary.Get(upper);
            Rect b = LetterStrokeLibrary.Bounds(strokes);
            Vector3 centerOffset = new Vector3(-b.center.x, 0, 0);

            var root = new GameObject("Character_" + upper);
            var body = new GameObject("Body");
            body.transform.SetParent(root.transform, false);
            var glyph = new GameObject("Glyph");
            glyph.transform.SetParent(body.transform, false);
            glyph.transform.localPosition = centerOffset;
            meshes.body ??= GlyphMeshBuilder.Build(strokes, BodyRadius, BodyDepth, 12);
            meshes.body.name = "CharacterBody_" + upper;
            AddMesh(glyph, meshes.body, mat(primary, 2));

            // Face: on the stroke nearest the upper-middle of the glyph.
            Vector2 target = new Vector2(b.center.x, b.yMin + b.height * 0.62f);
            Vector2 face = NearestOnStrokes(strokes, target);
            float frontZ = -BodyRadius * BodyDepth;
            var faceT = new GameObject("Face").transform;
            faceT.SetParent(glyph.transform, false);
            faceT.localPosition = new Vector3(face.x, face.y, frontZ);

            meshes.sphere ??= GlyphMeshBuilder.Sphere(0.5f, 18);
            var eyes = new List<Transform>();
            var pupils = new List<Transform>();
            foreach (float side in new[] { -1f, 1f })
            {
                Transform eye = Part("Eye", faceT, meshes.sphere, mat(Color.white, 2), new Vector3(side * 0.085f, 0.03f, -0.01f), new Vector3(0.13f, 0.15f, 0.06f));
                Transform pupil = Part("Pupil", eye, meshes.sphere, mat(new Color(0.12f, 0.1f, 0.18f), 2), new Vector3(0, -0.05f, -0.42f), new Vector3(0.55f, 0.6f, 0.4f));
                Part("Shine", pupil, meshes.sphere, mat(Color.white, 1), new Vector3(0.18f, 0.22f, -0.45f), new Vector3(0.35f, 0.35f, 0.3f));
                Part("Cheek", faceT, meshes.sphere, mat(new Color(1f, 0.55f, 0.6f), 0), new Vector3(side * 0.15f, -0.07f, 0.005f), new Vector3(0.07f, 0.045f, 0.02f));
                eyes.Add(eye);
                pupils.Add(pupil);
            }
            meshes.smile ??= BuildSmile();
            Transform mouth = Part("Mouth", faceT, meshes.smile, mat(new Color(0.35f, 0.12f, 0.18f), 0), new Vector3(0, -0.075f, -0.012f), Vector3.one);

            // Feet at the lowest points of the glyph.
            List<Vector2> bottoms = BottomPoints(strokes, b);
            Color footColor = Color.Lerp(primary, Color.black, 0.25f);
            foreach (Vector2 p in bottoms)
                Part("Foot", glyph.transform, meshes.sphere, mat(footColor, 0), new Vector3(p.x, -0.06f, -0.02f), new Vector3(0.17f, 0.09f, 0.22f));

            // Anchors.
            var character = root.AddComponent<LetterCharacter>();
            character.letterId = LetterDefinition.IdFor(upper);
            character.characterId = CharacterDefinition.IdFor(upper);
            character.faceAnchor = faceT;
            character.aboveHeadAnchor = Anchor("AboveHead", root.transform, new Vector3(0, b.yMax + 0.25f, 0));
            character.leftHandAnchor = Anchor("LeftHand", root.transform, new Vector3(-b.width * 0.5f - 0.15f, b.height * 0.45f, 0));
            character.rightHandAnchor = Anchor("RightHand", root.transform, new Vector3(b.width * 0.5f + 0.15f, b.height * 0.45f, 0));
            character.feetAnchor = Anchor("Feet", root.transform, Vector3.zero);

            if (badge != null)
            {
                var badgeGo = new GameObject("ThemeBadge");
                badgeGo.transform.SetParent(body.transform, false);
                badgeGo.transform.localPosition = new Vector3(b.width * 0.5f + 0.25f, b.yMax - 0.1f, -0.1f);
                badgeGo.transform.localScale = Vector3.one * (0.38f / Mathf.Max(0.01f, badge.bounds.size.y));
                var sr = badgeGo.AddComponent<SpriteRenderer>();
                sr.sprite = badge;
                badgeGo.AddComponent<Bob>();
            }

            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0, b.center.y, 0);
            col.size = new Vector3(Mathf.Max(0.6f, b.width + 0.45f), b.height + 0.4f, 0.6f);

            var anim = root.AddComponent<ProceduralCharacterAnimator>();
            anim.body = body.transform;
            anim.eyes = eyes.ToArray();
            anim.pupils = pupils.ToArray();
            anim.mouth = mouth;
            return root;
        }

        private static Transform Part(string name, Transform parent, Mesh mesh, Material m, Vector3 pos, Vector3 scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            AddMesh(go, mesh, m);
            return go.transform;
        }

        private static void AddMesh(GameObject go, Mesh mesh, Material m)
        {
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        private static Transform Anchor(string name, Transform parent, Vector3 pos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            return t;
        }

        public static Mesh BuildSmile()
        {
            var arc = new List<Vector2>();
            for (int i = 0; i <= 10; i++)
            {
                float a = Mathf.Deg2Rad * Mathf.Lerp(200, 340, i / 10f);
                arc.Add(new Vector2(Mathf.Cos(a) * 0.055f, Mathf.Sin(a) * 0.045f + 0.03f));
            }
            Mesh m = GlyphMeshBuilder.Build(new List<Vector2[]> { arc.ToArray() }, 0.016f, 0.5f, 8);
            m.name = "Smile";
            return m;
        }

        private static Vector2 NearestOnStrokes(List<Vector2[]> strokes, Vector2 target)
        {
            Vector2 best = target;
            float bestD = float.MaxValue;
            foreach (Vector2[] s in strokes)
            {
                foreach (Vector2 p in Tracing.TracingEvaluator.Resample(s, 0.02f))
                {
                    float d = (p - target).sqrMagnitude;
                    if (d < bestD) { bestD = d; best = p; }
                }
            }
            return best;
        }

        private static List<Vector2> BottomPoints(List<Vector2[]> strokes, Rect b)
        {
            var low = new List<Vector2>();
            foreach (Vector2[] s in strokes)
                foreach (Vector2 p in Tracing.TracingEvaluator.Resample(s, 0.03f))
                    if (p.y <= b.yMin + 0.04f) low.Add(p);
            if (low.Count == 0) return new List<Vector2> { new Vector2(b.center.x - 0.1f, b.yMin), new Vector2(b.center.x + 0.1f, b.yMin) };
            float minX = float.MaxValue, maxX = float.MinValue;
            foreach (Vector2 p in low) { minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x); }
            if (maxX - minX < 0.2f)
            {
                float c = (minX + maxX) * 0.5f;
                return new List<Vector2> { new Vector2(c - 0.11f, b.yMin), new Vector2(c + 0.11f, b.yMin) };
            }
            return new List<Vector2> { new Vector2(minX + 0.02f, b.yMin), new Vector2(maxX - 0.02f, b.yMin) };
        }
    }

    /// <summary>Small floating bob for badges and collectibles.</summary>
    public sealed class Bob : MonoBehaviour
    {
        public float height = 0.06f;
        public float speed = 2f;
        private Vector3 _p;
        private float _o;
        private void OnEnable() { _p = transform.localPosition; _o = UnityEngine.Random.value * 6f; }
        private void Update() => transform.localPosition = _p + Vector3.up * Mathf.Sin(Time.time * speed + _o) * height * UI.Tween.Motion;
    }
}
