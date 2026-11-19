using System.Collections.Generic;
using UnityEngine;

namespace CandyBalls
{
    /// <summary>
    /// Lightweight particle bursts. Each burst is a single GameObject that animates its own dots,
    /// which keeps the object count low compared to one GameObject per particle.
    /// </summary>
    public static class Effects
    {
        const int MaxConcurrentBursts = 24;
        static int _active;

        public static void Burst(Vector3 position, Color color, int count = 10)
        {
            if (_active >= MaxConcurrentBursts) return;

            var go = new GameObject("Burst");
            go.transform.position = position;

            var burst = go.AddComponent<ParticleBurst>();
            burst.Init(color, count);
        }

        internal static void Register() { _active++; }
        internal static void Unregister() { _active = Mathf.Max(0, _active - 1); }
    }

    /// <summary>Animates a handful of dots flying outwards and fading away.</summary>
    public class ParticleBurst : MonoBehaviour
    {
        class Dot
        {
            public Transform Transform;
            public SpriteRenderer Renderer;
            public Vector2 Velocity;
            public float Spin;
            public float StartScale;
            public float Delay;
        }

        const float Lifetime = 0.55f;

        readonly List<Dot> _dots = new List<Dot>();
        float _elapsed;
        Color _color;
        bool _released;

        public void Init(Color color, int count)
        {
            _color = color;
            Effects.Register();

            Sprite sprite = SpriteFactory.Dot();

            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Dot");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = Random.insideUnitCircle * 0.12f;

                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = 10;
                renderer.color = Color.Lerp(color, Color.white, Random.Range(0f, 0.45f));

                float angle = Random.Range(0f, Mathf.PI * 2f);
                float speed = Random.Range(1.2f, 3.4f);

                var dot = new Dot();
                dot.Transform = go.transform;
                dot.Renderer = renderer;
                dot.Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                dot.Spin = Random.Range(-540f, 540f);
                dot.StartScale = Random.Range(0.10f, 0.24f);
                dot.Delay = Random.Range(0f, 0.06f);

                go.transform.localScale = Vector3.one * dot.StartScale;
                _dots.Add(dot);
            }
        }

        void Update()
        {
            _elapsed += Time.deltaTime;

            float life = Mathf.Clamp01((_elapsed - 0.05f) / (Lifetime - 0.05f));

            for (int i = 0; i < _dots.Count; i++)
            {
                Dot dot = _dots[i];
                if (dot.Transform == null) continue;

                float t = Mathf.Clamp01((_elapsed - dot.Delay) / (Lifetime - dot.Delay));
                if (t <= 0f) continue;

                dot.Velocity += new Vector2(0f, -6f) * Time.deltaTime;
                dot.Transform.localPosition += (Vector3)(dot.Velocity * Time.deltaTime);
                dot.Transform.Rotate(0f, 0f, dot.Spin * Time.deltaTime);

                float scale = dot.StartScale * (1f - t * 0.85f);
                dot.Transform.localScale = Vector3.one * scale;

                Color c = dot.Renderer.color;
                c.a = 1f - t;
                dot.Renderer.color = c;
            }

            if (life >= 1f) Finish();
        }

        void Finish()
        {
            Release();
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            // Guard against the object being destroyed externally.
            Release();
            _dots.Clear();
        }

        void Release()
        {
            if (_released) return;
            _released = true;
            Effects.Unregister();
        }
    }
}
