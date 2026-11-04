using System.Collections.Generic;
using UnityEngine;

namespace ABCWonderworld.Content
{
    /// <summary>Root of all authored content. One instance lives at Resources/ContentCatalog.</summary>
    [CreateAssetMenu(menuName = "ABC Wonderworld/Content Catalog", fileName = "ContentCatalog")]
    public sealed class ContentCatalog : ScriptableObject
    {
        public const string ResourcePath = "ContentCatalog";

        public List<WorldDefinition> worlds = new List<WorldDefinition>();
        public List<LetterDefinition> letters = new List<LetterDefinition>();
        public List<CharacterDefinition> characters = new List<CharacterDefinition>();
        public List<RewardDefinition> rewards = new List<RewardDefinition>();
        public AudioLibrary audio;
        public Font uiFont;

        private Dictionary<string, LetterDefinition> _letters;
        private Dictionary<string, CharacterDefinition> _characters;
        private Dictionary<string, WorldDefinition> _worlds;

        public static ContentCatalog Load() => Resources.Load<ContentCatalog>(ResourcePath);

        public LetterDefinition Letter(string id) { Index(); return id != null && _letters.TryGetValue(id, out var l) ? l : null; }
        public LetterDefinition Letter(char c) => Letter(LetterDefinition.IdFor(c));
        public CharacterDefinition Character(string id) { Index(); return id != null && _characters.TryGetValue(id, out var c) ? c : null; }
        public WorldDefinition World(string id) { Index(); return id != null && _worlds.TryGetValue(id, out var w) ? w : null; }
        public CharacterDefinition CharacterFor(LetterDefinition l) => l != null ? Character(l.characterId) : null;

        public WorldDefinition WorldOf(LetterDefinition l) => l != null ? World(l.worldId) : null;

        public List<LetterDefinition> LettersIn(WorldDefinition w)
        {
            var list = new List<LetterDefinition>();
            if (w == null) return list;
            foreach (string id in w.letterIds)
            {
                LetterDefinition l = Letter(id);
                if (l != null) list.Add(l);
            }
            return list;
        }

        /// <summary>Rebuilds lookups (call after editing lists at runtime or in tests).</summary>
        public void Reindex()
        {
            _letters = new Dictionary<string, LetterDefinition>();
            _characters = new Dictionary<string, CharacterDefinition>();
            _worlds = new Dictionary<string, WorldDefinition>();
            foreach (var l in letters) if (l != null && !string.IsNullOrEmpty(l.id)) _letters[l.id] = l;
            foreach (var c in characters) if (c != null && !string.IsNullOrEmpty(c.id)) _characters[c.id] = c;
            foreach (var w in worlds) if (w != null && !string.IsNullOrEmpty(w.id)) _worlds[w.id] = w;
        }

        private void Index() { if (_letters == null) Reindex(); }
        private void OnEnable() => _letters = null;
    }
}
