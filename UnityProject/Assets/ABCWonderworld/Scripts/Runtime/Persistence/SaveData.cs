using System;
using System.Collections.Generic;

namespace ABCWonderworld.Persistence
{
    /// <summary>
    /// Versioned local save. Keys are stable content IDs, never display names. No personal data:
    /// the profile is a generic local profile (no child name is collected).
    /// Bump <see cref="CurrentVersion"/> and add a step to <see cref="SaveMigrator"/> for every schema change.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int schemaVersion = CurrentVersion;
        public long createdUtcTicks;
        public long lastSavedUtcTicks;
        public int sessionCount;
        public ProfileData profile = new ProfileData();
        public SettingsData settings = new SettingsData();
        public List<LetterProgressData> letters = new List<LetterProgressData>();
        public List<LearningEventData> recentEvents = new List<LearningEventData>();
        public List<ActivityCountData> activityCounts = new List<ActivityCountData>();
        public RewardStateData rewards = new RewardStateData();
        public List<DifficultyStateData> difficulty = new List<DifficultyStateData>();
        public long totalPlaySeconds;

        public static SaveData CreateNew(DateTime nowUtc)
        {
            return new SaveData { createdUtcTicks = nowUtc.Ticks, lastSavedUtcTicks = nowUtc.Ticks };
        }
    }

    public enum LearningProfile { Explorer = 0, Discoverer = 1, LittleLearner = 2 }

    [Serializable]
    public sealed class ProfileData
    {
        public string profileId = "local_default";
        public LearningProfile learningProfile = LearningProfile.Explorer;
        public bool adaptiveDifficulty = true;
        public bool firstRunComplete;
    }

    [Serializable]
    public sealed class SettingsData
    {
        public float voiceVolume = 1f;
        public float musicVolume = 0.6f;
        public float sfxVolume = 0.8f;
        public bool musicEnabled = true;
        public bool reducedMotion;
        public bool simplifiedInteraction;
        public int sessionReminderMinutes; // 0 = off
        public int graphicsQuality; // 0 Auto, 1 Low, 2 Medium, 3 High (additive field: older saves default to Auto)
        public string language = "en";
    }

    /// <summary>Aggregated per-letter evidence. Exposure, practice and recognition are recorded separately.</summary>
    [Serializable]
    public sealed class LetterProgressData
    {
        public string letterId;
        public int exposures;
        public int practiceCount;
        public int upperActivities;
        public int lowerActivities;
        public int recognitionCorrectUnassisted;
        public int recognitionCorrectAssisted;
        public int recognitionIncorrect;
        public List<int> recognitionSessions = new List<int>();
        public int tracingAttempts;
        public int tracingCompletionsUpper;
        public int tracingCompletionsLower;
        public int adventureCompletions;
        public List<string> vocabularyHeard = new List<string>();
        public long firstSeenUtcTicks;
        public long lastSeenUtcTicks;
    }

    public enum EvidenceKind
    {
        Exposure = 0,
        Practice = 1,
        Recognition = 2,
        Tracing = 3,
        Vocabulary = 4,
        Completion = 5
    }

    public enum EventOutcome { None = 0, Correct = 1, Incorrect = 2, Completed = 3, Attempted = 4 }

    /// <summary>One recorded learning moment, kept in a bounded recent-history ring for the parent dashboard.</summary>
    [Serializable]
    public sealed class LearningEventData
    {
        public long utcTicks;
        public int session;
        public string letterId;
        public int activityType;
        public int letterCase; // -1 when not applicable
        public EvidenceKind kind;
        public EventOutcome outcome;
        public int attempts;
        public int assistance; // 0 none, 1 hint, 2 demonstrated
        public string detail;
    }

    [Serializable]
    public sealed class ActivityCountData
    {
        public int activityType;
        public int started;
        public int completed;
        public long seconds;
    }

    /// <summary>Transparent adaptive-difficulty state per activity type (see AdaptiveDifficulty).</summary>
    [Serializable]
    public sealed class DifficultyStateData
    {
        public int activityType;
        public int level;
        public int easyStreak;
        public int struggleStreak;
    }

    [Serializable]
    public sealed class RewardStateData
    {
        public int wonderStars;
        public List<string> unlocked = new List<string>();
        public List<string> seenUnlocks = new List<string>();
        public List<string> equipped = new List<string>();
    }
}
