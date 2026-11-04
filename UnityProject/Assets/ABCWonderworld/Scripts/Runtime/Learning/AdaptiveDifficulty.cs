using System;
using System.Linq;
using ABCWonderworld.Content;
using ABCWonderworld.Persistence;

namespace ABCWonderworld.Learning
{
    /// <summary>
    /// Simple, transparent and reversible difficulty rules (GDD §2.2):
    /// level starts from the parent-selected profile; <see cref="StepUpAfter"/> consecutive easy rounds
    /// (first try, no hint) raise it by one; <see cref="StepDownAfter"/> consecutive struggling rounds
    /// (2+ wrong attempts or a demonstration) lower it by one. Parents can switch adaptation off.
    /// </summary>
    public sealed class AdaptiveDifficulty
    {
        public const int StepUpAfter = 4;
        public const int StepDownAfter = 2;
        private readonly SaveData _data;

        public AdaptiveDifficulty(SaveData data) { _data = data ?? throw new ArgumentNullException(nameof(data)); }

        public static int BaseLevelFor(LearningProfile p) => p switch
        {
            LearningProfile.LittleLearner => 2,
            LearningProfile.Discoverer => 1,
            _ => 0
        };

        public int LevelFor(ActivityType type)
        {
            if (!_data.profile.adaptiveDifficulty) return BaseLevelFor(_data.profile.learningProfile);
            return State(type).level;
        }

        /// <summary>Records one round. Returns the (possibly changed) level.</summary>
        public int RecordRound(ActivityType type, int wrongAttempts, Assistance assistance)
        {
            DifficultyStateData s = State(type);
            if (!_data.profile.adaptiveDifficulty) return LevelFor(type);
            bool easy = wrongAttempts == 0 && assistance == Assistance.None;
            bool struggle = wrongAttempts >= 2 || assistance == Assistance.Demonstrated;
            if (easy) { s.easyStreak++; s.struggleStreak = 0; }
            else if (struggle) { s.struggleStreak++; s.easyStreak = 0; }
            else { s.easyStreak = 0; }

            if (s.easyStreak >= StepUpAfter && s.level < AdaptiveLevels.Max) { s.level++; s.easyStreak = 0; }
            if (s.struggleStreak >= StepDownAfter && s.level > 0) { s.level--; s.struggleStreak = 0; }
            return s.level;
        }

        /// <summary>Called when a parent changes profile: every activity restarts from the profile level.</summary>
        public void ResetToProfile()
        {
            int lvl = BaseLevelFor(_data.profile.learningProfile);
            foreach (DifficultyStateData s in _data.difficulty) { s.level = lvl; s.easyStreak = 0; s.struggleStreak = 0; }
        }

        /// <summary>Find the Letter choice counts per GDD §6.3: Beginner 2, Intermediate 3–4, Advanced 5–6.</summary>
        public static int ChoiceCount(int level, Random rng) => level switch
        {
            0 => 2,
            1 => rng.Next(3, 5),
            _ => rng.Next(5, 7)
        };

        private DifficultyStateData State(ActivityType type)
        {
            DifficultyStateData s = _data.difficulty.FirstOrDefault(d => d.activityType == (int)type);
            if (s == null)
            {
                s = new DifficultyStateData { activityType = (int)type, level = BaseLevelFor(_data.profile.learningProfile) };
                _data.difficulty.Add(s);
            }
            return s;
        }
    }
}
