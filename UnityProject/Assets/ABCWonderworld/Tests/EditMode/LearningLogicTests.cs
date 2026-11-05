using System;
using System.Linq;
using ABCWonderworld.Content;
using ABCWonderworld.Rewards;
using ABCWonderworld.Learning;
using ABCWonderworld.Parent;
using ABCWonderworld.Persistence;
using NUnit.Framework;

namespace ABCWonderworld.Tests
{
    public class LearningLogicTests
    {
        private SaveData _data;
        private ProgressService _p;

        [SetUp]
        public void SetUp()
        {
            _data = SaveData.CreateNew(DateTime.UtcNow);
            _p = new ProgressService(_data);
            _p.BeginSession();
        }

        [Test]
        public void StatesProgressFromIntroducedToPractised()
        {
            Assert.That(_p.StateOf("letter_a"), Is.EqualTo(LetterLearningState.NotYetSeen));
            _p.RecordExposure("letter_a", ActivityType.MeetLetter);
            Assert.That(_p.StateOf("letter_a"), Is.EqualTo(LetterLearningState.Introduced));
            _p.RecordPractice("letter_a", ActivityType.FindLetter, LetterCase.Upper);
            Assert.That(_p.StateOf("letter_a"), Is.EqualTo(LetterLearningState.Practised));
        }

        [Test]
        public void OneCorrectAnswerIsNotRecognitionEvidence()
        {
            _p.RecordPractice("letter_b", ActivityType.FindLetter, LetterCase.Upper);
            _p.RecordRecognition("letter_b", ActivityType.FindLetter, LetterCase.Upper, true, 1, Assistance.None);
            Assert.That(_p.StateOf("letter_b"), Is.EqualTo(LetterLearningState.Practised));
        }

        [Test]
        public void RecognitionNeedsRepeatsAcrossSessions()
        {
            _p.RecordPractice("letter_c", ActivityType.FindLetter, LetterCase.Upper);
            for (int i = 0; i < 5; i++) _p.RecordRecognition("letter_c", ActivityType.FindLetter, LetterCase.Upper, true, 1, Assistance.None);
            Assert.That(_p.StateOf("letter_c"), Is.EqualTo(LetterLearningState.Practised), "single session must not count");
            _p.BeginSession();
            _p.RecordRecognition("letter_c", ActivityType.FindLetter, LetterCase.Upper, true, 1, Assistance.None);
            Assert.That(_p.StateOf("letter_c"), Is.EqualTo(LetterLearningState.ShowingRecognition));
        }

        [Test]
        public void AssistedOrRetriedAnswersDoNotCountAsUnassisted()
        {
            for (int s = 0; s < 3; s++)
            {
                _p.BeginSession();
                _p.RecordRecognition("letter_d", ActivityType.FindLetter, LetterCase.Upper, true, 3, Assistance.Hint);
            }
            LetterProgressData d = _p.Get("letter_d");
            Assert.That(d.recognitionCorrectUnassisted, Is.EqualTo(0));
            Assert.That(d.recognitionCorrectAssisted, Is.EqualTo(3));
            Assert.That(_p.StateOf("letter_d"), Is.Not.EqualTo(LetterLearningState.ShowingRecognition));
        }

        [Test]
        public void RecentEventsAreBounded()
        {
            for (int i = 0; i < ProgressRules.RecentEventCap + 50; i++) _p.RecordExposure("letter_e", ActivityType.MeetLetter);
            Assert.That(_data.recentEvents.Count, Is.EqualTo(ProgressRules.RecentEventCap));
        }

        [Test]
        public void TracingAndVocabularyAreSeparateEvidence()
        {
            _p.RecordTracing("letter_f", LetterCase.Lower, true, Assistance.None);
            _p.RecordVocabulary("letter_f", "vocab_fish", ActivityType.MeetLetter);
            _p.RecordVocabulary("letter_f", "vocab_fish", ActivityType.MeetLetter);
            LetterProgressData f = _p.Get("letter_f");
            Assert.That(f.tracingCompletionsLower, Is.EqualTo(1));
            Assert.That(f.tracingCompletionsUpper, Is.EqualTo(0));
            Assert.That(f.vocabularyHeard, Is.EquivalentTo(new[] { "vocab_fish" }));
            Assert.That(f.recognitionCorrectUnassisted, Is.EqualTo(0), "tracing is not recognition evidence");
        }

        [Test]
        public void AdaptiveDifficultyStepsUpAndDownGradually()
        {
            var ad = new AdaptiveDifficulty(_data);
            Assert.That(ad.LevelFor(ActivityType.FindLetter), Is.EqualTo(0));
            for (int i = 0; i < AdaptiveDifficulty.StepUpAfter; i++) ad.RecordRound(ActivityType.FindLetter, 0, Assistance.None);
            Assert.That(ad.LevelFor(ActivityType.FindLetter), Is.EqualTo(1));
            ad.RecordRound(ActivityType.FindLetter, 3, Assistance.Hint);
            Assert.That(ad.LevelFor(ActivityType.FindLetter), Is.EqualTo(1), "one hard round must not drop the level");
            ad.RecordRound(ActivityType.FindLetter, 2, Assistance.None);
            Assert.That(ad.LevelFor(ActivityType.FindLetter), Is.EqualTo(0));
        }

        [Test]
        public void ParentsCanDisableAdaptation()
        {
            _data.profile.adaptiveDifficulty = false;
            _data.profile.learningProfile = LearningProfile.Discoverer;
            var ad = new AdaptiveDifficulty(_data);
            for (int i = 0; i < 20; i++) ad.RecordRound(ActivityType.FindLetter, 0, Assistance.None);
            Assert.That(ad.LevelFor(ActivityType.FindLetter), Is.EqualTo(1));
        }

        [Test]
        public void ChoiceCountsMatchTheGdd()
        {
            var rng = new Random(1);
            for (int i = 0; i < 50; i++)
            {
                Assert.That(AdaptiveDifficulty.ChoiceCount(0, rng), Is.EqualTo(2));
                Assert.That(AdaptiveDifficulty.ChoiceCount(1, rng), Is.InRange(3, 4));
                Assert.That(AdaptiveDifficulty.ChoiceCount(2, rng), Is.InRange(5, 6));
            }
        }

        [Test]
        public void ChoicesContainTargetOnceAndAreDistinct()
        {
            var rng = new Random(42);
            for (char c = 'A'; c <= 'Z'; c++)
            {
                foreach (char t in new[] { c, char.ToLowerInvariant(c) })
                {
                    for (int n = 2; n <= 6; n++)
                    {
                        var choices = ChoiceGenerator.Generate(t, n, rng, n >= 5);
                        Assert.That(choices.Count, Is.EqualTo(n));
                        Assert.That(choices.Count(x => x == t), Is.EqualTo(1));
                        Assert.That(choices.Distinct().Count(), Is.EqualTo(n));
                        Assert.That(choices.All(x => char.IsUpper(x) == char.IsUpper(t)), "case must match target");
                        if (n < 5) Assert.That(choices.Where(x => x != t).All(x => !ChoiceGenerator.AreConfusable(x, t)), $"confusable with {t}");
                    }
                }
            }
        }

        [Test]
        public void ClassicConfusionsAreKeptApartForBeginners()
        {
            foreach (var (a, b) in new[] { ('B', 'D'), ('b', 'd'), ('p', 'q'), ('M', 'W'), ('E', 'F'), ('P', 'R') })
            {
                Assert.IsTrue(ChoiceGenerator.AreConfusable(a, b), $"{a}/{b}");
                var rng = new Random(3);
                for (int i = 0; i < 40; i++)
                    Assert.That(ChoiceGenerator.Generate(a, 4, rng, false, "ABCDE"), Has.No.Member(b));
            }
        }

        [Test]
        public void TargetPositionIsRandomised()
        {
            var rng = new Random(5);
            var positions = Enumerable.Range(0, 60).Select(_ => ChoiceGenerator.Generate('M', 4, rng, false).IndexOf('M')).Distinct().Count();
            Assert.That(positions, Is.EqualTo(4));
        }

        [Test]
        public void ParentGateRequiresHoldThenBothNumbersInOrder()
        {
            var gate = new ParentGate(new Random(9));
            Assert.IsFalse(gate.Press(gate.First), "pressing before the hold must not count");
            gate.UpdateHold(0.5f);
            Assert.IsFalse(gate.HoldPassed);
            gate.UpdateHold(ParentGate.HoldSeconds);
            Assert.IsFalse(gate.Press(gate.First));
            Assert.IsTrue(gate.Press(gate.Second));
            Assert.That(gate.ButtonOrder.OrderBy(x => x), Is.EqualTo(Enumerable.Range(1, 9)));
        }

        [Test]
        public void ParentGateWrongTapGivesANewChallenge()
        {
            var gate = new ParentGate(new Random(11));
            gate.UpdateHold(5);
            int wrong = Enumerable.Range(1, 9).First(n => n != gate.First);
            gate.Press(wrong);
            Assert.IsFalse(gate.Passed);
            Assert.IsTrue(gate.Press(gate.First) == false && gate.Press(gate.Second));
        }

        [Test]
        public void SessionReminderOnlyAtNaturalPausesAndOnce()
        {
            var s = new SessionService { ReminderMinutes = 1 };
            for (int i = 0; i < 59; i++) s.Tick(1, false);
            Assert.IsFalse(s.TrySuggestBreakAtNaturalPause());
            s.Tick(5, false); // long frame clamped to 1s
            Assert.IsTrue(s.ShouldSuggestBreak);
            Assert.IsTrue(s.TrySuggestBreakAtNaturalPause());
            Assert.IsFalse(s.TrySuggestBreakAtNaturalPause());
            s.Snooze();
            Assert.IsFalse(s.ShouldSuggestBreak);
        }

        [Test]
        public void WardrobeEquipsOneHatAtATimeAndOnlyEarnedItems()
        {
            var hat1 = UnityEngine.ScriptableObject.CreateInstance<RewardDefinition>(); hat1.id = "h1"; hat1.type = RewardType.Hat; hat1.condition = UnlockCondition.StarTotal; hat1.amount = 1;
            var hat2 = UnityEngine.ScriptableObject.CreateInstance<RewardDefinition>(); hat2.id = "h2"; hat2.type = RewardType.Hat; hat2.condition = UnlockCondition.StarTotal; hat2.amount = 2;
            var gl = UnityEngine.ScriptableObject.CreateInstance<RewardDefinition>(); gl.id = "g"; gl.type = RewardType.Glasses; gl.condition = UnlockCondition.StarTotal; gl.amount = 50;
            var r = new Rewards.RewardService(_data, new[] { hat1, hat2, gl }, _p);
            r.Equip("g");
            Assert.That(_data.rewards.equipped, Is.Empty, "locked items cannot be equipped");
            r.AwardStars(2);
            r.Equip("h1"); r.Equip("h2");
            Assert.That(_data.rewards.equipped, Is.EquivalentTo(new[] { "h2" }));
            r.Equip("h2");
            Assert.That(_data.rewards.equipped, Is.Empty, "tapping again takes it off");
        }

        [Test]
        public void AbcSongCoversEveryLetterInOrder()
        {
            var letters = Audio.AbcSong.Notes.Where(n => n.Letter != '\0').Select(n => n.Letter).ToArray();
            Assert.That(new string(letters), Is.EqualTo("ABCDEFGHIJKLMNOPQRSTUVWXYZ"));
            Assert.That(Audio.AbcSong.NoteAt((Audio.AbcSong.LeadInBeats + 0.1f) * Audio.AbcSong.SecondsPerBeat), Is.EqualTo(0));
        }

        [Test]
        public void ResetClearsProgressButKeepsSettings()
        {
            _data.settings.musicVolume = 0.2f;
            _p.RecordExposure("letter_a", ActivityType.MeetLetter);
            _data.rewards.wonderStars = 7;
            _p.ResetAll();
            Assert.That(_data.letters, Is.Empty);
            Assert.That(_data.rewards.wonderStars, Is.EqualTo(0));
            Assert.That(_data.settings.musicVolume, Is.EqualTo(0.2f));
        }

        // ---------------------------------------------------------------- bug-sweep regressions (2026-11-05)

        [Test]
        public void UnfinishedRoundCountsEveryWrongAttempt()
        {
            _p.RecordRecognition("letter_a", ActivityType.FindLetter, LetterCase.Upper, false, 3, Assistance.None);
            LetterProgressData p = _data.letters.First(l => l.letterId == "letter_a");
            Assert.That(p.recognitionIncorrect, Is.EqualTo(3), "an aborted round's attempts were all wrong");
            _p.RecordRecognition("letter_a", ActivityType.FindLetter, LetterCase.Upper, true, 3, Assistance.None);
            Assert.That(p.recognitionIncorrect, Is.EqualTo(5), "a correct round's last attempt was right");
        }

        [Test]
        public void QuietExposureDoesNotFloodRecentActivity()
        {
            _p.RecordPractice("letter_b", ActivityType.Match, null);
            int events = _data.recentEvents.Count;
            for (int i = 0; i < 400; i++) _p.RecordExposure("letter_c", ActivityType.Music, logEvent: false);
            Assert.That(_data.recentEvents.Count, Is.EqualTo(events));
            Assert.That(_data.letters.First(l => l.letterId == "letter_c").exposures, Is.EqualTo(400));
        }

        [Test]
        public void SnoozingTheBreakReminderKeepsLifetimePlayTime()
        {
            var s = new SessionService { ReminderMinutes = 1 };
            for (int i = 0; i < 90; i++) s.Tick(1, false);
            s.Snooze();
            Assert.That(s.ActiveSeconds, Is.EqualTo(0));
            Assert.That(s.TotalSeconds, Is.EqualTo(90));
        }

        [Test]
        public void NegativePlayTimeFromOlderBuildsIsClamped()
        {
            _data.totalPlaySeconds = -1;
            SaveMigrator.Sanitize(_data);
            Assert.That(_data.totalPlaySeconds, Is.EqualTo(0));
        }

        [Test]
        public void NarrowScreensKeepTheSameHorizontalFraming()
        {
            const float fov = 40f;
            Assert.That(World.KeepHorizontalFraming.FovFor(fov, 16f / 9f), Is.EqualTo(fov));
            Assert.That(World.KeepHorizontalFraming.FovFor(fov, 20f / 9f), Is.EqualTo(fov), "wider screens are never narrowed");
            float halfW(float v, float a) => UnityEngine.Mathf.Atan(UnityEngine.Mathf.Tan(v * 0.5f * UnityEngine.Mathf.Deg2Rad) * a);
            float tablet = World.KeepHorizontalFraming.FovFor(fov, 4f / 3f);
            Assert.That(tablet, Is.GreaterThan(fov));
            Assert.That(halfW(tablet, 4f / 3f), Is.EqualTo(halfW(fov, 16f / 9f)).Within(1e-4f));
        }
    }
}
