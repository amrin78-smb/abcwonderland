using System;
using System.Collections.Generic;
using System.Linq;
using ABCWonderworld.Content;
using ABCWonderworld.Core;
using ABCWonderworld.Learning;
using ABCWonderworld.Parent;
using ABCWonderworld.Persistence;
using ABCWonderworld.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ABCWonderworld.Screens
{
    /// <summary>
    /// GDD §10 Parent Zone. Entry is a small lock in the corner that must be HELD (2 s), followed by a
    /// reading challenge ("Tap seven, then two"). Inside: dashboard (cautious labels), settings, and a
    /// two-step progress reset. Adult-facing text is fine here; nothing child-facing depends on it.
    /// </summary>
    public sealed class ParentZone : MonoBehaviour
    {
        private GameServices _s;
        private RectTransform _ui;
        private RectTransform _overlay;
        private ParentGate _gate;
        private UiSkin _skin;
        private RectTransform _content;
        public bool IsOpen => _overlay != null;
        public ParentGate Gate => _gate;

        private static readonly Color Panel = new Color(0.97f, 0.97f, 1f);
        private static readonly Color Ink = new Color(0.22f, 0.2f, 0.32f);
        private static readonly Color Muted = new Color(0.45f, 0.43f, 0.55f);

        public static ParentZone CreateEntry(RectTransform ui, GameServices s)
        {
            UiSkin skin = UiSkin.Load();
            Image lockBtn = UiFactory.Image("ParentLock", ui, skin.circle, new Color(0.45f, 0.42f, 0.6f, 0.85f), true);
            UiFactory.Place(lockBtn.rectTransform, new Vector2(1, 1), new Vector2(-90, -90), new Vector2(110, 110));
            Image ring = UiFactory.Image("Ring", lockBtn.transform, skin.ring, Color.white);
            UiFactory.Stretch(ring.rectTransform, -8);
            ring.type = Image.Type.Filled; ring.fillMethod = Image.FillMethod.Radial360; ring.fillOrigin = 2; ring.fillAmount = 0;
            Image ic = UiFactory.Image("Icon", lockBtn.transform, skin.iconLock, Color.white);
            UiFactory.Stretch(ic.rectTransform, 24);
            var zone = lockBtn.gameObject.AddComponent<ParentZone>();
            zone._s = s;
            zone._ui = ui;
            zone._skin = skin;
            var hold = lockBtn.gameObject.AddComponent<HoldButton>();
            hold.holdSeconds = ParentGate.HoldSeconds;
            hold.fill = ring;
            hold.onHeld.AddListener(zone.OpenGate);
            // A quick tap tells the child (by voice) this is for grown-ups; nothing opens.
            lockBtn.gameObject.AddComponent<TapVoice>().key = "vo.grown_ups";
            return zone;
        }

        // ------------------------------------------------------------------ gate
        public void OpenGate()
        {
            if (_overlay != null) return;
            _gate = new ParentGate();
            _gate.UpdateHold(ParentGate.HoldSeconds);
            _overlay = Overlay("ParentGate");
            Image card = UiFactory.Image("Card", _overlay, _skin.rounded, Panel, true);
            UiFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100, 820));
            Text title = UiFactory.Label("Title", card.transform, "For grown-ups", 64, Ink);
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -80), new Vector2(1000, 90));
            Text prompt = UiFactory.Label("Prompt", card.transform, _gate.Prompt, 56, Muted);
            UiFactory.Place(prompt.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -170), new Vector2(1000, 80));
            RectTransform grid = UiFactory.Rect("Numbers", card.transform);
            UiFactory.Place(grid, new Vector2(0.5f, 0.42f), Vector2.zero, new Vector2(700, 480));
            BuildNumberGrid(grid, prompt);
            BigButton close = UiFactory.IconButton("Close", card.transform, _skin.iconBack, _skin.secondary, 110, Close);
            UiFactory.Place((RectTransform)close.transform, new Vector2(0, 1), new Vector2(80, -80), new Vector2(110, 110));
        }

        private void BuildNumberGrid(RectTransform grid, Text prompt)
        {
            foreach (Transform c in grid) Destroy(c.gameObject);
            for (int i = 0; i < _gate.ButtonOrder.Count; i++)
            {
                int n = _gate.ButtonOrder[i];
                BigButton b = UiFactory.PillButton("N" + n, grid, n.ToString(), null, new Color(0.55f, 0.5f, 0.85f), new Vector2(190, 130), () =>
                {
                    bool open = _gate.Press(n);
                    if (open) { OpenDashboard(); return; }
                    prompt.text = _gate.Prompt;
                    if (!GateOrderMatches(grid)) BuildNumberGrid(grid, prompt);
                }, 64);
                UiFactory.Place((RectTransform)b.transform, new Vector2(0.5f, 0.5f), new Vector2((i % 3 - 1) * 230, (1 - i / 3) * 160), new Vector2(190, 130));
            }
        }

        private bool GateOrderMatches(RectTransform grid)
        {
            for (int i = 0; i < grid.childCount && i < _gate.ButtonOrder.Count; i++)
                if (grid.GetChild(i).name != "N" + _gate.ButtonOrder[i]) return false;
            return true;
        }

        /// <summary>Tests/AutoPilot: answers the gate the way an adult would.</summary>
        public void SolveGateForTests()
        {
            if (_gate == null) OpenGate();
            _gate.Press(_gate.First);
            if (_gate.Press(_gate.Second)) OpenDashboard();
        }

        private RectTransform Overlay(string name)
        {
            RectTransform root = UiFactory.Rect(name, _ui);
            UiFactory.Stretch(root);
            root.SetAsLastSibling();
            Image dim = UiFactory.Image("Dim", root, null, new Color(0.15f, 0.12f, 0.25f, 0.55f), true);
            UiFactory.Stretch(dim.rectTransform);
            return root;
        }

        public void Close()
        {
            if (_overlay != null) Destroy(_overlay.gameObject);
            _overlay = null;
            _gate = null;
        }

        // ------------------------------------------------------------------ dashboard
        private void OpenDashboard()
        {
            if (_overlay != null) Destroy(_overlay.gameObject);
            _overlay = Overlay("ParentZone");
            Image card = UiFactory.Image("Card", _overlay, _skin.rounded, Panel, true);
            UiFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1780, 1000));
            Text title = UiFactory.Label("Title", card.transform, "Parent Zone", 60, Ink, TextAnchor.MiddleLeft);
            UiFactory.Place(title.rectTransform, new Vector2(0, 1), new Vector2(330, -70), new Vector2(500, 80));
            string[] tabs = { "Progress", "Settings", "Reset" };
            for (int i = 0; i < tabs.Length; i++)
            {
                int k = i;
                BigButton t = UiFactory.PillButton("Tab_" + tabs[i], card.transform, tabs[i], null, i == 2 ? new Color(0.85f, 0.45f, 0.45f) : new Color(0.45f, 0.55f, 0.9f), new Vector2(250, 100), () => ShowTab(k), 44);
                UiFactory.Place((RectTransform)t.transform, new Vector2(0, 1), new Vector2(760 + i * 280, -70), new Vector2(250, 100));
            }
            BigButton close = UiFactory.IconButton("Close", card.transform, _skin.iconCheck, _skin.success, 110, Close);
            UiFactory.Place((RectTransform)close.transform, new Vector2(1, 1), new Vector2(-80, -70), new Vector2(110, 110));
            _content = UiFactory.Rect("Content", card.transform);
            _content.anchorMin = new Vector2(0, 0); _content.anchorMax = new Vector2(1, 1);
            _content.offsetMin = new Vector2(50, 40); _content.offsetMax = new Vector2(-50, -150);
            ShowTab(0);
        }

        public void ShowTab(int i)
        {
            foreach (Transform c in _content) Destroy(c.gameObject);
            if (i == 0) Dashboard();
            else if (i == 1) Settings();
            else ResetPanel();
        }

        private void Dashboard()
        {
            ContentCatalog cat = _s.Catalog;
            ProgressService p = _s.Progress;
            var ids = cat.letters.Select(l => l.id).ToList();
            int intro = ids.Count(id => p.StateOf(id) >= LetterLearningState.Introduced);
            int practised = ids.Count(id => p.StateOf(id) >= LetterLearningState.Practised);
            int recog = ids.Count(id => p.StateOf(id) == LetterLearningState.ShowingRecognition);
            var all = _s.Save.letters;
            int upper = all.Sum(l => l.upperActivities), lower = all.Sum(l => l.lowerActivities);
            int traceA = all.Sum(l => l.tracingAttempts), traceC = all.Sum(l => l.tracingCompletionsUpper + l.tracingCompletionsLower);
            int vocab = all.Sum(l => l.vocabularyHeard.Count);
            long minutes = Math.Max(_s.Save.totalPlaySeconds, (long)_s.Session.ActiveSeconds) / 60;
            string fav = string.Join(", ", p.FavouriteModes().Select(ModeName));

            string summary =
                $"Letters introduced: {intro}/26    Practised: {practised}/26    Showing recognition: {recog}/26\n" +
                $"Uppercase activities: {upper}    Lowercase activities: {lower}\n" +
                $"Tracing: {traceA} attempts, {traceC} completed    Words heard: {vocab}\n" +
                $"Matching: {Count(ActivityType.Match)}   Adventures: {Count(ActivityType.Adventure)}   Find the Letter: {Count(ActivityType.FindLetter)}\n" +
                $"Approximate play time: {minutes} min    Favourite modes: {(string.IsNullOrEmpty(fav) ? "—" : fav)}";
            Text s = UiFactory.Label("Summary", _content, summary, 36, Ink, TextAnchor.UpperLeft);
            UiFactory.Place(s.rectTransform, new Vector2(0, 1), new Vector2(560, -130), new Vector2(1100, 260));

            Text legend = UiFactory.Label("Legend", _content,
                "Grey: not yet seen · Blue: introduced · Green: practised · Gold: showing recognition (repeated, unprompted correct choices across sessions).\n" +
                "These are activity records, not assessments of ability.", 26, Muted, TextAnchor.UpperLeft);
            UiFactory.Place(legend.rectTransform, new Vector2(0, 0), new Vector2(560, 70), new Vector2(1100, 110));

            RectTransform grid = UiFactory.Rect("Letters", _content);
            UiFactory.Place(grid, new Vector2(0.5f, 0.42f), new Vector2(0, -20), new Vector2(1600, 330));
            for (int i = 0; i < cat.letters.Count; i++)
            {
                LetterDefinition l = cat.letters[i];
                LetterLearningState st = p.StateOf(l.id);
                Color c = st switch
                {
                    LetterLearningState.ShowingRecognition => new Color(1f, 0.78f, 0.25f),
                    LetterLearningState.Practised => new Color(0.45f, 0.8f, 0.45f),
                    LetterLearningState.Introduced => new Color(0.5f, 0.7f, 1f),
                    _ => new Color(0.86f, 0.86f, 0.9f)
                };
                Image cell = UiFactory.Image("L_" + l.upper, grid, _skin.rounded, c);
                UiFactory.Place(cell.rectTransform, new Vector2(0.5f, 0.5f), new Vector2((i % 13 - 6) * 120, i < 13 ? 80 : -80), new Vector2(105, 140));
                GlyphGraphic g = UiFactory.Glyph("G", cell.transform, l.upper, Ink, 0.2f);
                UiFactory.Stretch(g.rectTransform, 18);
                g.rectTransform.offsetMin = new Vector2(18, 34);
                Text lab = UiFactory.Label("S", cell.transform, Short(st), 20, Ink);
                UiFactory.Place(lab.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(105, 30));
            }

            List<LearningEventData> recent = _s.Save.recentEvents.Where(e => e.kind != EvidenceKind.Exposure).Reverse().Take(4).ToList();
            string hist = recent.Count == 0 ? "No activity yet." : string.Join("\n", recent.Select(e =>
                $"{new DateTime(e.utcTicks, DateTimeKind.Utc).ToLocalTime():MMM d HH:mm}  {ModeName((ActivityType)e.activityType)}  {cat.Letter(e.letterId)?.upper}  {e.kind} {(e.outcome == EventOutcome.None ? "" : e.outcome.ToString().ToLowerInvariant())}"));
            Text h = UiFactory.Label("Recent", _content, "Recent activity\n" + hist, 26, Muted, TextAnchor.UpperLeft);
            UiFactory.Place(h.rectTransform, new Vector2(1, 1), new Vector2(-290, -130), new Vector2(560, 260));
        }

        private int Count(ActivityType t) => _s.Progress.CountFor(t).started;

        private static string Short(LetterLearningState s) => s switch
        {
            LetterLearningState.ShowingRecognition => "recognises",
            LetterLearningState.Practised => "practised",
            LetterLearningState.Introduced => "introduced",
            _ => ""
        };

        public static string ModeName(ActivityType t) => t switch
        {
            ActivityType.MeetLetter => "Meet the Letters",
            ActivityType.Tracing => "Tracing",
            ActivityType.FindLetter => "Find the Letter",
            ActivityType.Match => "Matching",
            ActivityType.Adventure => "Adventures",
            ActivityType.Puzzle => "Puzzles",
            ActivityType.Music => "Music",
            ActivityType.FreePlay => "Free Play",
            _ => t.ToString()
        };

        // ------------------------------------------------------------------ settings
        private void Settings()
        {
            SettingsService st = _s.Settings;
            float y = -40;
            Slider("Voice volume", st.VoiceVolume, v => st.VoiceVolume = v, ref y);
            Slider("Music volume", st.MusicVolume, v => st.MusicVolume = v, ref y);
            Slider("Sound effects", st.SfxVolume, v => { st.SfxVolume = v; _s.Audio.PlaySfx("tap"); }, ref y);
            Toggle("Music on", st.MusicEnabled, v => st.MusicEnabled = v, ref y);
            Toggle("Reduced motion", st.ReducedMotion, v => st.ReducedMotion = v, ref y);
            Toggle("Simplified interaction (fewer choices, wider tracing)", st.SimplifiedInteraction, v => st.SimplifiedInteraction = v, ref y);

            float y2 = -40;
            Choice("Learning profile", new[] { "Explorer (2–3)", "Discoverer (3–4)", "Little Learner (4–5)" }, (int)st.Profile, i =>
            {
                st.Profile = (LearningProfile)i;
                _s.Difficulty.ResetToProfile();
            }, ref y2, 820);
            Toggle("Adapt difficulty automatically", st.AdaptiveDifficulty, v => st.AdaptiveDifficulty = v, ref y2, 820);
            int[] mins = { 0, 10, 20, 30 };
            Choice("Gentle break reminder", new[] { "Off", "10 min", "20 min", "30 min" }, Math.Max(0, Array.IndexOf(mins, st.SessionReminderMinutes)), i => st.SessionReminderMinutes = mins[i], ref y2, 820);
            Choice("Graphics", new[] { "Auto", "Low", "Medium", "High" }, (int)st.Quality, i => st.Quality = (GraphicsQuality)i, ref y2, 820);
            Text note = UiFactory.Label("Privacy", _content,
                "ABC Wonderworld works fully offline. Progress is stored only on this device. No ads, no accounts, no data collection.", 26, Muted, TextAnchor.UpperLeft);
            UiFactory.Place(note.rectTransform, new Vector2(0, 1), new Vector2(1240, y2 - 20), new Vector2(800, 120));
        }

        private void Slider(string label, float value, Action<float> set, ref float y, float x = 0)
        {
            Text t = UiFactory.Label(label, _content, label, 34, Ink, TextAnchor.MiddleLeft);
            UiFactory.Place(t.rectTransform, new Vector2(0, 1), new Vector2(x + 260, y), new Vector2(500, 60));
            RectTransform track = UiFactory.Rect("Track", _content);
            UiFactory.Place(track, new Vector2(0, 1), new Vector2(x + 560, y - 60), new Vector2(560, 50));
            Image bg = UiFactory.Image("Bg", track, _skin.rounded, new Color(0.85f, 0.85f, 0.92f), true);
            UiFactory.Stretch(bg.rectTransform);
            Image fill = UiFactory.Image("Fill", track, _skin.rounded, new Color(0.45f, 0.55f, 0.9f));
            fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = new Vector2(value, 1);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            var drag = bg.gameObject.AddComponent<SliderDrag>();
            drag.onValue = v => { fill.rectTransform.anchorMax = new Vector2(v, 1); set(v); };
            y -= 140;
        }

        private void Toggle(string label, bool value, Action<bool> set, ref float y, float x = 0)
        {
            bool state = value;
            BigButton b = null;
            b = UiFactory.PillButton(label, _content, label + (state ? ": On" : ": Off"), null, state ? new Color(0.4f, 0.75f, 0.45f) : new Color(0.7f, 0.7f, 0.78f), new Vector2(760, 90), () =>
            {
                state = !state;
                set(state);
                b.GetComponent<Image>().color = state ? new Color(0.4f, 0.75f, 0.45f) : new Color(0.7f, 0.7f, 0.78f);
                b.GetComponentInChildren<Text>().text = label + (state ? ": On" : ": Off");
            }, 32);
            UiFactory.Place((RectTransform)b.transform, new Vector2(0, 1), new Vector2(x + 400, y - 20), new Vector2(760, 90));
            y -= 115;
        }

        private void Choice(string label, string[] options, int selected, Action<int> set, ref float y, float x = 0)
        {
            Text t = UiFactory.Label(label, _content, label, 34, Ink, TextAnchor.MiddleLeft);
            UiFactory.Place(t.rectTransform, new Vector2(0, 1), new Vector2(x + 400, y), new Vector2(760, 60));
            var buttons = new List<BigButton>();
            float w = 760f / options.Length - 12;
            for (int i = 0; i < options.Length; i++)
            {
                int k = i;
                BigButton b = UiFactory.PillButton(options[i], _content, options[i], null, i == selected ? new Color(0.45f, 0.55f, 0.9f) : new Color(0.7f, 0.7f, 0.78f), new Vector2(w, 80), () =>
                {
                    set(k);
                    for (int j = 0; j < buttons.Count; j++) buttons[j].GetComponent<Image>().color = j == k ? new Color(0.45f, 0.55f, 0.9f) : new Color(0.7f, 0.7f, 0.78f);
                }, 26);
                UiFactory.Place((RectTransform)b.transform, new Vector2(0, 1), new Vector2(x + 20 + w * 0.5f + i * (w + 12), y - 75), new Vector2(w, 80));
                buttons.Add(b);
            }
            y -= 175;
        }

        // ------------------------------------------------------------------ reset (two explicit confirmations)
        private void ResetPanel()
        {
            Text t = UiFactory.Label("Explain", _content,
                "Resetting erases ALL learning progress, activity history, Wonder Stars and stickers on this device.\n" +
                "Settings (volumes, profile, reminders) are kept. This cannot be undone.", 36, Ink, TextAnchor.UpperLeft);
            UiFactory.Place(t.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -120), new Vector2(1500, 200));
            BigButton first = null;
            first = UiFactory.PillButton("ResetStep1", _content, "Reset progress…", null, new Color(0.85f, 0.45f, 0.45f), new Vector2(520, 110), () =>
            {
                first.gameObject.SetActive(false);
                Text q = UiFactory.Label("Confirm", _content, "Are you sure? All progress will be permanently erased.", 38, new Color(0.7f, 0.2f, 0.2f));
                UiFactory.Place(q.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(1500, 80));
                BigButton yes = UiFactory.PillButton("ResetConfirm", _content, "Yes, erase progress", null, new Color(0.8f, 0.3f, 0.3f), new Vector2(560, 110), () =>
                {
                    _s.Progress.ResetAll();
                    FindAnyObjectByType<ApplicationBootstrapper>()?.SaveNow();
                    ShowTab(2);
                    Text done = UiFactory.Label("Done", _content, "Progress has been reset.", 38, new Color(0.2f, 0.55f, 0.3f));
                    UiFactory.Place(done.rectTransform, new Vector2(0.5f, 0.3f), Vector2.zero, new Vector2(1000, 80));
                }, 34);
                UiFactory.Place((RectTransform)yes.transform, new Vector2(0.5f, 0.3f), new Vector2(-320, 0), new Vector2(560, 110));
                BigButton no = UiFactory.PillButton("ResetCancel", _content, "No, keep progress", null, new Color(0.4f, 0.7f, 0.45f), new Vector2(560, 110), () => ShowTab(2), 34);
                UiFactory.Place((RectTransform)no.transform, new Vector2(0.5f, 0.3f), new Vector2(320, 0), new Vector2(560, 110));
            }, 36);
            UiFactory.Place((RectTransform)first.transform, new Vector2(0.5f, 0.45f), Vector2.zero, new Vector2(520, 110));
        }
    }

    /// <summary>Speaks a line on a quick tap (used where the real action needs a hold).</summary>
    public sealed class TapVoice : MonoBehaviour, IPointerClickHandler
    {
        public string key;
        public void OnPointerClick(PointerEventData e) => GameServices.Current?.Audio?.Speak(key, Audio.VoiceMode.IfIdle);
    }

    /// <summary>Large, simple horizontal slider (0..1) for parent settings.</summary>
    public sealed class SliderDrag : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        public Action<float> onValue;
        public void OnPointerDown(PointerEventData e) => Apply(e);
        public void OnDrag(PointerEventData e) => Apply(e);
        private void Apply(PointerEventData e)
        {
            var rt = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out Vector2 local);
            float v = Mathf.Clamp01((local.x - rt.rect.xMin) / Mathf.Max(1, rt.rect.width));
            onValue?.Invoke(v);
        }
    }
}
