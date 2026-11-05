using System;
using ABCWonderworld.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ABCWonderworld.UI
{
    /// <summary>Keeps children inside the device safe area (notches, rounded corners, nav bars).</summary>
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private Rect _last;
        private Vector2Int _lastScreen;
        private void OnEnable() => Apply();
        private void Update() { if (Screen.safeArea != _last || _lastScreen.x != Screen.width || _lastScreen.y != Screen.height) Apply(); }

        private void Apply()
        {
            _last = Screen.safeArea;
            _lastScreen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(_last.xMin / Screen.width, _last.yMin / Screen.height);
            rt.anchorMax = new Vector2(_last.xMax / Screen.width, _last.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }

    /// <summary>
    /// Large toddler button: press squash, generous hit area, optional spoken label, debounced clicks.
    /// </summary>
    public sealed class BigButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IPointerExitHandler
    {
        public UnityEvent onClick = new UnityEvent();
        public string sfx = "tap";
        public string voiceKey;
        public bool guarded; // navigation: uses the global debounce
        public float cooldown = 0.35f;
        private float _last = -10f;
        private Vector3 _scale = Vector3.one;
        private bool _down;
        private int _pressPointer = int.MinValue;
        public bool Interactable = true;

        private void Awake() => _scale = transform.localScale;

        public void OnPointerDown(PointerEventData e)
        {
            if (!Interactable) return;
            if (!ABCWonderworld.Input.PointerGate.TryClaim(e)) return; // a second finger / palm is ignored
            _pressPointer = e.pointerId;
            _down = true;
            transform.localScale = _scale * 0.92f;
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId != _pressPointer) return;
            ABCWonderworld.Input.PointerGate.Release(e);
            _down = false;
            transform.localScale = _scale;
        }

        public void OnPointerExit(PointerEventData e) { if (_down && e.pointerId == _pressPointer) transform.localScale = _scale; }

        public void OnPointerClick(PointerEventData e)
        {
            if (!Interactable || e.dragging || e.pointerId != _pressPointer) return;
            _pressPointer = int.MinValue;
            Click();
        }

        /// <summary>Programmatic click (AutoPilot, keyboard). Same rules as a tap.</summary>
        public bool Click()
        {
            if (!Interactable) return false;
            if (Time.unscaledTime - _last < cooldown) return false;
            GameServices s = GameServices.Current;
            if (guarded && s?.Touch != null && !s.Touch.TryGuardedTap()) return false;
            if (guarded && s?.Flow != null && s.Flow.IsTransitioning) return false;
            _last = Time.unscaledTime;
            s?.Audio?.PlaySfx(sfx);
            if (!string.IsNullOrEmpty(voiceKey)) s?.Audio?.Speak(voiceKey);
            Tween.Pop(this, transform, 0.12f, 0.25f);
            onClick.Invoke();
            return true;
        }
    }

    /// <summary>
    /// Press-and-hold button (default 0.8 s) with a filling ring. Used for Home/Back inside activities so a
    /// stray palm during tracing or dragging cannot leave the activity.
    /// </summary>
    public sealed class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public float holdSeconds = 0.8f;
        public Image fill;
        public UnityEvent onHeld = new UnityEvent();
        public Action<float> onProgress;
        private float _held = -1f;

        private int _pointer = int.MinValue;

        public void OnPointerDown(PointerEventData e)
        {
            // Not gated: a parent must always be able to leave, even while a palm rests on the screen.
            _pointer = e.pointerId;
            _held = 0f;
            GameServices.Current?.Audio?.PlaySfx("tap", 0.6f);
        }

        public void OnPointerUp(PointerEventData e) { if (e.pointerId == _pointer) Cancel(); }
        public void OnPointerExit(PointerEventData e) { if (e.pointerId == _pointer) Cancel(); }

        private void Cancel()
        {
            _held = -1f;
            if (fill != null) fill.fillAmount = 0f;
            onProgress?.Invoke(0f);
        }

        private void Update()
        {
            if (_held < 0f) return;
            _held += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(_held / holdSeconds);
            if (fill != null) fill.fillAmount = k;
            onProgress?.Invoke(k);
            if (k >= 1f)
            {
                _held = -1f;
                if (fill != null) fill.fillAmount = 0f;
                onHeld.Invoke();
            }
        }

        /// <summary>AutoPilot / tests.</summary>
        public void Trigger() => onHeld.Invoke();
    }

    /// <summary>
    /// Shrinks a fixed-size layout (designed for 16:9) uniformly so it fits its parent on narrower (4:3, 3:2)
    /// or shorter (21:9) screens. <see cref="reserved"/> is space to keep free (e.g. button columns either side).
    /// Never scales above 1. Re-evaluates when the parent rect changes (rotation, safe-area changes).
    /// </summary>
    public sealed class ScaleToFit : MonoBehaviour
    {
        public Vector2 designSize;
        public Vector2 reserved;
        private Vector2 _lastParent = new Vector2(-1, -1);

        public static ScaleToFit Add(RectTransform rt, Vector2 designSize, Vector2 reserved)
        {
            var f = rt.gameObject.AddComponent<ScaleToFit>();
            f.designSize = designSize;
            f.reserved = reserved;
            f.Apply();
            return f;
        }

        private void LateUpdate() => Apply();

        private void Apply()
        {
            if (!(transform.parent is RectTransform parent)) return;
            Vector2 p = parent.rect.size;
            if (p == _lastParent || p.x <= 0 || p.y <= 0 || designSize.x <= 0 || designSize.y <= 0) return;
            _lastParent = p;
            float k = Mathf.Min(1f, (p.x - reserved.x) / designSize.x, (p.y - reserved.y) / designSize.y);
            transform.localScale = Vector3.one * Mathf.Max(0.5f, k);
        }
    }

    /// <summary>Gentle idle bob/pulse to draw attention to the next thing to tap.</summary>
    public sealed class Attention : MonoBehaviour
    {
        public float amount = 0.08f;
        public float speed = 2.4f;
        private Vector3 _base;
        private float _phase;

        /// <summary>Adds (or re-arms) the single Attention pulse on <paramref name="go"/>; never stacks components.</summary>
        public static Attention Ensure(GameObject go, float amount)
        {
            if (go == null) return null;
            if (!go.TryGetComponent(out Attention a)) a = go.AddComponent<Attention>();
            a.amount = amount;
            a.enabled = true;
            return a;
        }

        private void OnEnable() { _base = transform.localScale; _phase = UnityEngine.Random.value * 6f; }
        private void OnDisable() => transform.localScale = _base;
        private void Update()
        {
            float m = Tween.Motion;
            float s = 1 + Mathf.Sin(Time.time * speed + _phase) * amount * m;
            transform.localScale = _base * s;
        }
    }
}
