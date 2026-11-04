using System;
using System.Collections;
using System.Collections.Generic;
using ABCWonderworld.Activities;
using ABCWonderworld.Characters;
using ABCWonderworld.Content;
using ABCWonderworld.Core;
using ABCWonderworld.UI;
using ABCWonderworld.World;
using UnityEngine;

namespace ABCWonderworld.Screens
{
    /// <summary>
    /// Activity scene controller (ActivityCoordinator): builds the stage and HUD, creates the requested
    /// activity, and handles completion (rewards, celebration, what's next). The HUD's exit is a
    /// press-and-hold so tracing or dragging cannot leave the activity by accident.
    /// </summary>
    public sealed class ActivityScreen : ScreenBase
    {
        public ActivityBase Current { get; private set; }
        public static event Action<ActivityScreen> Ready;

        private ActivityContext _ctx;
        private RectTransform _ui;
        private NavigationContext _nav;

        /// <summary>Suggested order of modes for one letter (GDD core loop).</summary>
        public static readonly ActivityType[] LetterJourney =
            { ActivityType.MeetLetter, ActivityType.Tracing, ActivityType.FindLetter, ActivityType.Match, ActivityType.Puzzle, ActivityType.Adventure };

        protected override void Build()
        {
            _nav = S.Flow.Current;
            if (_nav.Screen != AppScreen.Activity || S.Catalog == null)
            {
                // Opened directly in the Editor: default to Meet the Letter A.
                _nav = new NavigationContext(AppScreen.Activity, "world_happy_meadow", "letter_a", ActivityType.MeetLetter);
                S.Flow.SetCurrentForTests(_nav);
            }
            LetterDefinition letter = S.Catalog.Letter(_nav.LetterId) ?? S.Catalog.Letter('A');
            WorldDefinition world = S.Catalog.World(_nav.WorldId) ?? S.Catalog.WorldOf(letter);
            CharacterDefinition chDef = S.Catalog.CharacterFor(letter);

            Camera cam = EnvironmentBuilder.SetupCameraAndLight(world, new Vector3(0, 2.4f, -8.5f), new Vector3(0, 1.4f, 0), 38f);
            Transform env = EnvironmentBuilder.Build(world, false, letter.Index + 3);
            var stage = new GameObject("Stage").transform;

            // Character on the left third, facing the activity.
            GameObject chGo = chDef != null && chDef.prefab != null
                ? Instantiate(chDef.prefab)
                : CharacterFactory.Build(letter.upper, chDef != null ? chDef.primaryColor : Color.red, Color.white, null, null);
            chGo.transform.SetParent(stage, false);
            chGo.transform.localPosition = new Vector3(-3.6f, 0, -0.5f);
            chGo.transform.localScale = Vector3.one * 2.1f;
            chGo.transform.localRotation = Quaternion.Euler(0, 18, 0);
            var character = chGo.GetComponent<LetterCharacter>();
            Cosmetics.Apply(character, S);
            character.Tapped += c => { S.Audio.Speak(letter.introAudioKey, Audio.VoiceMode.Replace); c.SetSpeaking(true); };

            _ui = UiFactory.Canvas("ActivityCanvas", 10);
            _ctx = new ActivityContext
            {
                Services = S, Letter = letter, Character = chDef, World = world, CharacterInstance = character,
                UiRoot = _ui, Stage = stage, Camera = cam, Case = _nav.Case,
                Level = S.Difficulty.LevelFor(_nav.Activity), Rng = new System.Random(Environment.TickCount)
            };
            BuildHud();
            StartActivity(_nav.Activity);
        }

        private void BuildHud()
        {
            UiSkin skin = UiSkin.Load();
            HoldButton exit = UiFactory.HoldIconButton("Exit", _ui, skin.iconHome, skin.secondary, UiFactory.CornerButton, Exit);
            UiFactory.Place((RectTransform)exit.transform, new Vector2(0, 1), new Vector2(110, -110), Vector2.one * UiFactory.CornerButton);
            BigButton replay = UiFactory.IconButton("Replay", _ui, skin.iconSpeaker, skin.primary, UiFactory.CornerButton, () => Current?.ReplayInstruction());
            UiFactory.Place((RectTransform)replay.transform, new Vector2(1, 1), new Vector2(-110, -110), Vector2.one * UiFactory.CornerButton);
        }

        private void StartActivity(ActivityType type)
        {
            if (Current != null) Destroy(Current.gameObject);
            var holder = new GameObject("Activity_" + type);
            holder.transform.SetParent(transform, false);
            Current = type switch
            {
                ActivityType.MeetLetter => holder.AddComponent<MeetLetterActivity>(),
                ActivityType.Tracing => holder.AddComponent<TracingActivity>(),
                ActivityType.FindLetter => holder.AddComponent<FindLetterActivity>(),
                ActivityType.Match => holder.AddComponent<MatchActivity>(),
                ActivityType.Adventure => holder.AddComponent<AdventureActivity>(),
                ActivityType.Puzzle => holder.AddComponent<PuzzleActivity>(),
                _ => holder.AddComponent<MeetLetterActivity>()
            };
            _ctx.Level = S.Difficulty.LevelFor(Current.Type);
            Current.Finished += OnFinished;
            Current.Begin(_ctx);
            Ready?.Invoke(this);
        }

        private void OnFinished(ActivityResult r)
        {
            S.Rewards.AwardStars(r.Stars);
            List<RewardDefinition> unlocked = S.Rewards.TakeUnseenUnlocks();
            if (unlocked.Count > 0) S.Audio.Speak("vo.sticker", Audio.VoiceMode.Queue);
            ActivityType? next = NextActivity(Current.Type);
            Celebration.Show(_ui, r.Stars, unlocked,
                again: () => S.Flow.StartActivity(Current.Type, _ctx.Letter.id, _ctx.World?.id, _nav.Case),
                next: next.HasValue ? () => GoNext(next.Value) : (Action)null,
                back: () => S.Flow.Back());
        }

        /// <summary>Next mode for this letter, or Meet the next letter of the world after the adventure.</summary>
        private ActivityType? NextActivity(ActivityType done)
        {
            int i = Array.IndexOf(LetterJourney, done);
            return i >= 0 && i < LetterJourney.Length - 1 ? LetterJourney[i + 1] : ActivityType.MeetLetter;
        }

        private void GoNext(ActivityType next)
        {
            string letterId = _ctx.Letter.id;
            if (Current.Type == ActivityType.Adventure && _ctx.World != null)
            {
                int idx = _ctx.World.letterIds.IndexOf(letterId);
                letterId = _ctx.World.letterIds[(idx + 1) % _ctx.World.letterIds.Count];
            }
            S.Flow.StartActivity(next, letterId, _ctx.World?.id, _nav.Case);
        }

        private void Exit()
        {
            Current?.Abort();
            S.Flow.Back();
        }

        private void OnApplicationPause(bool paused)
        {
            if (Current == null) return;
            if (paused) Current.Pause(); else Current.Resume();
        }

        /// <summary>AutoPilot/tests: complete the current activity through its normal handlers.</summary>
        public IEnumerator AutoPlayCurrent() => Current.AutoPlay();
    }
}
