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

        private void Awake()
        {
            if (GameServices.Current != null && !_initialized)
            {
                Destroy(gameObject);
                return;
            }
            Initialize(openHomeOnStart);
        }

        internal void Initialize(bool startFlow)
        {
            if (_initialized) return;
            _initialized = true;
            DontDestroyOnLoad(gameObject);
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
            UnityEngine.Input.multiTouchEnabled = false; // one finger at a time: palms and second hands are ignored

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

        public void SaveNow()
        {
            GameServices s = GameServices.Current;
            if (s == null) return;
            s.Save.totalPlaySeconds = (long)s.Session.ActiveSeconds + _baseSeconds;
            if (s.Repository.Save(s.Save)) _dirty = false;
        }

        private long _baseSeconds = -1;

        private void Update()
        {
            GameServices s = GameServices.Current;
            if (s == null) return;
            if (_baseSeconds < 0) _baseSeconds = s.Save.totalPlaySeconds;
            s.Session.Tick(Time.unscaledDeltaTime, s.Flow.IsTransitioning);
            if (_dirty)
            {
                _saveTimer -= Time.unscaledDeltaTime;
                if (_saveTimer <= 0) SaveNow();
            }
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
