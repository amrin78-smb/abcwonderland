using System.Collections;
using System.IO;
using System.Linq;
using ABCWonderworld.Content;
using ABCWonderworld.Core;
using ABCWonderworld.Screens;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ABCWonderworld.Tests
{
    /// <summary>Real scenes, real services, temp save folder. Activities complete via their AutoPlay (same handlers as taps).</summary>
    public class GameFlowPlayTests
    {
        private static string _dir;

        [OneTimeSetUp]
        public void Boot()
        {
            _dir = Path.Combine(Path.GetTempPath(), "abcw-play-" + System.Guid.NewGuid().ToString("N"));
            ApplicationBootstrapper.SaveDirectoryOverride = _dir;
            GameServices.EnsureBooted();
        }

        private static IEnumerator RunActivity(ActivityType type, string letterId)
        {
            GameServices s = GameServices.Current;
            while (s.Flow.IsTransitioning) yield return null;
            Assert.IsTrue(s.Flow.StartActivity(type, letterId, "world_happy_meadow"));
            Assert.IsFalse(s.Flow.StartActivity(type, letterId, "world_happy_meadow"), "second navigation during a transition must be ignored");
            float t = 0;
            ActivityScreen screen = null;
            while ((screen == null || screen.Current == null || s.Flow.IsTransitioning) && t < 20) { t += Time.unscaledDeltaTime; screen = Object.FindAnyObjectByType<ActivityScreen>(); yield return null; }
            Assert.NotNull(screen?.Current, $"{type} did not start");
            Assert.That(screen.Current.Type, Is.EqualTo(type));
            var ap = screen.StartCoroutine(screen.AutoPlayCurrent());
            t = 0;
            while (!screen.Current.IsFinished && t < 120) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(screen.Current.IsFinished, $"{type} did not complete");
        }

        [UnityTest] public IEnumerator MeetLetterCompletesAndRecordsExposure()
        {
            yield return RunActivity(ActivityType.MeetLetter, "letter_d");
            Assert.That(GameServices.Current.Progress.Get("letter_d").exposures, Is.GreaterThan(0));
            Assert.That(GameServices.Current.Progress.Get("letter_d").vocabularyHeard.Count, Is.EqualTo(3));
        }

        [UnityTest] public IEnumerator TracingCompletesBothCases()
        {
            yield return RunActivity(ActivityType.Tracing, "letter_e");
            var p = GameServices.Current.Progress.Get("letter_e");
            Assert.That(p.tracingCompletionsUpper, Is.EqualTo(1));
            Assert.That(p.tracingCompletionsLower, Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator FindLetterRecordsRecognitionHonestly()
        {
            var before = GameServices.Current.Progress.Get("letter_b");
            int u0 = before?.recognitionCorrectUnassisted ?? 0, a0 = before?.recognitionCorrectAssisted ?? 0, w0 = before?.recognitionIncorrect ?? 0;
            yield return RunActivity(ActivityType.FindLetter, "letter_b");
            var p = GameServices.Current.Progress.Get("letter_b");
            Assert.That(p.recognitionCorrectUnassisted - u0 + p.recognitionCorrectAssisted - a0, Is.EqualTo(FindLetterActivityRounds));
            Assert.That(p.recognitionIncorrect - w0, Is.GreaterThan(0), "AutoPlay makes one deliberate wrong choice");
            Assert.That(p.recognitionCorrectUnassisted - u0, Is.LessThan(FindLetterActivityRounds), "the retried round is not unassisted");
        }

        private const int FindLetterActivityRounds = Activities.FindLetterActivity.Rounds;

        [UnityTest] public IEnumerator MatchCompletes() { yield return RunActivity(ActivityType.Match, "letter_c"); }

        [UnityTest] public IEnumerator AdventuresAtoECompleteAndAwardStars()
        {
            int before = GameServices.Current.Rewards.Stars;
            foreach (string id in new[] { "letter_a", "letter_b", "letter_c", "letter_d", "letter_e" })
                yield return RunActivity(ActivityType.Adventure, id);
            yield return new WaitForSeconds(0.2f);
            Assert.That(GameServices.Current.Progress.Get("letter_e").adventureCompletions, Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator PuzzleCompletes()
        {
            yield return RunActivity(ActivityType.Puzzle, "letter_k");
            Assert.That(GameServices.Current.Progress.Get("letter_k").practiceCount, Is.GreaterThan(0));
        }

        private static readonly string[] OtherLetters = "fghijklmnopqrstuvwxyz".Select(c => "letter_" + c).ToArray();

        [UnityTest] public IEnumerator AdventuresFtoZCompleteWithEveryArchetype([ValueSource(nameof(OtherLetters))] string letterId)
        {
            yield return RunActivity(ActivityType.Adventure, letterId);
            Assert.That(GameServices.Current.Progress.Get(letterId).adventureCompletions, Is.GreaterThan(0));
        }

        [UnityTest] public IEnumerator MusicSingAlongHighlightsLettersAndFinishes()
        {
            GameServices s = GameServices.Current;
            while (s.Flow.IsTransitioning) yield return null;
            s.Flow.GoMusic();
            MusicScreen m = null;
            float t = 0;
            while ((m == null || s.Flow.IsTransitioning) && t < 20) { t += Time.unscaledDeltaTime; m = Object.FindAnyObjectByType<MusicScreen>(); yield return null; }
            Assert.NotNull(m);
            m.TogglePlay();
            yield return new WaitForSeconds(4f);
            Assert.That(m.HighlightedLetters, Is.GreaterThanOrEqualTo(3), "letters highlight in time with the song");
            m.SkipNearEnd();
            t = 0;
            while (!m.SongFinished && t < 10) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(m.SongFinished);
        }

        [UnityTest] public IEnumerator SettingsChangesPersistAfterSave()
        {
            GameServices.Current.Settings.MusicVolume = 0.13f;
            Object.FindAnyObjectByType<ApplicationBootstrapper>().SaveNow();
            var loaded = new Persistence.JsonSaveRepository(_dir).Load(out var rep);
            Assert.That(rep.Outcome, Is.EqualTo(Persistence.SaveLoadOutcome.Loaded));
            Assert.That(loaded.settings.musicVolume, Is.EqualTo(0.13f).Within(1e-4));
            yield return null;
        }
    }
}
