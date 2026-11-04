using System;
using ABCWonderworld.Content;
using UnityEngine;

namespace ABCWonderworld.Core
{
    /// <summary>
    /// Owns high-level navigation. Rejects transitions while one is in progress (prevents double taps from
    /// loading a scene twice) and always has a safe fallback to Home.
    /// </summary>
    public sealed class GameFlowCoordinator
    {
        private readonly SceneLoader _loader;
        public NavigationContext Current { get; private set; } = new NavigationContext(AppScreen.Boot);
        public NavigationContext Previous { get; private set; }
        public bool IsTransitioning => _loader != null && _loader.IsLoading;
        public event Action<NavigationContext> Navigated;

        public GameFlowCoordinator(SceneLoader loader) { _loader = loader; }

        public bool GoHome() => Go(new NavigationContext(AppScreen.Home), SceneNames.Home);
        public bool GoWorldMap() => Go(new NavigationContext(AppScreen.WorldMap), SceneNames.WorldMap);
        public bool GoAlphabetBook() => Go(new NavigationContext(AppScreen.AlphabetBook), SceneNames.AlphabetBook);
        public bool GoMusic() => Go(new NavigationContext(AppScreen.Music), SceneNames.Music);

        public bool GoWorld(string worldId)
        {
            if (string.IsNullOrEmpty(worldId)) return false;
            return Go(new NavigationContext(AppScreen.World, worldId), SceneNames.World(worldId));
        }

        public bool StartActivity(ActivityType type, string letterId, string worldId, LetterCase letterCase = LetterCase.Upper)
        {
            return Go(new NavigationContext(AppScreen.Activity, worldId, letterId, type, letterCase), SceneNames.Activity);
        }

        /// <summary>Back from an activity returns to its world; from a world to the map; otherwise home.</summary>
        public bool Back()
        {
            switch (Current.Screen)
            {
                case AppScreen.Activity when !string.IsNullOrEmpty(Current.WorldId): return GoWorld(Current.WorldId);
                case AppScreen.World: return GoWorldMap();
                default: return GoHome();
            }
        }

        private bool Go(NavigationContext ctx, string scene)
        {
            if (IsTransitioning)
            {
                Debug.Log($"[Flow] Ignored navigation to {ctx.Screen}: transition in progress.");
                return false;
            }
            Previous = Current;
            Current = ctx;
            Navigated?.Invoke(ctx);
            if (_loader == null) return true; // tests without a loader
            _loader.Load(scene, failed =>
            {
                Debug.LogError($"[Flow] Could not load '{scene}' ({failed}). Returning home.");
                Current = new NavigationContext(AppScreen.Home);
                if (scene != SceneNames.Home) _loader.Load(SceneNames.Home, null);
            });
            return true;
        }

        internal void SetCurrentForTests(NavigationContext ctx) => Current = ctx;
    }
}
