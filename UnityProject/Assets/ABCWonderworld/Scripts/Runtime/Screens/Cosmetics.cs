using System.Linq;
using ABCWonderworld.Characters;
using ABCWonderworld.Content;
using ABCWonderworld.Core;
using ABCWonderworld.UI;
using ABCWonderworld.World;
using UnityEngine;

namespace ABCWonderworld.Screens
{
    /// <summary>
    /// GDD §9.3 cosmetic customisation: equipped hats/glasses are shown on every character; an equipped dance
    /// makes characters dance when tapped. Purely cosmetic, earned through ordinary play.
    /// </summary>
    public static class Cosmetics
    {
        public static void Apply(LetterCharacter c, GameServices s)
        {
            if (c == null || s?.Catalog == null) return;
            foreach (RewardDefinition r in s.Catalog.rewards.Where(r => r != null && s.Save.rewards.equipped.Contains(r.id)))
            {
                if (r.icon == null) continue;
                if (r.type == RewardType.Hat && c.aboveHeadAnchor != null)
                {
                    SpriteRenderer sr = EnvironmentBuilder.Billboard(c.aboveHeadAnchor, r.icon, new Vector3(0, -0.08f, 0), 0.42f);
                    sr.name = "Cosmetic_" + r.id;
                }
                else if (r.type == RewardType.Glasses && c.faceAnchor != null)
                {
                    SpriteRenderer sr = EnvironmentBuilder.Billboard(c.faceAnchor, r.icon, new Vector3(0, 0.035f, -0.06f), 0.2f);
                    sr.name = "Cosmetic_" + r.id;
                }
                else if (r.type == RewardType.Dance)
                {
                    c.Tapped += x => x.Play(CharacterState.Dance);
                }
            }
        }

        public static bool HasDecoration(GameServices s, string id) => s.Save.rewards.equipped.Contains(id);
    }

    /// <summary>Wardrobe overlay (from the Alphabet Book): earned items toggle on/off; locked items show the stars still to earn.</summary>
    public static class Wardrobe
    {
        public static RectTransform Show(RectTransform ui, GameServices s)
        {
            UiSkin skin = UiSkin.Load();
            RectTransform root = UiFactory.Rect("Wardrobe", ui);
            UiFactory.Stretch(root);
            UnityEngine.UI.Image dim = UiFactory.Image("Dim", root, null, new Color(0.2f, 0.15f, 0.3f, 0.4f), true);
            UiFactory.Stretch(dim.rectTransform);
            UnityEngine.UI.Image card = UiFactory.Image("Card", root, skin.rounded, Color.white, true);
            UiFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500, 760));
            ScaleToFit.Add(card.rectTransform, new Vector2(1500, 760), new Vector2(40, 30));
            var items = s.Catalog.rewards.Where(r => r != null && r.type != RewardType.Sticker).ToList();
            for (int i = 0; i < items.Count; i++)
            {
                RewardDefinition r = items[i];
                bool unlocked = s.Rewards.IsUnlocked(r.id);
                bool on = s.Save.rewards.equipped.Contains(r.id);
                Color bg = !unlocked ? new Color(0.85f, 0.85f, 0.88f) : on ? new Color(0.55f, 0.85f, 0.55f) : new Color(1f, 0.93f, 0.75f);
                BigButton b = UiFactory.IconButton("Item_" + r.id, card.transform, null, bg, 210, null);
                UiFactory.Place((RectTransform)b.transform, new Vector2(0.5f, 0.5f), new Vector2((i % 3 - 1) * 330, i < 3 ? 160 : -170), new Vector2(210, 210));
                UnityEngine.UI.Image ic = UiFactory.Image("Icon", b.transform, r.icon, unlocked ? Color.white : new Color(0.3f, 0.3f, 0.35f, 0.5f));
                UiFactory.Stretch(ic.rectTransform, 40);
                if (!unlocked)
                {
                    UnityEngine.UI.Image lk = UiFactory.Image("Lock", b.transform, skin.star, new Color(1f, 0.8f, 0.25f));
                    UiFactory.Place(lk.rectTransform, new Vector2(1, 0), new Vector2(-20, 20), new Vector2(90, 90));
                    UnityEngine.UI.Text t = UiFactory.Label("Need", lk.transform, r.amount.ToString(), 34, new Color(0.35f, 0.25f, 0.1f));
                    UiFactory.Stretch(t.rectTransform);
                    b.onClick.AddListener(() => s.Audio.Speak("vo.star", Audio.VoiceMode.IfIdle));
                }
                else
                {
                    b.sfx = "sparkle";
                    b.onClick.AddListener(() =>
                    {
                        s.Rewards.Equip(r.id);
                        Object.Destroy(root.gameObject);
                        Show(ui, s);
                    });
                }
            }
            BigButton close = UiFactory.IconButton("Close", card.transform, skin.iconCheck, skin.success, 130, () => Object.Destroy(root.gameObject));
            UiFactory.Place((RectTransform)close.transform, new Vector2(1, 1), new Vector2(-80, -80), new Vector2(130, 130));
            return root;
        }
    }
}
