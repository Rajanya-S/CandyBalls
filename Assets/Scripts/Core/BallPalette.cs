using UnityEngine;

namespace CandyBalls
{
    /// <summary>
    /// The colours the balls can take. The board only ever uses the first N entries, where N is
    /// configured on the GameManager, so you can make the game easier or harder by changing
    /// "colors" there.
    /// </summary>
    public static class BallPalette
    {
        static readonly Color[] Colors =
        {
            new Color(0.97f, 0.26f, 0.34f), // cherry red
            new Color(0.99f, 0.62f, 0.16f), // orange
            new Color(0.99f, 0.86f, 0.22f), // lemon
            new Color(0.36f, 0.83f, 0.40f), // lime
            new Color(0.25f, 0.64f, 0.98f), // blueberry
            new Color(0.71f, 0.42f, 0.95f), // grape
            new Color(0.18f, 0.85f, 0.81f), // mint
            new Color(0.99f, 0.50f, 0.79f), // bubblegum
        };

        public static int Count
        {
            get { return Colors.Length; }
        }

        public static Color Get(int index)
        {
            if (Colors.Length == 0) return Color.white;
            int i = index % Colors.Length;
            if (i < 0) i += Colors.Length;
            return Colors[i];
        }
    }
}
