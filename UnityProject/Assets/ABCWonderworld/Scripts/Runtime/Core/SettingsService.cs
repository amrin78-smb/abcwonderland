using System;
using ABCWonderworld.Persistence;

namespace ABCWonderworld.Core
{
    /// <summary>Parent-controlled settings with change notification. Values persist through the save.</summary>
    public sealed class SettingsService
    {
        private readonly SettingsData _s;
        private readonly ProfileData _p;
        public event Action Changed;

        public SettingsService(SettingsData settings, ProfileData profile) { _s = settings; _p = profile; }

        public float VoiceVolume { get => _s.voiceVolume; set { _s.voiceVolume = Clamp(value); Changed?.Invoke(); } }
        public float MusicVolume { get => _s.musicVolume; set { _s.musicVolume = Clamp(value); Changed?.Invoke(); } }
        public float SfxVolume { get => _s.sfxVolume; set { _s.sfxVolume = Clamp(value); Changed?.Invoke(); } }
        public bool MusicEnabled { get => _s.musicEnabled; set { _s.musicEnabled = value; Changed?.Invoke(); } }
        public bool ReducedMotion { get => _s.reducedMotion; set { _s.reducedMotion = value; Changed?.Invoke(); } }
        public bool SimplifiedInteraction { get => _s.simplifiedInteraction; set { _s.simplifiedInteraction = value; Changed?.Invoke(); } }
        public int SessionReminderMinutes { get => _s.sessionReminderMinutes; set { _s.sessionReminderMinutes = Math.Max(0, Math.Min(120, value)); Changed?.Invoke(); } }
        public GraphicsQuality Quality { get => (GraphicsQuality)_s.graphicsQuality; set { _s.graphicsQuality = (int)value; QualityService.Apply(value); Changed?.Invoke(); } }
        public LearningProfile Profile { get => _p.learningProfile; set { _p.learningProfile = value; Changed?.Invoke(); } }
        public bool AdaptiveDifficulty { get => _p.adaptiveDifficulty; set { _p.adaptiveDifficulty = value; Changed?.Invoke(); } }

        /// <summary>Motion multiplier for animations: reduced-motion shrinks movement amplitude.</summary>
        public float MotionScale => _s.reducedMotion ? 0.35f : 1f;

        private static float Clamp(float v) => float.IsNaN(v) ? 1f : Math.Max(0f, Math.Min(1f, v));
    }
}
