using UnityEngine;

namespace ABCWonderworld.Content
{
    [CreateAssetMenu(menuName = "ABC Wonderworld/Vocabulary", fileName = "Vocab_")]
    public sealed class VocabularyEntry : ScriptableObject
    {
        [Tooltip("Stable persistent key, e.g. vocab_apple.")]
        public string id;
        public string word;
        public string letterId;
        [Tooltip("True only if the word begins with the letter's selected beginner sound. False words are never used in initial-sound activities.")]
        public bool startsWithBeginnerSound = true;
        [Tooltip("Where the beginner sound is heard: start, end. Used for X (fox).")]
        public string soundPosition = "start";
        [TextArea] public string soundNote;
        public Sprite picture;
        public string audioKey;
        public ContentStatus artStatus = ContentStatus.Placeholder;
        [TextArea] public string artNote;

        public static string IdFor(string word) => "vocab_" + word.ToLowerInvariant().Replace(' ', '_').Replace('-', '_');
    }
}
