using System.Collections.Generic;
using System.Linq;
using ABCWonderworld.Content;
using ABCWonderworld.Core;
using ABCWonderworld.Screens;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using E = ABCWonderworld.EditorTools.EditorAssetUtil;

namespace ABCWonderworld.EditorTools
{
    /// <summary>
    /// Generates the game scenes. Each scene holds a camera, light, EventSystem and its screen controller;
    /// controllers build dynamic, data-driven content at runtime.
    /// </summary>
    public static class SceneBuilder
    {
        public const string Dir = E.Root + "/Scenes";

        public static void BuildAll()
        {
            E.EnsureFolder(Dir);
            var scenes = new List<string>
            {
                Make(SceneNames.Boot, go => go.AddComponent<ApplicationBootstrapper>(), withCamera: true),
                Make(SceneNames.Home, go => go.AddComponent<HomeScreen>()),
                Make(SceneNames.WorldMap, go => go.AddComponent<WorldMapScreen>()),
                Make(SceneNames.Activity, go => go.AddComponent<ActivityScreen>()),
                Make(SceneNames.AlphabetBook, go => go.AddComponent<AlphabetBookScreen>()),
                Make(SceneNames.Music, go => go.AddComponent<MusicScreen>()),
            };
            ContentCatalog catalog = AssetDatabase.LoadAssetAtPath<ContentCatalog>(E.Root + "/Resources/ContentCatalog.asset");
            foreach (WorldDefinition w in catalog.worlds)
            {
                scenes.Add(Make(w.sceneName, go =>
                {
                    var ws = go.AddComponent<WorldScreen>();
                    var so = new SerializedObject(ws);
                    so.FindProperty("worldId").stringValue = w.id;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }));
            }
            EditorBuildSettings.scenes = scenes.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
        }

        private static string Make(string name, System.Action<GameObject> addController, bool withCamera = true)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (withCamera)
            {
                var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cam.tag = "MainCamera";
                var c = cam.GetComponent<Camera>();
                c.clearFlags = CameraClearFlags.SolidColor;
                c.backgroundColor = new Color(1f, 0.95f, 0.85f);
                cam.transform.position = new Vector3(0, 2.5f, -10);
                var sun = new GameObject("Sun", typeof(Light));
                var l = sun.GetComponent<Light>();
                l.type = LightType.Directional;
                l.shadows = LightShadows.Soft;
                sun.transform.rotation = Quaternion.Euler(48, -32, 0);
            }
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var controller = new GameObject(name + "Controller");
            addController(controller);
            string path = $"{Dir}/{name}.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }
    }
}
