using System.Collections.Generic;
using System.Linq;

namespace ABCWonderworld.Content
{
    /// <summary>
    /// Resolves picture keys used in content data to resource names under Resources/Pictures.
    /// Keys: an emoji code ("1f34e" → emoji_u1f34e), "draw:name" or a drawn prop name (→ draw_name),
    /// or a named prop alias below.
    /// </summary>
    public static class ArtKeys
    {
        private static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>
        {
            { "egg", "1f95a" }, { "flower", "1f33c" }, { "shell", "1f41a" }, { "cloud", "2601" }, { "rock", "1faa8" },
            { "house", "1f3e0" }, { "water", "1f4a7" }, { "star", "2b50" }, { "crown", "1f451" }, { "tree", "1f333" },
            { "butterfly", "1f98b" }, { "bird", "1f426" }, { "sunflower", "1f33b" }, { "tulip", "1f337" }, { "mushroom", "1f344" },
            { "ladybug", "1f41e" }, { "bee", "1f41d" }, { "sparkles", "2728" }, { "rainbow", "1f308" }, { "wave", "1f30a" },
            { "coral", "1fab8" }, { "tropical_fish", "1f420" }, { "dolphin", "1f42c" }, { "lollipop", "1f36d" }, { "candy", "1f36c" },
            { "cupcake", "1f9c1" }, { "doughnut", "1f369" }, { "palm", "1f334" }, { "trex", "1f996" }, { "paws", "1f43e" },
            { "planet", "1fa90" }, { "rocket", "1f680" }, { "moon", "1f319" }, { "teddy", "1f9f8" }, { "ufo", "1f6f8" },
            { "glowing_star", "1f31f" }, { "music", "1f3b5" }, { "trophy", "1f3c6" }, { "puzzle", "1f9e9" }, { "book", "1f4d6" },
            { "palette", "1f3a8" }, { "smile", "1f60a" }, { "guitar", "1f3b8" }, { "trumpet", "1f3ba" }, { "drum", "1f941" },
            { "piano", "1f3b9" }, { "maracas", "1fa87" }, { "saxophone", "1f3b7" }, { "caterpillar", "1f41b" }, { "carrot", "1f955" }
        };

        public static string ResourceName(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (key.StartsWith("draw:")) return "draw_" + key.Substring(5);
            if (Aliases.TryGetValue(key, out string code)) key = code;
            return key.All(c => "0123456789abcdef_".IndexOf(c) >= 0) ? "emoji_u" + key : "draw_" + key;
        }
    }
}
