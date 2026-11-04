using System;
using System.Collections.Generic;
using System.Linq;

namespace ABCWonderworld.Learning
{
    /// <summary>Builds letter choice sets for recognition activities.</summary>
    public static class ChoiceGenerator
    {
        /// <summary>Visually confusable groups. Kept apart at beginner/intermediate levels, mixed at advanced.</summary>
        private static readonly string[] ConfusableGroups =
        {
            "bdpq", "mw", "nu", "il", "ij", "ft", "hn", "ce", "vy",
            "MWN", "EF", "BDPR", "OQCG", "IJLT", "VYW", "KX", "UV", "HN", "AV"
        };

        public static bool AreConfusable(char a, char b)
        {
            if (a == b) return false;
            foreach (string g in ConfusableGroups)
                if (g.IndexOf(a) >= 0 && g.IndexOf(b) >= 0) return true;
            return false;
        }

        /// <param name="target">Target glyph; its case selects the case of all choices.</param>
        /// <param name="count">Total number of choices (2–6), including the target exactly once.</param>
        /// <param name="allowConfusables">Advanced level only.</param>
        /// <param name="preferred">Optional pool tried first for distractors (e.g. letters of the current world).</param>
        public static List<char> Generate(char target, int count, Random rng, bool allowConfusables, IEnumerable<char> preferred = null)
        {
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            count = Math.Max(2, Math.Min(6, count));
            bool upper = char.IsUpper(target);
            char Norm(char c) => upper ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c);

            bool Ok(char c, List<char> picked) =>
                c != target && !picked.Contains(c) &&
                (allowConfusables || (!AreConfusable(c, target) && picked.All(p => !AreConfusable(c, p))));

            var result = new List<char> { target };
            var pools = new List<IEnumerable<char>>();
            if (preferred != null) pools.Add(preferred.Select(Norm).OrderBy(_ => rng.Next()).ToList());
            pools.Add(Enumerable.Range(0, 26).Select(i => Norm((char)('A' + i))).OrderBy(_ => rng.Next()).ToList());
            foreach (IEnumerable<char> pool in pools)
            {
                foreach (char c in pool)
                {
                    if (result.Count >= count) break;
                    if (Ok(c, result)) result.Add(c);
                }
            }
            // Fallback (only reachable with very restrictive pools): relax confusable rule.
            if (result.Count < count)
            {
                foreach (char c in Enumerable.Range(0, 26).Select(i => Norm((char)('A' + i))))
                {
                    if (result.Count >= count) break;
                    if (c != target && !result.Contains(c)) result.Add(c);
                }
            }
            // Fisher–Yates shuffle for randomised positions.
            for (int i = result.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (result[i], result[j]) = (result[j], result[i]);
            }
            return result;
        }
    }
}
