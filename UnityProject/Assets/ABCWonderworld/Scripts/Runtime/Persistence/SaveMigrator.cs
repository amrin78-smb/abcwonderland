using System;

namespace ABCWonderworld.Persistence
{
    public static class AdaptiveLevels { public const int Max = 2; }

    /// <summary>
    /// Upgrades older save files step by step. Never discards an old save: an unknown future version is
    /// rejected (and the file preserved) instead of being overwritten.
    /// </summary>
    public static class SaveMigrator
    {
        public enum Result { UpToDate, Migrated, UnsupportedFutureVersion, Invalid }

        public static Result Migrate(SaveData data)
        {
            if (data == null) return Result.Invalid;
            if (data.schemaVersion > SaveData.CurrentVersion) return Result.UnsupportedFutureVersion;
            if (data.schemaVersion < 0) return Result.Invalid;
            bool migrated = false;
            // Version 0 = files written before versioning existed (development builds). Fill defaults.
            if (data.schemaVersion == 0)
            {
                data.profile ??= new ProfileData();
                data.settings ??= new SettingsData();
                data.rewards ??= new RewardStateData();
                data.schemaVersion = 1;
                migrated = true;
            }
            // Future: if (data.schemaVersion == 1) { ...; data.schemaVersion = 2; migrated = true; }
            Sanitize(data);
            return migrated ? Result.Migrated : Result.UpToDate;
        }

        /// <summary>Repairs nulls and out-of-range values that JsonUtility can produce from partial files.</summary>
        public static void Sanitize(SaveData d)
        {
            d.profile ??= new ProfileData();
            d.settings ??= new SettingsData();
            d.rewards ??= new RewardStateData();
            d.letters ??= new System.Collections.Generic.List<LetterProgressData>();
            d.recentEvents ??= new System.Collections.Generic.List<LearningEventData>();
            d.activityCounts ??= new System.Collections.Generic.List<ActivityCountData>();
            d.difficulty ??= new System.Collections.Generic.List<DifficultyStateData>();
            foreach (DifficultyStateData ds in d.difficulty) ds.level = Math.Max(0, Math.Min(AdaptiveLevels.Max, ds.level));
            d.rewards.unlocked ??= new System.Collections.Generic.List<string>();
            d.rewards.seenUnlocks ??= new System.Collections.Generic.List<string>();
            d.rewards.equipped ??= new System.Collections.Generic.List<string>();
            d.letters.RemoveAll(l => l == null || string.IsNullOrEmpty(l.letterId));
            foreach (LetterProgressData l in d.letters)
            {
                l.recognitionSessions ??= new System.Collections.Generic.List<int>();
                l.vocabularyHeard ??= new System.Collections.Generic.List<string>();
            }
            d.recentEvents.RemoveAll(e => e == null);
            SettingsData s = d.settings;
            s.voiceVolume = Clamp01(s.voiceVolume);
            s.musicVolume = Clamp01(s.musicVolume);
            s.sfxVolume = Clamp01(s.sfxVolume);
            s.sessionReminderMinutes = Math.Max(0, Math.Min(120, s.sessionReminderMinutes));
            s.graphicsQuality = Math.Max(0, Math.Min(3, s.graphicsQuality));
            if (!Enum.IsDefined(typeof(LearningProfile), d.profile.learningProfile)) d.profile.learningProfile = LearningProfile.Explorer;
            d.rewards.wonderStars = Math.Max(0, d.rewards.wonderStars);
        }

        private static float Clamp01(float v) => float.IsNaN(v) ? 1f : Math.Max(0f, Math.Min(1f, v));
    }
}
