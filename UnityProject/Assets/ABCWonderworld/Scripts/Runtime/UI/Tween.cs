using System;
using System.Collections;
using ABCWonderworld.Core;
using UnityEngine;

namespace ABCWonderworld.UI
{
    /// <summary>Tiny coroutine tweens honouring the reduced-motion setting.</summary>
    public static class Tween
    {
        public static float Motion => GameServices.Current?.Settings?.MotionScale ?? 1f;

        public static float EaseOutBack(float t) { const float c1 = 1.70158f, c3 = c1 + 1; return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2); }
        public static float EaseInOut(float t) => t * t * (3f - 2f * t);
        public static float EaseOut(float t) => 1 - (1 - t) * (1 - t);

        public static Coroutine Run(MonoBehaviour host, float duration, Action<float> step, Action done = null)
        {
            if (host == null || !host.isActiveAndEnabled) { step?.Invoke(1f); done?.Invoke(); return null; }
            return host.StartCoroutine(Routine(duration, step, done));
        }

        private static IEnumerator Routine(float duration, Action<float> step, Action done)
        {
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                step(Mathf.Clamp01(t / duration));
                yield return null;
            }
            step(1f);
            done?.Invoke();
        }

        /// <summary>Squash-and-stretch "boing" used as universal tap feedback.</summary>
        public static Coroutine Pop(MonoBehaviour host, Transform t, float amount = 0.18f, float duration = 0.35f)
        {
            if (t == null) return null;
            Vector3 baseScale = t.localScale;
            float a = amount * Motion;
            return Run(host, duration, k =>
            {
                if (t == null) return;
                float w = Mathf.Sin(k * Mathf.PI * 2f) * (1 - k);
                t.localScale = new Vector3(baseScale.x * (1 + w * a), baseScale.y * (1 - w * a), baseScale.z * (1 + w * a));
            }, () => { if (t != null) t.localScale = baseScale; });
        }

        /// <summary>Side-to-side rotation wiggle: "not this one" without moving the object.</summary>
        public static Coroutine Wiggle(MonoBehaviour host, Transform t, float degrees = 14f, float duration = 0.45f)
        {
            if (t == null) return null;
            Quaternion r0 = t.localRotation;
            float a = degrees * Mathf.Max(0.3f, Motion);
            return Run(host, duration, k =>
            {
                if (t != null) t.localRotation = r0 * Quaternion.Euler(0, 0, Mathf.Sin(k * Mathf.PI * 5f) * a * (1 - k));
            }, () => { if (t != null) t.localRotation = r0; });
        }

        public static Coroutine Shake(MonoBehaviour host, Transform t, float amount = 12f, float duration = 0.4f)
        {
            if (t == null) return null;
            Vector3 p0 = t.localPosition;
            float a = amount * Motion;
            return Run(host, duration, k =>
            {
                if (t != null) t.localPosition = p0 + Vector3.right * Mathf.Sin(k * Mathf.PI * 6f) * a * (1 - k);
            }, () => { if (t != null) t.localPosition = p0; });
        }
    }
}
