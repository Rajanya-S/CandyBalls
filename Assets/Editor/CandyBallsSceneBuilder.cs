using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CandyBalls.EditorTools
{
    /// <summary>
    /// Authors the game as a real Unity scene, with every object visible in the hierarchy and all
    /// references wired up in the inspector.
    ///
    /// Run it from the menu (<b>Candy Balls ▸ Rebuild Game Scene</b>) or headlessly with
    /// <c>-executeMethod CandyBalls.EditorTools.CandyBallsSceneBuilder.Build</c>.
    /// </summary>
    public static class CandyBallsSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Game.unity";
        const string SceneFolder = "Assets/Scenes";

        [MenuItem("Candy Balls/Rebuild Game Scene")]
        public static void BuildFromMenu()
        {
            Build();
            EditorUtility.DisplayDialog(
                "Candy Balls",
                "Game scene rebuilt at " + ScenePath,
                "Nice");
        }

        /// <summary>Creates Assets/Scenes/Game.unity from scratch and adds it to the build list.</summary>
        public static void Build()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---------------------------------------------------------------- camera
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);

            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6.4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.078f, 0.071f, 0.153f);
            camera.nearClipPlane = -100f;
            camera.farClipPlane = 100f;
            cameraGo.AddComponent<AudioListener>();

            // ------------------------------------------------------------------ root
            var root = new GameObject("Candy Balls");
            var game = root.AddComponent<GameManager>();
            game.gameCamera = camera;

            // ------------------------------------------------------------ background
            var backgroundGo = new GameObject("Background");
            backgroundGo.transform.SetParent(root.transform, false);
            var background = backgroundGo.AddComponent<SpriteRenderer>();
            background.sortingOrder = -100;
            game.background = background;

            // ----------------------------------------------------------------- board
            var boardGo = new GameObject("Board");
            boardGo.transform.SetParent(root.transform, false);
            var board = boardGo.AddComponent<Board>();
            boardGo.AddComponent<BoardInput>();
            game.board = board;

            var ballsGo = new GameObject("Balls");
            ballsGo.transform.SetParent(boardGo.transform, false);
            board.ballsRoot = ballsGo.transform;

            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(boardGo.transform, false);
            var panel = panelGo.AddComponent<SpriteRenderer>();
            panel.sortingOrder = -70;
            game.boardPanel = panel;

            var tilesGo = new GameObject("Tiles");
            tilesGo.transform.SetParent(boardGo.transform, false);
            game.tilesRoot = tilesGo.transform;

            // ------------------------------------------------------------------- HUD
            var hud = Hud.Create(game, root.transform);
            game.hud = hud;

            // ------------------------------------------------------------------ save
            if (!Directory.Exists(SceneFolder))
            {
                Directory.CreateDirectory(SceneFolder);
                AssetDatabase.Refresh();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            RegisterInBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[CandyBalls] Game scene built at " + ScenePath);
        }

        static void RegisterInBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            int index = scenes.FindIndex(s => s.path == ScenePath);
            if (index >= 0)
            {
                scenes[index].enabled = true;
            }
            else
            {
                scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
