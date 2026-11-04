using System;
using System.Collections.Generic;
using ABCWonderworld.Content;
using ABCWonderworld.Core;
using UnityEngine;

namespace ABCWonderworld.Audio
{
    public enum VoiceMode
    {
        /// <summary>Stops the current line and clears the queue (new instruction replaces old).</summary>
        Replace = 0,
        /// <summary>Plays after everything already queued.</summary>
        Queue = 1,
        /// <summary>Plays only if no voice is playing or queued (ambient character chatter).</summary>
        IfIdle = 2
    }

    /// <summary>
    /// Voice, Music, SFX and Ambient channels with independent volumes, mute, music toggle and voice ducking.
    /// Exactly one voice line plays at a time; lines are sequenced, never layered. Missing clips are reported
    /// once and skipped after a short silent gap so gameplay timing still works (D-006).
    /// </summary>
    public sealed class AudioService : MonoBehaviour
    {
        private const int SfxVoices = 6;
        private const float DuckLevel = 0.3f;
        private const float MissingClipGap = 0.6f;

        private AudioSource _voice, _music, _ambient;
        private readonly List<AudioSource> _sfx = new List<AudioSource>();
        private int _nextSfx;
        private SettingsService _settings;
        private AudioLibrary _library;
        private readonly Queue<(string key, AudioClip clip, Action done)> _queue = new Queue<(string, AudioClip, Action)>();
        private Action _currentDone;
        private float _silentUntil = -1f;
        private bool _paused;
        private float _duck = 1f;
        private readonly HashSet<string> _reportedMissing = new HashSet<string>();
        private static readonly Dictionary<string, AudioClip> SfxCache = new Dictionary<string, AudioClip>();

        public bool IsSpeaking => _voice != null && (_voice.isPlaying || _silentUntil > Time.unscaledTime || _queue.Count > 0 || _currentDone != null);
        public event Action<string> LineStarted;

        public void Configure(SettingsService settings, AudioLibrary library)
        {
            _settings = settings;
            _library = library;
            if (_settings != null) _settings.Changed += ApplyVolumes;
            ApplyVolumes();
        }

        private void Awake()
        {
            _voice = NewSource("Voice", false);
            _music = NewSource("Music", true);
            _ambient = NewSource("Ambient", true);
            for (int i = 0; i < SfxVoices; i++) _sfx.Add(NewSource("Sfx" + i, false));
            _voice.priority = 0;
        }

        private AudioSource NewSource(string n, bool loop)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = loop;
            s.spatialBlend = 0f;
            return s;
        }

        // ------------------------------------------------------------------ voice
        public void Speak(string key, VoiceMode mode = VoiceMode.Replace, Action onDone = null)
        {
            if (mode == VoiceMode.IfIdle && IsSpeaking) return;
            if (mode == VoiceMode.Replace) StopVoice();
            AudioClip clip = _library != null ? _library.Clip(key) : null;
            if (clip == null && !string.IsNullOrEmpty(key) && _reportedMissing.Add(key))
                Debug.LogWarning($"[Audio] Placeholder voice event '{key}' has no clip (text: {_library?.Text(key)})");
            _queue.Enqueue((key, clip, onDone));
            if (!_voice.isPlaying && _silentUntil < Time.unscaledTime && _currentDone == null) PlayNext();
        }

        /// <summary>Plays several keys in order as one utterance (e.g. "B" + "says" + /b/).</summary>
        public void SpeakSequence(IEnumerable<string> keys, VoiceMode mode = VoiceMode.Replace, Action onDone = null)
        {
            bool first = true;
            var list = new List<string>(keys);
            for (int i = 0; i < list.Count; i++)
            {
                Speak(list[i], first ? mode : VoiceMode.Queue, i == list.Count - 1 ? onDone : null);
                first = false;
            }
            if (list.Count == 0) onDone?.Invoke();
        }

        public void StopVoice()
        {
            _queue.Clear();
            _currentDone = null;
            _silentUntil = -1f;
            if (_voice != null) _voice.Stop();
        }

        private void PlayNext()
        {
            if (_queue.Count == 0) { _currentDone = null; return; }
            var (key, clip, done) = _queue.Dequeue();
            _currentDone = done ?? NoOp;
            LineStarted?.Invoke(key);
            if (clip != null)
            {
                _voice.clip = clip;
                _voice.Play();
            }
            else
            {
                _silentUntil = Time.unscaledTime + MissingClipGap;
            }
        }

        private static void NoOp() { }

        private void Update()
        {
            if (_paused) return;
            bool busy = _voice.isPlaying || _silentUntil > Time.unscaledTime;
            if (!busy && _currentDone != null)
            {
                Action done = _currentDone;
                _currentDone = null;
                _silentUntil = -1f;
                done();
                if (_currentDone == null) PlayNext();
            }
            else if (!busy && _queue.Count > 0) PlayNext();

            float target = _voice.isPlaying ? DuckLevel : 1f;
            _duck = Mathf.MoveTowards(_duck, target, Time.unscaledDeltaTime * 2.5f);
            ApplyMusicVolume();
        }

        // ------------------------------------------------------------------ music / ambient / sfx
        public void PlayMusic(AudioClip clip)
        {
            if (_music.clip == clip && _music.isPlaying) return;
            _music.clip = clip;
            if (clip != null) _music.Play(); else _music.Stop();
        }

        public void PlayAmbient(AudioClip clip)
        {
            if (_ambient.clip == clip && _ambient.isPlaying) return;
            _ambient.clip = clip;
            if (clip != null) _ambient.Play(); else _ambient.Stop();
        }

        public void PlaySfx(string sfxName, float volume = 1f, float pitch = 1f)
        {
            AudioClip clip = LoadSfx(sfxName);
            if (clip == null) return;
            AudioSource s = _sfx[_nextSfx];
            _nextSfx = (_nextSfx + 1) % _sfx.Count;
            s.pitch = pitch;
            s.volume = (_settings?.SfxVolume ?? 1f) * volume;
            s.PlayOneShot(clip);
        }

        public static AudioClip LoadSfx(string sfxName)
        {
            if (string.IsNullOrEmpty(sfxName)) return null;
            if (!SfxCache.TryGetValue(sfxName, out AudioClip clip))
            {
                clip = Resources.Load<AudioClip>("SFX/" + sfxName);
                SfxCache[sfxName] = clip;
            }
            return clip;
        }

        public static AudioClip LoadMusic(string name) => Resources.Load<AudioClip>("Music/" + name);

        public void SetPaused(bool paused)
        {
            _paused = paused;
            if (paused) { _voice.Pause(); _music.Pause(); _ambient.Pause(); }
            else { _voice.UnPause(); _music.UnPause(); _ambient.UnPause(); }
        }

        private void ApplyVolumes()
        {
            if (_voice == null) return;
            _voice.volume = _settings?.VoiceVolume ?? 1f;
            _ambient.volume = (_settings?.SfxVolume ?? 1f) * 0.5f;
            ApplyMusicVolume();
        }

        private void ApplyMusicVolume()
        {
            bool on = _settings?.MusicEnabled ?? true;
            _music.volume = on ? (_settings?.MusicVolume ?? 0.6f) * _duck * 0.7f : 0f;
        }
    }
}
