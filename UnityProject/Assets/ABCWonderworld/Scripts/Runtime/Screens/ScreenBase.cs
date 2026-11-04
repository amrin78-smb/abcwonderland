using ABCWonderworld.Core;
using ABCWonderworld.Input;
using UnityEngine;

namespace ABCWonderworld.Screens
{
    /// <summary>Base for scene controllers: guarantees services, an EventSystem and the music bed.</summary>
    public abstract class ScreenBase : MonoBehaviour
    {
        protected GameServices S { get; private set; }

        protected virtual void Start()
        {
            S = GameServices.EnsureBooted();
            TouchInteractionService.EnsureEventSystem();
            if (S.Settings.MusicEnabled) S.Audio.PlayMusic(Audio.AudioService.LoadMusic("home_loop"));
            Build();
        }

        protected abstract void Build();
    }
}
