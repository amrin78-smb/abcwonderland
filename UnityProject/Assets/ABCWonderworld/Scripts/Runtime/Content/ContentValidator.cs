using System.Collections.Generic;
using System.Linq;

namespace ABCWonderworld.Content
{
    public enum IssueSeverity { Info = 0, Warning = 1, Error = 2 }

    public readonly struct ContentIssue
    {
        public readonly IssueSeverity Severity;
        public readonly string Subject;
        public readonly string Message;

        public ContentIssue(IssueSeverity severity, string subject, string message)
        {
            Severity = severity; Subject = subject; Message = message;
        }

        public override string ToString() => $"[{Severity}] {Subject}: {Message}";
    }

    /// <summary>
    /// Validates content references and completeness. Errors block the build pipeline; warnings
    /// (placeholders, provisional audio, pending review) feed the content-completeness register.
    /// </summary>
    public static class ContentValidator
    {
        public const int MinimumVocabularyPerLetter = 3;

        public static List<ContentIssue> Validate(ContentCatalog catalog)
        {
            var issues = new List<ContentIssue>();
            void Err(string s, string m) => issues.Add(new ContentIssue(IssueSeverity.Error, s, m));
            void Warn(string s, string m) => issues.Add(new ContentIssue(IssueSeverity.Warning, s, m));

            if (catalog == null) { Err("catalog", "ContentCatalog is missing."); return issues; }
            catalog.Reindex();

            // Alphabet completeness: exactly A-Z, unique ids.
            for (char c = 'A'; c <= 'Z'; c++)
            {
                if (catalog.Letter(c) == null) Err(LetterDefinition.IdFor(c), "Letter is missing from the catalogue.");
            }
            foreach (var dup in catalog.letters.Where(l => l != null).GroupBy(l => l.id).Where(g => g.Count() > 1))
                Err(dup.Key, "Duplicate letter id.");
            if (catalog.letters.Any(l => l == null)) Err("catalog", "Null entry in letters list.");

            var allVocabIds = new HashSet<string>();
            foreach (LetterDefinition l in catalog.letters.Where(l => l != null))
            {
                string s = l.id;
                if (l.id != LetterDefinition.IdFor(l.upper)) Err(s, $"Id does not match glyph '{l.upper}'.");
                if (char.ToLowerInvariant(l.upper) != l.lower || !char.IsUpper(l.upper)) Err(s, "Upper/lower glyph pair is invalid.");
                if (string.IsNullOrWhiteSpace(l.spokenName)) Err(s, "Spoken letter name is missing.");
                if (string.IsNullOrWhiteSpace(l.phonemeDisplay)) Err(s, "Beginner phoneme is missing.");
                if (!string.IsNullOrWhiteSpace(l.phonemeDisplay) && l.phonemeDisplay.Trim('/').Equals(l.spokenName, System.StringComparison.OrdinalIgnoreCase))
                    Err(s, "Phoneme equals the letter name; name and sound must be distinct.");
                if (catalog.Character(l.characterId) == null) Err(s, $"Character '{l.characterId}' not found.");
                WorldDefinition w = catalog.World(l.worldId);
                if (w == null) Err(s, $"World '{l.worldId}' not found.");
                else if (!w.letterIds.Contains(l.id)) Err(s, $"World '{w.id}' does not list this letter.");

                if (l.vocabulary.Count(v => v != null) < MinimumVocabularyPerLetter)
                    Err(s, $"Needs at least {MinimumVocabularyPerLetter} vocabulary entries.");
                foreach (VocabularyEntry v in l.vocabulary)
                {
                    if (v == null) { Err(s, "Null vocabulary reference."); continue; }
                    if (!allVocabIds.Add(v.id)) Err(v.id, "Duplicate vocabulary id.");
                    if (v.letterId != l.id) Err(v.id, $"Vocabulary letterId '{v.letterId}' does not match '{l.id}'.");
                    if (string.IsNullOrWhiteSpace(v.word)) Err(v.id, "Word is empty.");
                    else if (char.ToLowerInvariant(v.word[0]) != l.lower && v.soundPosition != "end")
                        Err(v.id, $"Word '{v.word}' does not contain the letter at the start.");
                    if (v.picture == null) Err(v.id, "Picture is missing.");
                    else if (v.artStatus != ContentStatus.Final) Warn(v.id, $"Picture is {v.artStatus}." + (string.IsNullOrEmpty(v.artNote) ? "" : " " + v.artNote));
                    if (string.IsNullOrEmpty(v.audioKey)) Err(v.id, "Audio key is missing.");
                }
                if (l.vocabulary.Count(v => v != null && v.startsWithBeginnerSound) == 0 && l.upper != 'X')
                    Err(s, "No vocabulary word begins with the beginner sound.");

                ValidateTracing(l, LetterCase.Upper, issues);
                ValidateTracing(l, LetterCase.Lower, issues);

                if (l.adventure == null) Err(s, "Adventure is missing.");
                else
                {
                    if (l.adventure.letterId != l.id) Err(l.adventure.id, "Adventure letterId mismatch.");
                    if (l.adventure.targetCount < 1) Err(l.adventure.id, "Target count must be at least 1.");
                    if (string.IsNullOrWhiteSpace(l.adventure.instruction)) Err(l.adventure.id, "Instruction is missing.");
                }

                if (string.IsNullOrEmpty(l.nameAudioKey) || string.IsNullOrEmpty(l.phonemeAudioKey))
                    Err(s, "Letter name/phoneme audio keys are missing.");
                if (l.reviewStatus != "approved") Warn(s, "Phonics content pending expert review.");
            }

            // Worlds: five, contiguous, every letter in exactly one world.
            if (catalog.worlds.Count(w => w != null) != 5) Err("worlds", "Expected 5 worlds.");
            var seen = new Dictionary<string, string>();
            foreach (WorldDefinition w in catalog.worlds.Where(w => w != null))
            {
                if (string.IsNullOrEmpty(w.sceneName)) Err(w.id, "Scene name is missing.");
                foreach (string lid in w.letterIds)
                {
                    if (catalog.Letter(lid) == null) Err(w.id, $"Lists unknown letter '{lid}'.");
                    if (seen.TryGetValue(lid, out string other)) Err(w.id, $"Letter '{lid}' is also in '{other}'.");
                    seen[lid] = w.id;
                }
                if (w.environmentStatus != ContentStatus.Final) Warn(w.id, $"Environment art is {w.environmentStatus}.");
            }

            foreach (CharacterDefinition ch in catalog.characters.Where(c => c != null))
            {
                if (catalog.Letter(ch.letterId) == null) Err(ch.id, $"Letter '{ch.letterId}' not found.");
                if (ch.prefab == null) Err(ch.id, "Character prefab is missing.");
                if (ch.artStatus != ContentStatus.Final) Warn(ch.id, $"Character art is {ch.artStatus}.");
            }

            if (catalog.audio == null) Err("audio", "AudioLibrary is missing.");
            else
            {
                foreach (string key in RequiredAudioKeys(catalog))
                {
                    AudioLibrary.Entry e = catalog.audio.Find(key);
                    if (e == null || e.clip == null) Warn(key, "Voice clip missing (placeholder event; gameplay continues without it).");
                    else if (e.status != ContentStatus.Final) { /* provisional audio is reported in aggregate */ }
                }
                int provisional = catalog.audio.entries.Count(e => e.clip != null && e.status != ContentStatus.Final);
                if (provisional > 0) Warn("audio", $"{provisional} clips are provisional (synthesised) and must be replaced by recorded voice.");
            }
            return issues;
        }

        public static IEnumerable<string> RequiredAudioKeys(ContentCatalog catalog)
        {
            foreach (LetterDefinition l in catalog.letters.Where(l => l != null))
            {
                yield return l.nameAudioKey;
                yield return l.phonemeAudioKey;
                yield return l.introAudioKey;
                foreach (VocabularyEntry v in l.vocabulary.Where(v => v != null)) yield return v.audioKey;
                if (l.adventure != null) yield return l.adventure.instructionAudioKey;
            }
        }

        private static void ValidateTracing(LetterDefinition l, LetterCase c, List<ContentIssue> issues)
        {
            TracingDefinition t = l.Tracing(c);
            string s = TracingDefinition.IdFor(l.upper, c);
            if (t == null) { issues.Add(new ContentIssue(IssueSeverity.Error, s, "Tracing definition missing.")); return; }
            if (t.strokes == null || t.strokes.Count == 0) { issues.Add(new ContentIssue(IssueSeverity.Error, s, "No strokes.")); return; }
            for (int i = 0; i < t.strokes.Count; i++)
            {
                if (t.strokes[i]?.points == null || t.strokes[i].points.Count < 2)
                    issues.Add(new ContentIssue(IssueSeverity.Error, s, $"Stroke {i + 1} has fewer than 2 points."));
            }
            if (t.tolerance <= 0f) issues.Add(new ContentIssue(IssueSeverity.Error, s, "Tolerance must be positive."));
        }
    }
}
