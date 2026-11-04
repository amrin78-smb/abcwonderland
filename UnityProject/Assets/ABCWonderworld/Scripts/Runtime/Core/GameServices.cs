using System;
using ABCWonderworld.Audio;
using ABCWonderworld.Content;
using ABCWonderworld.Input;
using ABCWonderworld.Learning;
using ABCWonderworld.Parent;
using ABCWonderworld.Persistence;
using ABCWonderworld.Rewards;
using UnityEngine;

namespace ABCWonderworld.Core
{
    /// <summary>
    /// Explicit composition root (D-002). Built once by <see cref="ApplicationBootstrapper"/>; scene controllers
    /// read their dependencies from here in Start(). This is the only global; services themselves are plain
    /// objects or components owned by the bootstrapper and receive their dependencies through constructors.
    /// </summary>
    public sealed class GameServices
    {
        public static GameServices Current { get; private set; }

        public ContentCatalog Catalog { get; }
        public ISaveRepository Repository { get; }
        public SaveData Save { get; }
        public SaveLoadReport LoadReport { get; }
        public SettingsService Settings { get; }
        public ProgressService Progress { get; }
        public AdaptiveDifficulty Difficulty { get; }
        public RewardService Rewards { get; }
        public SessionService Session { get; }
        public AudioService Audio { get; }
        public SceneLoader Loader { get; }
        public GameFlowCoordinator Flow { get; }
        public TouchInteractionService Touch { get; }
        public UI.UiSkin Skin { get; }

        internal GameServices(ContentCatalog catalog, ISaveRepository repo, AudioService audio, SceneLoader loader,
            TouchInteractionService touch, UI.UiSkin skin)
        {
            Catalog = catalog;
            Repository = repo;
            Save = repo.Load(out SaveLoadReport report);
            LoadReport = report;
            Settings = new SettingsService(Save.settings, Save.profile);
            Progress = new ProgressService(Save);
            Difficulty = new AdaptiveDifficulty(Save);
            Rewards = new RewardService(Save, catalog != null ? catalog.rewards : null, Progress);
            Session = new SessionService { ReminderMinutes = Save.settings.sessionReminderMinutes };
            Audio = audio;
            Loader = loader;
            Touch = touch;
            Skin = skin;
            Flow = new GameFlowCoordinator(loader);
        }

        internal static void SetCurrent(GameServices s) => Current = s;

        /// <summary>Makes any scene playable on its own (Editor play from a non-boot scene, PlayMode tests).</summary>
        public static GameServices EnsureBooted()
        {
            if (Current != null) return Current;
            var existing = UnityEngine.Object.FindAnyObjectByType<ApplicationBootstrapper>();
            if (existing == null)
            {
                var go = new GameObject("[ABC Wonderworld Services]");
                existing = go.AddComponent<ApplicationBootstrapper>();
            }
            existing.Initialize(startFlow: false);
            if (Current == null) throw new InvalidOperationException("Bootstrap failed");
            return Current;
        }
    }
}
