using System.IO;
using UnityEditor;
using UnityEngine;

namespace ABCWonderworld.EditorTools
{
    /// <summary>Safe, re-runnable asset helpers. Existing assets are updated in place so GUIDs survive.</summary>
    public static class EditorAssetUtil
    {
        public const string Root = "Assets/ABCWonderworld";

        public static void EnsureFolder(string path)
        {
            path = path.Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        public static T LoadOrCreateSO<T>(string path) where T : ScriptableObject
        {
            EnsureFolder(Path.GetDirectoryName(path));
            T so = AssetDatabase.LoadAssetAtPath<T>(path);
            if (so != null) return so;
            so = ScriptableObject.CreateInstance<T>();
            so.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(so, path);
            return so;
        }

        public static T CreateOrReplace<T>(T asset, string path) where T : Object
        {
            EnsureFolder(Path.GetDirectoryName(path));
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(asset, path);
                return asset;
            }
            if (existing is Mesh em && asset is Mesh nm) em.Clear();
            EditorUtility.CopySerialized(asset, existing);
            existing.name = Path.GetFileNameWithoutExtension(path);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        public static GameObject SavePrefab(GameObject go, string path)
        {
            EnsureFolder(Path.GetDirectoryName(path));
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path, out bool ok);
            if (!ok) Debug.LogError("[Setup] Failed to save prefab " + path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;

        /// <summary>Writes a PNG (only if changed) and configures it as a sprite.</summary>
        public static Sprite WriteSprite(string path, Texture2D tex, int maxSize, Vector4 border = default, bool mipmaps = false)
        {
            EnsureFolder(Path.GetDirectoryName(path));
            byte[] png = tex.EncodeToPNG();
            string full = Path.GetFullPath(path);
            if (!File.Exists(full) || !BytesEqual(File.ReadAllBytes(full), png)) File.WriteAllBytes(full, png);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            ConfigureSprite(path, maxSize, border, mipmaps);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        public static void ConfigureSprite(string path, int maxSize, Vector4 border = default, bool mipmaps = false)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return;
            bool dirty = imp.textureType != TextureImporterType.Sprite || imp.maxTextureSize != maxSize ||
                         imp.spriteBorder != border || imp.mipmapEnabled != mipmaps || imp.alphaIsTransparency != true ||
                         imp.spriteImportMode != SpriteImportMode.Single;
            if (!dirty) return;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = mipmaps;
            imp.maxTextureSize = maxSize;
            imp.spriteBorder = border;
            imp.textureCompression = TextureImporterCompression.Compressed;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.filterMode = FilterMode.Bilinear;
            imp.SaveAndReimport();
        }

        private static bool BytesEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }
    }
}
