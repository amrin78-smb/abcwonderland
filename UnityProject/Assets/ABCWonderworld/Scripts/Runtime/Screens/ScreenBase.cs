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
            // Always start the bed; "Music off" mutes it via volume, so switching music back on in the Parent Zone
            // is heard immediately instead of only after the next scene load.
            S.Audio.PlayMusic(Audio.AudioService.LoadMusic("home_loop"));
            Build();
        }

        protected abstract void Build();
    }
}
