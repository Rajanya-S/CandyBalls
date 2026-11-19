using UnityEngine;
using UnityEngine.UI;

namespace CandyBalls
{
    /// <summary>A score popup that drifts upwards and fades out.</summary>
    public class FloatingText : MonoBehaviour
    {
        const float Lifetime = 1f;
        const float RiseDistance = 90f;

        Text _text;
        RectTransform _rect;
        Vector2 _startPosition;
        float _elapsed;

        public static void Spawn(Transform parent, string content, Vector2 anchoredPosition, int fontSize, Color color)
        {
            var go = new GameObject("FloatingText", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(400f, 120f);

            var text = go.AddComponent<Text>();
            text.font = UIFont.Get();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(2f, -2f);

            var instance = go.AddComponent<FloatingText>();
            instance._text = text;
            instance._rect = rect;
            instance._startPosition = anchoredPosition;
        }

        void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / Lifetime);

            _rect.anchoredPosition = _startPosition + new Vector2(0f, RiseDistance * Easing.OutQuad(t));

            float scale = 1f + Mathf.Sin(Mathf.Min(t, 0.35f) / 0.35f * Mathf.PI) * 0.25f;
            _rect.localScale = Vector3.one * scale;

            Color c = _text.color;
            c.a = 1f - Mathf.Pow(t, 2f);
            _text.color = c;

            if (t >= 1f) Destroy(gameObject);
        }
    }
}
