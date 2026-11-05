using ABCWonderworld.Audio;
using ABCWonderworld.Content;
using ABCWonderworld.Input;
using ABCWonderworld.Persistence;
using UnityEngine;

namespace ABCWonderworld.Core
{
    /// <summary>
    /// Boot-scene entry point. Builds services in a fixed, explicit order, validates content, starts the
    /// session and opens the Home scene. Persists across scenes and owns autosave.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class ApplicationBootstrapper : MonoBehaviour
    {
        [SerializeField] private bool openHomeOnStart = true;
        private bool _initialized;
        private bool _dirty;
        private float _saveTimer;
        private const float AutosaveDelay = 2f;

        public static string SaveDirectoryOverride; // PlayMode tests point this at a temp folder

        /// <summary>Set while <see cref="GameServices.EnsureBooted"/> creates a bootstrapper, so its Awake does not
        /// navigate to Home (a scene opened on its own must stay where it is).</summary>
        internal static bool SuppressStartFlow;

        private void Awake()
        {
            if (GameServices.Current != null && !_initialized)
            {
                Destroy(gameObject);
                return;
            }
            Initialize(openHomeOnStart && !SuppressStartFlow);
        }

        internal void Initialize(bool startFlow)
        {
            if (_initialized) return;
            _initialized = true;
            DontDestroyOnLoad(gameObject);
            // Keep the screen on while the child plays: the ABC song, voice-led intros and celebrations run with no
            // touches, and a toddler cannot unlock a phone that dimmed mid-song. Update() hands control back to
            // the system after a long idle spell so a forgotten phone still sleeps.
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            // One finger at a time is enforced by Input.PointerGate (the legacy multiTouchEnabled flag does not
            // affect the Input System UI module).

            ContentCatalog catalog = ContentCatalog.Load();
            if (catalog == null) Debug.LogError("[Boot] ContentCatalog missing from Resources. Run ABC Wonderworld > Setup Project.");
            var repo = string.IsNullOrEmpty(SaveDirectoryOverride)
                ? JsonSaveRepository.CreateDefault()
                : new JsonSaveRepository(SaveDirectoryOverride);
            var skin = UI.UiSkin.Load();
            var audio = gameObject.AddComponent<AudioService>();
            var loader = gameObject.AddComponent<SceneLoader>();
            var touch = gameObject.AddComponent<TouchInteractionService>();
            var services = new GameServices(catalog, repo, audio, loader, touch, skin);
            GameServices.SetCurrent(services);
            audio.Configure(services.Settings, catalog != null ? catalog.audio : null);
            QualityService.Apply(services.Settings.Quality);
            Debug.Log($"[Boot] Save: {services.LoadReport}");

            if (catalog != null)
            {
                int errors = 0;
                foreach (ContentIssue issue in ContentValidator.Validate(catalog))
                {
                    if (issue.Severity == IssueSeverity.Error) { errors++; Debug.LogError("[Content] " + issue); }
                }
                if (errors > 0) Debug.LogError($"[Content] {errors} content errors: affected items fall back to safe placeholders.");
            }

            services.Progress.BeginSession();
            services.Progress.Changed += MarkDirty;
            services.Settings.Changed += () =>
            {
                services.Session.ReminderMinutes = services.Settings.SessionReminderMinutes;
                MarkDirty();
            };
            services.Rewards.StarsAwarded += _ => MarkDirty();
            if (!services.Save.profile.firstRunComplete)
            {
                services.Save.profile.firstRunComplete = true;
                MarkDirty();
            }
            SaveNow();

            if (startFlow) services.Flow.GoHome();
        }

        private void MarkDirty()
        {
            if (!_dirty) _saveTimer = AutosaveDelay;
            _dirty = true;
        }

        private float _retryDelay = FirstRetryDelay;
        private const float FirstRetryDelay = 10f;

        public void SaveNow()
        {
            GameServices s = GameServices.Current;
            if (s == null) return;
            AccumulatePlayTime(s);
            if (s.Repository.Save(s.Save))
            {
                _dirty = false;
                _retryDelay = FirstRetryDelay;
            }
            else
            {
                // Storage full / unavailable: back off instead of retrying (and fsyncing) every frame.
                _dirty = true;
                _saveTimer = _retryDelay;
                _retryDelay = Mathf.Min(_retryDelay * 2f, 120f);
            }
        }

        /// <summary>
        /// Moves whole seconds of session play into the saved lifetime total. The save is the source of truth,
        /// so a parent's progress reset (which zeroes it) sticks, and snoozing the break reminder loses nothing.
        /// </summary>
        private double _countedSeconds;
        private void AccumulatePlayTime(GameServices s)
        {
            double pending = s.Session.TotalSeconds - _countedSeconds;
            if (pending < 1.0) return;
            long whole = (long)pending;
            s.Save.totalPlaySeconds = System.Math.Max(0, s.Save.totalPlaySeconds) + whole;
            _countedSeconds += whole;
        }

        private float _idleSeconds;
        private const float SleepAfterIdleSeconds = 300f;

        private void Update()
        {
            GameServices s = GameServices.Current;
            if (s == null) return;
            s.Session.Tick(Time.unscaledDeltaTime, s.Flow.IsTransitioning);
            AccumulatePlayTime(s);
            UpdateSleep(s);
            if (_dirty)
            {
                _saveTimer -= Time.unscaledDeltaTime;
                if (_saveTimer <= 0) SaveNow();
            }
        }

        /// <summary>NeverSleep while in use; after 5 idle minutes with nothing being said, let the OS sleep again.</summary>
        private void UpdateSleep(GameServices s)
        {
            bool touched = Input.PointerGate.IsHeld || UnityEngine.InputSystem.Pointer.current != null && UnityEngine.InputSystem.Pointer.current.press.isPressed;
            bool speaking = s.Audio != null && s.Audio.IsSpeaking;
            _idleSeconds = touched || speaking ? 0f : _idleSeconds + Time.unscaledDeltaTime;
            int wanted = _idleSeconds > SleepAfterIdleSeconds ? SleepTimeout.SystemSetting : SleepTimeout.NeverSleep;
            if (Screen.sleepTimeout != wanted) Screen.sleepTimeout = wanted;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveNow();
            GameServices.Current?.Audio?.SetPaused(paused);
        }

        private void OnApplicationFocus(bool focus)
        {
            if (!focus) SaveNow();
        }

        private void OnApplicationQuit() => SaveNow();
    }
}
