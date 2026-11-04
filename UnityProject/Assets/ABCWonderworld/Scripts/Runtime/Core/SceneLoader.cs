using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ABCWonderworld.Core
{
    /// <summary>
    /// Asynchronous single-scene loading behind a friendly full-screen cover that also blocks input,
    /// so taps during a transition cannot trigger anything.
    /// </summary>
    public sealed class SceneLoader : MonoBehaviour
    {
        public bool IsLoading { get; private set; }
        public event Action<string> SceneReady;

        private CanvasGroup _cover;
        private RectTransform _spinner;
        private const float Fade = 0.25f;

        private void Awake() => BuildCover();

        public static bool SceneExists(string name) => Application.CanStreamedLevelBeLoaded(name);

        public void Load(string scene, Action<string> onFailed)
        {
            if (IsLoading) return;
            if (!SceneExists(scene))
            {
                onFailed?.Invoke("scene not in build");
                return;
            }
            StartCoroutine(LoadRoutine(scene, onFailed));
        }

        private IEnumerator LoadRoutine(string scene, Action<string> onFailed)
        {
            IsLoading = true;
            _cover.blocksRaycasts = true;
            _cover.gameObject.SetActive(true);
            for (float t = 0; t < Fade; t += Time.unscaledDeltaTime) { _cover.alpha = t / Fade; yield return null; }
            _cover.alpha = 1;
            GameServices.Current?.Audio?.StopVoice();
            AsyncOperation op = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
            if (op == null)
            {
                IsLoading = false;
                _cover.alpha = 0; _cover.blocksRaycasts = false; _cover.gameObject.SetActive(false);
                onFailed?.Invoke("LoadSceneAsync returned null");
                yield break;
            }
            while (!op.isDone) yield return null;
            yield return null; // let scene Start() run under the cover
            SceneReady?.Invoke(scene);
            for (float t = 0; t < Fade; t += Time.unscaledDeltaTime) { _cover.alpha = 1 - t / Fade; yield return null; }
            _cover.alpha = 0;
            _cover.blocksRaycasts = false;
            _cover.gameObject.SetActive(false);
            IsLoading = false;
        }

        private void Update()
        {
            if (IsLoading && _spinner != null) _spinner.Rotate(0, 0, -180f * Time.unscaledDeltaTime);
        }

        private void BuildCover()
        {
            var canvasGo = new GameObject("LoadingCover", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            _cover = canvasGo.GetComponent<CanvasGroup>();
            var bg = new GameObject("Bg", typeof(Image));
            bg.transform.SetParent(canvasGo.transform, false);
            var img = bg.GetComponent<Image>();
            img.color = new Color(1f, 0.93f, 0.8f, 1f);
            var rt = img.rectTransform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var spin = new GameObject("Spinner", typeof(Image));
            spin.transform.SetParent(canvasGo.transform, false);
            var sImg = spin.GetComponent<Image>();
            UI.UiSkin skin = UI.UiSkin.Load();
            sImg.sprite = skin != null ? skin.star : null;
            sImg.color = new Color(1f, 0.75f, 0.2f);
            _spinner = sImg.rectTransform;
            _spinner.sizeDelta = new Vector2(140, 140);
            _cover.alpha = 0;
            _cover.blocksRaycasts = false;
            canvasGo.SetActive(false);
        }
    }
}
