using System;
using System.IO;
using UnityEngine;

namespace ABCWonderworld.Persistence
{
    public interface ISaveRepository
    {
        /// <summary>Loads the save, recovering from backup or starting fresh. Never throws.</summary>
        SaveData Load(out SaveLoadReport report);

        /// <summary>Writes atomically. Returns false (and keeps the previous file) on failure.</summary>
        bool Save(SaveData data);
    }

    public enum SaveLoadOutcome { FirstLaunch, Loaded, Migrated, RecoveredFromBackup, RecoveredFromTemp, ResetAfterCorruption, FutureVersionPreserved }

    public readonly struct SaveLoadReport
    {
        public readonly SaveLoadOutcome Outcome;
        public readonly string Detail;
        public SaveLoadReport(SaveLoadOutcome o, string d) { Outcome = o; Detail = d; }
        public override string ToString() => $"{Outcome}{(string.IsNullOrEmpty(Detail) ? "" : ": " + Detail)}";
    }

    /// <summary>
    /// Local JSON persistence (D-004). Write path: serialize → save.json.tmp → copy current to save.json.bak →
    /// replace save.json. Read path: save.json → save.json.tmp → save.json.bak → fresh.
    /// Unreadable files are kept as save.corrupt-*.json, never silently deleted.
    /// </summary>
    public sealed class JsonSaveRepository : ISaveRepository
    {
        public const string FileName = "save.json";
        private readonly string _dir;
        private readonly Func<DateTime> _clock;

        public string MainPath => Path.Combine(_dir, FileName);
        public string TempPath => MainPath + ".tmp";
        public string BackupPath => MainPath + ".bak";

        /// <summary>Set by tests to simulate disk-full or permission failures.</summary>
        internal Func<string, bool> WriteFailureInjector;

        public JsonSaveRepository(string directory, Func<DateTime> clock = null)
        {
            _dir = directory;
            _clock = clock ?? (() => DateTime.UtcNow);
        }

        public static JsonSaveRepository CreateDefault() => new JsonSaveRepository(Application.persistentDataPath);

        public SaveData Load(out SaveLoadReport report)
        {
            bool anyFile = File.Exists(MainPath) || File.Exists(TempPath) || File.Exists(BackupPath);
            if (!anyFile)
            {
                report = new SaveLoadReport(SaveLoadOutcome.FirstLaunch, null);
                return SaveData.CreateNew(_clock());
            }

            string failure = null;
            if (TryRead(MainPath, out SaveData data, out SaveMigrator.Result r, ref failure))
            {
                report = new SaveLoadReport(r == SaveMigrator.Result.Migrated ? SaveLoadOutcome.Migrated : SaveLoadOutcome.Loaded, null);
                return data;
            }
            if (r == SaveMigrator.Result.UnsupportedFutureVersion)
            {
                // Written by a newer game version: preserve it untouched and run on a fresh in-memory save.
                Preserve(MainPath, "future");
                report = new SaveLoadReport(SaveLoadOutcome.FutureVersionPreserved, failure);
                return SaveData.CreateNew(_clock());
            }
            if (File.Exists(MainPath)) Preserve(MainPath, "corrupt");
            // A crash between deleting save.json and moving the temp file leaves a complete temp file.
            if (TryRead(TempPath, out data, out _, ref failure))
            {
                report = new SaveLoadReport(SaveLoadOutcome.RecoveredFromTemp, failure);
                return data;
            }
            if (TryRead(BackupPath, out data, out _, ref failure))
            {
                report = new SaveLoadReport(SaveLoadOutcome.RecoveredFromBackup, failure);
                return data;
            }
            report = new SaveLoadReport(SaveLoadOutcome.ResetAfterCorruption, failure);
            return SaveData.CreateNew(_clock());
        }

        public bool Save(SaveData data)
        {
            if (data == null) return false;
            try
            {
                Directory.CreateDirectory(_dir);
                data.schemaVersion = SaveData.CurrentVersion;
                data.lastSavedUtcTicks = _clock().Ticks;
                string json = JsonUtility.ToJson(data);
                if (WriteFailureInjector != null && WriteFailureInjector(TempPath)) throw new IOException("Injected write failure");
                using (var fs = new FileStream(TempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var w = new StreamWriter(fs, new System.Text.UTF8Encoding(false)))
                {
                    w.Write(json);
                    w.Flush();
                    fs.Flush(true);
                }
                if (File.Exists(MainPath))
                {
                    File.Copy(MainPath, BackupPath, true);
                    File.Delete(MainPath);
                }
                File.Move(TempPath, MainPath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Save] Write failed, previous save kept: " + e.Message);
                try { if (File.Exists(TempPath) && File.Exists(MainPath)) File.Delete(TempPath); } catch { /* best effort */ }
                return false;
            }
        }

        private static bool TryRead(string path, out SaveData data, out SaveMigrator.Result result, ref string failure)
        {
            data = null;
            result = SaveMigrator.Result.Invalid;
            if (!File.Exists(path)) return false;
            try
            {
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith("{")) { failure = $"{Path.GetFileName(path)} empty or not JSON"; return false; }
                data = JsonUtility.FromJson<SaveData>(json);
                result = SaveMigrator.Migrate(data);
                if (result == SaveMigrator.Result.Invalid || result == SaveMigrator.Result.UnsupportedFutureVersion)
                {
                    failure = $"{Path.GetFileName(path)}: {result}";
                    data = null;
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                failure = $"{Path.GetFileName(path)}: {e.Message}";
                data = null;
                return false;
            }
        }

        private void Preserve(string path, string tag)
        {
            try
            {
                string dest = Path.Combine(_dir, $"save.{tag}-{_clock():yyyyMMdd-HHmmss}.json");
                if (!File.Exists(dest)) File.Copy(path, dest);
                Debug.LogWarning($"[Save] Preserved unreadable save as {Path.GetFileName(dest)}");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Save] Could not preserve file: " + e.Message);
            }
        }
    }

    /// <summary>In-memory repository for tests and editor previews.</summary>
    public sealed class MemorySaveRepository : ISaveRepository
    {
        public string Json;
        public int SaveCount;

        public SaveData Load(out SaveLoadReport report)
        {
            if (string.IsNullOrEmpty(Json))
            {
                report = new SaveLoadReport(SaveLoadOutcome.FirstLaunch, null);
                return SaveData.CreateNew(DateTime.UtcNow);
            }
            var d = JsonUtility.FromJson<SaveData>(Json);
            SaveMigrator.Migrate(d);
            report = new SaveLoadReport(SaveLoadOutcome.Loaded, null);
            return d;
        }

        public bool Save(SaveData data)
        {
            Json = JsonUtility.ToJson(data);
            SaveCount++;
            return true;
        }
    }
}
