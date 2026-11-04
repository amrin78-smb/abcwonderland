using System;
using System.Collections.Generic;
using System.Linq;
using ABCWonderworld.Content;
using ABCWonderworld.Learning;
using ABCWonderworld.Persistence;

namespace ABCWonderworld.Rewards
{
    /// <summary>
    /// Wonder Stars and deterministic cosmetic unlocks (GDD §9). Stars are never lost; rewards never gate
    /// learning content; there is no randomness and nothing to buy.
    /// </summary>
    public sealed class RewardService
    {
        private readonly SaveData _data;
        private readonly IReadOnlyList<RewardDefinition> _defs;
        private readonly ProgressService _progress;

        public event Action<int> StarsAwarded;
        public event Action<RewardDefinition> Unlocked;

        public int Stars => _data.rewards.wonderStars;

        public RewardService(SaveData data, IReadOnlyList<RewardDefinition> definitions, ProgressService progress)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _defs = definitions ?? Array.Empty<RewardDefinition>();
            _progress = progress;
        }

        public bool IsUnlocked(string rewardId) => _data.rewards.unlocked.Contains(rewardId);

        public IEnumerable<RewardDefinition> UnlockedRewards => _defs.Where(d => d != null && IsUnlocked(d.id));

        public void AwardStars(int amount)
        {
            if (amount <= 0) return;
            _data.rewards.wonderStars += amount;
            StarsAwarded?.Invoke(amount);
            Evaluate();
        }

        /// <summary>Checks every rule and unlocks anything newly earned. Returns the new unlocks.</summary>
        public List<RewardDefinition> Evaluate()
        {
            var newly = new List<RewardDefinition>();
            foreach (RewardDefinition d in _defs)
            {
                if (d == null || IsUnlocked(d.id) || !Earned(d)) continue;
                _data.rewards.unlocked.Add(d.id);
                newly.Add(d);
                Unlocked?.Invoke(d);
            }
            return newly;
        }

        public List<RewardDefinition> TakeUnseenUnlocks()
        {
            var unseen = UnlockedRewards.Where(d => !_data.rewards.seenUnlocks.Contains(d.id)).ToList();
            foreach (RewardDefinition d in unseen) _data.rewards.seenUnlocks.Add(d.id);
            return unseen;
        }

        public bool Earned(RewardDefinition d)
        {
            LetterProgressData p = _progress?.Get(d.param);
            return d.condition switch
            {
                UnlockCondition.StarTotal => Stars >= d.amount,
                UnlockCondition.MeetLetter => p != null && p.exposures > 0,
                UnlockCondition.CompleteAdventure => p != null && p.adventureCompletions > 0,
                UnlockCondition.TraceLetter => p != null && p.tracingCompletionsUpper + p.tracingCompletionsLower > 0,
                _ => false
            };
        }

        /// <summary>Toggles an earned cosmetic; hats and glasses are one-at-a-time per type.</summary>
        public void Equip(string rewardId)
        {
            if (!IsUnlocked(rewardId)) return;
            if (_data.rewards.equipped.Remove(rewardId)) return;
            RewardDefinition def = _defs.FirstOrDefault(d => d != null && d.id == rewardId);
            if (def != null && (def.type == RewardType.Hat || def.type == RewardType.Glasses))
                _data.rewards.equipped.RemoveAll(id => _defs.Any(d => d != null && d.id == id && d.type == def.type));
            _data.rewards.equipped.Add(rewardId);
            StarsAwarded?.Invoke(0); // triggers autosave
        }

        public void ToggleEquip(string rewardId)
        {
            if (!IsUnlocked(rewardId)) return;
            if (!_data.rewards.equipped.Remove(rewardId)) _data.rewards.equipped.Add(rewardId);
        }
    }
}
