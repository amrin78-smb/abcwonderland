using System.IO;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

namespace ABCWonderworld.EditorTools
{
    /// <summary>
    /// Offline-first, child-directed app: removes the INTERNET (and network-state) permission that Unity adds by
    /// default, using tools:node="remove" so it cannot be re-merged from libraries. Verified with aapt after the build.
    /// </summary>
    public sealed class AndroidManifestPrivacy : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 100;
        private const string AndroidNs = "http://schemas.android.com/apk/res/android";
        private const string ToolsNs = "http://schemas.android.com/tools";
        private static readonly string[] Removed = { "android.permission.INTERNET", "android.permission.ACCESS_NETWORK_STATE" };

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            foreach (string manifest in new[] { Path.Combine(path, "src/main/AndroidManifest.xml"), Path.Combine(path, "../launcher/src/main/AndroidManifest.xml") })
            {
                if (!File.Exists(manifest)) continue;
                var doc = new XmlDocument();
                doc.Load(manifest);
                XmlElement root = doc.DocumentElement;
                if (root == null) continue;
                if (!root.HasAttribute("xmlns:tools")) root.SetAttribute("xmlns:tools", ToolsNs);
                foreach (string perm in Removed)
                {
                    XmlElement existing = null;
                    foreach (XmlNode n in root.SelectNodes("uses-permission"))
                        if (n is XmlElement e && e.GetAttribute("name", AndroidNs) == perm) existing = e;
                    if (existing == null)
                    {
                        existing = doc.CreateElement("uses-permission");
                        existing.SetAttribute("name", AndroidNs, perm);
                        root.PrependChild(existing);
                    }
                    existing.SetAttribute("node", ToolsNs, "remove");
                }
                doc.Save(manifest);
                Debug.Log("[Build] Removed network permissions in " + manifest);
            }
        }
    }
}
