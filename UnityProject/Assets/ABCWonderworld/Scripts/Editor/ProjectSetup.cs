using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ABCWonderworld.Content;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using E = ABCWonderworld.EditorTools.EditorAssetUtil;

namespace ABCWonderworld.EditorTools
{
    /// <summary>
    /// One-click (or batch) project generator: settings, art, audio, materials, characters, content, scenes.
    /// Safe to re-run; assets keep their GUIDs. Batch: Tools/setup.sh
    /// </summary>
    public static class ProjectSetup
    {
        public const string MenuRoot = "ABC Wonderworld/";
        /// <summary>DEVELOPMENT identifier (D-005). Replace once the publisher namespace is confirmed.</summary>
        public const string BundleId = "com.nocvault.abcwonderworld.dev";
        public const string ProductName = "ABC Wonderworld";

        [MenuItem(MenuRoot + "Setup Project (generate everything)", priority = 0)]
        public static void RunInteractive()
        {
            if (!EditorUtility.DisplayDialog(ProductName, "Regenerate settings, placeholder art, audio, content assets and scenes?\n\nGenerated scenes are rebuilt.", "Regenerate", "Cancel")) return;
            Run();
        }

        public static void RunBatch()
        {
            try
            {
                Run();
                List<ContentIssue> issues = ContentValidator.Validate(ContentCatalog.Load() ?? AssetDatabase.LoadAssetAtPath<ContentCatalog>(E.Root + "/Resources/ContentCatalog.asset"));
                ContentReport.Write(issues);
                int errors = issues.Count(i => i.Severity == IssueSeverity.Error);
                foreach (ContentIssue i in issues.Where(i => i.Severity == IssueSeverity.Error)) Debug.LogError("[Validate] " + i);
                Debug.Log($"[Setup] Validation: {errors} errors, {issues.Count(i => i.Severity == IssueSeverity.Warning)} warnings");
                if (errors > 0) EditorApplication.Exit(1);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        public static void Run()
        {
            Step("Folders & settings");
            EnsureFolders();
            ConfigurePlayerSettings();
            ConfigureRenderPipeline();
            Step("Pictures & UI sprites");
            ArtPipeline.ConfigurePictureImports();
            ArtPipeline.BuildPictures();
            var skin = ArtPipeline.BuildUiSkin();
            Step("Audio");
            AudioSynth.GenerateAll();
            Step("Materials & characters");
            CharacterPrefabBuilder.BuildMaterialLibrary();
            AssetDatabase.SaveAssets();
            Dictionary<char, GameObject> prefabs = CharacterPrefabBuilder.BuildAll();
            Step("Content");
            ContentBuilder.Build(prefabs);
            Step("Scenes");
            SceneBuilder.BuildAll();
            ConfigureSplashAndIcon(skin);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Setup] ABC Wonderworld generated successfully.");
        }

        private static void Step(string s) => Debug.Log("[Setup] " + s);

        private static void EnsureFolders()
        {
            foreach (string f in new[] { "Art/Characters", "Art/Environments", "Art/Props", "Art/UI", "Art/Vocabulary", "Audio/Music", "Audio/Voice/en",
                         "Audio/SFX", "Animations", "Materials", "Prefabs", "Scenes", "ScriptableObjects", "Settings", "UI", "Tests", "Resources" })
                E.EnsureFolder($"{E.Root}/{f}");
        }

        public static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "Nocvault";
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = "0.1.1";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, BundleId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, BundleId);
            PlayerSettings.Android.bundleVersionCode = Math.Max(2, PlayerSettings.Android.bundleVersionCode); // 0.1.1 = 2 (installs over 0.1.0)

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            if ((int)PlayerSettings.Android.minSdkVersion < 25) PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.renderOutsideSafeArea = false;
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.Android.forceSDCardPermission = false;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Minimal);
            ConfigureAndroidGraphics();
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.runInBackground = true;
            PlayerSettings.muteOtherAudioSources = false;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.resizableWindow = true;
            // Offline, no analytics/telemetry from the engine where configurable.
            PlayerSettings.enableCrashReportAPI = false;
            EditorUserBuildSettings.buildAppBundle = false;
        }

        public const string VulkanFilterPath = E.Root + "/Settings/VulkanDeviceFilterLists.asset";
        public const string PowerVrPattern = ".*PowerVR.*";

        /// <summary>
        /// Vulkan + GLES3, with PowerVR denied Vulkan (learned on Little Burger World: PowerVR GE8xxx drivers crash
        /// on the saved Vulkan pipeline cache from the second launch onwards).
        /// </summary>
        private static void ConfigureAndroidGraphics()
        {
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });
            E.EnsureFolder(Path.GetDirectoryName(VulkanFilterPath));
            var lists = AssetDatabase.LoadAssetAtPath<VulkanDeviceFilterLists>(VulkanFilterPath);
            if (lists == null)
            {
                lists = new VulkanDeviceFilterLists { name = "VulkanDeviceFilterLists" };
                AssetDatabase.CreateAsset(lists, VulkanFilterPath);
            }
            var so = new SerializedObject(lists);
            SerializedProperty deny = null;
            SerializedProperty it = so.GetIterator();
            while (it.Next(true))
            {
                if (deny == null && it.isArray && it.propertyType == SerializedPropertyType.Generic && it.name.IndexOf("deny", StringComparison.OrdinalIgnoreCase) >= 0)
                    deny = it.Copy();
            }
            if (deny == null) { Debug.LogWarning("[Setup] Vulkan deny list field not found"); return; }
            bool present = false;
            for (int i = 0; i < deny.arraySize && !present; i++)
            {
                SerializedProperty d = FindString(deny.GetArrayElementAtIndex(i), "deviceName");
                present = d != null && d.stringValue == PowerVrPattern;
            }
            if (!present)
            {
                deny.arraySize++;
                SerializedProperty dn = FindString(deny.GetArrayElementAtIndex(deny.arraySize - 1), "deviceName");
                if (dn != null) dn.stringValue = PowerVrPattern;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(lists);
            AssetDatabase.SaveAssets();
            PlayerSettings.Android.androidVulkanDeviceFilterListAsset = lists;
        }

        private static SerializedProperty FindString(SerializedProperty element, string nameContains)
        {
            SerializedProperty end = element.GetEndProperty();
            SerializedProperty p = element.Copy();
            if (!p.Next(true)) return null;
            while (!SerializedProperty.EqualContents(p, end))
            {
                if (p.propertyType == SerializedPropertyType.String && p.name.IndexOf(nameContains, StringComparison.OrdinalIgnoreCase) >= 0) return p.Copy();
                if (!p.Next(true)) break;
            }
            return null;
        }

        private static void ConfigureRenderPipeline()
        {
            var mobile = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            var pc = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            foreach (RenderPipelineAsset a in new[] { mobile, pc })
            {
                if (a == null) continue;
                var so = new SerializedObject(a);
                void SetI(string n, int v) { var p = so.FindProperty(n); if (p != null) p.intValue = v; }
                void SetF(string n, float v) { var p = so.FindProperty(n); if (p != null) p.floatValue = v; }
                void SetB(string n, bool v) { var p = so.FindProperty(n); if (p != null) p.boolValue = v; }
                SetB("m_MainLightShadowsSupported", true);
                SetB("m_SoftShadowsSupported", true);
                SetF("m_ShadowDistance", 30f);
                SetI("m_ShadowCascadeCount", 1);
                SetI("m_MainLightShadowmapResolution", a == mobile ? 1024 : 2048);
                SetI("m_MSAA", 4);
                if (a == mobile) { SetB("m_SupportsHDR", false); SetF("m_RenderScale", 1f); }
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(a);
            }
        }

        private static void ConfigureSplashAndIcon(UI.UiSkin skin)
        {
            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.backgroundColor = new Color(1f, 0.95f, 0.85f);
            PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.DarkOnLight;
            Texture2D icon = AppIcon.Build();
            if (icon != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        }
    }

    /// <summary>Placeholder app icon: a smiling chunky "A" on a sky-to-meadow gradient.</summary>
    public static class AppIcon
    {
        public const string Path = E.Root + "/Art/UI/Generated/app_icon.png";

        public static Texture2D Build()
        {
            var p = new Painter(512);
            Painter.Sdf bg = Painter.Box(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0.18f);
            p.Fill(bg, new Color(0.62f, 0.86f, 1f)).Fill(Painter.Intersect(bg, Painter.Circle(new Vector2(0.5f, -0.55f), 0.8f)), new Color(0.49f, 0.8f, 0.36f));
            Painter.Sdf a = Painter.Letter('A', new Rect(0.24f, 0.22f, 0.52f, 0.58f), 0.075f);
            p.Fill(a, new Color(1f, 0.36f, 0.37f), 0.02f, new Color(0.35f, 0.15f, 0.2f)).Shade(a, 0.8f, 0.2f, 0.25f);
            p.Fill(Painter.Circle(new Vector2(0.44f, 0.6f), 0.035f), Color.white).Fill(Painter.Circle(new Vector2(0.56f, 0.6f), 0.035f), Color.white);
            p.Fill(Painter.Circle(new Vector2(0.44f, 0.595f), 0.018f), new Color(0.15f, 0.1f, 0.2f)).Fill(Painter.Circle(new Vector2(0.56f, 0.595f), 0.018f), new Color(0.15f, 0.1f, 0.2f));
            p.Fill(Painter.Star(new Vector2(0.82f, 0.82f), 0.09f, 0.04f), new Color(1f, 0.85f, 0.3f));
            E.WriteSprite(Path, p.ToTexture(), 512);
            var imp = (TextureImporter)AssetImporter.GetAtPath(Path);
            imp.textureType = TextureImporterType.Default;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(Path);
        }
    }
}
