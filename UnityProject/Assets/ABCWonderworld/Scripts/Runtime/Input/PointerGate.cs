using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ABCWonderworld.Input
{
    /// <summary>
    /// One finger at a time for gameplay input. The Input System UI module delivers every touch as its own
    /// pointer (the legacy <c>Input.multiTouchEnabled</c> flag has no effect on it), so a resting palm or a
    /// second hand would otherwise drag a second piece, end a tracing stroke or tap two answers at once.
    /// The first gameplay pointer to press claims the gate; other pointers are ignored until it lifts.
    /// <see cref="TouchInteractionService"/> frees the gate whenever no pointer is pressed, so a lost
    /// pointer-up (object destroyed mid-drag, scene change) can never leave input blocked.
    /// </summary>
    public static class PointerGate
    {
        private const int None = int.MinValue;
        private static int _owner = None;

        public static bool IsHeld => _owner != None;

        /// <summary>Claims the gate for this pointer (call on press / begin drag). False = ignore this pointer.</summary>
        public static bool TryClaim(PointerEventData e)
        {
            if (e == null) return true;
            if (_owner == None || _owner == e.pointerId) { _owner = e.pointerId; return true; }
            return false;
        }

        /// <summary>True when this pointer may act (the gate is free or it owns it). Does not claim.</summary>
        public static bool Allows(PointerEventData e) => e == null || _owner == None || _owner == e.pointerId;

        /// <summary>True only for the pointer that currently owns the gate.</summary>
        public static bool Owns(PointerEventData e) => e == null || _owner == e.pointerId;

        public static void Release(PointerEventData e)
        {
            if (e == null || _owner == e.pointerId) _owner = None;
        }

        public static void Reset() => _owner = None;

        /// <summary>Called every frame: frees the gate once nothing is touching the screen.</summary>
        internal static void ReleaseIfIdle()
        {
            if (_owner == None || AnyPressed()) return;
            _owner = None;
        }

        private static bool AnyPressed()
        {
            Touchscreen ts = Touchscreen.current;
            if (ts != null)
                foreach (var t in ts.touches)
                    if (t.press.isPressed) return true;
            Mouse m = Mouse.current;
            if (m != null && m.leftButton.isPressed) return true;
            Pen p = Pen.current;
            return p != null && p.tip.isPressed;
        }
    }
}
