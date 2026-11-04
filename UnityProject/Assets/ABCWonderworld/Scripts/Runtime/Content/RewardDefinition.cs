using UnityEngine;

namespace ABCWonderworld.Content
{
    public enum UnlockCondition
    {
        [Tooltip("Unlocked the first time the letter in 'param' is met.")] MeetLetter = 0,
        [Tooltip("Unlocked when total Wonder Stars reach 'amount'.")] StarTotal = 1,
        [Tooltip("Unlocked when the adventure of the letter in 'param' is completed.")] CompleteAdventure = 2,
        [Tooltip("Unlocked when any tracing of the letter in 'param' is completed.")] TraceLetter = 3
    }

    /// <summary>Cosmetic, deterministic reward. Rewards never gate learning content.</summary>
    [CreateAssetMenu(menuName = "ABC Wonderworld/Reward", fileName = "Reward_")]
    public sealed class RewardDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public RewardType type;
        public Sprite icon;
        public UnlockCondition condition;
        public string param;
        public int amount;
    }
}
