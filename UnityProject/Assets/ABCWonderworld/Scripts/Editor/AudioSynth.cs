using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using E = ABCWonderworld.EditorTools.EditorAssetUtil;

namespace ABCWonderworld.EditorTools
{
    /// <summary>
    /// Deterministic synthesised placeholder SFX and an original music loop (no samples, no third-party
    /// material). Soft envelopes and modest peaks: nothing sudden or loud (GDD §7.4).
    /// </summary>
    public static class AudioSynth
    {
        public const int Rate = 22050;
        public const string SfxDir = E.Root + "/Resources/SFX";
        public const string MusicDir = E.Root + "/Resources/Music";

        private static readonly float[] Scale = { 523.25f, 587.33f, 659.25f, 698.46f, 783.99f, 880f, 987.77f, 1046.5f };

        public static void GenerateAll()
        {
            var rng = new System.Random(1234);
            Write(SfxDir, "tap", Env(Tone(0.07f, t => Mathf.Sin(2 * Mathf.PI * 900 * t) * 0.6f), 0.002f, 0.06f), 0.45f);
            Write(SfxDir, "pop", Env(Tone(0.12f, t => Mathf.Sin(2 * Mathf.PI * (300 + 2400 * t) * t) + Noise(rng) * 0.25f * Mathf.Exp(-t * 60)), 0.001f, 0.1f), 0.6f);
            Write(SfxDir, "chime", Mix(Bell(Scale[2] * 2, 0.6f), Delay(Bell(Scale[4] * 2, 0.7f), 0.11f)), 0.5f);
            Write(SfxDir, "success", Mix(Bell(Scale[0] * 2, 0.5f), Delay(Bell(Scale[2] * 2, 0.5f), 0.1f), Delay(Bell(Scale[4] * 2, 0.5f), 0.2f), Delay(Bell(Scale[7] * 2, 0.9f), 0.3f)), 0.5f);
            Write(SfxDir, "celebrate", Mix(Bell(Scale[0] * 2, 0.4f), Delay(Bell(Scale[2] * 2, 0.4f), 0.12f), Delay(Bell(Scale[4] * 2, 0.4f), 0.24f),
                Delay(Bell(Scale[7] * 2, 0.5f), 0.36f), Delay(Bell(Scale[4] * 2, 0.4f), 0.52f), Delay(Bell(Scale[7] * 2, 1.2f), 0.64f), Delay(Sparkle(rng, 1.0f), 0.6f)), 0.5f);
            Write(SfxDir, "sparkle", Sparkle(rng, 0.7f), 0.35f);
            Write(SfxDir, "star", Mix(Bell(1567.98f, 0.5f), Delay(Bell(2093f, 0.6f), 0.07f)), 0.4f);
            Write(SfxDir, "boing", Env(Tone(0.45f, t => Mathf.Sin(2 * Mathf.PI * (220 + 180 * Mathf.Sin(t * 30) * Mathf.Exp(-t * 6)) * t)), 0.005f, 0.4f), 0.5f);
            Write(SfxDir, "whoosh", Env(Tone(0.5f, t => Noise(rng) * Mathf.Sin(Mathf.PI * t / 0.5f)), 0.05f, 0.2f, lowpass: 0.08f), 0.4f);
            Write(SfxDir, "splash", Env(Tone(0.5f, t => Noise(rng) * Mathf.Exp(-t * 8)), 0.002f, 0.45f, lowpass: 0.25f), 0.5f);
            // Not a "wrong" buzzer: a soft, low, curious two-note marimba (GDD: no punitive sounds).
            Write(SfxDir, "gentle", Mix(Marimba(392f, 0.35f), Delay(Marimba(329.63f, 0.45f), 0.16f)), 0.35f);
            Write(SfxDir, "drum", Env(Tone(0.35f, t => Mathf.Sin(2 * Mathf.PI * (120 - 60 * t) * t) + Noise(rng) * 0.1f * Mathf.Exp(-t * 40)), 0.002f, 0.32f), 0.6f);
            Write(SfxDir, "reveal", Mix(Bell(Scale[4] * 2, 0.4f), Delay(Bell(Scale[7] * 2, 0.6f), 0.09f), Delay(Sparkle(rng, 0.5f), 0.05f)), 0.45f);
            Write(SfxDir, "drop", Env(Tone(0.2f, t => Mathf.Sin(2 * Mathf.PI * (900 - 2000 * t) * t)), 0.002f, 0.18f), 0.4f);
            for (int i = 0; i < Scale.Length; i++) Write(SfxDir, "note_" + i, Marimba(Scale[i], 0.9f), 0.5f);
            Write(MusicDir, "home_loop", HomeLoop(), 0.45f, true);
        }

        // ------------------------------------------------------------------ building blocks
        private static float Noise(System.Random r) => (float)(r.NextDouble() * 2 - 1);

        private static float[] Tone(float seconds, Func<float, float> f)
        {
            var s = new float[Mathf.CeilToInt(seconds * Rate)];
            for (int i = 0; i < s.Length; i++) s[i] = f(i / (float)Rate);
            return s;
        }

        private static float[] Env(float[] s, float attack, float decay, float lowpass = 0f)
        {
            float lp = 0;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)Rate;
                float a = t < attack ? t / attack : Mathf.Exp(-(t - attack) / Mathf.Max(0.001f, decay) * 3f);
                float v = s[i] * a;
                if (lowpass > 0) { lp += (v - lp) * lowpass; v = lp * 3f; }
                s[i] = v;
            }
            return s;
        }

        private static float[] Bell(float f, float seconds) =>
            Env(Tone(seconds, t => Mathf.Sin(2 * Mathf.PI * f * t) * 0.7f + Mathf.Sin(2 * Mathf.PI * f * 2.76f * t) * 0.2f * Mathf.Exp(-t * 8) + Mathf.Sin(2 * Mathf.PI * f * 5.4f * t) * 0.08f * Mathf.Exp(-t * 14)), 0.003f, seconds * 0.6f);

        private static float[] Marimba(float f, float seconds) =>
            Env(Tone(seconds, t => Mathf.Sin(2 * Mathf.PI * f * t) * 0.8f + Mathf.Sin(2 * Mathf.PI * f * 4f * t) * 0.25f * Mathf.Exp(-t * 25) + Mathf.Sin(2 * Mathf.PI * f * 10f * t) * 0.05f * Mathf.Exp(-t * 60)), 0.002f, seconds * 0.45f);

        private static float[] Sparkle(System.Random r, float seconds)
        {
            var outBuf = new float[Mathf.CeilToInt(seconds * Rate)];
            for (int k = 0; k < 8; k++)
            {
                float f = Scale[r.Next(Scale.Length)] * 4;
                float[] b = Bell(f, 0.25f);
                int off = (int)(k * seconds / 9f * Rate);
                for (int i = 0; i < b.Length && off + i < outBuf.Length; i++) outBuf[off + i] += b[i] * 0.35f;
            }
            return outBuf;
        }

        private static float[] Delay(float[] s, float seconds)
        {
            int off = (int)(seconds * Rate);
            var o = new float[s.Length + off];
            Array.Copy(s, 0, o, off, s.Length);
            return o;
        }

        private static float[] Mix(params float[][] parts)
        {
            int len = 0;
            foreach (float[] p in parts) len = Mathf.Max(len, p.Length);
            var o = new float[len];
            foreach (float[] p in parts) for (int i = 0; i < p.Length; i++) o[i] += p[i];
            return o;
        }

        /// <summary>Original 8-bar loop in C major: marimba melody, soft bass, light shaker. 112 bpm.</summary>
        private static float[] HomeLoop()
        {
            const float beat = 60f / 112f;
            int[] melody = { 0, 2, 4, 2, 4, 5, 4, -1, 2, 4, 5, 7, 5, 4, 2, -1, 0, 2, 4, 2, 4, 5, 4, 2, 4, 2, 1, 2, 0, -1, -1, -1 };
            int[] bass = { 0, 0, 3, 3, 4, 4, 0, 0 };
            float len = melody.Length * beat;
            var o = new float[Mathf.CeilToInt(len * Rate)];
            for (int i = 0; i < melody.Length; i++)
            {
                if (melody[i] < 0) continue;
                float[] n = Marimba(Scale[melody[i]], beat * 1.6f);
                int off = (int)(i * beat * Rate);
                for (int k = 0; k < n.Length; k++) o[(off + k) % o.Length] += n[k] * 0.45f;
            }
            for (int bar = 0; bar < bass.Length; bar++)
            {
                float f = Scale[bass[bar]] / 4f;
                float[] n = Env(Tone(beat * 3.8f, t => Mathf.Sin(2 * Mathf.PI * f * t) + Mathf.Sin(2 * Mathf.PI * f * 2 * t) * 0.2f), 0.02f, beat * 2.5f);
                int off = (int)(bar * 4 * beat * Rate);
                for (int k = 0; k < n.Length; k++) o[(off + k) % o.Length] += n[k] * 0.3f;
            }
            var r = new System.Random(7);
            for (int i = 0; i < melody.Length * 2; i++)
            {
                float[] n = Env(Tone(0.06f, t => Noise(r)), 0.002f, 0.03f, 0.5f);
                int off = (int)(i * beat * 0.5f * Rate);
                for (int k = 0; k < n.Length; k++) o[(off + k) % o.Length] += n[k] * (i % 2 == 0 ? 0.05f : 0.03f);
            }
            return o;
        }

        private static void Write(string dir, string name, float[] samples, float peak, bool loop = false)
        {
            E.EnsureFolder(dir);
            float max = 0.0001f;
            foreach (float s in samples) max = Mathf.Max(max, Mathf.Abs(s));
            string path = $"{dir}/{name}.wav";
            using (var w = new BinaryWriter(File.Create(Path.GetFullPath(path))))
            {
                int bytes = samples.Length * 2;
                w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + bytes); w.Write(new[] { 'W', 'A', 'V', 'E' });
                w.Write(new[] { 'f', 'm', 't', ' ' }); w.Write(16); w.Write((short)1); w.Write((short)1);
                w.Write(Rate); w.Write(Rate * 2); w.Write((short)2); w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(bytes);
                foreach (float s in samples) w.Write((short)Mathf.Clamp(s / max * peak * 32767f, -32767, 32767));
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            ConfigureClip(path, loop ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad);
        }

        public static void ConfigureClip(string path, AudioClipLoadType load)
        {
            var imp = AssetImporter.GetAtPath(path) as AudioImporter;
            if (imp == null) return;
            AudioImporterSampleSettings s = imp.defaultSampleSettings;
            if (imp.forceToMono && s.loadType == load && s.compressionFormat == AudioCompressionFormat.Vorbis) return;
            imp.forceToMono = true;
            s.loadType = load;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.6f;
            s.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
            imp.defaultSampleSettings = s;
            imp.SaveAndReimport();
        }
    }
}
