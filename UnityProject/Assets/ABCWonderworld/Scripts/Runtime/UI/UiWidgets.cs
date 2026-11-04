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
        public bool Interactable = true;

        private void Awake() => _scale = transform.localScale;

        public void OnPointerDown(PointerEventData e)
        {
            if (!Interactable) return;
            _down = true;
            transform.localScale = _scale * 0.92f;
        }

        public void OnPointerUp(PointerEventData e) { _down = false; transform.localScale = _scale; }
        public void OnPointerExit(PointerEventData e) { if (_down) transform.localScale = _scale; }

        public void OnPointerClick(PointerEventData e)
        {
            if (!Interactable || e.dragging) return;
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

        public void OnPointerDown(PointerEventData e) { _held = 0f; GameServices.Current?.Audio?.PlaySfx("tap", 0.6f); }
        public void OnPointerUp(PointerEventData e) => Cancel();
        public void OnPointerExit(PointerEventData e) => Cancel();

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

    /// <summary>Gentle idle bob/pulse to draw attention to the next thing to tap.</summary>
    public sealed class Attention : MonoBehaviour
    {
        public float amount = 0.08f;
        public float speed = 2.4f;
        private Vector3 _base;
        private float _phase;
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
