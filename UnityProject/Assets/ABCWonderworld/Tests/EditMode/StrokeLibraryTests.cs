using System.Linq;
using ABCWonderworld.Content;
using ABCWonderworld.Tracing;
using NUnit.Framework;
using UnityEngine;

namespace ABCWonderworld.Tests
{
    public class StrokeLibraryTests
    {
        private static readonly char[] AllForms =
            Enumerable.Range(0, 26).SelectMany(i => new[] { (char)('A' + i), (char)('a' + i) }).ToArray();

        [Test]
        public void EveryFormHasUsableStrokes([ValueSource(nameof(AllForms))] char glyph)
        {
            var strokes = LetterStrokeLibrary.Get(glyph);
            Assert.That(strokes.Count, Is.GreaterThan(0), $"{glyph} has no strokes");
            foreach (var s in strokes) Assert.That(s.Length, Is.GreaterThanOrEqualTo(2), $"{glyph} stroke too short");
        }

        [Test]
        public void GlyphsSitInTheirMetricBands([ValueSource(nameof(AllForms))] char glyph)
        {
            Rect b = LetterStrokeLibrary.Bounds(LetterStrokeLibrary.Get(glyph));
            Assert.That(b.yMax, Is.LessThanOrEqualTo(1.05f), $"{glyph} exceeds cap height");
            Assert.That(b.yMin, Is.GreaterThanOrEqualTo(LetterStrokeLibrary.Descender - 0.05f), $"{glyph} below descender");
            Assert.That(b.width, Is.InRange(0f, 1.05f), $"{glyph} too wide");
            if (char.IsUpper(glyph)) Assert.That(b.yMax, Is.GreaterThan(0.95f), $"{glyph} should reach cap height");
            string descenders = "gjpqy";
            if (char.IsLower(glyph) && descenders.IndexOf(glyph) < 0)
                Assert.That(b.yMin, Is.GreaterThan(-0.02f), $"{glyph} should not descend");
            if (descenders.IndexOf(glyph) >= 0) Assert.That(b.yMin, Is.LessThan(-0.1f), $"{glyph} should descend");
        }

        [Test]
        public void OnlyIAndJHaveDots()
        {
            foreach (char g in AllForms)
            {
                bool hasDot = LetterStrokeLibrary.Get(g).Any(s => LetterStrokeLibrary.IsDot(s));
                Assert.That(hasDot, Is.EqualTo(g == 'i' || g == 'j'), $"dot mismatch for {g}");
            }
        }

        [Test]
        public void LettersAreDistinctShapes()
        {
            // Guards against copy-paste errors: no two forms may share identical stroke data (except o/O, which are scaled differently, s/S etc.).
            var keys = AllForms.Select(g => (g, key: string.Join("|", LetterStrokeLibrary.Get(g).Select(s => string.Join(";", s.Select(p => $"{p.x:0.00},{p.y:0.00}")))))).ToList();
            foreach (var dup in keys.GroupBy(k => k.key).Where(gr => gr.Count() > 1))
                Assert.Fail("Identical stroke data: " + string.Join(",", dup.Select(d => d.g)));
        }

        [Test]
        public void ResampleKeepsEndpointsAndSpacing()
        {
            var pts = new[] { Vector2.zero, new Vector2(1, 0) };
            Vector2[] r = TracingEvaluator.Resample(pts, 0.1f);
            Assert.That(r.First(), Is.EqualTo(Vector2.zero));
            Assert.That((r.Last() - new Vector2(1, 0)).magnitude, Is.LessThan(1e-4f));
            Assert.That(r.Length, Is.InRange(10, 12));
        }
    }
}
