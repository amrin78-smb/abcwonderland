using System.Collections.Generic;
using System.IO;
using ABCWonderworld.Characters;
using ABCWonderworld.Content;
using UnityEditor;
using UnityEngine;
using E = ABCWonderworld.EditorTools.EditorAssetUtil;

namespace ABCWonderworld.EditorTools
{
    /// <summary>Bakes the 26 placeholder character prefabs and the shared material library.</summary>
    public static class CharacterPrefabBuilder
    {
        public const string MaterialDir = E.Root + "/Materials";
        public const string MeshDir = E.Root + "/Art/Characters/Meshes";
        public const string PrefabDir = E.Root + "/Prefabs/Characters";

        [System.Serializable] private class Root { public LetterDto[] letters; }
        [System.Serializable] private class LetterDto { public string upper; public CharacterDto character; public VocabDto[] vocabulary; }
        [System.Serializable] private class CharacterDto { public string color, accent, theme; }
        [System.Serializable] private class VocabDto { public string word, emoji; }

        public static MaterialLibrary BuildMaterialLibrary()
        {
            var lib = E.LoadOrCreateSO<MaterialLibrary>(E.Root + "/Resources/MaterialLibrary.asset");
            lib.baseLit = MakeMaterial("Base_Matte", "Universal Render Pipeline/Lit", Color.white, 0.22f);
            lib.glossy = MakeMaterial("Base_Glossy", "Universal Render Pipeline/Lit", Color.white, 0.62f);
            lib.unlit = MakeMaterial("Base_Unlit", "Universal Render Pipeline/Unlit", Color.white, 0f);
            EditorUtility.SetDirty(lib);
            return lib;
        }

        private static Material MakeMaterial(string name, string shader, Color c, float smooth)
        {
            string path = $"{MaterialDir}/{name}.mat";
            E.EnsureFolder(MaterialDir);
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find(shader));
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = Shader.Find(shader);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (m.HasProperty("_SpecularHighlights")) m.SetFloat("_SpecularHighlights", 1f);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Material asset per (colour, kind), so prefabs reference saved assets.</summary>
        public static Material TintAsset(Color c, int kind)
        {
            string hex = ColorUtility.ToHtmlStringRGB(c);
            string path = $"{MaterialDir}/Tints/Tint_{hex}_{kind}.mat";
            E.EnsureFolder($"{MaterialDir}/Tints");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            MaterialLibrary lib = MaterialLibrary.Instance;
            Material src = kind == 1 ? lib.unlit : kind == 2 ? lib.glossy : lib.baseLit;
            if (m == null)
            {
                m = new Material(src);
                AssetDatabase.CreateAsset(m, path);
            }
            m.CopyPropertiesFromMaterial(src);
            m.shader = src.shader;
            m.SetColor("_BaseColor", c);
            EditorUtility.SetDirty(m);
            return m;
        }

        public static Dictionary<char, GameObject> BuildAll()
        {
            var root = JsonUtility.FromJson<Root>(File.ReadAllText(ContentBuilder.SourcePath));
            var prefabs = new Dictionary<char, GameObject>();
            E.EnsureFolder(MeshDir);
            var shared = new CharacterFactory.Parts
            {
                sphere = E.CreateOrReplace(GlyphMeshBuilder.Sphere(0.5f, 18), $"{MeshDir}/Sphere.asset"),
                smile = E.CreateOrReplace(CharacterFactory.BuildSmile(), $"{MeshDir}/Smile.asset")
            };
            foreach (LetterDto l in root.letters)
            {
                char up = l.upper[0];
                Mesh body = E.CreateOrReplace(GlyphMeshBuilder.Build(LetterStrokeLibrary.Get(up), CharacterFactory.BodyRadius, CharacterFactory.BodyDepth, 12),
                    $"{MeshDir}/Body_{up}.asset");
                var parts = new CharacterFactory.Parts { body = body, sphere = shared.sphere, smile = shared.smile };
                string badgeKey = l.vocabulary.Length > 0 ? l.vocabulary[0].emoji : null;
                foreach (VocabDto v in l.vocabulary)
                    if (l.character.theme.ToLowerInvariant().Contains(v.word)) { badgeKey = v.emoji; break; }
                Sprite badge = ContentBuilder.LoadPicture(badgeKey);
                GameObject go = CharacterFactory.Build(up, E.Hex(l.character.color), E.Hex(l.character.accent), badge, TintAsset, parts);
                go.name = "Character_" + up;
                prefabs[up] = E.SavePrefab(go, $"{PrefabDir}/Character_{up}.prefab");
            }
            return prefabs;
        }
    }
}
