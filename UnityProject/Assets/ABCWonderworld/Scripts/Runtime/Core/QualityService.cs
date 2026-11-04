using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ABCWonderworld.Core
{
    public enum GraphicsQuality { Auto = 0, Low = 1, Medium = 2, High = 3 }

    /// <summary>
    /// Low/Medium/High presets (master prompt §17). Auto picks Low on budget hardware (≤3.5 GB RAM, PowerVR,
    /// ≤4 cores, e.g. Redmi A-series), High on ≥8 GB devices (e.g. Galaxy S25 Ultra), Medium otherwise.
    /// Low stays clear and playable: no realtime shadows, no MSAA, 0.8 render scale, 30 fps cap.
    /// The URP asset is only modified in player builds (Editor assets stay untouched).
    /// </summary>
    public static class QualityService
    {
        public static GraphicsQuality Effective { get; private set; } = GraphicsQuality.High;
        public static bool Shadows => Effective != GraphicsQuality.Low;

        public static GraphicsQuality Detect()
        {
            int mem = SystemInfo.systemMemorySize;
            string gpu = (SystemInfo.graphicsDeviceName ?? "").ToLowerInvariant();
            if (mem > 0 && mem <= 3584 || gpu.Contains("powervr") || SystemInfo.processorCount <= 4) return GraphicsQuality.Low;
            if (mem >= 7800) return GraphicsQuality.High;
            return GraphicsQuality.Medium;
        }

        public static void Apply(GraphicsQuality requested)
        {
            Effective = requested == GraphicsQuality.Auto ? Detect() : requested;
            Application.targetFrameRate = Effective == GraphicsQuality.Low ? 30 : 60;
            QualitySettings.vSyncCount = 0;
            if (!Application.isEditor && GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
            {
                urp.msaaSampleCount = Effective == GraphicsQuality.Low ? 1 : Effective == GraphicsQuality.Medium ? 2 : 4;
                urp.renderScale = Effective == GraphicsQuality.Low ? 0.8f : 1f;
                urp.shadowDistance = Effective == GraphicsQuality.Low ? 0f : Effective == GraphicsQuality.Medium ? 20f : 30f;
            }
            foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) l.shadows = Shadows ? (Effective == GraphicsQuality.High ? LightShadows.Soft : LightShadows.Hard) : LightShadows.None;
            Debug.Log($"[Quality] {Effective} (requested {requested}; RAM {SystemInfo.systemMemorySize} MB, GPU {SystemInfo.graphicsDeviceName}, cores {SystemInfo.processorCount})");
        }
    }
}
