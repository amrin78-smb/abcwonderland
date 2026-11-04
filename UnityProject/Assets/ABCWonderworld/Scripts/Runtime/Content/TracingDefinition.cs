using System;
using System.Collections.Generic;
using UnityEngine;

namespace ABCWonderworld.Content
{
    [Serializable]
    public sealed class StrokeData
    {
        [Tooltip("Ordered points in glyph space: baseline y=0, cap height y=1.")]
        public List<Vector2> points = new List<Vector2>();
    }

    /// <summary>Ordered stroke paths for one letter form. Tracing follows the path, not pixels.</summary>
    [CreateAssetMenu(menuName = "ABC Wonderworld/Tracing", fileName = "Tracing_")]
    public sealed class TracingDefinition : ScriptableObject
    {
        public string id;
        public string letterId;
        public LetterCase letterCase;
        public List<StrokeData> strokes = new List<StrokeData>();
        [Tooltip("Accepted distance from the path, in glyph units, at the base (guided) level.")]
        public float tolerance = 0.16f;
        [Tooltip("Fraction of each stroke that must be followed for it to count.")]
        [Range(0.5f, 1f)] public float completionThreshold = 0.85f;
        public ContentStatus status = ContentStatus.Provisional;

        public static string IdFor(char letter, LetterCase c) =>
            $"tracing_{char.ToLowerInvariant(letter)}_{(c == LetterCase.Upper ? "upper" : "lower")}";
    }
}
