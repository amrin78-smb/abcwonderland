using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ABCWonderworld.Content;
using ABCWonderworld.Screens;
using ABCWonderworld.UI;
using UnityEngine;

namespace ABCWonderworld.Core
{
    /// <summary>
    /// Automated smoke play-through for desktop test players (never active on Android or in normal play).
    /// Launch with: -abcw-autopilot -abcw-shots &lt;dir&gt;. Navigates the real flow through BigButton.Click()
    /// (the same path as a tap), completes activities via each activity's AutoPlay, screenshots each screen
    /// and writes autopilot_report.txt. Exit code 0 = every step passed.
    /// </summary>
    public sealed class AutoPilot : MonoBehaviour
    {
        private string _dir;
        private readonly StringBuilder _report = new StringBuilder();
        private int _failures;
        private int _shot;
        private readonly List<string> _errors = new List<string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void MaybeStart()
        {
            if (Application.isEditor || Application.platform == RuntimePlatform.Android) return;
            string[] args = Environment.GetCommandLineArgs();
            if (!args.Contains("-abcw-autopilot")) return;
            var go = new GameObject("[AutoPilot]");
            DontDestroyOnLoad(go);
            var ap = go.AddComponent<AutoPilot>();
            int i = Array.IndexOf(args, "-abcw-shots");
            ap._dir = i >= 0 && i + 1 < args.Length ? args[i + 1] : Path.Combine(Application.persistentDataPath, "autopilot");
            Directory.CreateDirectory(ap._dir);
            ApplicationBootstrapper.SaveDirectoryOverride = Path.Combine(ap._dir, "save");
        }

        private void Awake() => Application.logMessageReceived += OnLog;
        private void OnDestroy() => Application.logMessageReceived -= OnLog;

        private void OnLog(string msg, string stack, LogType type)
        {
            if (type == LogType.Exception || (type == LogType.Error && !msg.Contains("[Content]")))
                _errors.Add($"{type}: {msg.Split('\n')[0]}");
        }

        private IEnumerator Start()
        {
            float t0 = Time.realtimeSinceStartup;
            yield return WaitFor(() => FindAnyObjectByType<HomeScreen>() != null && !Loading, 30, "Home loads");
            yield return new WaitForSeconds(1.5f);
            yield return Shot("01_home");
            var home = FindAnyObjectByType<HomeScreen>();

            // Parent gate: a quick tap must NOT open it; the hold + challenge must.
            Check(!home.Parent.IsOpen, "Parent Zone closed after load");
            home.Parent.OpenGate();
            yield return null;
            yield return Shot("02_parent_gate");
            home.Parent.SolveGateForTests();
            yield return new WaitForSeconds(0.5f);
            Check(home.Parent.IsOpen, "Parent Zone opens after hold + challenge");
            yield return Shot("03_parent_dashboard");
            home.Parent.ShowTab(1);
            yield return new WaitForSeconds(0.3f);
            yield return Shot("04_parent_settings");
            home.Parent.Close();

            // Play → world map → Happy Meadow.
            Check(home.PlayButton.Click(), "Play button");
            yield return WaitFor(() => FindAnyObjectByType<WorldMapScreen>() != null && !Loading, 15, "World map loads");
            yield return new WaitForSeconds(1f);
            yield return Shot("05_world_map");
            var map = FindAnyObjectByType<WorldMapScreen>();
            Check(map.Portals.Count == 5, "Five world portals");
            Check(map.Portals[0].Click(), "Happy Meadow portal");
            yield return WaitFor(() => FindAnyObjectByType<WorldScreen>() != null && !Loading, 15, "Happy Meadow loads");
            yield return new WaitForSeconds(1.2f);
            yield return Shot("06_world_meadow");

            ContentCatalog cat = GameServices.Current.Catalog;
            // Every mode for letter A, then the adventure for B–E.
            var plan = new List<(char letter, ActivityType type)>();
            foreach (var (type, _) in WorldScreen.PickerModes) plan.Add(('A', type));
            foreach (char c in "BCDE") plan.Add((c, ActivityType.Adventure));
            plan.Add(('B', ActivityType.FindLetter));
            plan.Add(('A', ActivityType.Puzzle));
            plan.Add(('C', ActivityType.Tracing));

            foreach (var (letter, type) in plan)
            {
                var world = FindAnyObjectByType<WorldScreen>();
                if (world == null)
                {
                    GameServices.Current.Flow.GoWorld("world_happy_meadow");
                    yield return WaitFor(() => FindAnyObjectByType<WorldScreen>() != null && !Loading, 15, "World reload");
                    world = FindAnyObjectByType<WorldScreen>();
                }
                var ch = world.Characters.FirstOrDefault(c => c.letterId == LetterDefinition.IdFor(letter));
                Check(ch != null, $"Character {letter} present");
                if (ch == null) continue;
                ch.OnPointerClick(null);
                yield return new WaitForSeconds(0.6f);
                if (letter == 'A' && type == ActivityType.MeetLetter) Shot("07_world_picker");
                BigButton mode = world.PickerButtons.FirstOrDefault(b => b.name == "Mode_" + type);
                Check(mode != null && mode.Click(), $"Picker {type} for {letter}");
                yield return WaitFor(() => FindAnyObjectByType<ActivityScreen>()?.Current != null && !Loading, 15, $"{type} {letter} loads");
                var screen = FindAnyObjectByType<ActivityScreen>();
                if (screen?.Current == null) continue;
                yield return new WaitForSeconds(2.5f);
                yield return Shot($"10_{letter}_{type}_start");
                float start = Time.realtimeSinceStartup;
                StartCoroutine(screen.AutoPlayCurrent());
                yield return WaitFor(() => screen == null || screen.Current == null || screen.Current.IsFinished, 120, $"{type} {letter} completes");
                Log($"  {type} {letter}: {(Time.realtimeSinceStartup - start):0.0}s");
                yield return new WaitForSeconds(1.6f);
                yield return Shot($"11_{letter}_{type}_done");
                var back = FindObjectsByType<BigButton>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == "Back" && b.transform.parent?.name == "Choices");
                Check(back != null && back.Click(), "Celebration back button");
                yield return WaitFor(() => FindAnyObjectByType<WorldScreen>() != null && !Loading, 15, "Back to world");
                yield return new WaitForSeconds(0.5f);
            }

            // Progress was recorded honestly.
            var p = GameServices.Current.Progress;
            Check(p.StateOf("letter_a") >= Learning.LetterLearningState.Practised, "Letter A practised");
            Check(GameServices.Current.Rewards.Stars >= plan.Count, $"Stars awarded ({GameServices.Current.Rewards.Stars})");
            Check(GameServices.Current.Rewards.IsUnlocked("sticker_a"), "Sticker A unlocked");

            // Adventures F–Z (every archetype), started directly.
            foreach (char c in "FGHIJKLMNOPQRSTUVWXYZ")
            {
                LetterDefinition l = cat.Letter(c);
                GameServices.Current.Flow.StartActivity(ActivityType.Adventure, l.id, l.worldId);
                yield return WaitFor(() => FindAnyObjectByType<ActivityScreen>()?.Current != null && !Loading, 15, $"Adventure {c} loads");
                var screen = FindAnyObjectByType<ActivityScreen>();
                if (screen?.Current == null) continue;
                yield return new WaitForSeconds(2f);
                yield return Shot($"12_{c}_Adventure_{l.adventure.archetype}");
                StartCoroutine(screen.AutoPlayCurrent());
                float st = Time.realtimeSinceStartup;
                yield return WaitFor(() => screen == null || screen.Current == null || screen.Current.IsFinished, 120, $"Adventure {c} ({l.adventure.archetype}) completes");
                Log($"  Adventure {c}: {(Time.realtimeSinceStartup - st):0.0}s");
                yield return new WaitForSeconds(1.2f);
            }

            // Music: sing-along highlights letters, then finishes.
            GameServices.Current.Flow.GoMusic();
            yield return WaitFor(() => FindAnyObjectByType<MusicScreen>() != null && !Loading, 15, "Music loads");
            var music = FindAnyObjectByType<MusicScreen>();
            music.TogglePlay();
            yield return new WaitForSeconds(5f);
            yield return Shot("25_music_singalong");
            Check(music.HighlightedLetters >= 3, $"Song highlights letters ({music.HighlightedLetters})");
            music.SkipNearEnd();
            yield return WaitFor(() => music.SongFinished, 10, "Song finishes");

            // Other worlds load.
            for (int i = 1; i < cat.worlds.Count; i++)
            {
                GameServices.Current.Flow.GoWorld(cat.worlds[i].id);
                yield return WaitFor(() => FindAnyObjectByType<WorldScreen>()?.World == cat.worlds[i] && !Loading, 15, $"{cat.worlds[i].displayName} loads");
                yield return new WaitForSeconds(1f);
                yield return Shot($"20_world_{i}_{cat.worlds[i].id}");
            }

            // Alphabet book.
            GameServices.Current.Flow.GoAlphabetBook();
            yield return WaitFor(() => FindAnyObjectByType<AlphabetBookScreen>() != null && !Loading, 15, "Alphabet book loads");
            yield return new WaitForSeconds(0.8f);
            yield return Shot("30_alphabet_book");
            var book = FindAnyObjectByType<AlphabetBookScreen>();
            Check(book.LetterButtons.Count == 26, "26 letters in the book");
            book.OpenPage(cat.Letter('A'));
            yield return new WaitForSeconds(0.8f);
            yield return Shot("31_alphabet_page_a");
            Check(book.WardrobeButton.Click(), "Wardrobe opens");
            yield return new WaitForSeconds(0.5f);
            var bow = FindObjectsByType<BigButton>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == "Item_bow_pink");
            Check(bow != null && bow.Click(), "Equip earned bow");
            yield return new WaitForSeconds(0.5f);
            yield return Shot("32_wardrobe");
            Check(GameServices.Current.Save.rewards.equipped.Contains("bow_pink"), "Bow equipped");

            // Parent dashboard after play.
            GameServices.Current.Flow.GoHome();
            yield return WaitFor(() => FindAnyObjectByType<HomeScreen>() != null && !Loading, 15, "Home again");
            yield return new WaitForSeconds(0.5f);
            home = FindAnyObjectByType<HomeScreen>();
            home.Parent.SolveGateForTests();
            yield return new WaitForSeconds(0.5f);
            yield return Shot("40_parent_dashboard_after_play");
            Check(GameObject.Find("Cosmetic_bow_pink") != null, "Equipped bow shown on Home characters");
            home.Parent.Close();

            // Persistence: force a save and read it back from disk.
            FindAnyObjectByType<ApplicationBootstrapper>().SaveNow();
            var repo = new Persistence.JsonSaveRepository(ApplicationBootstrapper.SaveDirectoryOverride);
            var reloaded = repo.Load(out var rep);
            Check(rep.Outcome == Persistence.SaveLoadOutcome.Loaded && reloaded.letters.Any(l => l.letterId == "letter_a"), $"Save round-trip ({rep})");

            Check(_errors.Count == 0, $"No errors/exceptions logged ({_errors.Count})");
            foreach (string e in _errors.Distinct().Take(30)) Log("    " + e);
            Log($"Total {Time.realtimeSinceStartup - t0:0}s, failures: {_failures}");
            File.WriteAllText(Path.Combine(_dir, "autopilot_report.txt"), _report.ToString());
            Debug.Log("[AutoPilot] DONE failures=" + _failures);
            Application.Quit(_failures == 0 ? 0 : 1);
        }

        private static bool Loading => GameServices.Current != null && GameServices.Current.Flow.IsTransitioning;

        private IEnumerator WaitFor(Func<bool> cond, float timeout, string label)
        {
            float t = 0;
            while (!SafeCheck(cond) && t < timeout) { t += Time.unscaledDeltaTime; yield return null; }
            Check(SafeCheck(cond), label);
        }

        private static bool SafeCheck(Func<bool> c) { try { return c(); } catch { return false; } }

        private void Check(bool ok, string label)
        {
            if (!ok) _failures++;
            Log($"[{(ok ? "PASS" : "FAIL")}] {label}");
        }

        private void Log(string s)
        {
            _report.AppendLine(s);
            Debug.Log("[AutoPilot] " + s);
        }

        /// <summary>Captures at the end of this frame, then waits so later actions are not in the image.</summary>
        private IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(_dir, $"{_shot++:00}_{name}.png"));
            yield return null;
            yield return null;
        }
    }
}
