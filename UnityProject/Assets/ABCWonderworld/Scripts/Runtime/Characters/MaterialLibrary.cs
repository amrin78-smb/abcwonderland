using System.Collections.Generic;
using UnityEngine;

namespace ABCWonderworld.Characters
{
    /// <summary>
    /// Provides tinted copies of the project's toon-ish URP Lit base material. The base asset lives in
    /// Resources so the shader is always included in player builds.
    /// </summary>
    [CreateAssetMenu(menuName = "ABC Wonderworld/Material Library", fileName = "MaterialLibrary")]
    public sealed class MaterialLibrary : ScriptableObject
    {
        public Material baseLit;
        public Material unlit;
        public Material glossy;
        private static readonly Dictionary<(Color32, int), Material> Cache = new Dictionary<(Color32, int), Material>();
        private static MaterialLibrary _instance;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        public static MaterialLibrary Instance => _instance != null ? _instance : (_instance = Resources.Load<MaterialLibrary>("MaterialLibrary"));

        /// <summary>kind: 0 matte, 1 unlit, 2 glossy.</summary>
        public static Material Get(Color c, int kind = 0)
        {
            var key = ((Color32)c, kind);
            if (Cache.TryGetValue(key, out Material m) && m != null) return m;
            MaterialLibrary lib = Instance;
            Material src = lib == null ? null : kind == 1 ? lib.unlit : kind == 2 ? lib.glossy : lib.baseLit;
            if (src == null)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                src = new Material(sh);
            }
            m = new Material(src) { name = $"Tint_{ColorUtility.ToHtmlStringRGB(c)}_{kind}" };
            if (m.HasProperty(BaseColor)) m.SetColor(BaseColor, c);
            m.color = c;
            Cache[key] = m;
            return m;
        }
    }
}
