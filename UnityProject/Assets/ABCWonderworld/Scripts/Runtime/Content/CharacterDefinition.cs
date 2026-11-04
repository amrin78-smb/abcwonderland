using UnityEngine;

namespace ABCWonderworld.Content
{
    [CreateAssetMenu(menuName = "ABC Wonderworld/Character", fileName = "Character_")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public string letterId;
        public string personality;
        public string themeObject;
        public Color primaryColor = Color.white;
        public Color accentColor = Color.white;
        public GameObject prefab;
        public Sprite portrait;
        public string introAudioKey;
        public ContentStatus artStatus = ContentStatus.Placeholder;

        public static string IdFor(char letter) => "character_" + char.ToLowerInvariant(letter);
    }
}
