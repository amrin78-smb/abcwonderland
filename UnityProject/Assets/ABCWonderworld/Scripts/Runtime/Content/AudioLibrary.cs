using System;
using System.Collections.Generic;
using ABCWonderworld.Content;
using UnityEngine;


namespace ABCWonderworld.Content
{
    /// <summary>Maps stable audio keys to clips. A missing clip is a tracked placeholder, never a crash.</summary>
    [CreateAssetMenu(menuName = "ABC Wonderworld/Audio Library", fileName = "AudioLibrary")]
    public sealed class AudioLibrary : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string key;
            [TextArea] public string text;
            public AudioClip clip;
            public ContentStatus status = ContentStatus.Missing;
        }

        public List<Entry> entries = new List<Entry>();
        private Dictionary<string, Entry> _map;

        public Entry Find(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_map == null || _map.Count != entries.Count)
            {
                _map = new Dictionary<string, Entry>();
                foreach (Entry e in entries) if (e != null && !string.IsNullOrEmpty(e.key)) _map[e.key] = e;
            }
            return _map.TryGetValue(key, out Entry found) ? found : null;
        }

        public AudioClip Clip(string key) => Find(key)?.clip;
        public string Text(string key) => Find(key)?.text;
        private void OnEnable() => _map = null;
    }
}
