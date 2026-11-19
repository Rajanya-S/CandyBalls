using UnityEngine;

namespace CandyBalls
{
    /// <summary>
    /// Zero-setup entry point. Runs automatically after any scene loads and builds the game if one
    /// is not already present, so you can press Play in an empty scene and the game appears.
    ///
    /// If you would rather wire things up by hand, drop a GameManager into your scene and this
    /// bootstrap will step aside.
    /// </summary>
    public static class GameBootstrap
    {
        /// <summary>Set to false before the first scene loads to stop the automatic setup.</summary>
        public static bool AutoStart = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            if (!AutoStart) return;
            if (FindExistingGame() != null) return;

            Application.targetFrameRate = 60;

            var go = new GameObject("Candy Balls");
            go.AddComponent<GameManager>();
        }

        static GameManager FindExistingGame()
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<GameManager>();
#else
            return Object.FindObjectOfType<GameManager>();
#endif
        }
    }
}
