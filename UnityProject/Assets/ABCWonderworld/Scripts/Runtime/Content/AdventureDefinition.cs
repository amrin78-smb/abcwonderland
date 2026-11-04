using UnityEngine;

namespace ABCWonderworld.Content
{
    /// <summary>Configures one letter's mini-adventure from a reusable archetype.</summary>
    [CreateAssetMenu(menuName = "ABC Wonderworld/Adventure", fileName = "Adventure_")]
    public sealed class AdventureDefinition : ScriptableObject
    {
        public string id;
        public string letterId;
        public string title;
        public AdventureArchetype archetype;
        [Tooltip("Main prop: an emoji code or a built-in prop key (balloon, bubble, egg...).")]
        public string propKey;
        public Sprite propSprite;
        [Min(1)] public int targetCount = 3;
        [TextArea] public string instruction;
        public string instructionAudioKey;
        public int rewardStars = 1;

        public static string IdFor(char letter) => "adventure_" + char.ToLowerInvariant(letter);
    }
}
