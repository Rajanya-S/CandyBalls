using UnityEngine;

namespace CandyBalls
{
    /// <summary>
    /// Resolves a usable font for the HUD. Unity renamed the built-in Arial asset in 2022.2, so we
    /// try both names and fall back to an OS font if neither exists.
    /// </summary>
    public static class UIFont
    {
        static Font _font;

        public static Font Get()
        {
            if (_font != null) return _font;

            // Unity renamed the built-in font in 2022.2. Try the right one first so we don't
            // spam the console with a "resource could not be loaded" message.
#if UNITY_2022_2_OR_NEWER
            _font = TryBuiltin("LegacyRuntime.ttf");
            if (_font == null) _font = TryBuiltin("Arial.ttf");
#else
            _font = TryBuiltin("Arial.ttf");
            if (_font == null) _font = TryBuiltin("LegacyRuntime.ttf");
#endif

            if (_font == null)
            {
                string[] candidates = { "Arial", "Helvetica", "Verdana", "Segoe UI", "Roboto" };
                for (int i = 0; i < candidates.Length && _font == null; i++)
                {
                    try
                    {
                        _font = Font.CreateDynamicFontFromOSFont(candidates[i], 32);
                    }
                    catch
                    {
                        _font = null;
                    }
                }
            }

            if (_font == null)
                Debug.LogWarning("[CandyBalls] No usable font found; HUD text will not render.");

            return _font;
        }

        static Font TryBuiltin(string name)
        {
            try
            {
                return Resources.GetBuiltinResource<Font>(name);
            }
            catch
            {
                return null;
            }
        }
    }
}
