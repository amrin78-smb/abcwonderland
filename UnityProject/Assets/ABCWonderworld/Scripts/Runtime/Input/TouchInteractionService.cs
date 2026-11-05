using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace ABCWonderworld.Input
{
    /// <summary>
    /// Touch-first input setup with mouse parity (both go through the Input System UI module, which also
    /// drives 3D objects via a PhysicsRaycaster). Provides a toddler-friendly drag threshold and a tap
    /// debouncer for anything that triggers navigation.
    /// </summary>
    public sealed class TouchInteractionService : MonoBehaviour
    {
        /// <summary>Touch slop in millimetres before a press becomes a drag.</summary>
        public const float DragThresholdMm = 4f;
        private float _lastGuardedTap = -10f;

        /// <summary>Makes sure the scene has a correctly configured EventSystem; returns it.</summary>
        public static EventSystem EnsureEventSystem()
        {
            EventSystem es = EventSystem.current != null ? EventSystem.current : FindAnyObjectByType<EventSystem>();
            if (es == null)
            {
                var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                es = go.GetComponent<EventSystem>();
            }
            if (es.GetComponent<BaseInputModule>() == null) es.gameObject.AddComponent<InputSystemUIInputModule>();
            float dpi = Screen.dpi > 0 ? Screen.dpi : 160f;
            es.pixelDragThreshold = Mathf.Max(8, Mathf.RoundToInt(DragThresholdMm / 25.4f * dpi));
            return es;
        }

        /// <summary>Adds a PhysicsRaycaster so 3D colliders receive pointer events.</summary>
        public static void EnablePhysicsPointer(Camera cam)
        {
            if (cam != null && cam.GetComponent<PhysicsRaycaster>() == null) cam.gameObject.AddComponent<PhysicsRaycaster>();
        }

        /// <summary>
        /// Returns false if another guarded tap happened within <paramref name="window"/> seconds. Use for
        /// navigation and other actions that must never double-trigger.
        /// </summary>
        /// <summary>
        /// Android back button / back gesture (reported as Escape). Toddlers trigger edge-swipe "back" constantly, so:
        /// inside an activity it does nothing (exit is the press-and-hold Home); on Home it never quits the app;
        /// elsewhere it goes back one level. A parent can always leave via the system Home/recents.
        /// </summary>
        private void Update()
        {
            PointerGate.ReleaseIfIdle();
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;
            Core.GameServices s = Core.GameServices.Current;
            if (s == null || s.Flow.IsTransitioning) return;
            var parent = Object.FindAnyObjectByType<Screens.ParentZone>();
            if (parent != null && parent.IsOpen) { parent.Close(); return; }
            switch (s.Flow.Current.Screen)
            {
                case Core.AppScreen.Activity:
                case Core.AppScreen.Home:
                case Core.AppScreen.Boot:
                    return;
                default:
                    if (TryGuardedTap()) s.Flow.Back();
                    return;
            }
        }

        public bool TryGuardedTap(float window = 0.6f)
        {
            if (Time.unscaledTime - _lastGuardedTap < window) return false;
            _lastGuardedTap = Time.unscaledTime;
            return true;
        }
    }
}
