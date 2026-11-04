using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ABCWonderworld.EditorTools
{
    /// <summary>Player builds: Android APK (Builds/Android) and a Linux player for automated play-tests.</summary>
    public static class BuildScript
    {
        public const string AndroidPath = "../Builds/Android/ABCWonderworld-dev.apk";
        public const string LinuxPath = "../Builds/Linux/ABCWonderworld.x86_64";

        private static string[] Scenes => EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

        [MenuItem(ProjectSetup.MenuRoot + "Build Android APK (dev)", priority = 30)]
        public static BuildReport BuildAndroid()
        {
            ProjectSetup.ConfigurePlayerSettings();
            EditorUserBuildSettings.buildAppBundle = false;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(AndroidPath)));
            BuildReport r = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = Scenes, locationPathName = AndroidPath, target = BuildTarget.Android, targetGroup = BuildTargetGroup.Android, options = BuildOptions.None
            });
            Log(r);
            return r;
        }

        public static void BuildAndroidBatch()
        {
            if (BuildAndroid().summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        [MenuItem(ProjectSetup.MenuRoot + "Build Linux Test Player", priority = 31)]
        public static BuildReport BuildLinux()
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(LinuxPath)));
            BuildReport r = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = Scenes, locationPathName = LinuxPath, target = BuildTarget.StandaloneLinux64, targetGroup = BuildTargetGroup.Standalone, options = BuildOptions.None
            });
            Log(r);
            return r;
        }

        public static void BuildLinuxBatch()
        {
            if (BuildLinux().summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        private static void Log(BuildReport report)
        {
            BuildSummary s = report.summary;
            Debug.Log($"[Build] {s.platform} result={s.result} size={s.totalSize / (1024f * 1024f):0.0} MB time={s.totalTime.TotalSeconds:0}s " +
                      $"errors={s.totalErrors} warnings={s.totalWarnings} output={s.outputPath}");
            foreach (BuildStep step in report.steps)
                foreach (BuildStepMessage m in step.messages)
                    if (m.type == LogType.Error || m.type == LogType.Exception) Debug.LogError("[Build] " + m.content);
        }
    }
}
