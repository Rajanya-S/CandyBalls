using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CandyBalls
{
    /// <summary>
    /// Owns the grid of balls: spawning, match detection, swapping, gravity, refilling and the
    /// cascade loop. It is deliberately the only place that knows the rules of the game.
    /// </summary>
    public class Board : MonoBehaviour
    {
        public const int PointsPerBall = 20;

        // Timings (seconds).
        const float SwapDuration = 0.15f;
        const float FallDuration = 0.20f;
        const float ClearDelay = 0.16f;

        int _cols;
        int _rows;
        int _colorCount;
        float _cell;
        float _ballSize;

        [Header("Scene References")]
        [Tooltip("Container the balls are parented to. Created automatically when empty.")]
        public Transform ballsRoot;

        Ball[,] _balls;
        Transform _ballsRoot;
        Ball _selected;
        Ball _hintA;
        Ball _hintB;
        Coroutine _hintRoutine;

        public GameManager Game { get; private set; }
        public bool Busy { get; private set; }
        public Ball SelectedBall { get { return _selected; } }
        public int Columns { get { return _cols; } }
        public int Rows { get { return _rows; } }
        public float Cell { get { return _cell; } }
        public float WorldWidth { get { return _cols * _cell; } }
        public float WorldHeight { get { return _rows * _cell; } }

        // ------------------------------------------------------------------ setup

        public void Initialize(GameManager game, int columns, int rows, int colorCount, float cell, float ballSize)
        {
            Game = game;
            _cols = Mathf.Max(3, columns);
            _rows = Mathf.Max(3, rows);
            _colorCount = Mathf.Clamp(colorCount, 3, BallPalette.Count);
            _cell = cell;
            _ballSize = ballSize;

            _balls = new Ball[_cols, _rows];

            if (ballsRoot == null)
            {
                var root = new GameObject("Balls");
                ballsRoot = root.transform;
                ballsRoot.SetParent(transform, false);
            }

            _ballsRoot = ballsRoot;
        }

        /// <summary>Wipes the grid and deals a brand new board. Used at start and on restart.</summary>
        public void Rebuild()
        {
            CancelHint();
            StopAllCoroutines();
            Busy = false;
            _selected = null;
            ClearAll();
            FillFresh();
        }

        public Vector3 CellToWorld(int col, int row)
        {
            float x = (col - (_cols - 1) * 0.5f) * _cell;
            float y = (row - (_rows - 1) * 0.5f) * _cell;
            return transform.position + new Vector3(x, y, 0f);
        }

        Ball SpawnBall(int col, int row, int colorIndex)
        {
            var go = new GameObject("Ball");
            go.transform.SetParent(_ballsRoot, false);
            go.transform.position = CellToWorld(col, row);

            var ball = go.AddComponent<Ball>();
            ball.Initialize(colorIndex, new Vector2Int(col, row), _ballSize);
            _balls[col, row] = ball;
            return ball;
        }

        void FillFresh()
        {
            int attempt = 0;
            do
            {
                ClearAll();
                for (int c = 0; c < _cols; c++)
                {
                    for (int r = 0; r < _rows; r++)
                    {
                        int color = 0;
                        int guard = 0;
                        do
                        {
                            color = Random.Range(0, _colorCount);
                            guard++;
                        }
                        while (guard < 40 && WouldCreateMatch(c, r, color));

                        SpawnBall(c, r, color);
                    }
                }
                attempt++;
            }
            while (!HasPossibleMove() && attempt < 30);
        }

        void ClearAll()
        {
            if (_balls == null) return;

            for (int c = 0; c < _cols; c++)
            {
                for (int r = 0; r < _rows; r++)
                {
                    if (_balls[c, r] != null)
                    {
                        _balls[c, r].gameObject.SetActive(false);
                        Destroy(_balls[c, r].gameObject);
                        _balls[c, r] = null;
                    }
                }
            }
        }

        bool WouldCreateMatch(int col, int row, int color)
        {
            if (col >= 2 && _balls[col - 1, row] != null && _balls[col - 2, row] != null &&
                _balls[col - 1, row].ColorIndex == color && _balls[col - 2, row].ColorIndex == color)
                return true;

            if (row >= 2 && _balls[col, row - 1] != null && _balls[col, row - 2] != null &&
                _balls[col, row - 1].ColorIndex == color && _balls[col, row - 2].ColorIndex == color)
                return true;

            return false;
        }

        // -------------------------------------------------------------- queries

        public Ball BallAt(Vector2Int grid)
        {
            if (grid.x < 0 || grid.x >= _cols || grid.y < 0 || grid.y >= _rows) return null;
            return _balls[grid.x, grid.y];
        }

        public Ball BallAtWorld(Vector3 world)
        {
            Vector3 local = world - transform.position;
            float fx = local.x / _cell + (_cols - 1) * 0.5f;
            float fy = local.y / _cell + (_rows - 1) * 0.5f;

            int c = Mathf.RoundToInt(fx);
            int r = Mathf.RoundToInt(fy);

            if (c < 0 || c >= _cols || r < 0 || r >= _rows) return null;
            return _balls[c, r];
        }

        public Ball BallAtScreen(Vector2 screen)
        {
            Camera cam = Game != null ? Game.GameCamera : Camera.main;
            if (cam == null) return null;

            float depth = Mathf.Abs(cam.transform.position.z - transform.position.z);
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
            return BallAtWorld(world);
        }

        public static bool AreNeighbours(Ball a, Ball b)
        {
            if (a == null || b == null) return false;
            return Mathf.Abs(a.Grid.x - b.Grid.x) + Mathf.Abs(a.Grid.y - b.Grid.y) == 1;
        }

        // ------------------------------------------------------------ selection

        public void Select(Ball ball)
        {
            if (_selected == ball) return;

            if (_selected != null) _selected.SetSelected(false);
            _selected = ball;
            if (_selected != null) _selected.SetSelected(true);
        }

        public void ClearSelection()
        {
            Select(null);
        }

        // -------------------------------------------------------------- swapping

        public void RequestSwap(Ball a, Ball b)
        {
            if (Busy || !AreNeighbours(a, b)) return;
            CancelHint();
            ClearSelection();
            StartCoroutine(SwapRoutine(a, b));
        }

        IEnumerator SwapRoutine(Ball a, Ball b)
        {
            Busy = true;
            Sfx.Swap();

            SwapCells(a, b);
            yield return MoveBoth(a, b, SwapDuration);

            if (FindMatches().Count == 0)
            {
                // Illegal move: put them back.
                SwapCells(a, b);
                yield return MoveBoth(a, b, SwapDuration);
                Game.OnInvalidSwap(a, b);
                Busy = false;
                yield break;
            }

            Game.OnMoveUsed();
            yield return StartCoroutine(ResolveRoutine());
            Busy = false;
            Game.OnBoardSettled();
        }

        void SwapCells(Ball a, Ball b)
        {
            _balls[a.Grid.x, a.Grid.y] = b;
            _balls[b.Grid.x, b.Grid.y] = a;

            Vector2Int tmp = a.Grid;
            a.Grid = b.Grid;
            b.Grid = tmp;
        }

        IEnumerator MoveBoth(Ball a, Ball b, float duration)
        {
            Coroutine ca = StartCoroutine(a.MoveTo(CellToWorld(a.Grid.x, a.Grid.y), duration));
            Coroutine cb = StartCoroutine(b.MoveTo(CellToWorld(b.Grid.x, b.Grid.y), duration));
            yield return ca;
            yield return cb;
        }

        // ------------------------------------------------------------ matching

        public List<Ball> FindMatches()
        {
            var matched = new List<Ball>();
            var seen = new HashSet<Ball>();

            // Horizontal runs.
            for (int r = 0; r < _rows; r++)
            {
                int runStart = 0;
                for (int c = 1; c <= _cols; c++)
                {
                    bool same = c < _cols &&
                                _balls[c, r] != null &&
                                _balls[runStart, r] != null &&
                                _balls[c, r].ColorIndex == _balls[runStart, r].ColorIndex;

                    if (!same)
                    {
                        if (c - runStart >= 3)
                        {
                            for (int k = runStart; k < c; k++)
                            {
                                if (_balls[k, r] != null && seen.Add(_balls[k, r])) matched.Add(_balls[k, r]);
                            }
                        }
                        runStart = c;
                    }
                }
            }

            // Vertical runs.
            for (int c = 0; c < _cols; c++)
            {
                int runStart = 0;
                for (int r = 1; r <= _rows; r++)
                {
                    bool same = r < _rows &&
                                _balls[c, r] != null &&
                                _balls[c, runStart] != null &&
                                _balls[c, r].ColorIndex == _balls[c, runStart].ColorIndex;

                    if (!same)
                    {
                        if (r - runStart >= 3)
                        {
                            for (int k = runStart; k < r; k++)
                            {
                                if (_balls[c, k] != null && seen.Add(_balls[c, k])) matched.Add(_balls[c, k]);
                            }
                        }
                        runStart = r;
                    }
                }
            }

            return matched;
        }

        bool SameColor(int col, int row, int color)
        {
            Ball b = _balls[col, row];
            return b != null && b.ColorIndex == color;
        }

        /// <summary>True when the ball at (col,row) is part of a run of three or more.</summary>
        bool HasLineThrough(int col, int row)
        {
            Ball ball = _balls[col, row];
            if (ball == null) return false;

            int color = ball.ColorIndex;
            int count = 1;

            for (int x = col - 1; x >= 0 && SameColor(x, row, color); x--) count++;
            for (int x = col + 1; x < _cols && SameColor(x, row, color); x++) count++;
            if (count >= 3) return true;

            count = 1;
            for (int y = row - 1; y >= 0 && SameColor(col, y, color); y--) count++;
            for (int y = row + 1; y < _rows && SameColor(col, y, color); y++) count++;
            return count >= 3;
        }

        bool SwapMakesMatch(int c1, int r1, int c2, int r2)
        {
            Ball a = _balls[c1, r1];
            Ball b = _balls[c2, r2];
            if (a == null || b == null) return false;

            int colorA = a.ColorIndex;
            int colorB = b.ColorIndex;

            a.SetColorIndex(colorB);
            b.SetColorIndex(colorA);

            bool result = HasLineThrough(c1, r1) || HasLineThrough(c2, r2);

            a.SetColorIndex(colorA);
            b.SetColorIndex(colorB);
            return result;
        }

        /// <summary>Returns a legal move, or null when the board is stuck.</summary>
        public Ball[] FindPossibleMove()
        {
            for (int c = 0; c < _cols; c++)
            {
                for (int r = 0; r < _rows; r++)
                {
                    if (c + 1 < _cols && SwapMakesMatch(c, r, c + 1, r))
                        return new[] { _balls[c, r], _balls[c + 1, r] };

                    if (r + 1 < _rows && SwapMakesMatch(c, r, c, r + 1))
                        return new[] { _balls[c, r], _balls[c, r + 1] };
                }
            }
            return null;
        }

        public bool HasPossibleMove()
        {
            return FindPossibleMove() != null;
        }

        // -------------------------------------------------------------- cascades

        IEnumerator ResolveRoutine()
        {
            int cascade = 0;

            while (true)
            {
                List<Ball> matches = FindMatches();
                if (matches.Count == 0) break;

                cascade++;

                int points = PointsPerBall * matches.Count * cascade;
                Vector3 centre = Vector3.zero;
                for (int i = 0; i < matches.Count; i++) centre += matches[i].transform.position;
                centre /= matches.Count;

                Game.OnMatchResolved(matches, points, cascade, centre);

                for (int i = 0; i < matches.Count; i++)
                {
                    Ball ball = matches[i];
                    if (ball == null) continue;

                    _balls[ball.Grid.x, ball.Grid.y] = null;
                    StartCoroutine(ball.PopAndDestroy());
                }

                yield return new WaitForSeconds(ClearDelay);
                yield return StartCoroutine(CollapseRoutine());
            }

            if (!HasPossibleMove())
                yield return StartCoroutine(ShuffleRoutine());
        }

        IEnumerator CollapseRoutine()
        {
            var running = new List<Coroutine>();

            for (int c = 0; c < _cols; c++)
            {
                int writeRow = 0;

                // Let existing balls fall into the gaps.
                for (int r = 0; r < _rows; r++)
                {
                    Ball ball = _balls[c, r];
                    if (ball == null) continue;

                    if (r != writeRow)
                    {
                        _balls[c, writeRow] = ball;
                        _balls[c, r] = null;
                        ball.Grid = new Vector2Int(c, writeRow);
                        running.Add(StartCoroutine(ball.MoveTo(CellToWorld(c, writeRow), FallDuration)));
                    }

                    writeRow++;
                }

                // Spawn replacements above the top edge and drop them in.
                int spawnIndex = 0;
                for (int r = writeRow; r < _rows; r++)
                {
                    spawnIndex++;
                    int color = Random.Range(0, _colorCount);
                    Ball ball = SpawnBall(c, r, color);

                    Vector3 target = CellToWorld(c, r);
                    Vector3 from = CellToWorld(c, _rows - 1 + spawnIndex);
                    running.Add(StartCoroutine(ball.DropIn(from, target, FallDuration, 0.03f * spawnIndex)));
                }
            }

            for (int i = 0; i < running.Count; i++) yield return running[i];
        }

        IEnumerator ShuffleRoutine()
        {
            Game.OnShuffling();

            int guard = 0;
            do
            {
                for (int c = 0; c < _cols; c++)
                {
                    for (int r = 0; r < _rows; r++)
                    {
                        if (_balls[c, r] != null) _balls[c, r].SetColor(Random.Range(0, _colorCount));
                    }
                }
                guard++;
            }
            while ((FindMatches().Count > 0 || !HasPossibleMove()) && guard < 40);

            var pulses = new List<Coroutine>();
            for (int c = 0; c < _cols; c++)
            {
                for (int r = 0; r < _rows; r++)
                {
                    if (_balls[c, r] != null) pulses.Add(StartCoroutine(_balls[c, r].Pulse(0.4f)));
                }
            }

            for (int i = 0; i < pulses.Count; i++) yield return pulses[i];
        }

        // ----------------------------------------------------------------- hints

        public void ShowHint()
        {
            if (Busy) return;

            Ball[] move = FindPossibleMove();
            if (move == null) return;

            CancelHint();
            _hintA = move[0];
            _hintB = move[1];
            _hintRoutine = StartCoroutine(HintRoutine(move[0], move[1]));
        }

        public void CancelHint()
        {
            if (_hintRoutine != null)
            {
                StopCoroutine(_hintRoutine);
                _hintRoutine = null;
            }

            if (_hintA != null) _hintA.ResetVisual();
            if (_hintB != null) _hintB.ResetVisual();
            _hintA = null;
            _hintB = null;
        }

        IEnumerator HintRoutine(Ball a, Ball b)
        {
            for (int i = 0; i < 2; i++)
            {
                if (a == null || b == null) yield break;
                yield return StartCoroutine(a.Pulse());
                if (a == null || b == null) yield break;
                yield return StartCoroutine(b.Pulse());
            }
            _hintRoutine = null;
        }
    }
}
