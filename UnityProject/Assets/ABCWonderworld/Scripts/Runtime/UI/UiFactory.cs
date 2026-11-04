using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ABCWonderworld.UI
{
    /// <summary>Code-built uGUI with consistent styling. All child-facing sizes are in reference pixels (1920×1080).</summary>
    public static class UiFactory
    {
        public static readonly Vector2 Reference = new Vector2(1920, 1080);
        public const float ChildButton = 180f;   // ≥ 15 mm on a 6" phone
        public const float CornerButton = 150f;

        public static UiSkin Skin => UiSkin.Load();

        public static RectTransform Canvas(string name, int order = 0, Transform parent = null)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent != null) go.transform.SetParent(parent, false);
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Reference;
            scaler.matchWidthOrHeight = 0.65f;
            var safe = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaFitter));
            safe.transform.SetParent(go.transform, false);
            Stretch((RectTransform)safe.transform);
            return (RectTransform)safe.transform;
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static void Stretch(RectTransform rt, float inset = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
        }

        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Image(string name, Transform parent, Sprite sprite, Color color, bool raycast = false)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            if (sprite != null && sprite.border != Vector4.zero) img.type = UnityEngine.UI.Image.Type.Sliced;
            img.preserveAspect = sprite != null && sprite.border == Vector4.zero;
            return img;
        }

        public static Text Label(string name, Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = Skin != null && Skin.font != null ? Skin.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        public static GlyphGraphic Glyph(string name, Transform parent, char c, Color color, float thickness = 0.17f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(GlyphGraphic));
            go.transform.SetParent(parent, false);
            var g = go.GetComponent<GlyphGraphic>();
            g.SetGlyph(c);
            g.color = color;
            g.Thickness = thickness;
            g.raycastTarget = false;
            return g;
        }

        /// <summary>Round coloured button with an icon (or picture). Hit area extends beyond the visual.</summary>
        public static BigButton IconButton(string name, Transform parent, Sprite icon, Color bg, float size, UnityAction onClick, bool guarded = false)
        {
            Image back = Image(name, parent, Skin != null ? Skin.circle : null, bg, true);
            back.raycastPadding = new Vector4(-24, -24, -24, -24);
            back.rectTransform.sizeDelta = new Vector2(size, size);
            Shadow(back.transform, size);
            if (icon != null)
            {
                Image ic = Image("Icon", back.transform, icon, Color.white);
                Stretch(ic.rectTransform, size * 0.2f);
            }
            var b = back.gameObject.AddComponent<BigButton>();
            b.guarded = guarded;
            if (onClick != null) b.onClick.AddListener(onClick);
            return b;
        }

        /// <summary>Pill button with picture and short label (labels are for adults; children use icons and voice).</summary>
        public static BigButton PillButton(string name, Transform parent, string label, Sprite icon, Color bg, Vector2 size, UnityAction onClick, int fontSize = 52)
        {
            Image back = Image(name, parent, Skin != null ? Skin.rounded : null, bg, true);
            back.raycastPadding = new Vector4(-16, -16, -16, -16);
            back.rectTransform.sizeDelta = size;
            float x = 0;
            if (icon != null)
            {
                Image ic = Image("Icon", back.transform, icon, Color.white);
                Place(ic.rectTransform, new Vector2(0, 0.5f), new Vector2(size.y * 0.55f, 0), new Vector2(size.y * 0.7f, size.y * 0.7f));
                x = size.y * 0.45f;
            }
            if (!string.IsNullOrEmpty(label))
            {
                Text t = Label("Label", back.transform, label, fontSize, Color.white);
                Stretch(t.rectTransform);
                t.rectTransform.offsetMin = new Vector2(x + 12, 0);
            }
            var b = back.gameObject.AddComponent<BigButton>();
            if (onClick != null) b.onClick.AddListener(onClick);
            return b;
        }

        public static HoldButton HoldIconButton(string name, Transform parent, Sprite icon, Color bg, float size, UnityAction onHeld, float hold = 0.8f)
        {
            Image back = Image(name, parent, Skin != null ? Skin.circle : null, bg, true);
            back.raycastPadding = new Vector4(-16, -16, -16, -16);
            back.rectTransform.sizeDelta = new Vector2(size, size);
            Image ring = Image("Ring", back.transform, Skin != null ? Skin.ring : null, new Color(1, 1, 1, 0.95f));
            Stretch(ring.rectTransform, -10);
            ring.type = UnityEngine.UI.Image.Type.Filled;
            ring.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
            ring.fillOrigin = 2;
            ring.fillAmount = 0;
            if (icon != null)
            {
                Image ic = Image("Icon", back.transform, icon, Color.white);
                Stretch(ic.rectTransform, size * 0.22f);
            }
            var h = back.gameObject.AddComponent<HoldButton>();
            h.fill = ring;
            h.holdSeconds = hold;
            if (onHeld != null) h.onHeld.AddListener(onHeld);
            return h;
        }

        /// <summary>Soft drop shadow (uGUI Shadow effect draws an offset copy behind the graphic).</summary>
        public static void Shadow(Transform target, float size)
        {
            var sh = target.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            sh.effectColor = new Color(0.1f, 0.05f, 0.2f, 0.22f);
            sh.effectDistance = new Vector2(0, -Mathf.Max(4f, size * 0.05f));
        }

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;

        public static Color Lighten(Color c, float k) => Color.Lerp(c, Color.white, k);
        public static Color Darken(Color c, float k) => Color.Lerp(c, Color.black, k);
    }
}
