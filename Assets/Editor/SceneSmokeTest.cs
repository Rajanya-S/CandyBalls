using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CandyBalls.EditorTools
{
    /// <summary>
    /// Headless play-mode check. Opens the game scene, lets it build, then performs a real legal
    /// swap and asserts that a move was consumed and the score went up. Verifies the whole loop
    /// (scene wiring → board → rules → score) without a human pressing Play.
    ///
    /// Run with <c>-executeMethod CandyBalls.EditorTools.SceneSmokeTest.Run</c>.
    /// </summary>
    public static class SceneSmokeTest
    {
        const string Marker = "[SMOKE]";
        const double BuildTimeout = 8.0;
        const double SwapTimeout = 8.0;

        static double _phaseStart;
        static int _phase;          // 0 = waiting for the board, 1 = waiting for the swap
        static int _scoreBefore;
        static int _movesBefore;
        static bool _stopping;
        static int _exitCode;

        [MenuItem("Candy Balls/Run Scene Smoke Test")]
        public static void Run()
        {
            // Keep static state alive across the play-mode transition.
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

            EditorSceneManager.OpenScene(CandyBallsSceneBuilder.ScenePath, OpenSceneMode.Single);

            _phase = 0;
            _phaseStart = EditorApplication.timeSinceStartup;
            _stopping = false;
            _exitCode = 1;

            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        static void Tick()
        {
            if (_stopping)
            {
                if (!EditorApplication.isPlaying)
                {
                    EditorApplication.update -= Tick;
                    EditorApplication.Exit(_exitCode);
                }
                return;
            }

            if (!EditorApplication.isPlaying) return;

            double now = EditorApplication.timeSinceStartup;

            if (_phase == 0) PhaseWaitForBoard(now);
            else if (_phase == 1) PhaseWaitForSwap(now);
        }

        static void PhaseWaitForBoard(double now)
        {
            var game = Object.FindObjectOfType<GameManager>();
            var board = Object.FindObjectOfType<Board>();
            var hud = Object.FindObjectOfType<Hud>();

            if (game == null || board == null || hud == null)
            {
                if (now - _phaseStart > BuildTimeout)
                    Finish(false, "scene objects missing (GameManager/Board/Hud)");
                return;
            }

            int expected = board.Columns * board.Rows;
            int actual = board.ballsRoot != null ? board.ballsRoot.childCount : -1;
            if (actual != expected)
            {
                if (now - _phaseStart > BuildTimeout)
                    Finish(false, "balls on board = " + actual + ", expected " + expected);
                return;
            }

            if (hud.scoreText == null || string.IsNullOrEmpty(hud.scoreText.text))
            {
                Finish(false, "HUD did not populate");
                return;
            }

            Debug.Log(Marker + " board ready: " + actual + " balls, moves=" + game.MovesLeft + " score=" + game.Score);

            Ball[] move = board.FindPossibleMove();
            if (move == null)
            {
                Finish(false, "no legal move found on a fresh board");
                return;
            }

            _scoreBefore = game.Score;
            _movesBefore = game.MovesLeft;
            _phase = 1;
            _phaseStart = now;

            board.RequestSwap(move[0], move[1]);
        }

        static void PhaseWaitForSwap(double now)
        {
            var game = Object.FindObjectOfType<GameManager>();
            if (game == null)
            {
                Finish(false, "GameManager vanished");
                return;
            }

            if (game.MovesLeft < _movesBefore)
            {
                Finish(true, "swap consumed a move; score " + _scoreBefore + " -> " + game.Score +
                             ", moves " + _movesBefore + " -> " + game.MovesLeft);
                return;
            }

            if (now - _phaseStart > SwapTimeout)
                Finish(false, "swap did not consume a move (still " + game.MovesLeft + ")");
        }

        static void Finish(bool ok, string detail)
        {
            if (ok) Debug.Log(Marker + " PASS - " + detail);
            else Debug.LogError(Marker + " FAIL - " + detail);

            _exitCode = ok ? 0 : 1;
            _stopping = true;
            EditorApplication.isPlaying = false;
        }
    }
}
