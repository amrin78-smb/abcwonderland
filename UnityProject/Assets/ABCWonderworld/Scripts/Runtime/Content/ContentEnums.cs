namespace ABCWonderworld.Content
{
    public enum LetterCase { Upper = 0, Lower = 1 }

    /// <summary>The eight GDD game modes. Values are persisted: append only, never renumber.</summary>
    public enum ActivityType
    {
        MeetLetter = 0,
        Tracing = 1,
        FindLetter = 2,
        Match = 3,
        Adventure = 4,
        Puzzle = 5,
        Music = 6,
        FreePlay = 7
    }

    /// <summary>Reusable mini-game templates that the 26 letter adventures are configured from.</summary>
    public enum AdventureArchetype
    {
        TapCollect = 0,
        PopTarget = 1,
        FindHidden = 2,
        Fill = 3,
        DragToTarget = 4,
        PlaceParts = 5,
        MatchPairs = 6,
        DragCollect = 7,
        PathSteps = 8,
        Pattern = 9,
        Music = 10,
        Swipe = 11
    }

    /// <summary>Production state of a content item; anything below Final is listed in the completeness register.</summary>
    public enum ContentStatus
    {
        Missing = 0,
        Placeholder = 1,
        Provisional = 2,
        Final = 3
    }

    public enum RewardType { Sticker = 0, Hat = 1, Glasses = 2, Decoration = 3, Dance = 4 }
}
