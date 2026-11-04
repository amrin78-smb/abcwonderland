using System.Collections.Generic;
using System.IO;
using ABCWonderworld.UI;
using UnityEditor;
using UnityEngine;
using static ABCWonderworld.EditorTools.Painter;
using E = ABCWonderworld.EditorTools.EditorAssetUtil;

namespace ABCWonderworld.EditorTools
{
    /// <summary>
    /// Generates UI sprites and the procedural placeholder pictures (words with no suitable emoji, and
    /// tintable props). Configures import settings for downloaded Noto pictures. All output is PLACEHOLDER art
    /// tracked in Documentation/ASSET_REGISTER.md.
    /// </summary>
    public static class ArtPipeline
    {
        public const string UiDir = E.Root + "/Art/UI/Generated";
        public const string PicturesDir = E.Root + "/Resources/Pictures";
        public const string FontPath = E.Root + "/Art/UI/Fredoka-SemiBold.ttf";

        private static readonly Color Ink = new Color(0.24f, 0.18f, 0.3f);
        private static Vector2 V(float x, float y) => new Vector2(x, y);

        public static void ConfigurePictureImports()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { PicturesDir }))
                E.ConfigureSprite(AssetDatabase.GUIDToAssetPath(guid), 256, default, true);
        }

        public static UiSkin BuildUiSkin()
        {
            UiSkin skin = E.LoadOrCreateSO<UiSkin>(E.Root + "/Resources/UiSkin.asset");
            Color w = Color.white;
            skin.rounded = E.WriteSprite(UiDir + "/rounded.png", new Painter(128).Fill(Box(V(0.5f, 0.5f), V(0.5f, 0.5f), 0.38f), w).ToTexture(), 128, new Vector4(52, 52, 52, 52));
            skin.circle = Sprite("circle", new Painter(256).Fill(Circle(V(0.5f, 0.5f), 0.5f), w));
            skin.ring = Sprite("ring", new Painter(256).Fill(Arc(V(0.5f, 0.5f), 0.46f, 0.035f, 0, 359.9f), w));
            skin.star = Sprite("star", new Painter(256).Fill(Rounded(Star(V(0.5f, 0.48f), 0.44f, 0.2f), 0.03f), w));
            skin.iconStarOutline = Sprite("icon_star_outline", new Painter(256).Fill(Subtract(Rounded(Star(V(0.5f, 0.48f), 0.44f, 0.2f), 0.03f), Rounded(Star(V(0.5f, 0.48f), 0.31f, 0.13f), 0.01f)), w));
            skin.iconHome = Sprite("icon_home", new Painter(256)
                .Fill(Rounded(Polygon(V(0.12f, 0.52f), V(0.5f, 0.86f), V(0.88f, 0.52f)), 0.03f), w)
                .Fill(Box(V(0.5f, 0.34f), V(0.27f, 0.22f), 0.04f), w)
                .Erase(Box(V(0.5f, 0.24f), V(0.07f, 0.12f), 0.05f)));
            skin.iconBack = Sprite("icon_back", Arrow(new Painter(256), false));
            skin.iconArrow = Sprite("icon_arrow", Arrow(new Painter(256), true));
            skin.iconSpeaker = Sprite("icon_speaker", new Painter(256)
                .Fill(Box(V(0.24f, 0.5f), V(0.08f, 0.11f), 0.02f), w)
                .Fill(Rounded(Polygon(V(0.26f, 0.6f), V(0.48f, 0.78f), V(0.48f, 0.22f), V(0.26f, 0.4f)), 0.02f), w)
                .Fill(Arc(V(0.5f, 0.5f), 0.14f, 0.035f, -45, 45), w)
                .Fill(Arc(V(0.5f, 0.5f), 0.27f, 0.035f, -45, 45), w));
            skin.iconPlay = Sprite("icon_play", new Painter(256).Fill(Rounded(Polygon(V(0.33f, 0.2f), V(0.33f, 0.8f), V(0.8f, 0.5f)), 0.04f), w));
            skin.iconPause = Sprite("icon_pause", new Painter(256).Fill(Box(V(0.36f, 0.5f), V(0.08f, 0.28f), 0.05f), w).Fill(Box(V(0.64f, 0.5f), V(0.08f, 0.28f), 0.05f), w));
            skin.iconLock = Sprite("icon_lock", new Painter(256)
                .Fill(Arc(V(0.5f, 0.56f), 0.17f, 0.045f, 0, 180), w)
                .Fill(Capsule(V(0.33f, 0.56f), V(0.33f, 0.45f), 0.045f), w).Fill(Capsule(V(0.67f, 0.56f), V(0.67f, 0.45f), 0.045f), w)
                .Fill(Box(V(0.5f, 0.32f), V(0.26f, 0.2f), 0.05f), w)
                .Erase(Circle(V(0.5f, 0.34f), 0.05f)));
            skin.iconCheck = Sprite("icon_check", new Painter(256).Fill(Union(Capsule(V(0.2f, 0.5f), V(0.42f, 0.28f), 0.07f), Capsule(V(0.42f, 0.28f), V(0.8f, 0.72f), 0.07f)), w));
            skin.iconHand = Sprite("icon_hand", Hand(new Painter(256)));
            skin.iconBook = Sprite("icon_book", new Painter(256)
                .Fill(Rounded(Polygon(V(0.1f, 0.25f), V(0.48f, 0.2f), V(0.48f, 0.78f), V(0.1f, 0.8f)), 0.03f), w)
                .Fill(Rounded(Polygon(V(0.52f, 0.2f), V(0.9f, 0.25f), V(0.9f, 0.8f), V(0.52f, 0.78f)), 0.03f), w));
            skin.iconMusic = Sprite("icon_music", new Painter(256)
                .Fill(Ellipse(V(0.33f, 0.27f), V(0.12f, 0.09f)), w).Fill(Ellipse(V(0.71f, 0.33f), V(0.12f, 0.09f)), w)
                .Fill(Capsule(V(0.43f, 0.28f), V(0.43f, 0.8f), 0.035f), w).Fill(Capsule(V(0.81f, 0.34f), V(0.81f, 0.86f), 0.035f), w)
                .Fill(Capsule(V(0.43f, 0.8f), V(0.81f, 0.86f), 0.06f), w));
            skin.iconPuzzle = Sprite("icon_puzzle", new Painter(256)
                .Fill(Union(Box(V(0.46f, 0.42f), V(0.28f, 0.28f), 0.05f), Circle(V(0.46f, 0.76f), 0.1f), Circle(V(0.8f, 0.42f), 0.1f)), w)
                .Erase(Circle(V(0.18f, 0.42f), 0.09f)));
            skin.iconPencil = Sprite("icon_pencil", new Painter(256)
                .Fill(Capsule(V(0.32f, 0.32f), V(0.74f, 0.74f), 0.1f), w)
                .Fill(Rounded(Polygon(V(0.16f, 0.16f), V(0.36f, 0.22f), V(0.22f, 0.36f)), 0.02f), w));
            skin.iconSearch = Sprite("icon_search", new Painter(256).Fill(Arc(V(0.43f, 0.57f), 0.22f, 0.05f, 0, 359.9f), w).Fill(Capsule(V(0.6f, 0.4f), V(0.8f, 0.2f), 0.07f), w));
            skin.iconMatch = Sprite("icon_match", new Painter(256)
                .Fill(Circle(V(0.25f, 0.5f), 0.15f), w).Fill(Circle(V(0.75f, 0.5f), 0.15f), w)
                .Fill(Capsule(V(0.35f, 0.5f), V(0.65f, 0.5f), 0.04f), w));
            skin.iconRefresh = Sprite("icon_refresh", new Painter(256)
                .Fill(Arc(V(0.5f, 0.5f), 0.27f, 0.05f, 60, 340), w)
                .Fill(Rounded(Polygon(V(0.6f, 0.86f), V(0.82f, 0.72f), V(0.58f, 0.6f)), 0.02f), w));
            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            skin.font = font;
            EditorUtility.SetDirty(skin);
            return skin;
        }

        private static Sprite Sprite(string name, Painter p) => E.WriteSprite($"{UiDir}/{name}.png", p.ToTexture(), 256);

        private static Painter Arrow(Painter p, bool right)
        {
            float s = right ? 1 : -1;
            Vector2 c = V(0.5f, 0.5f);
            Vector2 tip = c + V(0.3f * s, 0), tail = c - V(0.28f * s, 0);
            return p.Fill(Union(Capsule(tail, tip, 0.07f), Capsule(tip, tip + V(-0.22f * s, 0.22f), 0.07f), Capsule(tip, tip + V(-0.22f * s, -0.22f), 0.07f)), Color.white);
        }

        private static Painter Hand(Painter p)
        {
            Color skin = new Color(1f, 0.85f, 0.7f);
            Sdf palm = Union(Box(V(0.5f, 0.3f), V(0.2f, 0.18f), 0.1f), Capsule(V(0.45f, 0.45f), V(0.45f, 0.86f), 0.075f),
                Capsule(V(0.58f, 0.42f), V(0.6f, 0.55f), 0.07f), Capsule(V(0.69f, 0.38f), V(0.7f, 0.5f), 0.065f), Capsule(V(0.3f, 0.3f), V(0.24f, 0.46f), 0.065f));
            return p.Fill(palm, skin, 0.025f, Ink).Shade(palm, 0.9f, 0.1f, 0.15f);
        }

        // ------------------------------------------------------------------ pictures (512 px, outlined to sit with Noto art)
        public static void BuildPictures()
        {
            Pic("igloo", Igloo());
            Pic("jam", Jam());
            Pic("lamp", Lamp());
            Pic("quilt", Quilt());
            Pic("xylophone", Xylophone());
            Pic("zipper", Zipper());
            Pic("zoo", Zoo());
            Pic("balloon", Balloon());
            Pic("bubble", Bubble());
            Pic("bush", Bush());
            Pic("mane", Mane());
            Pic("stripe", new Painter(256).Fill(Rounded(Polygon(V(0.2f, 0.1f), V(0.4f, 0.1f), V(0.8f, 0.9f), V(0.6f, 0.9f)), 0.06f), new Color(0.15f, 0.15f, 0.2f)), 256);
            Pic("cushion", Cushion());
            Pic("cone", Cone());
            Pic("scoop", Scoop());
            Pic("bucket", Bucket());
            Pic("raindrop", new Painter(256).Fill(Union(Circle(V(0.5f, 0.38f), 0.22f), Polygon(V(0.29f, 0.42f), V(0.5f, 0.88f), V(0.71f, 0.42f))), Color.white, 0.02f, new Color(0.3f, 0.5f, 0.8f)), 256);
            Pic("roof", new Painter(256).Fill(Rounded(Polygon(V(0.08f, 0.25f), V(0.5f, 0.82f), V(0.92f, 0.25f)), 0.03f), new Color(0.9f, 0.3f, 0.3f), 0.02f, Ink), 256);
            Pic("wall", new Painter(256).Fill(Box(V(0.5f, 0.5f), V(0.42f, 0.36f), 0.03f), new Color(1f, 0.9f, 0.65f), 0.02f, Ink), 256);
            Pic("door", new Painter(256).Fill(Box(V(0.5f, 0.45f), V(0.22f, 0.38f), 0.12f), new Color(0.6f, 0.38f, 0.22f), 0.025f, Ink).Fill(Circle(V(0.62f, 0.42f), 0.03f), new Color(1f, 0.85f, 0.3f)), 256);
            Pic("window", new Painter(256).Fill(Box(V(0.5f, 0.5f), V(0.36f, 0.36f), 0.05f), new Color(0.6f, 0.85f, 1f), 0.025f, Ink)
                .Fill(Box(V(0.5f, 0.5f), V(0.02f, 0.36f)), Ink).Fill(Box(V(0.5f, 0.5f), V(0.36f, 0.02f)), Ink), 256);
            Pic("lion_face", LionFace());
            Pic("turtle_plain", TurtlePlain());
            Pic("zebra_plain", ZebraPlain());
            Pic("crown", new Painter(256).Fill(Rounded(Polygon(V(0.15f, 0.25f), V(0.85f, 0.25f), V(0.88f, 0.72f), V(0.7f, 0.5f), V(0.5f, 0.78f), V(0.3f, 0.5f), V(0.12f, 0.72f)), 0.03f), Color.white, 0.025f, new Color(0.45f, 0.4f, 0.5f))
                .Fill(Circle(V(0.5f, 0.38f), 0.05f), new Color(0.9f, 0.9f, 0.95f)), 256);
            Pic("ice", new Painter(256).Fill(Box(V(0.5f, 0.5f), V(0.44f, 0.2f), 0.12f), new Color(0.82f, 0.95f, 1f), 0.025f, new Color(0.4f, 0.65f, 0.85f)), 256);
        }

        private static void Pic(string name, Painter p, int max = 256) =>
            E.WriteSprite($"{PicturesDir}/draw_{name}.png", p.ToTexture(), max, default, true);

        private static Painter Igloo()
        {
            var p = new Painter(512);
            Color snow = new Color(0.93f, 0.97f, 1f), line = new Color(0.62f, 0.75f, 0.88f);
            Sdf dome = Intersect(Ellipse(V(0.5f, 0.3f), V(0.4f, 0.44f)), Box(V(0.5f, 0.55f), V(0.5f, 0.25f)));
            p.Fill(Ellipse(V(0.5f, 0.3f), V(0.46f, 0.06f)), new Color(0.85f, 0.92f, 1f));
            p.Fill(dome, snow, 0.012f, Ink).Shade(dome, 0.74f, 0.3f, 0.18f);
            foreach (float y in new[] { 0.42f, 0.54f, 0.64f })
                p.Fill(Intersect(Box(V(0.5f, y), V(0.5f, 0.006f)), Ellipse(V(0.5f, 0.3f), V(0.39f, 0.43f))), line);
            p.Fill(Intersect(Box(V(0.3f, 0.48f), V(0.006f, 0.06f)), dome), line).Fill(Intersect(Box(V(0.62f, 0.48f), V(0.006f, 0.06f)), dome), line)
             .Fill(Intersect(Box(V(0.45f, 0.59f), V(0.006f, 0.05f)), dome), line);
            Sdf door = Intersect(Ellipse(V(0.5f, 0.3f), V(0.12f, 0.17f)), Box(V(0.5f, 0.38f), V(0.2f, 0.08f)));
            p.Fill(door, new Color(0.25f, 0.32f, 0.45f), 0.012f, Ink);
            return p;
        }

        private static Painter Jam()
        {
            var p = new Painter(512);
            Sdf jar = Box(V(0.5f, 0.4f), V(0.25f, 0.28f), 0.08f);
            p.Fill(jar, new Color(0.85f, 0.15f, 0.25f), 0.014f, Ink).Shade(jar, 0.68f, 0.12f, 0.3f);
            p.Fill(Capsule(V(0.34f, 0.25f), V(0.34f, 0.55f), 0.025f), new Color(1, 1, 1, 0.45f));
            p.Fill(Box(V(0.5f, 0.42f), V(0.15f, 0.1f), 0.03f), new Color(1f, 0.96f, 0.85f), 0.01f, Ink);
            p.Fill(Circle(V(0.5f, 0.42f), 0.05f), new Color(0.85f, 0.15f, 0.25f));
            Sdf lid = Union(Box(V(0.5f, 0.72f), V(0.3f, 0.06f), 0.04f), Circle(V(0.5f, 0.8f), 0.04f));
            p.Fill(lid, new Color(0.95f, 0.95f, 0.95f), 0.014f, Ink);
            for (int i = 0; i < 6; i++)
                p.Fill(Intersect(Box(V(0.25f + i * 0.1f, 0.72f), V(0.025f, 0.06f)), lid), new Color(0.9f, 0.25f, 0.3f));
            return p;
        }

        private static Painter Lamp()
        {
            var p = new Painter(512);
            p.Fill(Circle(V(0.5f, 0.62f), 0.36f), new Color(1f, 0.95f, 0.6f, 0.35f));
            Sdf shade = Rounded(Polygon(V(0.26f, 0.5f), V(0.74f, 0.5f), V(0.62f, 0.84f), V(0.38f, 0.84f)), 0.02f);
            p.Fill(Capsule(V(0.5f, 0.18f), V(0.5f, 0.5f), 0.03f), new Color(0.55f, 0.4f, 0.3f), 0.012f, Ink);
            p.Fill(Ellipse(V(0.5f, 0.14f), V(0.2f, 0.05f)), new Color(0.6f, 0.45f, 0.35f), 0.012f, Ink);
            p.Fill(shade, new Color(1f, 0.8f, 0.3f), 0.014f, Ink).Shade(shade, 0.84f, 0.5f, 0.25f);
            return p;
        }

        private static Painter Quilt()
        {
            var p = new Painter(512);
            Color[] cols = { new Color(1f, 0.5f, 0.5f), new Color(1f, 0.85f, 0.35f), new Color(0.45f, 0.75f, 1f), new Color(0.55f, 0.85f, 0.5f), new Color(0.8f, 0.6f, 0.95f) };
            Sdf all = Box(V(0.5f, 0.5f), V(0.4f, 0.36f), 0.04f);
            p.Fill(all, Color.white, 0.014f, Ink);
            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 4; x++)
                {
                    Vector2 c = V(0.2f + x * 0.2f, 0.26f + y * 0.24f);
                    p.Fill(Intersect(Box(c, V(0.095f, 0.115f), 0.015f), all), cols[(x + y * 2) % cols.Length]);
                    p.Fill(Intersect(Circle(c, 0.035f), all), new Color(1, 1, 1, 0.6f));
                }
            return p;
        }

        private static Painter Xylophone()
        {
            var p = new Painter(512);
            Color[] cols = { new Color(0.95f, 0.3f, 0.3f), new Color(1f, 0.6f, 0.2f), new Color(1f, 0.85f, 0.25f), new Color(0.4f, 0.8f, 0.4f), new Color(0.3f, 0.6f, 0.95f), new Color(0.65f, 0.45f, 0.9f) };
            p.Fill(Capsule(V(0.12f, 0.62f), V(0.88f, 0.52f), 0.02f), new Color(0.55f, 0.38f, 0.25f), 0.01f, Ink);
            p.Fill(Capsule(V(0.12f, 0.3f), V(0.88f, 0.4f), 0.02f), new Color(0.55f, 0.38f, 0.25f), 0.01f, Ink);
            for (int i = 0; i < 6; i++)
            {
                float x = 0.18f + i * 0.128f;
                float h = 0.26f - i * 0.022f;
                Sdf bar = Box(V(x, 0.47f), V(0.05f, h), 0.025f);
                p.Fill(bar, cols[i], 0.012f, Ink).Shade(bar, 0.47f + h, 0.47f - h, 0.2f);
                p.Fill(Circle(V(x, 0.47f + h * 0.7f), 0.012f), Ink).Fill(Circle(V(x, 0.47f - h * 0.7f), 0.012f), Ink);
            }
            p.Fill(Capsule(V(0.62f, 0.86f), V(0.82f, 0.7f), 0.012f), new Color(0.6f, 0.45f, 0.3f), 0.006f, Ink).Fill(Circle(V(0.62f, 0.86f), 0.04f), new Color(0.95f, 0.3f, 0.3f), 0.01f, Ink);
            return p;
        }

        private static Painter Zipper()
        {
            var p = new Painter(512);
            Color tape = new Color(0.3f, 0.55f, 0.9f);
            p.Fill(Box(V(0.38f, 0.5f), V(0.1f, 0.42f), 0.03f), tape, 0.012f, Ink).Fill(Box(V(0.62f, 0.5f), V(0.1f, 0.42f), 0.03f), tape, 0.012f, Ink);
            for (int i = 0; i < 12; i++)
            {
                float y = 0.14f + i * 0.055f;
                bool left = i % 2 == 0;
                p.Fill(Box(V(left ? 0.47f : 0.53f, y), V(0.045f, 0.018f), 0.008f), new Color(0.85f, 0.85f, 0.9f), 0.006f, Ink);
            }
            Sdf slider = Box(V(0.5f, 0.78f), V(0.08f, 0.06f), 0.03f);
            p.Fill(slider, new Color(0.8f, 0.8f, 0.85f), 0.012f, Ink);
            p.Fill(Box(V(0.5f, 0.66f), V(0.04f, 0.08f), 0.03f), new Color(0.8f, 0.8f, 0.85f), 0.012f, Ink).Erase(Box(V(0.5f, 0.64f), V(0.015f, 0.04f), 0.01f));
            return p;
        }

        private static Painter Zoo()
        {
            var p = new Painter(512);
            Color wood = new Color(0.62f, 0.42f, 0.25f), green = new Color(0.45f, 0.75f, 0.35f);
            p.Fill(Ellipse(V(0.5f, 0.12f), V(0.48f, 0.06f)), green);
            Sdf postL = Box(V(0.18f, 0.4f), V(0.05f, 0.3f), 0.02f), postR = Box(V(0.82f, 0.4f), V(0.05f, 0.3f), 0.02f);
            p.Fill(postL, wood, 0.012f, Ink).Fill(postR, wood, 0.012f, Ink);
            for (int i = 0; i < 6; i++) p.Fill(Capsule(V(0.29f + i * 0.084f, 0.12f), V(0.29f + i * 0.084f, 0.5f), 0.012f), new Color(0.35f, 0.35f, 0.4f));
            Sdf sign = Box(V(0.5f, 0.74f), V(0.4f, 0.13f), 0.05f);
            p.Fill(sign, new Color(1f, 0.85f, 0.35f), 0.014f, Ink).Shade(sign, 0.87f, 0.61f, 0.2f);
            Color letters = new Color(0.3f, 0.25f, 0.45f);
            p.Fill(Letter('Z', new Rect(0.2f, 0.66f, 0.15f, 0.16f), 0.022f), letters);
            p.Fill(Letter('O', new Rect(0.41f, 0.66f, 0.16f, 0.16f), 0.022f), letters);
            p.Fill(Letter('O', new Rect(0.63f, 0.66f, 0.16f, 0.16f), 0.022f), letters);
            return p;
        }

        private static Painter Balloon()
        {
            var p = new Painter(512);
            p.Fill(Capsule(V(0.5f, 0.22f), V(0.47f, 0.02f), 0.006f), new Color(0.5f, 0.5f, 0.55f));
            Sdf body = Ellipse(V(0.5f, 0.55f), V(0.3f, 0.36f));
            p.Fill(Rounded(Polygon(V(0.45f, 0.17f), V(0.55f, 0.17f), V(0.5f, 0.22f)), 0.01f), Color.white, 0.008f, new Color(0.6f, 0.6f, 0.65f));
            p.Fill(body, Color.white, 0.01f, new Color(0.6f, 0.6f, 0.65f)).Shade(body, 0.9f, 0.2f, 0.18f);
            p.Fill(Ellipse(V(0.4f, 0.7f), V(0.05f, 0.09f)), new Color(1, 1, 1, 0.8f));
            return p;
        }

        private static Painter Bubble()
        {
            var p = new Painter(512);
            p.Fill(Circle(V(0.5f, 0.5f), 0.44f), new Color(0.8f, 0.93f, 1f, 0.35f));
            p.Fill(Arc(V(0.5f, 0.5f), 0.43f, 0.012f, 0, 359.9f), new Color(0.6f, 0.85f, 1f, 0.9f));
            p.Fill(Arc(V(0.5f, 0.5f), 0.34f, 0.03f, 110, 160), new Color(1, 1, 1, 0.9f));
            p.Fill(Circle(V(0.68f, 0.32f), 0.035f), new Color(1, 1, 1, 0.7f));
            return p;
        }

        private static Painter Bush()
        {
            var p = new Painter(512);
            Sdf b = Union(Circle(V(0.3f, 0.36f), 0.2f), Circle(V(0.5f, 0.48f), 0.26f), Circle(V(0.72f, 0.38f), 0.2f), Box(V(0.5f, 0.25f), V(0.38f, 0.1f), 0.08f));
            p.Fill(b, new Color(0.35f, 0.72f, 0.35f), 0.014f, new Color(0.15f, 0.35f, 0.18f)).Shade(b, 0.74f, 0.15f, 0.25f);
            p.Fill(Circle(V(0.4f, 0.55f), 0.03f), new Color(1f, 0.5f, 0.6f)).Fill(Circle(V(0.65f, 0.47f), 0.03f), new Color(1f, 0.9f, 0.4f));
            return p;
        }

        private static Painter Mane()
        {
            var p = new Painter(256);
            Sdf m = Union(Circle(V(0.5f, 0.55f), 0.3f), Circle(V(0.5f, 0.3f), 0.22f));
            p.Fill(m, new Color(0.95f, 0.55f, 0.15f), 0.02f, new Color(0.6f, 0.3f, 0.1f)).Shade(m, 0.85f, 0.1f, 0.25f);
            return p;
        }

        private static Painter LionFace()
        {
            var p = new Painter(512);
            Color fur = new Color(1f, 0.8f, 0.4f), dark = new Color(0.35f, 0.22f, 0.15f);
            p.Fill(Circle(V(0.3f, 0.72f), 0.08f), fur, 0.014f, Ink).Fill(Circle(V(0.7f, 0.72f), 0.08f), fur, 0.014f, Ink);
            Sdf face = Circle(V(0.5f, 0.5f), 0.27f);
            p.Fill(face, fur, 0.014f, Ink).Shade(face, 0.77f, 0.23f, 0.15f);
            p.Fill(Ellipse(V(0.5f, 0.4f), V(0.13f, 0.09f)), new Color(1f, 0.93f, 0.8f));
            p.Fill(Circle(V(0.4f, 0.56f), 0.035f), dark).Fill(Circle(V(0.6f, 0.56f), 0.035f), dark);
            p.Fill(Rounded(Polygon(V(0.45f, 0.46f), V(0.55f, 0.46f), V(0.5f, 0.4f)), 0.015f), dark);
            p.Fill(Arc(V(0.5f, 0.38f), 0.05f, 0.008f, 200, 340), dark);
            return p;
        }

        private static Painter TurtlePlain()
        {
            var p = new Painter(512);
            Color skin = new Color(0.55f, 0.8f, 0.45f), shell = new Color(0.45f, 0.62f, 0.35f);
            foreach (float x in new[] { 0.3f, 0.7f }) p.Fill(Ellipse(V(x, 0.28f), V(0.07f, 0.09f)), skin, 0.012f, Ink);
            p.Fill(Circle(V(0.86f, 0.42f), 0.09f), skin, 0.012f, Ink).Fill(Circle(V(0.89f, 0.45f), 0.016f), Ink);
            Sdf dome = Intersect(Ellipse(V(0.5f, 0.35f), V(0.36f, 0.32f)), Box(V(0.5f, 0.6f), V(0.5f, 0.25f)));
            p.Fill(dome, shell, 0.014f, Ink).Shade(dome, 0.67f, 0.35f, 0.2f);
            p.Fill(Box(V(0.5f, 0.35f), V(0.37f, 0.025f), 0.02f), new Color(0.8f, 0.7f, 0.4f), 0.01f, Ink);
            return p;
        }

        private static Painter ZebraPlain()
        {
            var p = new Painter(512);
            Color w = new Color(0.97f, 0.97f, 0.97f), k = new Color(0.15f, 0.15f, 0.2f);
            foreach (float x in new[] { 0.3f, 0.4f, 0.6f, 0.7f }) p.Fill(Capsule(V(x, 0.12f), V(x, 0.38f), 0.035f), w, 0.012f, Ink).Fill(Circle(V(x, 0.11f), 0.035f), k);
            p.Fill(Capsule(V(0.22f, 0.5f), V(0.13f, 0.32f), 0.015f), k);
            Sdf body = Ellipse(V(0.5f, 0.45f), V(0.27f, 0.14f));
            p.Fill(body, w, 0.014f, Ink);
            p.Fill(Capsule(V(0.7f, 0.5f), V(0.78f, 0.72f), 0.07f), w, 0.014f, Ink);
            p.Fill(Ellipse(V(0.82f, 0.74f), V(0.1f, 0.07f)), w, 0.014f, Ink).Fill(Ellipse(V(0.9f, 0.71f), V(0.035f, 0.03f)), new Color(0.3f, 0.3f, 0.35f));
            p.Fill(Circle(V(0.8f, 0.77f), 0.014f), k).Fill(Capsule(V(0.75f, 0.82f), V(0.73f, 0.89f), 0.02f), w, 0.01f, Ink);
            p.Fill(Capsule(V(0.66f, 0.56f), V(0.74f, 0.8f), 0.022f), k);
            return p;
        }

        private static Painter Cushion()
        {
            var p = new Painter(256);
            Sdf c = Box(V(0.5f, 0.42f), V(0.4f, 0.2f), 0.14f);
            p.Fill(c, Color.white, 0.02f, new Color(0.5f, 0.5f, 0.6f)).Shade(c, 0.62f, 0.22f, 0.2f);
            return p;
        }

        private static Painter Cone()
        {
            var p = new Painter(256);
            Sdf c = Rounded(Polygon(V(0.28f, 0.66f), V(0.72f, 0.66f), V(0.5f, 0.08f)), 0.03f);
            p.Fill(c, new Color(0.9f, 0.7f, 0.4f), 0.02f, new Color(0.55f, 0.35f, 0.2f));
            p.Fill(Intersect(Capsule(V(0.3f, 0.5f), V(0.6f, 0.2f), 0.008f), c), new Color(0.7f, 0.5f, 0.3f));
            p.Fill(Intersect(Capsule(V(0.7f, 0.5f), V(0.4f, 0.2f), 0.008f), c), new Color(0.7f, 0.5f, 0.3f));
            return p;
        }

        private static Painter Scoop()
        {
            var p = new Painter(256);
            Sdf s = Union(Circle(V(0.5f, 0.55f), 0.32f), Circle(V(0.3f, 0.3f), 0.1f), Circle(V(0.5f, 0.27f), 0.1f), Circle(V(0.7f, 0.3f), 0.1f));
            p.Fill(s, Color.white, 0.02f, new Color(0.55f, 0.55f, 0.6f)).Shade(s, 0.87f, 0.2f, 0.2f);
            return p;
        }

        private static Painter Bucket()
        {
            var p = new Painter(512);
            Sdf b = Rounded(Polygon(V(0.22f, 0.7f), V(0.78f, 0.7f), V(0.68f, 0.12f), V(0.32f, 0.12f)), 0.03f);
            p.Fill(Arc(V(0.5f, 0.7f), 0.28f, 0.015f, 0, 180), new Color(0.5f, 0.5f, 0.55f));
            p.Fill(b, new Color(0.95f, 0.75f, 0.3f), 0.014f, Ink).Shade(b, 0.7f, 0.12f, 0.25f);
            p.Fill(Ellipse(V(0.5f, 0.7f), V(0.28f, 0.05f)), new Color(0.75f, 0.55f, 0.2f), 0.01f, Ink);
            return p;
        }
    }
}
