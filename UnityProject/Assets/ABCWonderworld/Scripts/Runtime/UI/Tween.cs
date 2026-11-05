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

        // Overlapping Pops/Wiggles on one object (fast drumming, double taps) must share the object's true rest
        // pose; otherwise the second tween captures a mid-squash scale as "rest" and leaves the object distorted.
        // An entry is reused only while the object still shows exactly what the tween last wrote, so a
        // deliberate change of scale/rotation by game code always starts a fresh rest pose.
        private sealed class Rest<T> { public T rest, written; public int active; }
        private static readonly System.Collections.Generic.Dictionary<Transform, Rest<Vector3>> RestScale = new System.Collections.Generic.Dictionary<Transform, Rest<Vector3>>();
        private static readonly System.Collections.Generic.Dictionary<Transform, Rest<Quaternion>> RestRot = new System.Collections.Generic.Dictionary<Transform, Rest<Quaternion>>();

        private static Rest<TV> Acquire<TV>(System.Collections.Generic.Dictionary<Transform, Rest<TV>> map, Transform t, TV current, Func<TV, TV, bool> same)
        {
            if (map.Count > 64) Purge(map);
            if (!map.TryGetValue(t, out Rest<TV> r) || r.active <= 0 || !same(current, r.written))
                map[t] = r = new Rest<TV> { rest = current, written = current };
            r.active++;
            return r;
        }

        private static void ReleaseRest<TV>(System.Collections.Generic.Dictionary<Transform, Rest<TV>> map, Transform t, Rest<TV> r)
        {
            r.active--;
            if (r.active <= 0 && t != null && map.TryGetValue(t, out Rest<TV> cur) && cur == r) map.Remove(t);
        }

        private static void Purge<TV>(System.Collections.Generic.Dictionary<Transform, Rest<TV>> map)
        {
            var dead = new System.Collections.Generic.List<Transform>();
            foreach (var kv in map) if (kv.Key == null) dead.Add(kv.Key);
            foreach (Transform d in dead) map.Remove(d);
        }

        /// <summary>Squash-and-stretch "boing" used as universal tap feedback.</summary>
        public static Coroutine Pop(MonoBehaviour host, Transform t, float amount = 0.18f, float duration = 0.35f)
        {
            if (t == null) return null;
            Rest<Vector3> r = Acquire(RestScale, t, t.localScale, (a, b) => (a - b).sqrMagnitude < 1e-8f);
            Vector3 baseScale = r.rest;
            float amt = amount * Motion;
            return Run(host, duration, k =>
            {
                if (t == null) return;
                float w = Mathf.Sin(k * Mathf.PI * 2f) * (1 - k);
                t.localScale = r.written = new Vector3(baseScale.x * (1 + w * amt), baseScale.y * (1 - w * amt), baseScale.z * (1 + w * amt));
            }, () => { if (t != null) t.localScale = r.written = baseScale; ReleaseRest(RestScale, t, r); });
        }

        /// <summary>Side-to-side rotation wiggle: "not this one" without moving the object.</summary>
        public static Coroutine Wiggle(MonoBehaviour host, Transform t, float degrees = 14f, float duration = 0.45f)
        {
            if (t == null) return null;
            Rest<Quaternion> r = Acquire(RestRot, t, t.localRotation, (a, b) => Quaternion.Angle(a, b) < 0.01f);
            Quaternion r0 = r.rest;
            float amt = degrees * Mathf.Max(0.3f, Motion);
            return Run(host, duration, k =>
            {
                if (t != null) t.localRotation = r.written = r0 * Quaternion.Euler(0, 0, Mathf.Sin(k * Mathf.PI * 5f) * amt * (1 - k));
            }, () => { if (t != null) t.localRotation = r.written = r0; ReleaseRest(RestRot, t, r); });
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
