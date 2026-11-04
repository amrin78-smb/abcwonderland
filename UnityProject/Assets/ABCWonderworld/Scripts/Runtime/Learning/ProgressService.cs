using System;
using System.Collections.Generic;
using System.Linq;
using ABCWonderworld.Content;
using ABCWonderworld.Persistence;

namespace ABCWonderworld.Learning
{
    /// <summary>Conservative parent-facing label. Never a mastery or ability claim.</summary>
    public enum LetterLearningState { NotYetSeen = 0, Introduced = 1, Practised = 2, ShowingRecognition = 3 }

    public enum Assistance { None = 0, Hint = 1, Demonstrated = 2 }

    /// <summary>Transparent, documented thresholds (Documentation/GAMEPLAY_SPECIFICATIONS.md §Learning evidence).</summary>
    public static class ProgressRules
    {
        public const int RecognitionCorrectNeeded = 3;
        public const int RecognitionSessionsNeeded = 2;
        public const int RecentEventCap = 300;

        public static LetterLearningState StateOf(LetterProgressData p)
        {
            if (p == null) return LetterLearningState.NotYetSeen;
            if (ShowsRecognition(p)) return LetterLearningState.ShowingRecognition;
            if (p.practiceCount > 0) return LetterLearningState.Practised;
            if (p.exposures > 0) return LetterLearningState.Introduced;
            return LetterLearningState.NotYetSeen;
        }

        /// <summary>
        /// Repeated, unassisted, first-try correct selections across more than one session, and at least as many
        /// unassisted successes as incorrect choices. One correct answer is never treated as evidence.
        /// </summary>
        public static bool ShowsRecognition(LetterProgressData p) =>
            p.recognitionCorrectUnassisted >= RecognitionCorrectNeeded &&
            p.recognitionSessions.Distinct().Count() >= RecognitionSessionsNeeded &&
            p.recognitionCorrectUnassisted >= p.recognitionIncorrect;

        public static string Label(LetterLearningState s) => s switch
        {
            LetterLearningState.Introduced => "Introduced",
            LetterLearningState.Practised => "Practised",
            LetterLearningState.ShowingRecognition => "Showing Recognition",
            _ => "Not yet seen"
        };
    }

    /// <summary>
    /// Records learning evidence into the save. Exposure, practice, recognition, tracing and vocabulary are kept
    /// as separate categories. Stores local data only.
    /// </summary>
    public sealed class ProgressService
    {
        private readonly SaveData _data;
        private readonly Func<DateTime> _clock;

        public event Action Changed;
        public int CurrentSession => _data.sessionCount;
        public SaveData Data => _data;

        public ProgressService(SaveData data, Func<DateTime> clock = null)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _clock = clock ?? (() => DateTime.UtcNow);
        }

        public void BeginSession() { _data.sessionCount++; Changed?.Invoke(); }

        public LetterProgressData Get(string letterId) => _data.letters.FirstOrDefault(l => l.letterId == letterId);

        public LetterLearningState StateOf(string letterId) => ProgressRules.StateOf(Get(letterId));

        public void RecordExposure(string letterId, ActivityType type)
        {
            LetterProgressData p = GetOrCreate(letterId);
            p.exposures++;
            Add(letterId, type, -1, EvidenceKind.Exposure, EventOutcome.None, 0, Assistance.None, null);
        }

        public void RecordPractice(string letterId, ActivityType type, LetterCase? letterCase)
        {
            LetterProgressData p = GetOrCreate(letterId);
            p.practiceCount++;
            if (p.exposures == 0) p.exposures = 1;
            if (letterCase == LetterCase.Upper) p.upperActivities++;
            else if (letterCase == LetterCase.Lower) p.lowerActivities++;
            Add(letterId, type, letterCase.HasValue ? (int)letterCase.Value : -1, EvidenceKind.Practice, EventOutcome.Attempted, 0, Assistance.None, null);
        }

        /// <summary>One recognition round: correct means the child chose the target; attempts counts taps used.</summary>
        public void RecordRecognition(string letterId, ActivityType type, LetterCase letterCase, bool correct, int attempts, Assistance assistance)
        {
            LetterProgressData p = GetOrCreate(letterId);
            if (p.exposures == 0) p.exposures = 1;
            if (correct && attempts <= 1 && assistance == Assistance.None)
            {
                p.recognitionCorrectUnassisted++;
                if (!p.recognitionSessions.Contains(_data.sessionCount)) p.recognitionSessions.Add(_data.sessionCount);
                if (p.recognitionSessions.Count > 20) p.recognitionSessions.RemoveAt(0);
            }
            else if (correct)
            {
                p.recognitionCorrectAssisted++;
            }
            if (!correct || attempts > 1) p.recognitionIncorrect += Math.Max(1, attempts - 1);
            Add(letterId, type, (int)letterCase, EvidenceKind.Recognition, correct ? EventOutcome.Correct : EventOutcome.Incorrect, attempts, assistance, null);
        }

        public void RecordTracing(string letterId, LetterCase letterCase, bool completed, Assistance assistance)
        {
            LetterProgressData p = GetOrCreate(letterId);
            if (p.exposures == 0) p.exposures = 1;
            p.tracingAttempts++;
            if (completed)
            {
                if (letterCase == LetterCase.Upper) p.tracingCompletionsUpper++;
                else p.tracingCompletionsLower++;
            }
            Add(letterId, ActivityType.Tracing, (int)letterCase, EvidenceKind.Tracing, completed ? EventOutcome.Completed : EventOutcome.Attempted, 1, assistance, null);
        }

        public void RecordVocabulary(string letterId, string vocabularyId, ActivityType type)
        {
            LetterProgressData p = GetOrCreate(letterId);
            if (!p.vocabularyHeard.Contains(vocabularyId)) p.vocabularyHeard.Add(vocabularyId);
            Add(letterId, type, -1, EvidenceKind.Vocabulary, EventOutcome.None, 0, Assistance.None, vocabularyId);
        }

        public void RecordAdventureCompleted(string letterId)
        {
            GetOrCreate(letterId).adventureCompletions++;
            Add(letterId, ActivityType.Adventure, -1, EvidenceKind.Completion, EventOutcome.Completed, 0, Assistance.None, null);
        }

        public void RecordActivityStarted(ActivityType type)
        {
            CountFor(type).started++;
            Changed?.Invoke();
        }

        public void RecordActivityFinished(ActivityType type, bool completed, double seconds)
        {
            ActivityCountData c = CountFor(type);
            if (completed) c.completed++;
            c.seconds += (long)Math.Max(0, Math.Min(seconds, 3600));
            Changed?.Invoke();
        }

        public ActivityCountData CountFor(ActivityType type)
        {
            ActivityCountData c = _data.activityCounts.FirstOrDefault(a => a.activityType == (int)type);
            if (c == null) { c = new ActivityCountData { activityType = (int)type }; _data.activityCounts.Add(c); }
            return c;
        }

        /// <summary>Activity types ordered by time spent then starts: "favourite modes" for the dashboard.</summary>
        public List<ActivityType> FavouriteModes(int max = 3) =>
            _data.activityCounts.Where(a => a.started > 0)
                .OrderByDescending(a => a.seconds).ThenByDescending(a => a.started)
                .Take(max).Select(a => (ActivityType)a.activityType).ToList();

        public int CountInState(LetterLearningState state, IEnumerable<string> letterIds) => letterIds.Count(id => StateOf(id) == state);

        public void ResetAll()
        {
            _data.letters.Clear();
            _data.recentEvents.Clear();
            _data.activityCounts.Clear();
            _data.difficulty.Clear();
            _data.totalPlaySeconds = 0;
            _data.rewards = new RewardStateData();
            Changed?.Invoke();
        }

        private LetterProgressData GetOrCreate(string letterId)
        {
            if (string.IsNullOrEmpty(letterId)) throw new ArgumentException("letterId required");
            LetterProgressData p = Get(letterId);
            long now = _clock().Ticks;
            if (p == null)
            {
                p = new LetterProgressData { letterId = letterId, firstSeenUtcTicks = now };
                _data.letters.Add(p);
            }
            p.lastSeenUtcTicks = now;
            return p;
        }

        private void Add(string letterId, ActivityType type, int letterCase, EvidenceKind kind, EventOutcome outcome, int attempts, Assistance assistance, string detail)
        {
            _data.recentEvents.Add(new LearningEventData
            {
                utcTicks = _clock().Ticks,
                session = _data.sessionCount,
                letterId = letterId,
                activityType = (int)type,
                letterCase = letterCase,
                kind = kind,
                outcome = outcome,
                attempts = attempts,
                assistance = (int)assistance,
                detail = detail
            });
            int overflow = _data.recentEvents.Count - ProgressRules.RecentEventCap;
            if (overflow > 0) _data.recentEvents.RemoveRange(0, overflow);
            Changed?.Invoke();
        }
    }
}
