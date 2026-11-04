using System.Collections.Generic;
using UnityEngine;

namespace ABCWonderworld.Content
{
    /// <summary>One letter of the alphabet with its learning content. Generated from Content/alphabet.json.</summary>
    [CreateAssetMenu(menuName = "ABC Wonderworld/Letter", fileName = "Letter_")]
    public sealed class LetterDefinition : ScriptableObject
    {
        [Tooltip("Stable persistent key, e.g. letter_a. Never change after release.")]
        public string id;
        public char upper;
        public char lower;
        [Tooltip("Display form of the letter name (the name is NOT the sound).")]
        public string displayName;
        [Tooltip("Spoken letter name, e.g. 'Bee'.")]
        public string spokenName;
        [Tooltip("Beginner phoneme shown to parents, e.g. /b/.")]
        public string phonemeDisplay;
        [Tooltip("IPA used to synthesise provisional phoneme audio.")]
        public string phonemeIpa;
        [Tooltip("Short parent-facing hint, e.g. 'b, as in ball'.")]
        public string soundHint;
        [TextArea] public string soundNote;
        public string characterId;
        public string worldId;
        public List<VocabularyEntry> vocabulary = new List<VocabularyEntry>();
        public TracingDefinition upperTracing;
        public TracingDefinition lowerTracing;
        public AdventureDefinition adventure;
        public string reviewStatus = "pending-expert-review";

        [Header("Audio keys (resolved through the AudioLibrary)")]
        public string nameAudioKey;
        public string phonemeAudioKey;
        public string introAudioKey;

        public int Index => char.ToUpperInvariant(upper) - 'A';
        public char Glyph(LetterCase c) => c == LetterCase.Upper ? upper : lower;
        public TracingDefinition Tracing(LetterCase c) => c == LetterCase.Upper ? upperTracing : lowerTracing;

        public static string IdFor(char letter) => "letter_" + char.ToLowerInvariant(letter);
    }
}
