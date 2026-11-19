using UnityEngine;

namespace CandyBalls
{
    /// <summary>
    /// All sound effects are synthesised into AudioClips at runtime, so the project ships without
    /// any audio files. Press M to mute.
    /// </summary>
    public static class Sfx
    {
        const int SampleRate = 44100;

        static AudioSource _source;
        static AudioClip _select;
        static AudioClip _swap;
        static AudioClip _invalid;
        static AudioClip _match;
        static AudioClip _win;
        static AudioClip _lose;

        public static bool Muted { get; private set; }

        public static void Initialize(GameObject host)
        {
            if (_source != null) return;

            _source = host.GetComponent<AudioSource>();
            if (_source == null) _source = host.AddComponent<AudioSource>();

            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _source.volume = 0.55f;

            _select = Tone("select", 660f, 880f, 0.07f, 0.35f);
            _swap = Tone("swap", 420f, 560f, 0.09f, 0.30f);
            _invalid = Tone("invalid", 220f, 150f, 0.16f, 0.35f, true);
            _match = Tone("match", 520f, 1180f, 0.20f, 0.45f);
            _win = Chord("win", new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.55f, 0.35f);
            _lose = Chord("lose", new[] { 392f, 329.63f, 261.63f }, 0.6f, 0.35f);
        }

        public static void ToggleMute()
        {
            Muted = !Muted;
            if (_source != null) _source.mute = Muted;
        }

        public static void Stop()
        {
            if (_source != null) _source.Stop();
        }

        public static void Select() { Play(_select, 1f, 0.5f); }
        public static void Swap() { Play(_swap, 1f, 0.7f); }
        public static void Invalid() { Play(_invalid, 1f, 0.8f); }
        public static void Win() { Play(_win, 1f, 1f); }
        public static void Lose() { Play(_lose, 1f, 1f); }

        public static void Match(int cascade)
        {
            float pitch = Mathf.Clamp(1f + (cascade - 1) * 0.14f, 1f, 2.4f);
            Play(_match, pitch, 0.9f);
        }

        static void Play(AudioClip clip, float pitch, float volume)
        {
            if (Muted || _source == null || clip == null) return;
            _source.pitch = pitch;
            _source.PlayOneShot(clip, volume);
        }

        // ------------------------------------------------------------ synthesis

        static AudioClip Tone(string name, float startFreq, float endFreq, float duration, float volume, bool square = false)
        {
            int sampleCount = Mathf.Max(16, Mathf.RoundToInt(SampleRate * duration));
            float[] data = new float[sampleCount];

            float phase = 0f;
            float attackSamples = SampleRate * 0.005f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                phase += freq / SampleRate * Mathf.PI * 2f;

                float wave = Mathf.Sin(phase);
                if (square) wave = Mathf.Sign(wave) * 0.55f + wave * 0.45f;
                else wave = wave * 0.85f + Mathf.Sin(phase * 2f) * 0.15f;

                float attack = Mathf.Clamp01(i / attackSamples);
                float decay = Mathf.Pow(1f - t, 2.2f);

                data[i] = wave * attack * decay * volume;
            }

            var clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Chord(string name, float[] notes, float duration, float volume)
        {
            int sampleCount = Mathf.Max(16, Mathf.RoundToInt(SampleRate * duration));
            float[] data = new float[sampleCount];
            float[] phases = new float[notes.Length];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float sum = 0f;

                for (int n = 0; n < notes.Length; n++)
                {
                    // Stagger the notes so the chord arpeggiates slightly.
                    float start = n * 0.06f;
                    float noteT = (t * duration - start) / 0.5f;
                    if (noteT <= 0f) continue;
                    if (noteT > 1f) noteT = 1f;

                    phases[n] += notes[n] / SampleRate * Mathf.PI * 2f;
                    float envelope = Mathf.Pow(1f - noteT, 1.6f);
                    sum += Mathf.Sin(phases[n]) * envelope;
                }

                float master = Mathf.Pow(1f - t, 1.2f);
                data[i] = Mathf.Clamp(sum / notes.Length, -1f, 1f) * master * volume;
            }

            var clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
