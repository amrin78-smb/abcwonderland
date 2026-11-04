using ABCWonderworld.Content;

namespace ABCWonderworld.Core
{
    public enum AppScreen { Boot = 0, Home = 1, WorldMap = 2, World = 3, Activity = 4, AlphabetBook = 5, ParentZone = 6, Music = 7 }

    /// <summary>What the next scene should show. Immutable; created by GameFlowCoordinator.</summary>
    public sealed class NavigationContext
    {
        public AppScreen Screen { get; }
        public string WorldId { get; }
        public string LetterId { get; }
        public ActivityType Activity { get; }
        public LetterCase Case { get; }

        public NavigationContext(AppScreen screen, string worldId = null, string letterId = null,
            ActivityType activity = ActivityType.MeetLetter, LetterCase letterCase = LetterCase.Upper)
        {
            Screen = screen; WorldId = worldId; LetterId = letterId; Activity = activity; Case = letterCase;
        }

        public override string ToString() => $"{Screen} world={WorldId} letter={LetterId} activity={Activity} case={Case}";
    }

    public static class SceneNames
    {
        public const string Boot = "Boot";
        public const string Home = "Home";
        public const string WorldMap = "WorldMap";
        public const string Activity = "Activity";
        public const string AlphabetBook = "AlphabetBook";
        public const string Music = "Music";
        public static string World(string worldId) => "World_" + worldId.Replace("world_", "");
    }
}
