using System;
using System.IO;
using ABCWonderworld.Persistence;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ABCWonderworld.Tests
{
    public class SaveRepositoryTests
    {
        private string _dir;
        private JsonSaveRepository _repo;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "abcw-save-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            _repo = new JsonSaveRepository(_dir);
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_dir, true); } catch { }
        }

        private static SaveData Sample()
        {
            var d = SaveData.CreateNew(DateTime.UtcNow);
            d.settings.musicVolume = 0.25f;
            d.rewards.wonderStars = 12;
            d.letters.Add(new LetterProgressData { letterId = "letter_a", exposures = 3 });
            return d;
        }

        [Test]
        public void FirstLaunchReturnsFreshSave()
        {
            SaveData d = _repo.Load(out SaveLoadReport r);
            Assert.That(r.Outcome, Is.EqualTo(SaveLoadOutcome.FirstLaunch));
            Assert.That(d.schemaVersion, Is.EqualTo(SaveData.CurrentVersion));
        }

        [Test]
        public void RoundTripPreservesData()
        {
            Assert.IsTrue(_repo.Save(Sample()));
            SaveData d = _repo.Load(out SaveLoadReport r);
            Assert.That(r.Outcome, Is.EqualTo(SaveLoadOutcome.Loaded));
            Assert.That(d.settings.musicVolume, Is.EqualTo(0.25f));
            Assert.That(d.rewards.wonderStars, Is.EqualTo(12));
            Assert.That(d.letters[0].exposures, Is.EqualTo(3));
            Assert.IsFalse(File.Exists(_repo.TempPath), "temp file must be gone after a good write");
        }

        [Test]
        public void SecondSaveKeepsBackup()
        {
            _repo.Save(Sample());
            var d = Sample(); d.rewards.wonderStars = 99;
            _repo.Save(d);
            Assert.IsTrue(File.Exists(_repo.BackupPath));
            StringAssert.Contains("\"wonderStars\":12", File.ReadAllText(_repo.BackupPath));
        }

        [Test]
        public void CorruptMainRecoversFromBackupAndPreservesTheCorruptFile()
        {
            _repo.Save(Sample());
            _repo.Save(Sample());
            File.WriteAllText(_repo.MainPath, "{\"schemaVersion\":1, \"rewa");
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Preserved unreadable save"));
            SaveData d = _repo.Load(out SaveLoadReport r);
            Assert.That(r.Outcome, Is.EqualTo(SaveLoadOutcome.RecoveredFromBackup));
            Assert.That(d.rewards.wonderStars, Is.EqualTo(12));
            Assert.That(Directory.GetFiles(_dir, "save.corrupt-*.json").Length, Is.EqualTo(1));
        }

        [Test]
        public void SaveAfterBackupRecoveryDoesNotOverwriteTheGoodBackup()
        {
            _repo.Save(Sample());
            _repo.Save(Sample());
            File.WriteAllText(_repo.MainPath, "{\"schemaVersion\":1, \"rewa");
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Preserved unreadable save"));
            SaveData d = _repo.Load(out _);
            Assert.IsFalse(File.Exists(_repo.MainPath), "the corrupt main is set aside after recovery");
            d.rewards.wonderStars = 13;
            Assert.IsTrue(_repo.Save(d));
            StringAssert.Contains("\"wonderStars\":12", File.ReadAllText(_repo.BackupPath), "backup is still the last good save");
            Assert.That(_repo.Load(out SaveLoadReport r).rewards.wonderStars, Is.EqualTo(13));
            Assert.That(r.Outcome, Is.EqualTo(SaveLoadOutcome.Loaded));
        }

        [Test]
        public void GarbageEverywhereStartsFreshWithoutThrowing()
        {
            File.WriteAllText(_repo.MainPath, "\0\0garbage");
            File.WriteAllText(_repo.BackupPath, "");
            LogAssert.ignoreFailingMessages = true;
            SaveData d = _repo.Load(out SaveLoadReport r);
            Assert.That(r.Outcome, Is.EqualTo(SaveLoadOutcome.ResetAfterCorruption));
            Assert.NotNull(d);
        }

        [Test]
        public void InterruptedWriteRecoversFromTempFile()
        {
            // Simulates termination after the old file was removed but before the temp file was moved.
            var d = Sample();
            File.WriteAllText(_repo.TempPath, JsonUtility.ToJson(d));
            SaveData loaded = _repo.Load(out SaveLoadReport r);
            Assert.That(r.Outcome, Is.EqualTo(SaveLoadOutcome.RecoveredFromTemp));
            Assert.That(loaded.rewards.wonderStars, Is.EqualTo(12));
        }

        [Test]
        public void WriteFailureKeepsPreviousSave()
        {
            _repo.Save(Sample());
            _repo.WriteFailureInjector = _ => true;
            var d = Sample(); d.rewards.wonderStars = 1;
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Write failed"));
            Assert.IsFalse(_repo.Save(d));
            _repo.WriteFailureInjector = null;
            Assert.That(_repo.Load(out _).rewards.wonderStars, Is.EqualTo(12));
        }

        [Test]
        public void FutureVersionIsPreservedNotOverwritten()
        {
            var d = Sample(); d.schemaVersion = SaveData.CurrentVersion + 5;
            File.WriteAllText(_repo.MainPath, JsonUtility.ToJson(d));
            LogAssert.ignoreFailingMessages = true;
            _repo.Load(out SaveLoadReport r);
            Assert.That(r.Outcome, Is.EqualTo(SaveLoadOutcome.FutureVersionPreserved));
            Assert.That(Directory.GetFiles(_dir, "save.future-*.json").Length, Is.EqualTo(1));
        }

        [Test]
        public void VersionZeroIsMigratedWithDefaults()
        {
            File.WriteAllText(_repo.MainPath, "{\"schemaVersion\":0,\"sessionCount\":4}");
            SaveData d = _repo.Load(out SaveLoadReport r);
            Assert.That(r.Outcome, Is.EqualTo(SaveLoadOutcome.Migrated));
            Assert.That(d.schemaVersion, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(d.sessionCount, Is.EqualTo(4));
            Assert.NotNull(d.settings);
        }

        [Test]
        public void SanitizeClampsOutOfRangeValues()
        {
            var d = Sample();
            d.settings.voiceVolume = 7f;
            d.settings.sessionReminderMinutes = -3;
            d.rewards.wonderStars = -2;
            d.letters.Add(null);
            SaveMigrator.Sanitize(d);
            Assert.That(d.settings.voiceVolume, Is.EqualTo(1f));
            Assert.That(d.settings.sessionReminderMinutes, Is.EqualTo(0));
            Assert.That(d.rewards.wonderStars, Is.EqualTo(0));
            Assert.That(d.letters.Count, Is.EqualTo(1));
        }
    }
}
