using UnityEngine;

namespace CandyBalls
{
    /// <summary>
    /// Translates pointer input into board actions. Supports both swipe-to-swap and
    /// tap-a-ball-then-tap-a-neighbour. Also drives the idle hint timer.
    /// </summary>
    public class BoardInput : MonoBehaviour
    {
        const float HintDelay = 6f;

        GameManager _game;
        Ball _pressedBall;
        Vector2 _pressScreen;
        bool _pointerDown;
        bool _dragged;
        float _idleTime;

        public void Initialize(GameManager game)
        {
            _game = game;
        }

        void Update()
        {
            if (_game == null || _game.GameOver) return;

            Board board = _game.Board;
            if (board == null) return;

            if (board.Busy)
            {
                _pointerDown = false;
                _dragged = false;
                return;
            }

            Vector2 screen = PointerInput.Position;

            if (PointerInput.PressedDown)
                OnPressed(board, screen);

            if (_pointerDown && PointerInput.IsHeld)
                OnHeld(board, screen);

            if (_pointerDown && PointerInput.Released)
                OnReleased(board, screen);

            UpdateHintTimer(board);
        }

        void OnPressed(Board board, Vector2 screen)
        {
            _pointerDown = true;
            _dragged = false;
            _pressScreen = screen;
            _pressedBall = board.BallAtScreen(screen);
            _idleTime = 0f;
            board.CancelHint();
        }

        void OnHeld(Board board, Vector2 screen)
        {
            if (_dragged || _pressedBall == null) return;

            float threshold = 0.35f * board.Cell * PixelsPerWorldUnit(_game.GameCamera);
            Vector2 delta = screen - _pressScreen;

            if (delta.magnitude < threshold) return;

            _dragged = true;

            Vector2Int direction = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                ? new Vector2Int(delta.x > 0f ? 1 : -1, 0)
                : new Vector2Int(0, delta.y > 0f ? 1 : -1);

            Ball neighbour = board.BallAt(_pressedBall.Grid + direction);
            if (neighbour != null)
                board.RequestSwap(_pressedBall, neighbour);
        }

        void OnReleased(Board board, Vector2 screen)
        {
            _pointerDown = false;

            if (!_dragged && _pressedBall != null && _pressedBall == board.BallAtScreen(screen))
                HandleTap(board, _pressedBall);

            _dragged = false;
            _pressedBall = null;
            _idleTime = 0f;
        }

        void HandleTap(Board board, Ball ball)
        {
            Ball selected = board.SelectedBall;

            if (selected == null)
            {
                board.Select(ball);
                Sfx.Select();
                return;
            }

            if (selected == ball)
            {
                board.ClearSelection();
                return;
            }

            if (Board.AreNeighbours(selected, ball))
            {
                board.RequestSwap(selected, ball);
            }
            else
            {
                board.Select(ball);
                Sfx.Select();
            }
        }

        void UpdateHintTimer(Board board)
        {
            if (_pointerDown)
            {
                _idleTime = 0f;
                return;
            }

            _idleTime += Time.deltaTime;
            if (_idleTime >= HintDelay)
            {
                _idleTime = 0f;
                board.ShowHint();
            }
        }

        static float PixelsPerWorldUnit(Camera cam)
        {
            if (cam == null || !cam.orthographic) return 100f;
            return Screen.height / (cam.orthographicSize * 2f);
        }
    }
}
