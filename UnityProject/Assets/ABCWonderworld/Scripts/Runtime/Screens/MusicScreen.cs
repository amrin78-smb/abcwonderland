using System.Collections.Generic;
using ABCWonderworld.Audio;
using ABCWonderworld.Characters;
using ABCWonderworld.Content;
using ABCWonderworld.UI;
using ABCWonderworld.World;
using UnityEngine;
using UnityEngine.UI;

namespace ABCWonderworld.Screens
{
    /// <summary>
    /// GDD §6.6 Music and Rhymes. Top: ABC sing-along with every letter highlighted in sync (timing from
    /// <see cref="AbcSong"/>), pause/play and replay. Bottom: a free-play music area (8 bells, drum) with
    /// characters who dance whenever music is made. Uses the parent's music and voice volumes independently.
    /// </summary>
    public sealed class MusicScreen : ScreenBase
    {
        private readonly List<GlyphGraphic> _letters = new List<GlyphGraphic>();
        private readonly List<LetterCharacter> _dancers = new List<LetterCharacter>();
        private AudioSource _accompaniment;
        private readonly List<AudioSource> _singers = new List<AudioSource>();
        private double _startDsp = -1;
        private double _pausedAt = -1;
        private int _lastNote = -2;
        private BigButton _playPause;
        private Image _playIcon;
        public bool IsPlaying => _accompaniment != null && _accompaniment.isPlaying;
        public bool SongFinished { get; private set; }
        public int HighlightedLetters { get; private set; }

        protected override void Build()
        {
            S.Audio.PlayMusic(null); // the song replaces the background music here
            WorldDefinition w = S.Catalog.worlds.Count > 2 ? S.Catalog.worlds[2] : null;
            EnvironmentBuilder.SetupCameraAndLight(w, new Vector3(0, 2.8f, -10f), new Vector3(0, 1.6f, 2f), 40f);
            EnvironmentBuilder.Build(w, false, 77);
            char[] cast = { 'M', 'U', 'S', 'I', 'C' };
            for (int i = 0; i < cast.Length; i++)
            {
                CharacterDefinition cd = S.Catalog.CharacterFor(S.Catalog.Letter(cast[i]));
                if (cd?.prefab == null) continue;
                GameObject go = Instantiate(cd.prefab);
                go.transform.position = new Vector3((i - 2) * 2.4f, 0, 2.5f);
                go.transform.localScale = Vector3.one * 1.5f;
                var lc = go.GetComponent<LetterCharacter>();
                Cosmetics.Apply(lc, S);
                lc.Tapped += c => c.Play(CharacterState.Dance);
                _dancers.Add(lc);
            }

            UiSkin skin = UiSkin.Load();
            RectTransform ui = UiFactory.Canvas("MusicCanvas", 10);
            BigButton home = UiFactory.IconButton("Home", ui, skin.iconHome, skin.secondary, UiFactory.CornerButton, () => S.Flow.GoHome(), true);
            UiFactory.Place((RectTransform)home.transform, new Vector2(0, 1), new Vector2(110, -110), Vector2.one * UiFactory.CornerButton);

            // Letter strip: 2 rows of 13.
            Image strip = UiFactory.Image("Strip", ui, skin.rounded, new Color(1, 1, 1, 0.88f));
            UiFactory.Place(strip.rectTransform, new Vector2(0.5f, 1), new Vector2(60, -190), new Vector2(1480, 290));
            for (int i = 0; i < 26; i++)
            {
                LetterDefinition l = S.Catalog.letters[i];
                Color col = S.Catalog.CharacterFor(l)?.primaryColor ?? skin.primary;
                GlyphGraphic g = UiFactory.Glyph("L_" + l.upper, strip.transform, l.upper, UiFactory.Lighten(col, 0.55f), 0.2f);
                UiFactory.Place(g.rectTransform, new Vector2(0.5f, 0.5f), new Vector2((i % 13 - 6) * 108, i < 13 ? 65 : -65), new Vector2(95, 115));
                _letters.Add(g);
            }

            _playPause = UiFactory.IconButton("PlayPause", ui, skin.iconPlay, skin.success, 200, TogglePlay);
            UiFactory.Place((RectTransform)_playPause.transform, new Vector2(1, 1), new Vector2(-150, -190), new Vector2(200, 200));
            _playIcon = _playPause.transform.Find("Icon").GetComponent<Image>();
            _playPause.gameObject.AddComponent<Attention>();
            BigButton replay = UiFactory.IconButton("Replay", ui, skin.iconRefresh, skin.gentle, 140, Restart);
            UiFactory.Place((RectTransform)replay.transform, new Vector2(1, 1), new Vector2(-150, -390), new Vector2(140, 140));

            // Free-play instruments.
            Color[] bars = { new Color(0.95f, 0.3f, 0.3f), new Color(1f, 0.6f, 0.2f), new Color(1f, 0.85f, 0.25f), new Color(0.4f, 0.8f, 0.4f),
                new Color(0.3f, 0.75f, 0.85f), new Color(0.3f, 0.55f, 0.95f), new Color(0.6f, 0.45f, 0.9f), new Color(0.9f, 0.45f, 0.75f) };
            for (int i = 0; i < 8; i++)
            {
                int n = i;
                Image bar = UiFactory.Image("Bell_" + i, ui, skin.rounded, bars[i], true);
                UiFactory.Place(bar.rectTransform, new Vector2(0.5f, 0), new Vector2(-560 + i * 125, 150), new Vector2(105, 250 - i * 14));
                bar.raycastPadding = new Vector4(-10, -20, -10, -20);
                var b = bar.gameObject.AddComponent<BigButton>();
                b.sfx = null;
                b.cooldown = 0.06f;
                b.onClick.AddListener(() => { S.Audio.PlaySfx("note_" + n); Dance(); });
            }
            BigButton drum = UiFactory.IconButton("Drum", ui, null, new Color(1f, 0.95f, 0.85f), 220, () => { S.Audio.PlaySfx("drum"); Dance(); });
            UiFactory.Place((RectTransform)drum.transform, new Vector2(0.5f, 0), new Vector2(560, 160), new Vector2(220, 220));
            Image drumPic = UiFactory.Image("Pic", drum.transform, UiSkin.Picture("drum"), Color.white);
            UiFactory.Stretch(drumPic.rectTransform, 30);
            drum.sfx = null;

            _accompaniment = gameObject.AddComponent<AudioSource>();
            _accompaniment.playOnAwake = false;
            _accompaniment.clip = AbcSong.BuildAccompaniment();
            for (int i = 0; i < 3; i++) { var a = gameObject.AddComponent<AudioSource>(); a.playOnAwake = false; _singers.Add(a); }
            S.Audio.Speak("vo.music_intro");
        }

        private void Dance()
        {
            if (_dancers.Count == 0) return;
            _dancers[Random.Range(0, _dancers.Count)].Play(CharacterState.Dance);
        }

        public void TogglePlay()
        {
            if (IsPlaying) Pause();
            else if (_pausedAt >= 0) Resume();
            else Restart();
        }

        public void Restart()
        {
            S.Audio.StopVoice();
            foreach (AudioSource a in _singers) a.Stop();
            _accompaniment.Stop();
            SongFinished = false;
            HighlightedLetters = 0;
            _lastNote = -2;
            _pausedAt = -1;
            ApplyVolumes();
            _startDsp = AudioSettings.dspTime + 0.15;
            _accompaniment.PlayScheduled(_startDsp);
            ScheduleLetters(0);
            SetIcon(true);
        }

        private void Pause()
        {
            _pausedAt = AudioSettings.dspTime - _startDsp;
            _accompaniment.Pause();
            foreach (AudioSource a in _singers) a.Stop();
            SetIcon(false);
        }

        private void Resume()
        {
            ApplyVolumes();
            _startDsp = AudioSettings.dspTime + 0.1 - _pausedAt;
            _accompaniment.time = (float)_pausedAt;
            _accompaniment.PlayScheduled(AudioSettings.dspTime + 0.1);
            ScheduleLetters(_pausedAt);
            _pausedAt = -1;
            SetIcon(true);
        }

        private void ApplyVolumes()
        {
            _accompaniment.volume = S.Settings.MusicEnabled ? S.Settings.MusicVolume : 0f;
            foreach (AudioSource a in _singers) a.volume = S.Settings.VoiceVolume;
        }

        /// <summary>Letter names are scheduled sample-accurately (rolling window in Update) from this point.</summary>
        private void ScheduleLetters(double fromSeconds) => _scheduledUpTo = -1;

        private int _scheduledUpTo = -1;
        private int _nextSinger;

        private void Update()
        {
            if (_startDsp < 0 || _accompaniment == null) return;
            if (!IsPlaying && _pausedAt < 0 && !SongFinished && AudioSettings.dspTime > _startDsp + 0.5)
            {
                SongFinished = true;
                foreach (LetterCharacter d in _dancers) d.Play(CharacterState.Celebrate);
                S.Audio.Speak("vo.hooray");
                S.Rewards.AwardStars(1);
                SetIcon(false);
                return;
            }
            if (!IsPlaying) return;
            double now = AudioSettings.dspTime - _startDsp;
            // Rolling schedule: keep the next ~1.2 s of letter names queued on the 3 singer sources.
            for (int i = _scheduledUpTo + 1; i < AbcSong.Notes.Count; i++)
            {
                AbcSong.Note n = AbcSong.Notes[i];
                double t = n.Start * AbcSong.SecondsPerBeat;
                if (t > now + 1.2) break;
                _scheduledUpTo = i;
                if (n.Letter == '\0' || t < now) continue;
                LetterDefinition l = S.Catalog.Letter(n.Letter);
                AudioClip clip = l != null ? S.Catalog.audio?.Clip(l.nameAudioKey) : null;
                if (clip == null) continue;
                AudioSource src = _singers[_nextSinger];
                _nextSinger = (_nextSinger + 1) % _singers.Count;
                src.clip = clip;
                src.PlayScheduled(_startDsp + t);
            }
            int note = AbcSong.NoteAt(now);
            if (note != _lastNote)
            {
                _lastNote = note;
                for (int i = 0; i < _letters.Count; i++) _letters[i].transform.localScale = Vector3.one;
                if (note >= 0)
                {
                    char c = AbcSong.Notes[note].Letter;
                    if (c != '\0')
                    {
                        int idx = c - 'A';
                        LetterDefinition l = S.Catalog.letters[idx];
                        _letters[idx].color = S.Catalog.CharacterFor(l)?.primaryColor ?? Color.red;
                        _letters[idx].transform.localScale = Vector3.one * 1.35f;
                        HighlightedLetters = Mathf.Max(HighlightedLetters, idx + 1);
                        S.Progress.RecordExposure(l.id, ActivityType.Music);
                        if (idx % 4 == 0) Dance();
                    }
                    else Dance();
                }
            }
        }

        private void SetIcon(bool playing)
        {
            UiSkin skin = UiSkin.Load();
            if (_playIcon != null) _playIcon.sprite = playing ? skin.iconPause : skin.iconPlay;
        }

        private void OnDestroy()
        {
            if (GameServices() is { } s) s.Progress.RecordActivityFinished(ActivityType.Music, SongFinished, Time.timeSinceLevelLoad);
        }

        private static Core.GameServices GameServices() => Core.GameServices.Current;

        /// <summary>AutoPilot/tests: shorten the wait by jumping close to the end after a few letters.</summary>
        public void SkipNearEnd()
        {
            if (!IsPlaying) return;
            double target = AbcSong.LengthSeconds - 2.0;
            _accompaniment.time = (float)target;
            _startDsp = AudioSettings.dspTime - target;
            _scheduledUpTo = AbcSong.Notes.Count - 1;
        }
    }
}
