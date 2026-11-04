using System.Collections.Generic;
using System.Linq;
using ABCWonderworld.Content;
using ABCWonderworld.Tracing;
using NUnit.Framework;
using UnityEngine;

namespace ABCWonderworld.Tests
{
    public class TracingEvaluatorTests
    {
        private static readonly char[] AllForms =
            Enumerable.Range(0, 26).SelectMany(i => new[] { (char)('A' + i), (char)('a' + i) }).ToArray();

        private static TracingEvaluator Make(char g, TraceMode mode = TraceMode.Guided) =>
            new TracingEvaluator(LetterStrokeLibrary.Get(g).Select(s => (IReadOnlyList<Vector2>)s).ToList(), 0.16f, 0.85f, mode);

        /// <summary>Simulates a child following every stroke with a little wobble.</summary>
        private static void TraceAll(TracingEvaluator ev, float wobble, int seed = 1)
        {
            var rng = new System.Random(seed);
            for (int s = 0; s < ev.StrokeCount && !ev.IsComplete; s++)
            {
                var samples = ev.StrokeSamples(ev.CurrentStroke);
                Assert.That(ev.Begin(samples[0]), Is.Not.EqualTo(TraceFeedback.Rejected));
                if (ev.IsComplete) break;
                int before = ev.CurrentStroke;
                for (int i = 0; i < samples.Count && ev.CurrentStroke == before; i += 2)
                {
                    Vector2 jitter = new Vector2((float)(rng.NextDouble() - 0.5), (float)(rng.NextDouble() - 0.5)) * wobble;
                    ev.Move(samples[i] + jitter);
                }
                if (ev.CurrentStroke == before) ev.End();
            }
        }

        [Test]
        public void FollowingThePathCompletesEveryForm([ValueSource(nameof(AllForms))] char g)
        {
            TracingEvaluator ev = Make(g);
            TraceAll(ev, 0.1f);
            Assert.IsTrue(ev.IsComplete, $"{g} not completed (stroke {ev.CurrentStroke}/{ev.StrokeCount}, progress {ev.StrokeProgress:0.00})");
        }

        [Test]
        public void FreeModeAlsoCompletesWithSmallWobble([ValueSource(nameof(AllForms))] char g)
        {
            TracingEvaluator ev = Make(g, TraceMode.Free);
            TraceAll(ev, 0.06f, 7);
            Assert.IsTrue(ev.IsComplete, g.ToString());
        }

        [Test]
        public void CannotSkipAroundTheO()
        {
            TracingEvaluator ev = Make('O');
            var s = ev.StrokeSamples(0);
            ev.Begin(s[0]);
            ev.Move(s[s.Count - 2]); // the end of the circle is right next to its start
            Assert.IsFalse(ev.IsComplete);
            Assert.That(ev.StrokeProgress, Is.LessThan(0.2f));
        }

        [Test]
        public void StartingFarFromTheStartIsGentlyRejectedInGuidedMode()
        {
            TracingEvaluator ev = Make('L');
            Assert.That(ev.Begin(new Vector2(0.5f, 0.0f)), Is.EqualTo(TraceFeedback.Rejected));
            Assert.That(ev.RejectedStarts, Is.EqualTo(1));
        }

        [Test]
        public void RandomScribbleDoesNotComplete()
        {
            TracingEvaluator ev = Make('B');
            var rng = new System.Random(3);
            for (int i = 0; i < 400; i++)
            {
                var p = new Vector2((float)rng.NextDouble(), (float)rng.NextDouble());
                if (i % 40 == 0) { ev.End(); ev.Begin(p); }
                ev.Move(p);
            }
            Assert.IsFalse(ev.IsComplete);
        }

        [Test]
        public void LiftingKeepsProgressAndCanContinue()
        {
            TracingEvaluator ev = Make('l');
            var s = ev.StrokeSamples(0);
            ev.Begin(s[0]);
            for (int i = 0; i < s.Count / 2; i++) ev.Move(s[i]);
            ev.End();
            float mid = ev.StrokeProgress;
            Assert.That(mid, Is.InRange(0.3f, 0.7f));
            Assert.That(ev.Begin(ev.NextPoint), Is.EqualTo(TraceFeedback.Started));
            for (int i = s.Count / 2; i < s.Count; i++) ev.Move(s[i]);
            Assert.IsTrue(ev.IsComplete);
        }

        [Test]
        public void DotIsCompletedByATapNearIt()
        {
            TracingEvaluator ev = Make('i');
            var stem = ev.StrokeSamples(0);
            ev.Begin(stem[0]);
            foreach (var p in stem) ev.Move(p);
            Assert.That(ev.CurrentStroke, Is.EqualTo(1));
            Assert.That(ev.Begin(ev.NextPoint + new Vector2(0.05f, 0)), Is.EqualTo(TraceFeedback.LetterComplete));
        }

        [Test]
        public void WanderingAwayReportsOffPathWithoutResetting()
        {
            TracingEvaluator ev = Make('I');
            var s = ev.StrokeSamples(0);
            ev.Begin(s[0]);
            for (int i = 0; i < s.Count / 2; i++) ev.Move(s[i]);
            float before = ev.StrokeProgress;
            Assert.That(ev.Move(new Vector2(1.5f, 0.5f)), Is.EqualTo(TraceFeedback.OffPath));
            Assert.That(ev.StrokeProgress, Is.EqualTo(before));
        }
    }
}
