using System.Collections;
using UnityEngine;

namespace CandyBalls
{
    /// <summary>
    /// One ball on the board. Holds its colour and grid position and exposes coroutine based
    /// animations that the <see cref="Board"/> drives.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class Ball : MonoBehaviour
    {
        public int ColorIndex { get; private set; }
        public Vector2Int Grid { get; set; }
        public SpriteRenderer Sprite { get; private set; }
        public bool IsAnimating { get; private set; }

        float _baseScale = 1f;
        bool _selected;

        void Awake()
        {
            Sprite = GetComponent<SpriteRenderer>();
        }

        public void Initialize(int colorIndex, Vector2Int grid, float size)
        {
            if (Sprite == null) Sprite = GetComponent<SpriteRenderer>();

            ColorIndex = colorIndex;
            Grid = grid;
            _baseScale = size;

            Sprite.sprite = SpriteFactory.Ball(BallPalette.Get(colorIndex));
            Sprite.color = Color.white;
            transform.localScale = Vector3.one * size;
            name = "Ball_" + colorIndex + "_" + grid.x + "_" + grid.y;
        }

        public void SetColor(int colorIndex)
        {
            SetColorIndex(colorIndex);
            Sprite.sprite = SpriteFactory.Ball(BallPalette.Get(colorIndex));
        }

        /// <summary>Changes only the logical colour. Used for cheap, purely hypothetical checks.</summary>
        public void SetColorIndex(int colorIndex)
        {
            ColorIndex = colorIndex;
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            if (!IsAnimating) transform.localScale = Vector3.one * (_selected ? _baseScale * 1.16f : _baseScale);
        }

        public void SnapScale()
        {
            transform.localScale = Vector3.one * (_selected ? _baseScale * 1.16f : _baseScale);
        }

        /// <summary>Clears any interrupted animation state and restores the resting look.</summary>
        public void ResetVisual()
        {
            IsAnimating = false;
            transform.localScale = Vector3.one * (_selected ? _baseScale * 1.16f : _baseScale);
        }

        // ------------------------------------------------------------- animation

        /// <summary>Glide to a world position.</summary>
        public IEnumerator MoveTo(Vector3 target, float duration, float arc = 0f)
        {
            if (duration <= 0f)
            {
                transform.position = target;
                yield break;
            }

            IsAnimating = true;
            Vector3 start = transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = Easing.OutQuad(t);

                Vector3 p = Vector3.Lerp(start, target, eased);
                if (arc != 0f) p.y += arc * Mathf.Sin(Mathf.PI * t);

                transform.position = p;
                yield return null;
            }

            transform.position = target;
            IsAnimating = false;
            SnapScale();
        }

        /// <summary>Fall in from above the board while scaling up.</summary>
        public IEnumerator DropIn(Vector3 from, Vector3 to, float duration, float delay)
        {
            IsAnimating = true;
            transform.position = from;
            transform.localScale = Vector3.one * (_baseScale * 0.55f);

            if (delay > 0f)
            {
                float waited = 0f;
                while (waited < delay)
                {
                    waited += Time.deltaTime;
                    yield return null;
                }
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float move = Easing.OutQuad(t);
                float pop = Easing.OutBack(Mathf.Clamp01(t * 1.6f));

                transform.position = Vector3.Lerp(from, to, move);
                transform.localScale = Vector3.one * Mathf.Lerp(_baseScale * 0.55f, _baseScale, pop);
                yield return null;
            }

            transform.position = to;
            transform.localScale = Vector3.one * _baseScale;
            IsAnimating = false;
        }

        /// <summary>Squash, flash and disappear.</summary>
        public IEnumerator PopAndDestroy(float duration = 0.24f)
        {
            IsAnimating = true;
            Vector3 startScale = transform.localScale;
            Color startColor = Sprite.color;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float scale;
                if (t < 0.32f)
                {
                    scale = Mathf.Lerp(1f, 1.35f, Easing.OutQuad(t / 0.32f));
                }
                else
                {
                    scale = Mathf.Lerp(1.35f, 0f, Easing.InQuad((t - 0.32f) / 0.68f));
                }

                transform.localScale = startScale * scale;
                Sprite.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t * 0.85f);
                yield return null;
            }

            Destroy(gameObject);
        }

        /// <summary>Little bounce used by the hint system.</summary>
        public IEnumerator Pulse(float duration = 0.34f)
        {
            IsAnimating = true;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float bump = Mathf.Sin(t * Mathf.PI) * 0.22f;
                transform.localScale = Vector3.one * (_baseScale * (1f + bump));
                yield return null;
            }

            IsAnimating = false;
            SnapScale();
        }
    }
}
