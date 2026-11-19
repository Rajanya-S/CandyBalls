using UnityEngine;
using UnityEngine.UI;

namespace CandyBalls
{
    /// <summary>
    /// The on-screen interface: score, moves, target, combo popups, transient hints and the
    /// end-of-level banner.
    ///
    /// Assign the text objects in the scene for full control, or leave them empty and the HUD
    /// builds itself at runtime.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        [Header("Scene References")]
        public Text scoreText;
        public Text movesText;
        public Text targetText;
        public Text restartText;
        public Text hintText;
        public GameObject banner;
        public Text bannerTitle;
        public Text bannerSubtitle;

        GameManager _game;
        RectTransform _canvasRect;
        Coroutine _hintRoutine;

        // ------------------------------------------------------------- lifecycle

        /// <summary>Creates a HUD from scratch, for projects with no authored scene.</summary>
        public static Hud Create(GameManager game, Transform parent)
        {
            var go = new GameObject("HUD", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var hud = go.AddComponent<Hud>();
            hud.Initialize(game);
            return hud;
        }

        /// <summary>Wires the HUD up to the game. Safe to call on a scene-authored HUD.</summary>
        public void Initialize(GameManager game)
        {
            _game = game;

            EnsureCanvas();
            _canvasRect = GetComponent<RectTransform>();

            if (!HasCompleteHierarchy())
                BuildHierarchy();

            ApplyStyles();

            HideBanner();
            if (hintText != null) hintText.text = string.Empty;
            SetTarget(game.targetScore);
            SetScore(0);
            SetMoves(game.startingMoves);
        }

        /// <summary>Guarantees the GameObject is a working screen-space canvas.</summary>
        void EnsureCanvas()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            if (GetComponent<CanvasScaler>() == null)
            {
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280f, 720f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }

            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();
        }

        bool HasCompleteHierarchy()
        {
            return scoreText != null && movesText != null && targetText != null &&
                   restartText != null && hintText != null &&
                   banner != null && bannerTitle != null && bannerSubtitle != null;
        }

        // ---------------------------------------------------------------- layout

        void BuildHierarchy()
        {
            scoreText = MakeText(transform, "Score",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(36f, -26f), new Vector2(320f, 150f), 56, TextAnchor.UpperLeft);

            movesText = MakeText(transform, "Moves",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-36f, -26f), new Vector2(320f, 150f), 56, TextAnchor.UpperRight);

            targetText = MakeText(transform, "Target",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -30f), new Vector2(560f, 60f), 30, TextAnchor.UpperCenter);

            restartText = MakeText(transform, "Restart",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 22f), new Vector2(900f, 60f), 26, TextAnchor.LowerCenter);

            hintText = MakeText(transform, "Hint",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 92f), new Vector2(900f, 60f), 30, TextAnchor.LowerCenter);

            BuildBanner();
        }

        static Text MakeText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var text = go.AddComponent<Text>();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(2f, -2f);

            return text;
        }

        void BuildBanner()
        {
            var bannerGo = new GameObject("Banner", typeof(RectTransform));
            bannerGo.transform.SetParent(transform, false);

            var rect = bannerGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var dim = bannerGo.AddComponent<Image>();
            dim.color = new Color(0.03f, 0.03f, 0.08f, 0.75f);
            dim.raycastTarget = false;

            bannerTitle = MakeText(bannerGo.transform, "Title",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 46f), new Vector2(1100f, 160f), 84, TextAnchor.MiddleCenter);

            bannerSubtitle = MakeText(bannerGo.transform, "Subtitle",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -64f), new Vector2(1100f, 120f), 34, TextAnchor.MiddleCenter);

            banner = bannerGo;
            bannerGo.SetActive(false);
        }

        // ---------------------------------------------------------------- styling

        void ApplyStyles()
        {
            if (_game == null) return;

            Color main = _game.textColor;
            Color muted = new Color(0.60f, 0.65f, 0.80f);
            Color accent = _game.accentColor;

            Font font = UIFont.Get();

            Style(scoreText, font, main);
            Style(movesText, font, main);
            Style(targetText, font, accent);
            Style(restartText, font, muted);
            Style(hintText, font, accent);
            Style(bannerTitle, font, main);
            Style(bannerSubtitle, font, muted);

            if (restartText != null)
                restartText.text = "Tap here or press <color=#FFD166>R</color> to restart   ·   <color=#FFD166>M</color> to mute";
        }

        static void Style(Text text, Font font, Color color)
        {
            if (text == null) return;
            text.font = font;
            text.color = color;
            text.raycastTarget = false;

            if (text.GetComponent<Shadow>() == null)
            {
                var shadow = text.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
                shadow.effectDistance = new Vector2(2f, -2f);
            }
        }

        // ------------------------------------------------------------- public API

        public void SetScore(int score)
        {
            if (scoreText != null)
                scoreText.text = "<size=26><color=#9BA3C7>SCORE</color></size>\n" + score;
        }

        public void SetMoves(int moves)
        {
            if (movesText != null)
                movesText.text = "<size=26><color=#9BA3C7>MOVES</color></size>\n" + moves;
        }

        public void SetTarget(int target)
        {
            if (targetText != null)
                targetText.text = "TARGET  " + target;
        }

        public void ShowCombo(int cascade, Vector3 worldPosition, int points)
        {
            Vector2 anchored = WorldToCanvas(worldPosition);
            Color accent = _game != null ? _game.accentColor : Color.white;
            FloatingText.Spawn(transform, "COMBO x" + cascade + "\n+" + points, anchored + new Vector2(0f, 30f), 44, accent);
        }

        public void FlashHint(string message)
        {
            if (hintText == null) return;
            hintText.text = message;

            if (_hintRoutine != null) StopCoroutine(_hintRoutine);
            _hintRoutine = StartCoroutine(ClearHintLater());
        }

        System.Collections.IEnumerator ClearHintLater()
        {
            yield return new WaitForSeconds(1.6f);
            if (hintText != null) hintText.text = string.Empty;
            _hintRoutine = null;
        }

        public void ShowBanner(string title, string subtitle)
        {
            if (banner == null) return;
            if (bannerTitle != null) bannerTitle.text = title;
            if (bannerSubtitle != null) bannerSubtitle.text = subtitle;
            banner.SetActive(true);
        }

        public void HideBanner()
        {
            if (banner != null) banner.SetActive(false);
        }

        /// <summary>
        /// True when the pointer went down on the restart control. Once the level is over the whole
        /// screen becomes a restart button.
        /// </summary>
        public bool RestartHitTest(Vector2 screenPosition)
        {
            if (_game != null && _game.GameOver) return true;
            if (restartText == null) return false;

            return RectTransformUtility.RectangleContainsScreenPoint(restartText.rectTransform, screenPosition, null);
        }

        Vector2 WorldToCanvas(Vector3 worldPosition)
        {
            Camera cam = _game != null ? _game.GameCamera : Camera.main;
            if (cam == null || _canvasRect == null) return Vector2.zero;

            Vector2 screen = cam.WorldToScreenPoint(worldPosition);
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, null, out local);
            return local;
        }
    }
}
