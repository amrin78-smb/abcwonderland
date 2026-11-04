using System.Collections.Generic;
using ABCWonderworld.Characters;
using ABCWonderworld.Content;
using ABCWonderworld.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace ABCWonderworld.World
{
    /// <summary>
    /// Builds a themed, modular placeholder 3D environment for a world (GDD §4): ground, hills, scenery,
    /// sky and lighting, plus tappable free-play props. Production environments replace this by authored
    /// scenes/prefabs; the WorldScreen only needs the returned layout anchors.
    /// </summary>
    public static class EnvironmentBuilder
    {
        public enum Theme { Meadow, Bay, Candy, Jungle, Mountain }

        public static Theme ThemeOf(WorldDefinition w) => w == null ? Theme.Meadow : w.order switch
        {
            1 => Theme.Bay, 2 => Theme.Candy, 3 => Theme.Jungle, 4 => Theme.Mountain, _ => Theme.Meadow
        };

        private static readonly Dictionary<PrimitiveType, Mesh> Prims = new Dictionary<PrimitiveType, Mesh>();

        private static Mesh PrimMesh(PrimitiveType t)
        {
            if (Prims.TryGetValue(t, out Mesh m) && m != null) return m;
            GameObject g = GameObject.CreatePrimitive(t);
            m = g.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(g);
            Prims[t] = m;
            return m;
        }

        public static GameObject Shape(PrimitiveType t, Transform parent, Vector3 pos, Vector3 scale, Color c, int kind = 0, bool collider = false)
        {
            var go = new GameObject(t.ToString());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = PrimMesh(t);
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = MaterialLibrary.Get(c, kind);
            if (collider) go.AddComponent<SphereCollider>();
            return go;
        }

        public static SpriteRenderer Billboard(Transform parent, Sprite s, Vector3 pos, float size)
        {
            var go = new GameObject(s != null ? s.name : "Billboard");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = Vector3.one * size / Mathf.Max(0.01f, s != null ? s.bounds.size.y : 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            go.AddComponent<FaceCamera>();
            return sr;
        }

        /// <summary>Camera, light, sky and ambient for a world.</summary>
        public static Camera SetupCameraAndLight(WorldDefinition w, Vector3 camPos, Vector3 lookAt, float fov = 40f)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var cgo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cgo.tag = "MainCamera";
                cam = cgo.GetComponent<Camera>();
            }
            cam.transform.position = camPos;
            cam.transform.LookAt(lookAt);
            cam.fieldOfView = fov;
            cam.clearFlags = CameraClearFlags.SolidColor;
            Color sky = w != null ? w.skyColor : new Color(0.62f, 0.86f, 1f);
            cam.backgroundColor = sky;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 120f;
            Input.TouchInteractionService.EnablePhysicsPointer(cam);

            Light sun = Object.FindAnyObjectByType<Light>();
            if (sun == null)
            {
                sun = new GameObject("Sun").AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.intensity = 1.15f;
            sun.shadows = !Core.QualityService.Shadows ? LightShadows.None : Core.QualityService.Effective == Core.GraphicsQuality.High ? LightShadows.Soft : LightShadows.Hard;
            sun.shadowStrength = 0.55f;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(sky, Color.white, 0.45f);
            RenderSettings.ambientEquatorColor = Color.Lerp(sky, Color.white, 0.6f) * 0.85f;
            RenderSettings.ambientGroundColor = (w != null ? w.groundColor : Color.green) * 0.55f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = sky;
            RenderSettings.fogStartDistance = 25f;
            RenderSettings.fogEndDistance = 70f;
            return cam;
        }

        /// <summary>Builds the environment under a new root and returns it.</summary>
        public static Transform Build(WorldDefinition w, bool freePlayProps, int seed = 1)
        {
            var root = new GameObject("Environment").transform;
            var rng = new System.Random(seed);
            Theme theme = ThemeOf(w);
            Color ground = w != null ? w.groundColor : new Color(0.48f, 0.8f, 0.36f);
            Color accent = w != null ? w.accentColor : Color.yellow;

            Shape(PrimitiveType.Cylinder, root, new Vector3(0, -0.1f, 6f), new Vector3(60, 0.1f, 40), ground);
            // Rolling hills / dunes / chocolate hills / mountains on the horizon.
            for (int i = 0; i < 7; i++)
            {
                float x = -24 + i * 8 + (float)rng.NextDouble() * 3;
                float h = 3 + (float)rng.NextDouble() * 4;
                Color hc = Color.Lerp(ground, theme == Theme.Mountain ? Color.white : Color.black, 0.08f + (float)rng.NextDouble() * 0.12f);
                if (theme == Theme.Mountain)
                    Shape(PrimitiveType.Capsule, root, new Vector3(x, h * 0.4f, 24 + (float)rng.NextDouble() * 6), new Vector3(7, h, 7), Color.Lerp(new Color(0.7f, 0.65f, 1f), Color.white, (float)rng.NextDouble() * 0.4f));
                else
                    Shape(PrimitiveType.Sphere, root, new Vector3(x, -h * 0.25f, 20 + (float)rng.NextDouble() * 8), new Vector3(12, h, 9), hc);
            }
            for (int i = 0; i < 4; i++) Cloud(root, new Vector3(-16 + i * 11 + (float)rng.NextDouble() * 3, 9 + (float)rng.NextDouble() * 3, 26), theme == Theme.Candy ? new Color(1f, 0.92f, 0.97f) : Color.white);

            switch (theme)
            {
                case Theme.Meadow: Meadow(root, rng, freePlayProps); break;
                case Theme.Bay: Bay(root, rng, freePlayProps); break;
                case Theme.Candy: Candy(root, rng, freePlayProps, accent); break;
                case Theme.Jungle: Jungle(root, rng, freePlayProps); break;
                case Theme.Mountain: Mountain(root, rng, freePlayProps); break;
            }
            return root;
        }

        private static void Cloud(Transform root, Vector3 p, Color c)
        {
            var cloud = new GameObject("Cloud").transform;
            cloud.SetParent(root, false);
            cloud.localPosition = p;
            Shape(PrimitiveType.Sphere, cloud, Vector3.zero, new Vector3(3, 1.6f, 1.6f), c, 1);
            Shape(PrimitiveType.Sphere, cloud, new Vector3(1.3f, 0.4f, 0), new Vector3(2.2f, 1.8f, 1.6f), c, 1);
            Shape(PrimitiveType.Sphere, cloud, new Vector3(-1.2f, 0.2f, 0), new Vector3(2f, 1.4f, 1.4f), c, 1);
            cloud.gameObject.AddComponent<Drift>();
        }

        private static float R(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);

        // Placement band for scenery: keep the central stage (|x| < 5, z < 4) clear for characters/activities.
        private static Vector3 Scenery(System.Random r, float zMin = 3f, float zMax = 14f)
        {
            float x = R(r, -16, 16);
            float z = R(r, zMin, zMax);
            if (Mathf.Abs(x) < 6 && z < 6) x += Mathf.Sign(x == 0 ? 1 : x) * 6;
            return new Vector3(x, 0, z);
        }

        private static void Tree(Transform root, Vector3 p, Color leaf, float s, bool interactive)
        {
            var t = new GameObject("Tree").transform;
            t.SetParent(root, false);
            t.localPosition = p;
            Shape(PrimitiveType.Cylinder, t, new Vector3(0, 1f * s, 0), new Vector3(0.35f, 1f, 0.35f) * s, new Color(0.55f, 0.38f, 0.24f));
            Shape(PrimitiveType.Sphere, t, new Vector3(0, 2.5f * s, 0), new Vector3(2.2f, 2f, 2.2f) * s, leaf);
            Shape(PrimitiveType.Sphere, t, new Vector3(0.6f * s, 2.1f * s, -0.3f * s), new Vector3(1.5f, 1.4f, 1.5f) * s, Color.Lerp(leaf, Color.white, 0.1f));
            if (interactive) AddInteract(t.gameObject, new Vector3(0, 2f * s, 0), 1.6f * s, Interactable.Reaction.Wobble, "boing");
        }

        private static void Flower(Transform root, Vector3 p, Color c, bool interactive)
        {
            var f = new GameObject("Flower").transform;
            f.SetParent(root, false);
            f.localPosition = p;
            Shape(PrimitiveType.Cylinder, f, new Vector3(0, 0.25f, 0), new Vector3(0.06f, 0.25f, 0.06f), new Color(0.3f, 0.65f, 0.3f));
            for (int i = 0; i < 5; i++)
            {
                float a = i * Mathf.PI * 2 / 5;
                Shape(PrimitiveType.Sphere, f, new Vector3(Mathf.Cos(a) * 0.15f, 0.55f + Mathf.Sin(a) * 0.15f, 0), new Vector3(0.2f, 0.2f, 0.08f), c);
            }
            Shape(PrimitiveType.Sphere, f, new Vector3(0, 0.55f, -0.04f), Vector3.one * 0.15f, new Color(1f, 0.85f, 0.3f));
            if (interactive) AddInteract(f.gameObject, new Vector3(0, 0.5f, 0), 0.5f, Interactable.Reaction.Grow, "note_" + Random.Range(0, 8));
        }

        public static Interactable AddInteract(GameObject go, Vector3 center, float radius, Interactable.Reaction reaction, string sfx, string revealKey = null)
        {
            var col = go.AddComponent<SphereCollider>();
            col.center = center;
            col.radius = Mathf.Max(0.6f, radius); // generous hit area
            var it = go.AddComponent<Interactable>();
            it.reaction = reaction;
            it.sfx = sfx;
            it.revealPicture = revealKey;
            return it;
        }

        private static void Meadow(Transform root, System.Random rng, bool fp)
        {
            // River with a wooden bridge.
            Shape(PrimitiveType.Cube, root, new Vector3(0, -0.04f, 10f), new Vector3(60, 0.05f, 2.2f), new Color(0.45f, 0.75f, 1f), 2);
            for (int i = 0; i < 6; i++) Shape(PrimitiveType.Cube, root, new Vector3(-9 + i * 0.5f, 0.15f, 10f), new Vector3(0.45f, 0.12f, 2.8f), new Color(0.7f, 0.5f, 0.3f));
            Color[] leaves = { new Color(0.35f, 0.7f, 0.3f), new Color(0.45f, 0.78f, 0.35f), new Color(0.3f, 0.62f, 0.32f) };
            for (int i = 0; i < 9; i++) Tree(root, Scenery(rng, 4, 16), leaves[i % 3], R(rng, 0.8f, 1.3f), fp);
            Color[] petals = { new Color(1f, 0.5f, 0.6f), new Color(1f, 0.85f, 0.3f), new Color(0.75f, 0.55f, 1f), Color.white };
            for (int i = 0; i < 26; i++) Flower(root, new Vector3(R(rng, -12, 12), 0, R(rng, -1.5f, 8)) + (i % 2 == 0 ? Vector3.zero : Vector3.right * 0.01f), petals[i % petals.Length], fp && i % 2 == 0);
            if (fp)
            {
                Critter(root, "butterfly", new Vector3(-6, 1.8f, 2), 0.9f, Interactable.Reaction.FlyAway);
                Critter(root, "bird", new Vector3(7, 3.2f, 6), 1f, Interactable.Reaction.FlyAway);
                Critter(root, "ladybug", new Vector3(4.5f, 0.35f, 0.5f), 0.6f, Interactable.Reaction.Hop);
                Critter(root, "bee", new Vector3(-4, 2.4f, 4), 0.7f, Interactable.Reaction.FlyAway);
                Critter(root, "mushroom", new Vector3(-7.5f, 0.45f, 1.5f), 0.9f, Interactable.Reaction.Reveal, "1f41c");
            }
        }

        private static void Bay(Transform root, System.Random rng, bool fp)
        {
            Color[] coral = { new Color(1f, 0.5f, 0.55f), new Color(1f, 0.65f, 0.3f), new Color(0.85f, 0.45f, 0.9f) };
            for (int i = 0; i < 12; i++)
            {
                Vector3 p = Scenery(rng, 3, 14);
                var c = new GameObject("Coral").transform; c.SetParent(root, false); c.localPosition = p;
                for (int k = 0; k < 3; k++) Shape(PrimitiveType.Capsule, c, new Vector3(R(rng, -0.4f, 0.4f), 0.6f, 0), new Vector3(0.3f, R(rng, 0.6f, 1.2f), 0.3f), coral[(i + k) % 3]);
                if (fp) AddInteract(c.gameObject, new Vector3(0, 0.8f, 0), 0.9f, Interactable.Reaction.Grow, "pop");
            }
            for (int i = 0; i < 14; i++)
            {
                var s = Shape(PrimitiveType.Capsule, root, new Vector3(R(rng, -14, 14), 1f, R(rng, 2, 14)), new Vector3(0.12f, R(rng, 0.8f, 1.6f), 0.12f), new Color(0.3f, 0.7f, 0.45f));
                s.AddComponent<Sway>();
            }
            for (int i = 0; i < 16; i++)
            {
                var b = Shape(PrimitiveType.Sphere, root, new Vector3(R(rng, -12, 12), R(rng, 0.5f, 5), R(rng, 0, 10)), Vector3.one * R(rng, 0.15f, 0.35f), new Color(0.85f, 0.95f, 1f), 2);
                b.AddComponent<Rise>();
                if (fp && i % 3 == 0) AddInteract(b, Vector3.zero, 0.7f, Interactable.Reaction.Pop, "pop");
            }
            if (fp)
            {
                Critter(root, "tropical_fish", new Vector3(-5, 2.2f, 2), 1.1f, Interactable.Reaction.FlyAway);
                Critter(root, "dolphin", new Vector3(6, 3f, 6), 1.4f, Interactable.Reaction.Hop);
                Critter(root, "shell", new Vector3(4.6f, 0.35f, 0.3f), 0.7f, Interactable.Reaction.Reveal, "1f41f");
                Critter(root, "1f419", new Vector3(-7, 0.6f, 3), 1.1f, Interactable.Reaction.Hop);
            }
        }

        private static void Candy(Transform root, System.Random rng, bool fp, Color accent)
        {
            Color[] candy = { new Color(1f, 0.45f, 0.7f), new Color(0.5f, 0.85f, 1f), new Color(1f, 0.85f, 0.35f), new Color(0.6f, 0.9f, 0.5f), new Color(0.8f, 0.6f, 1f) };
            for (int i = 0; i < 10; i++)
            {
                Vector3 p = Scenery(rng, 3, 15);
                var t = new GameObject("GumdropTree").transform; t.SetParent(root, false); t.localPosition = p;
                Shape(PrimitiveType.Cylinder, t, new Vector3(0, 0.9f, 0), new Vector3(0.25f, 0.9f, 0.25f), Color.white);
                Shape(PrimitiveType.Sphere, t, new Vector3(0, 2.2f, 0), new Vector3(1.8f, 1.5f, 1.8f), candy[i % candy.Length], 2);
                if (fp) AddInteract(t.gameObject, new Vector3(0, 1.8f, 0), 1.3f, Interactable.Reaction.Wobble, "boing");
            }
            for (int i = 0; i < 8; i++)
            {
                var l = new GameObject("Lollipop").transform; l.SetParent(root, false); l.localPosition = new Vector3(R(rng, -12, 12), 0, R(rng, -1, 7));
                if (Mathf.Abs(l.localPosition.x) < 5 && l.localPosition.z < 4) l.localPosition += Vector3.right * 6 * Mathf.Sign(l.localPosition.x + 0.01f);
                Shape(PrimitiveType.Cylinder, l, new Vector3(0, 0.6f, 0), new Vector3(0.06f, 0.6f, 0.06f), Color.white);
                Shape(PrimitiveType.Cylinder, l, new Vector3(0, 1.4f, 0), new Vector3(0.7f, 0.06f, 0.7f), candy[i % candy.Length], 2).transform.localRotation = Quaternion.Euler(90, 0, 0);
                if (fp) AddInteract(l.gameObject, new Vector3(0, 1.3f, 0), 0.6f, Interactable.Reaction.Spin, "note_" + (i % 8));
            }
            // Rainbow bridge.
            for (int b = 0; b < 5; b++)
                for (int i = 0; i <= 12; i++)
                {
                    float a = Mathf.PI * i / 12f;
                    Shape(PrimitiveType.Sphere, root, new Vector3(-10 + Mathf.Cos(a) * (3 + b * 0.35f), Mathf.Sin(a) * (3 + b * 0.35f), 12), Vector3.one * 0.45f, candy[b], 2);
                }
            if (fp)
            {
                Critter(root, "cupcake", new Vector3(-6.5f, 0.6f, 1.5f), 1f, Interactable.Reaction.Reveal, "1f36d");
                Critter(root, "doughnut", new Vector3(6.5f, 0.6f, 1f), 0.9f, Interactable.Reaction.Spin);
                Critter(root, "candy", new Vector3(3, 3f, 5), 0.8f, Interactable.Reaction.Hop);
            }
        }

        private static void Jungle(Transform root, System.Random rng, bool fp)
        {
            for (int i = 0; i < 10; i++)
            {
                Vector3 p = Scenery(rng, 3, 15);
                var t = new GameObject("Palm").transform; t.SetParent(root, false); t.localPosition = p;
                for (int k = 0; k < 5; k++) Shape(PrimitiveType.Cylinder, t, new Vector3(k * 0.08f, 0.4f + k * 0.7f, 0), new Vector3(0.3f - k * 0.02f, 0.38f, 0.3f - k * 0.02f), new Color(0.6f, 0.45f, 0.3f));
                for (int k = 0; k < 5; k++)
                {
                    var leaf = Shape(PrimitiveType.Sphere, t, new Vector3(0.4f, 3.6f, 0), new Vector3(2.2f, 0.25f, 0.7f), new Color(0.25f, 0.62f, 0.3f));
                    leaf.transform.localRotation = Quaternion.Euler(0, k * 72, -18);
                    leaf.transform.localPosition = new Vector3(0.4f, 3.6f, 0) + Quaternion.Euler(0, k * 72, 0) * Vector3.right * 0.9f;
                }
                if (fp) AddInteract(t.gameObject, new Vector3(0, 2.5f, 0), 1.6f, Interactable.Reaction.Wobble, "boing");
            }
            // Ruins and footprints.
            for (int i = 0; i < 5; i++) Shape(PrimitiveType.Cube, root, new Vector3(8 + i * 1.1f, 0.4f + (i % 2) * 0.4f, 9), new Vector3(1, 0.8f + (i % 2) * 0.8f, 1), new Color(0.65f, 0.62f, 0.55f));
            for (int i = 0; i < 8; i++) Shape(PrimitiveType.Sphere, root, new Vector3(-8 + i * 0.9f, 0.01f, 1 + (i % 2) * 0.6f), new Vector3(0.45f, 0.02f, 0.6f), new Color(0.3f, 0.5f, 0.3f));
            // Waterfall.
            Shape(PrimitiveType.Cube, root, new Vector3(-12, 3, 14), new Vector3(2.5f, 6, 0.3f), new Color(0.55f, 0.82f, 1f), 2);
            Shape(PrimitiveType.Cube, root, new Vector3(-12, 3, 14.4f), new Vector3(5, 7, 0.4f), new Color(0.55f, 0.52f, 0.45f));
            if (fp)
            {
                Critter(root, "trex", new Vector3(-6.5f, 1f, 2.5f), 1.8f, Interactable.Reaction.Hop);
                Critter(root, "1f995", new Vector3(7, 1.2f, 4), 2.2f, Interactable.Reaction.Hop);
                Critter(root, "egg", new Vector3(5, 0.4f, 0.3f), 0.7f, Interactable.Reaction.Reveal, "1f995");
                Critter(root, "paws", new Vector3(-3.5f, 0.2f, 0.6f), 0.5f, Interactable.Reaction.Hop);
            }
        }

        private static void Mountain(Transform root, System.Random rng, bool fp)
        {
            Color[] rainbow = { new Color(1f, 0.4f, 0.4f), new Color(1f, 0.65f, 0.3f), new Color(1f, 0.9f, 0.35f), new Color(0.45f, 0.85f, 0.45f), new Color(0.4f, 0.65f, 1f), new Color(0.7f, 0.5f, 1f) };
            for (int i = 0; i < 6; i++)
            {
                var isl = new GameObject("FloatingIsland").transform; isl.SetParent(root, false);
                isl.localPosition = new Vector3(-14 + i * 5.5f, R(rng, 3.5f, 7), R(rng, 10, 18));
                Shape(PrimitiveType.Sphere, isl, Vector3.zero, new Vector3(3, 1.4f, 3), new Color(0.75f, 0.68f, 0.95f));
                Shape(PrimitiveType.Cylinder, isl, new Vector3(0, 0.55f, 0), new Vector3(3, 0.15f, 3), new Color(0.6f, 0.88f, 0.6f));
                isl.gameObject.AddComponent<Bob>().height = 0.25f;
            }
            for (int b = 0; b < 6; b++)
                for (int i = 0; i <= 16; i++)
                {
                    float a = Mathf.PI * i / 16f;
                    Shape(PrimitiveType.Sphere, root, new Vector3(Mathf.Cos(a) * (8 + b * 0.5f), Mathf.Sin(a) * (8 + b * 0.5f) - 1, 20), Vector3.one * 0.6f, rainbow[b], 1);
                }
            for (int i = 0; i < 10; i++)
            {
                Vector3 p = Scenery(rng, 2, 10);
                var crystal = Shape(PrimitiveType.Capsule, root, p + Vector3.up * 0.6f, new Vector3(0.4f, 0.7f, 0.4f), rainbow[i % 6], 2);
                if (fp) AddInteract(crystal, Vector3.zero, 0.9f, Interactable.Reaction.Grow, "note_" + (i % 8));
            }
            if (fp)
            {
                Critter(root, "rocket", new Vector3(-6.5f, 1.2f, 2), 1.4f, Interactable.Reaction.FlyAway);
                Critter(root, "planet", new Vector3(6, 4.5f, 8), 1.6f, Interactable.Reaction.Spin);
                Critter(root, "glowing_star", new Vector3(3.5f, 3.5f, 3), 0.8f, Interactable.Reaction.Hop);
                Critter(root, "ufo", new Vector3(-3, 4.2f, 6), 1.2f, Interactable.Reaction.Reveal, "1f984");
            }
        }

        private static void Critter(Transform root, string key, Vector3 pos, float size, Interactable.Reaction reaction, string reveal = null)
        {
            Sprite s = UiSkin.Picture(key);
            if (s == null) return;
            SpriteRenderer sr = Billboard(root, s, pos, size);
            sr.gameObject.AddComponent<Bob>().height = 0.08f;
            AddInteract(sr.gameObject, Vector3.zero, s.bounds.size.y * 0.6f, reaction, reaction == Interactable.Reaction.Reveal ? "reveal" : "pop", reveal);
        }
    }

    /// <summary>Free-play object: every prominent object answers a tap with a meaningful, gentle reaction.</summary>
    public sealed class Interactable : MonoBehaviour, IPointerClickHandler
    {
        public enum Reaction { Wobble, Grow, Spin, Hop, FlyAway, Pop, Reveal }
        public Reaction reaction;
        public string sfx = "pop";
        public string revealPicture;
        private float _last = -10f;
        private bool _busy;
        public static event System.Action<Interactable> Tapped;

        public void OnPointerClick(PointerEventData e) => Activate();

        public void Activate()
        {
            if (_busy || Time.unscaledTime - _last < 0.35f) return;
            _last = Time.unscaledTime;
            Core.GameServices.Current?.Audio?.PlaySfx(sfx);
            Tapped?.Invoke(this);
            StartCoroutine(Run());
        }

        private System.Collections.IEnumerator Run()
        {
            _busy = true;
            Transform t = transform;
            Vector3 p0 = t.localPosition, s0 = t.localScale;
            Quaternion r0 = t.localRotation;
            float m = Tween.Motion;
            float dur = reaction == Reaction.FlyAway ? 2.2f : 0.7f;
            GameObject revealed = null;
            if (reaction == Reaction.Reveal && !string.IsNullOrEmpty(revealPicture))
            {
                Sprite s = UiSkin.Picture(revealPicture);
                if (s != null)
                {
                    revealed = EnvironmentBuilder.Billboard(t.parent, s, p0 + Vector3.up * 0.4f, 0.9f).gameObject;
                }
            }
            for (float k = 0; k < 1f; k += Time.deltaTime / dur)
            {
                switch (reaction)
                {
                    case Reaction.Wobble: t.localRotation = r0 * Quaternion.Euler(0, 0, Mathf.Sin(k * Mathf.PI * 6) * 8 * (1 - k) * m); break;
                    case Reaction.Grow:
                    case Reaction.Pop: t.localScale = s0 * (1 + Mathf.Sin(k * Mathf.PI) * 0.35f * Mathf.Max(0.4f, m)); break;
                    case Reaction.Spin: t.localRotation = r0 * Quaternion.Euler(0, k * 360f * (m > 0.5f ? 1 : 0.25f), 0); break;
                    case Reaction.Hop: t.localPosition = p0 + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.8f * Mathf.Max(0.3f, m); break;
                    case Reaction.FlyAway: t.localPosition = p0 + new Vector3(Mathf.Sin(k * Mathf.PI * 2) * 2f, Mathf.Sin(k * Mathf.PI) * 2.5f, 0) * Mathf.Max(0.3f, m); break;
                    case Reaction.Reveal:
                        t.localScale = s0 * (1 + Mathf.Sin(k * Mathf.PI) * 0.2f);
                        if (revealed != null) revealed.transform.localPosition = p0 + Vector3.up * (0.4f + Tween.EaseOutBack(k) * 1.4f);
                        break;
                }
                yield return null;
            }
            t.localPosition = p0; t.localScale = s0; t.localRotation = r0;
            if (revealed != null)
            {
                yield return new WaitForSeconds(1.5f);
                Destroy(revealed);
            }
            _busy = false;
        }
    }

    public sealed class FaceCamera : MonoBehaviour
    {
        private Camera _cam;
        private void LateUpdate()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam != null) transform.rotation = Quaternion.LookRotation(transform.position - _cam.transform.position, Vector3.up);
        }
    }

    public sealed class Drift : MonoBehaviour
    {
        private Vector3 _p;
        private void OnEnable() => _p = transform.localPosition;
        private void Update() => transform.localPosition = _p + Vector3.right * Mathf.Sin(Time.time * 0.05f + _p.x) * 3f * Tween.Motion;
    }

    public sealed class Sway : MonoBehaviour
    {
        private Quaternion _r;
        private float _o;
        private void OnEnable() { _r = transform.localRotation; _o = Random.value * 6; }
        private void Update() => transform.localRotation = _r * Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 1.2f + _o) * 8f * Tween.Motion);
    }

    public sealed class Rise : MonoBehaviour
    {
        private Vector3 _p;
        private void OnEnable() => _p = transform.localPosition;
        private void Update()
        {
            Vector3 q = transform.localPosition + Vector3.up * Time.deltaTime * 0.4f * Mathf.Max(0.4f, Tween.Motion);
            if (q.y > 7f) q.y = 0.3f;
            transform.localPosition = new Vector3(_p.x + Mathf.Sin(Time.time + _p.z) * 0.2f, q.y, _p.z);
        }
    }
}
