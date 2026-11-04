using System;
using System.Collections.Generic;
using ABCWonderworld.Content;
using UnityEngine;

namespace ABCWonderworld.Tracing
{
    /// <summary>GDD §6.2 guidance modes.</summary>
    public enum TraceMode
    {
        /// <summary>Prominent path, animated guide, must start at the start marker. Most forgiving tolerance.</summary>
        Guided = 0,
        /// <summary>Faint path and start dot only.</summary>
        LightlyGuided = 1,
        /// <summary>Outline only; may begin anywhere near the first part of the stroke.</summary>
        Free = 2
    }

    public enum TraceFeedback { None, Started, OnPath, OffPath, StrokeComplete, LetterComplete, Rejected }

    /// <summary>
    /// Path-progress tracing in glyph space (pure logic, no rendering). Progress only moves forward along the
    /// current stroke, and only within a look-ahead window, so a finger cannot "skip" around an O or jump
    /// between strokes. Lifting the finger keeps progress; the child can continue from where they stopped.
    /// </summary>
    public sealed class TracingEvaluator
    {
        public const float SampleSpacing = 0.02f;
        public const float LookAhead = 0.3f;

        private readonly List<Vector2[]> _samples = new List<Vector2[]>();
        private readonly float _tolerance;
        private readonly float _threshold;
        private readonly TraceMode _mode;
        private int _progressIndex;
        private bool _touching;

        public int StrokeCount => _samples.Count;
        public int CurrentStroke { get; private set; }
        public bool IsComplete => CurrentStroke >= _samples.Count;
        public int OffPathEvents { get; private set; }
        public int RejectedStarts { get; private set; }
        public float Tolerance => _tolerance;

        /// <summary>0..1 progress of the current stroke (1 when the letter is complete).</summary>
        public float StrokeProgress => IsComplete ? 1f : _samples[CurrentStroke].Length <= 1 ? 0f : _progressIndex / (float)(_samples[CurrentStroke].Length - 1);

        /// <summary>0..1 progress of the whole letter, by stroke count.</summary>
        public float TotalProgress => _samples.Count == 0 ? 1f : (CurrentStroke + (IsComplete ? 0 : StrokeProgress)) / _samples.Count;

        public TracingEvaluator(IReadOnlyList<IReadOnlyList<Vector2>> strokes, float tolerance, float completionThreshold, TraceMode mode)
        {
            if (strokes == null) throw new ArgumentNullException(nameof(strokes));
            _mode = mode;
            _tolerance = tolerance * ModeMultiplier(mode);
            _threshold = Mathf.Clamp(completionThreshold, 0.5f, 1f);
            foreach (IReadOnlyList<Vector2> s in strokes)
            {
                if (s == null || s.Count == 0) continue;
                _samples.Add(Resample(s, SampleSpacing));
            }
        }

        public static TracingEvaluator For(TracingDefinition def, TraceMode mode, float toleranceScale = 1f)
        {
            var strokes = new List<IReadOnlyList<Vector2>>();
            foreach (StrokeData s in def.strokes) strokes.Add(s.points);
            return new TracingEvaluator(strokes, def.tolerance * toleranceScale, def.completionThreshold, mode);
        }

        public static float ModeMultiplier(TraceMode m) => m switch
        {
            TraceMode.Guided => 1f,
            TraceMode.LightlyGuided => 0.85f,
            _ => 0.75f
        };

        /// <summary>The point the child should start (or continue) the current stroke from.</summary>
        public Vector2 NextPoint => IsComplete ? Vector2.zero : _samples[CurrentStroke][_progressIndex];

        public Vector2 CurrentStrokeEnd => IsComplete ? Vector2.zero : _samples[CurrentStroke][_samples[CurrentStroke].Length - 1];

        public IReadOnlyList<Vector2> StrokeSamples(int i) => _samples[i];

        public TraceFeedback Begin(Vector2 p)
        {
            if (IsComplete) return TraceFeedback.None;
            Vector2[] s = _samples[CurrentStroke];
            if (LetterStrokeLibrary.PathLength(s) < LetterStrokeLibrary.DotLength)
            {
                if (Vector2.Distance(p, s[0]) <= _tolerance * 1.6f) return CompleteStroke();
                RejectedStarts++;
                return TraceFeedback.Rejected;
            }

            float startTol = _tolerance * 1.5f;
            if (Vector2.Distance(p, s[_progressIndex]) <= startTol)
            {
                _touching = true;
                return TraceFeedback.Started;
            }
            if (_mode == TraceMode.Free && _progressIndex == 0)
            {
                // Free mode: accept a start anywhere in the first quarter of the stroke.
                int limit = Mathf.Max(1, s.Length / 4);
                for (int i = 0; i <= limit; i++)
                {
                    if (Vector2.Distance(p, s[i]) <= startTol) { _progressIndex = i; _touching = true; return TraceFeedback.Started; }
                }
            }
            RejectedStarts++;
            return TraceFeedback.Rejected;
        }

        public TraceFeedback Move(Vector2 p)
        {
            if (IsComplete || !_touching) return TraceFeedback.None;
            Vector2[] s = _samples[CurrentStroke];
            int window = Mathf.Max(2, Mathf.CeilToInt(LookAhead / SampleSpacing));
            int end = Mathf.Min(s.Length - 1, _progressIndex + window);
            int best = -1;
            float bestDist = float.MaxValue;
            for (int i = _progressIndex; i <= end; i++)
            {
                float d = Vector2.Distance(p, s[i]);
                // Prefer the furthest sample that is within tolerance; this lets fast fingers keep up.
                if (d <= _tolerance) best = i;
                if (d < bestDist) bestDist = d;
            }
            if (best > _progressIndex) _progressIndex = best;
            if (_progressIndex >= s.Length - 1) return CompleteStroke();
            if (bestDist > _tolerance * 1.5f)
            {
                OffPathEvents++;
                return TraceFeedback.OffPath;
            }
            return TraceFeedback.OnPath;
        }

        public TraceFeedback End()
        {
            if (IsComplete || !_touching) return TraceFeedback.None;
            _touching = false;
            return StrokeProgress >= _threshold ? CompleteStroke() : TraceFeedback.None;
        }

        /// <summary>Demonstration / parent assist: completes the current stroke.</summary>
        public TraceFeedback SkipStroke() => IsComplete ? TraceFeedback.None : CompleteStroke();

        private TraceFeedback CompleteStroke()
        {
            CurrentStroke++;
            _progressIndex = 0;
            _touching = false;
            return IsComplete ? TraceFeedback.LetterComplete : TraceFeedback.StrokeComplete;
        }

        public static Vector2[] Resample(IReadOnlyList<Vector2> pts, float spacing)
        {
            var result = new List<Vector2> { pts[0] };
            float carry = 0f;
            for (int i = 1; i < pts.Count; i++)
            {
                Vector2 a = pts[i - 1], b = pts[i];
                float seg = Vector2.Distance(a, b);
                if (seg <= 1e-6f) continue;
                float t = spacing - carry;
                while (t <= seg)
                {
                    result.Add(Vector2.Lerp(a, b, t / seg));
                    t += spacing;
                }
                carry = seg - (t - spacing);
            }
            if ((result[result.Count - 1] - pts[pts.Count - 1]).sqrMagnitude > 1e-8f) result.Add(pts[pts.Count - 1]);
            return result.ToArray();
        }
    }
}
