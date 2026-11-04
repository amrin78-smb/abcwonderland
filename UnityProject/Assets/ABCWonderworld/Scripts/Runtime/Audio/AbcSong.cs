using System.Collections.Generic;
using UnityEngine;

namespace ABCWonderworld.Audio
{
    /// <summary>
    /// The traditional ABC song (public-domain melody) as timing data: one entry per note, with the letter sung on
    /// it (or none for the closing lines). Drives the synthesised accompaniment, scheduled letter-name clips and
    /// letter highlighting, so everything stays in sync. PROVISIONAL: replace with an original sung recording plus
    /// a timing file in the same format.
    /// </summary>
    public static class AbcSong
    {
        public readonly struct Note
        {
            public readonly char Letter; // '\0' = no letter (closing lines)
            public readonly int Degree;  // C major scale degree, 0 = C
            public readonly float Beats;
            public readonly float Start; // beats from song start
            public Note(char l, int d, float b, float s) { Letter = l; Degree = d; Beats = b; Start = s; }
        }

        public const float SecondsPerBeat = 0.52f;
        public const float LeadInBeats = 2f;
        private static readonly float[] Scale = { 261.63f, 293.66f, 329.63f, 349.23f, 392f, 440f, 493.88f, 523.25f };
        private static List<Note> _notes;

        public static IReadOnlyList<Note> Notes => _notes ??= Build();

        private static List<Note> Build()
        {
            // (letter, degree, beats). C=0 D=1 E=2 F=3 G=4 A=5
            (char, int, float)[] raw =
            {
                ('A',0,1),('B',0,1),('C',4,1),('D',4,1),('E',5,1),('F',5,1),('G',4,2),
                ('H',3,1),('I',3,1),('J',2,1),('K',2,1),('L',1,0.5f),('M',1,0.5f),('N',1,0.5f),('O',1,0.5f),('P',0,2),
                ('Q',4,1),('R',4,1),('S',3,2),('T',2,1),('U',2,1),('V',1,2),
                ('W',4,1.5f),('X',3,0.5f),('Y',2,1),('\0',2,1),('Z',1,2),
                ('\0',0,1),('\0',0,1),('\0',4,1),('\0',4,1),('\0',5,1),('\0',5,1),('\0',4,2),
                ('\0',3,1),('\0',3,1),('\0',2,1),('\0',2,1),('\0',1,1),('\0',1,1),('\0',0,2),
            };
            var list = new List<Note>();
            float t = LeadInBeats;
            foreach (var (l, d, b) in raw) { list.Add(new Note(l, d, b, t)); t += b; }
            return list;
        }

        public static float LengthSeconds
        {
            get { Note last = Notes[Notes.Count - 1]; return (last.Start + last.Beats + 1f) * SecondsPerBeat; }
        }

        /// <summary>Index of the note sounding at time t (seconds), or -1.</summary>
        public static int NoteAt(double t)
        {
            float beat = (float)(t / SecondsPerBeat);
            for (int i = 0; i < Notes.Count; i++)
                if (beat >= Notes[i].Start && beat < Notes[i].Start + Notes[i].Beats) return i;
            return -1;
        }

        /// <summary>Synthesises the accompaniment (marimba melody + soft bass + count-in clicks). Runtime, no asset.</summary>
        public static AudioClip BuildAccompaniment()
        {
            const int rate = 22050;
            int len = Mathf.CeilToInt(LengthSeconds * rate);
            var s = new float[len];
            void Add(float f, float start, float dur, float amp, float decay)
            {
                int a = (int)(start * rate), n = (int)(dur * rate);
                for (int i = 0; i < n && a + i < len; i++)
                {
                    float t = i / (float)rate;
                    float env = Mathf.Min(1f, t / 0.004f) * Mathf.Exp(-t / decay);
                    s[a + i] += (Mathf.Sin(2 * Mathf.PI * f * t) + 0.25f * Mathf.Sin(2 * Mathf.PI * f * 4 * t) * Mathf.Exp(-t * 20)) * env * amp;
                }
            }
            for (int i = 0; i < 2; i++) Add(1567.98f, i * SecondsPerBeat, 0.1f, 0.15f, 0.03f);
            int[] bass = { 0, 0, 3, 0, 4, 4, 0, 0 };
            foreach (Note n in Notes)
            {
                float st = n.Start * SecondsPerBeat;
                Add(Scale[n.Degree] * 2f, st, n.Beats * SecondsPerBeat + 0.3f, 0.35f, 0.35f);
            }
            for (float b = LeadInBeats; b < Notes[Notes.Count - 1].Start + 2; b += 2)
                Add(Scale[bass[(int)((b - LeadInBeats) / 2) % bass.Length]] / 2f, b * SecondsPerBeat, SecondsPerBeat * 2f, 0.25f, 0.6f);
            float peak = 0.001f;
            foreach (float v in s) peak = Mathf.Max(peak, Mathf.Abs(v));
            for (int i = 0; i < len; i++) s[i] = s[i] / peak * 0.6f;
            AudioClip clip = AudioClip.Create("AbcSong_Accompaniment", len, 1, rate, false);
            clip.SetData(s, 0);
            return clip;
        }
    }
}
