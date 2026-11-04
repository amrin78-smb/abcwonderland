using System.Collections.Generic;
using UnityEngine;

namespace ABCWonderworld.Content
{
    [CreateAssetMenu(menuName = "ABC Wonderworld/World", fileName = "World_")]
    public sealed class WorldDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public int order;
        public List<string> letterIds = new List<string>();
        public string sceneName;
        public Color skyColor = Color.cyan;
        public Color groundColor = Color.green;
        public Color accentColor = Color.yellow;
        public Sprite portalArt;
        public string nameAudioKey;
        public string ambientAudioKey;
        public ContentStatus environmentStatus = ContentStatus.Placeholder;
    }
}
