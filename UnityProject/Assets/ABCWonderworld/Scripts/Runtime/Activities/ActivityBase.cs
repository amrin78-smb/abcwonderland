using System;
using System.Collections;
using System.Collections.Generic;
using ABCWonderworld.Audio;
using ABCWonderworld.Characters;
using ABCWonderworld.Content;
using ABCWonderworld.Core;
using ABCWonderworld.Learning;
using ABCWonderworld.UI;
using UnityEngine;

namespace ABCWonderworld.Activities
{
    /// <summary>Everything an activity needs, supplied by the ActivityScreen (no scene-wide searches).</summary>
    public sealed class ActivityContext
    {
        public GameServices Services;
        public LetterDefinition Letter;
        public CharacterDefinition Character;
        public WorldDefinition World;
        public LetterCharacter CharacterInstance;
        public RectTransform UiRoot;
        public Transform Stage;
        public Camera Camera;
        public LetterCase Case;
        public int Level;
        public System.Random Rng;
    }

    public readonly struct ActivityResult
    {
        public readonly bool Completed;
        public readonly int Stars;
        public ActivityResult(bool completed, int stars) { Completed = completed; Stars = stars; }
    }

    /// <summary>
    /// Common lifecycle: Begin → (Pause/Resume)* → Finish(result) or Abort. Activities never use timers that
    /// pressure the child, never fail the child, and always allow replaying the instruction.
    /// </summary>
    public abstract class ActivityBase : MonoBehaviour
    {
        protected ActivityContext Ctx;
        protected GameServices S => Ctx.Services;
        protected AudioService Audio => Ctx.Services.Audio;
        protected ProgressService Progress => Ctx.Services.Progress;
        protected LetterDefinition Letter => Ctx.Letter;
        public event Action<ActivityResult> Finished;
        public bool IsFinished { get; private set; }
        public bool IsPaused { get; private set; }
        public abstract ActivityType Type { get; }
        public float StartedAt { get; private set; }

        public void Begin(ActivityContext ctx)
        {
            Ctx = ctx;
            StartedAt = Time.time;
            Progress.RecordActivityStarted(Type);
            OnBegin();
        }

        protected abstract void OnBegin();

        /// <summary>Re-plays the current spoken instruction (HUD speaker button).</summary>
        public abstract void ReplayInstruction();

        public void Pause() { IsPaused = true; OnPause(); }
        public void Resume() { IsPaused = false; OnResume(); }
        protected virtual void OnPause() { }
        protected virtual void OnResume() { }

        protected void Finish(int stars = 1)
        {
            if (IsFinished) return;
            IsFinished = true;
            Progress.RecordActivityFinished(Type, true, Time.time - StartedAt);
            Finished?.Invoke(new ActivityResult(true, stars));
        }

        /// <summary>Called when the child leaves early. Partial participation is still recorded honestly.</summary>
        public virtual void Abort()
        {
            if (IsFinished) return;
            IsFinished = true;
            Progress.RecordActivityFinished(Type, false, Time.time - StartedAt);
        }

        /// <summary>
        /// Drives the activity to completion through the same handlers a child's taps use (AutoPilot
        /// smoke tests and PlayMode tests). Not reachable from child-facing UI.
        /// </summary>
        public abstract IEnumerator AutoPlay();

        // ------------------------------------------------------------------ helpers
        protected void Say(string key, VoiceMode mode = VoiceMode.Replace, Action done = null)
        {
            Ctx.CharacterInstance?.SetSpeaking(true);
            Audio.Speak(key, mode, done);
        }

        protected void SaySequence(IEnumerable<string> keys, Action done = null)
        {
            Ctx.CharacterInstance?.SetSpeaking(true);
            Audio.SpeakSequence(keys, VoiceMode.Replace, done);
        }

        protected void Character(CharacterState s) => Ctx.CharacterInstance?.Play(s);

        protected string NameKey(LetterDefinition l) => l.nameAudioKey;

        protected static readonly string[] Praise = { "vo.great_job", "vo.hooray", "vo.wonderful", "vo.you_did_it" };
        protected string RandomPraise() => Praise[Ctx.Rng.Next(Praise.Length)];

        protected IEnumerator WaitWhileSpeaking(float max = 8f)
        {
            float t = 0;
            yield return null;
            while (Audio.IsSpeaking && t < max) { t += Time.deltaTime; yield return null; }
        }

        protected static Color LetterColor(ActivityContext c) => c.Character != null ? c.Character.primaryColor : new Color(1f, 0.5f, 0.3f);
    }
}
