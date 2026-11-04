using System;
using System.Collections;
using System.Collections.Generic;
using ABCWonderworld.Content;
using ABCWonderworld.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ABCWonderworld.Activities
{
    /// <summary>
    /// Short, warm completion moment: soft confetti, star award and any new sticker, then large choices
    /// (again / next / back). Returns control to the child within a couple of seconds; no flashing.
    /// </summary>
    public sealed class Celebration : MonoBehaviour
    {
        private readonly List<RectTransform> _confetti = new List<RectTransform>();
        private readonly List<Vector2> _vel = new List<Vector2>();

        public static Celebration Show(RectTransform uiRoot, int stars, IList<RewardDefinition> newRewards,
            Action again, Action next, Action back)
        {
            UiSkin skin = UiSkin.Load();
            // Clear the stage: activity widgets fade out so choices never overlap them (HUD exit stays).
            foreach (Transform child in uiRoot)
            {
                if (child.name == "Exit") continue;
                if (!child.TryGetComponent(out CanvasGroup cg)) cg = child.gameObject.AddComponent<CanvasGroup>();
                cg.interactable = false;
                cg.blocksRaycasts = false;
                cg.alpha = 0f;
            }
            RectTransform root = UiFactory.Rect("Celebration", uiRoot);
            UiFactory.Stretch(root);
            Image dim = UiFactory.Image("Dim", root, null, new Color(1f, 0.98f, 0.9f, 0f), true);
            UiFactory.Stretch(dim.rectTransform);
            var c = root.gameObject.AddComponent<Celebration>();
            c.StartCoroutine(c.Run(root, dim, stars, newRewards, again, next, back, skin));
            return c;
        }

        private IEnumerator Run(RectTransform root, Image dim, int stars, IList<RewardDefinition> rewards, Action again, Action next, Action back, UiSkin skin)
        {
            var palette = new[] { new Color(1f, 0.45f, 0.45f), new Color(1f, 0.8f, 0.3f), new Color(0.4f, 0.8f, 1f), new Color(0.5f, 0.85f, 0.45f), new Color(0.8f, 0.55f, 1f) };
            int count = Tween.Motion < 0.5f ? 14 : 40;
            for (int i = 0; i < count; i++)
            {
                Image p = UiFactory.Image("Confetti", root, i % 3 == 0 ? skin.star : skin.circle, palette[i % palette.Length]);
                p.rectTransform.sizeDelta = Vector2.one * UnityEngine.Random.Range(26f, 48f);
                p.rectTransform.anchoredPosition = new Vector2(UnityEngine.Random.Range(-200f, 200f), -100f);
                _confetti.Add(p.rectTransform);
                _vel.Add(new Vector2(UnityEngine.Random.Range(-700f, 700f), UnityEngine.Random.Range(900f, 1500f)) * Mathf.Max(0.4f, Tween.Motion));
            }
            for (float t = 0; t < 0.3f; t += Time.deltaTime) { dim.color = new Color(1f, 0.98f, 0.9f, t / 0.3f * 0.55f); yield return null; }

            // Star(s)
            RectTransform starRow = UiFactory.Rect("Stars", root);
            UiFactory.Place(starRow, new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(600, 260));
            for (int i = 0; i < Mathf.Max(1, stars); i++)
            {
                Image s = UiFactory.Image("Star", starRow, skin.star, new Color(1f, 0.8f, 0.2f));
                s.rectTransform.sizeDelta = new Vector2(240, 240);
                s.rectTransform.anchoredPosition = new Vector2((i - (stars - 1) * 0.5f) * 260f, 0);
                s.transform.localScale = Vector3.zero;
                Transform st = s.transform;
                Tween.Run(this, 0.5f, k => st.localScale = Vector3.one * Tween.EaseOutBack(k), () => st.gameObject.AddComponent<Attention>().amount = 0.05f);
            }
            if (rewards != null && rewards.Count > 0)
            {
                RewardDefinition r = rewards[0];
                Image card = UiFactory.Image("NewSticker", root, skin.rounded, Color.white);
                UiFactory.Place(card.rectTransform, new Vector2(0.82f, 0.62f), Vector2.zero, new Vector2(260, 260));
                UiFactory.Shadow(card.transform, 260);
                if (r.icon != null)
                {
                    Image ic = UiFactory.Image("Icon", card.transform, r.icon, Color.white);
                    UiFactory.Stretch(ic.rectTransform, 36);
                }
                card.transform.localScale = Vector3.zero;
                Transform ct = card.transform;
                Tween.Run(this, 0.5f, k => ct.localScale = Vector3.one * Tween.EaseOutBack(k));
            }

            yield return new WaitForSeconds(0.9f);
            RectTransform row = UiFactory.Rect("Choices", root);
            UiFactory.Place(row, new Vector2(0.5f, 0.24f), Vector2.zero, new Vector2(900, 240));
            float x = -300;
            if (back != null) { var b = UiFactory.IconButton("Back", row, skin.iconHome, skin.secondary, 200, () => back(), true); b.transform.localPosition = new Vector3(x, 0); }
            if (again != null) { var b = UiFactory.IconButton("Again", row, skin.iconRefresh, skin.gentle, 200, () => again(), true); b.transform.localPosition = new Vector3(0, 0); }
            if (next != null)
            {
                var b = UiFactory.IconButton("Next", row, skin.iconArrow, skin.success, 230, () => next(), true);
                b.transform.localPosition = new Vector3(300, 0);
                b.gameObject.AddComponent<Attention>();
            }
        }

        private void Update()
        {
            for (int i = 0; i < _confetti.Count; i++)
            {
                if (_confetti[i] == null) continue;
                _vel[i] += Vector2.down * 1800f * Time.deltaTime;
                _confetti[i].anchoredPosition += _vel[i] * Time.deltaTime;
                _confetti[i].Rotate(0, 0, 200 * Time.deltaTime);
                if (_confetti[i].anchoredPosition.y < -900) { Destroy(_confetti[i].gameObject); _confetti[i] = null; }
            }
        }
    }
}
