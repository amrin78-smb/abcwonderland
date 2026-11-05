using System;

namespace ABCWonderworld.Parent
{
    /// <summary>
    /// Tracks active play time locally and raises a gentle, optional break suggestion. It never interrupts
    /// an activity: callers ask <see cref="ShouldSuggestBreak"/> only at natural pauses (back on a menu).
    /// </summary>
    public sealed class SessionService
    {
        private double _activeSeconds; // since launch or the last snooze (drives the break reminder only)
        private double _totalSeconds;  // since launch; never reset by Snooze (drives lifetime play time)
        private bool _suggested;

        public int ReminderMinutes { get; set; }
        public double ActiveSeconds => _activeSeconds;
        public double TotalSeconds => _totalSeconds;
        public event Action BreakSuggested;

        public void Tick(double deltaSeconds, bool paused)
        {
            if (paused || deltaSeconds <= 0) return;
            double d = Math.Min(deltaSeconds, 1.0); // clamp long frames (app was suspended)
            _activeSeconds += d;
            _totalSeconds += d;
        }

        public bool ShouldSuggestBreak => ReminderMinutes > 0 && !_suggested && _activeSeconds >= ReminderMinutes * 60.0;

        /// <summary>Called at a natural pause (menu, world map). Returns true when the reminder was raised.</summary>
        public bool TrySuggestBreakAtNaturalPause()
        {
            if (!ShouldSuggestBreak) return false;
            _suggested = true;
            BreakSuggested?.Invoke();
            return true;
        }

        /// <summary>Restarts the reminder timer (e.g. after a parent dismisses it with "a little longer").
        /// Lifetime play time (<see cref="TotalSeconds"/>) is unaffected.</summary>
        public void Snooze()
        {
            _activeSeconds = 0;
            _suggested = false;
        }
    }
}
