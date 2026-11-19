using System.Collections.Generic;
using UnityEngine;

namespace CandyBalls
{
    /// <summary>
    /// Owns the whole game: camera, background, board, HUD, score and level state.
    ///
    /// Every reference below can be assigned in the scene. Anything left empty is created at
    /// runtime, so the game also works if you drop the scripts into a blank project.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Board")]
        [Tooltip("Number of columns in the grid.")]
        public int columns = 8;
        [Tooltip("Number of rows in the grid.")]
        public int rows = 8;
        [Tooltip("How many different ball colours are used (3-8). Fewer colours means more matches.")]
        public int colors = 6;
        [Tooltip("Distance between two neighbouring ball centres, in world units.")]
        public float cellSize = 1f;
        [Tooltip("Ball diameter as a fraction of the cell size.")]
        public float ballSize = 0.86f;

        [Header("Rules")]
        [Tooltip("Moves available for the level.")]
        public int startingMoves = 30;
        [Tooltip("Score needed to win.")]
        public int targetScore = 2000;

        [Header("Look")]
        public Color backgroundBottom = new Color(0.078f, 0.071f, 0.153f);
        public Color backgroundTop = new Color(0.157f, 0.114f, 0.259f);
        public Color panelBottom = new Color(0.106f, 0.098f, 0.204f);
        public Color panelTop = new Color(0.180f, 0.149f, 0.318f);
        public Color textColor = new Color(0.949f, 0.957f, 1f);
        public Color accentColor = new Color(1f, 0.820f, 0.400f);

        [Header("Scene References")]
        [Tooltip("Camera that frames the board. Falls back to Camera.main.")]
        public Camera gameCamera;
        [Tooltip("Full-screen gradient behind everything. Created automatically when empty.")]
        public SpriteRenderer background;
        [Tooltip("The grid. Created automatically when empty.")]
        public Board board;
        [Tooltip("Rounded panel behind the grid. Created automatically when empty.")]
        public SpriteRenderer boardPanel;
        [Tooltip("Container the checkerboard tiles are parented to. Created automatically when empty.")]
        public Transform tilesRoot;
        [Tooltip("The HUD. Created automatically when empty.")]
        public Hud hud;

        public int Score { get; private set; }
        public int MovesLeft { get; private set; }
        public bool GameOver { get; private set; }
        public bool Won { get; private set; }
        public Camera GameCamera { get { return gameCamera; } }
        public Board Board { get { return board; } }

        int _lastScreenWidth;
        int _lastScreenHeight;

        void Start()
        {
            Build();
        }

        void Update()
        {
            if (PointerInput.RestartPressed) Restart();
            if (PointerInput.MutePressed) Sfx.ToggleMute();

            if (hud != null && PointerInput.PressedDown && hud.RestartHitTest(PointerInput.Position))
                Restart();

            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
            {
                _lastScreenWidth = Screen.width;
                _lastScreenHeight = Screen.height;
                FitCamera();
                FitBackground();
            }
        }

        // ------------------------------------------------------------------ build

        /// <summary>Resolves the scene references and starts a fresh level.</summary>
        public void Build()
        {
            ResolveCamera();
            ResolveBackground();
            ResolveBoard();
            ResolveHud();
            Restart();
        }

        void ResolveCamera()
        {
            if (gameCamera == null) gameCamera = Camera.main;

            if (gameCamera == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                go.transform.position = new Vector3(0f, 0f, -10f);
                gameCamera = go.AddComponent<Camera>();
            }

            gameCamera.orthographic = true;
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.backgroundColor = backgroundBottom;
            gameCamera.transform.position = new Vector3(0f, 0f, -10f);
            gameCamera.transform.rotation = Quaternion.identity;

            if (gameCamera.GetComponent<AudioListener>() == null)
                gameCamera.gameObject.AddComponent<AudioListener>();

            Sfx.Initialize(gameCamera.gameObject);
            FitCamera();
        }

        void FitCamera()
        {
            if (gameCamera == null) return;

            float boardWidth = columns * cellSize;
            float boardHeight = rows * cellSize;
            const float margin = 2.4f;

            float halfHeight = boardHeight * 0.5f + margin * 0.5f;
            float halfWidth = boardWidth * 0.5f + margin * 0.5f;
            float aspect = gameCamera.aspect > 0.01f ? gameCamera.aspect : 1.6f;

            gameCamera.orthographicSize = Mathf.Max(halfHeight, halfWidth / aspect);
        }

        void ResolveBackground()
        {
            if (background == null)
            {
                var go = new GameObject("Background");
                go.transform.SetParent(transform, false);
                background = go.AddComponent<SpriteRenderer>();
                background.sortingOrder = -100;
            }

            background.sprite = SpriteFactory.VerticalGradient(backgroundBottom, backgroundTop);
            background.color = Color.white;
            FitBackground();
        }

        void FitBackground()
        {
            if (background == null || gameCamera == null) return;

            float height = gameCamera.orthographicSize * 2f;
            float width = height * gameCamera.aspect;

            background.transform.position = new Vector3(gameCamera.transform.position.x, gameCamera.transform.position.y, 1f);
            background.transform.localScale = new Vector3(width, height, 1f);
        }

        void ResolveBoard()
        {
            if (board == null)
            {
                var go = new GameObject("Board");
                go.transform.SetParent(transform, false);
                board = go.AddComponent<Board>();
            }

            board.transform.position = Vector3.zero;
            board.Initialize(this, columns, rows, colors, cellSize, ballSize);

            var input = board.GetComponent<BoardInput>();
            if (input == null) input = board.gameObject.AddComponent<BoardInput>();
            input.Initialize(this);
        }

        void ResolveHud()
        {
            if (hud == null) hud = Hud.Create(this, transform);
            else hud.Initialize(this);
        }

        /// <summary>Builds the static furniture around the grid: the panel and the checkerboard.</summary>
        void BuildDecor()
        {
            if (board == null) return;

            float width = columns * cellSize;
            float height = rows * cellSize;
            const float padding = 0.45f;

            if (boardPanel == null)
            {
                var go = new GameObject("Panel");
                go.transform.SetParent(board.transform, false);
                boardPanel = go.AddComponent<SpriteRenderer>();
                boardPanel.sortingOrder = -70;
            }

            boardPanel.sprite = SpriteFactory.Panel(64, 18, panelBottom, panelTop);
            boardPanel.drawMode = SpriteDrawMode.Sliced;
            boardPanel.size = new Vector2(width + padding, height + padding);
            boardPanel.transform.position = board.transform.position;
            boardPanel.transform.localScale = Vector3.one;

            if (tilesRoot == null)
            {
                var go = new GameObject("Tiles");
                go.transform.SetParent(board.transform, false);
                tilesRoot = go.transform;
            }

            // Drop any tiles from a previous build (board size may have changed).
            for (int i = tilesRoot.childCount - 1; i >= 0; i--)
            {
                GameObject child = tilesRoot.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            Sprite tileSprite = SpriteFactory.Tile();
            float tileScale = cellSize * 0.94f;

            for (int c = 0; c < columns; c++)
            {
                for (int r = 0; r < rows; r++)
                {
                    var tileGo = new GameObject("Tile_" + c + "_" + r);
                    tileGo.transform.SetParent(tilesRoot, false);
                    tileGo.transform.position = board.CellToWorld(c, r);
                    tileGo.transform.localScale = Vector3.one * tileScale;

                    var tile = tileGo.AddComponent<SpriteRenderer>();
                    tile.sprite = tileSprite;
                    tile.sortingOrder = -60;

                    float alpha = (c + r) % 2 == 0 ? 0.085f : 0.045f;
                    tile.color = new Color(1f, 1f, 1f, alpha);
                }
            }
        }

        // -------------------------------------------------------------- level state

        public void Restart()
        {
            StopAllCoroutines();
            Sfx.Stop();

            Score = 0;
            MovesLeft = startingMoves;
            GameOver = false;
            Won = false;

            BuildDecor();

            if (board != null) board.Rebuild();
            if (hud != null)
            {
                hud.HideBanner();
                hud.SetTarget(targetScore);
            }

            RefreshHud();
        }

        void RefreshHud()
        {
            if (hud == null) return;
            hud.SetScore(Score);
            hud.SetMoves(MovesLeft);
        }

        // -------------------------------------------------------------- gameplay

        public void OnMoveUsed()
        {
            MovesLeft = Mathf.Max(0, MovesLeft - 1);
            RefreshHud();
        }

        public void OnMatchResolved(List<Ball> balls, int points, int cascade, Vector3 worldCentre)
        {
            Score += points;
            RefreshHud();

            Sfx.Match(cascade);

            Color tint = balls.Count > 0 ? BallPalette.Get(balls[0].ColorIndex) : Color.white;
            Effects.Burst(worldCentre, tint, Mathf.Clamp(6 + balls.Count, 6, 18));

            if (cascade >= 2 && hud != null)
                hud.ShowCombo(cascade, worldCentre, points);
        }

        public void OnInvalidSwap(Ball a, Ball b)
        {
            Sfx.Invalid();
            if (hud != null) hud.FlashHint("No match there!");
        }

        public void OnShuffling()
        {
            if (hud != null) hud.FlashHint("No moves left - shuffling!");
        }

        public void OnBoardSettled()
        {
            if (GameOver) return;

            if (Score >= targetScore)
            {
                EndGame(true);
                return;
            }

            if (MovesLeft <= 0)
                EndGame(false);
        }

        void EndGame(bool won)
        {
            GameOver = true;
            Won = won;

            if (board != null) board.CancelHint();

            if (won)
            {
                Sfx.Win();
                if (hud != null) hud.ShowBanner("Level Complete!", "You scored " + Score + " - tap to play again");
            }
            else
            {
                Sfx.Lose();
                if (hud != null) hud.ShowBanner("Out of Moves", "You scored " + Score + " / " + targetScore + " - tap to play again");
            }
        }
    }
}
