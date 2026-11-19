using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CandyBalls
{
    /// <summary>
    /// Thin wrapper over mouse/touch so the game works with either the old Input Manager or the
    /// new Input System package. It compiles and behaves correctly no matter which one the
    /// project has enabled (or if both are enabled).
    /// </summary>
    public static class PointerInput
    {
        /// <summary>Current pointer position in screen pixels.</summary>
        public static Vector2 Position
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Touchscreen touch = Touchscreen.current;
                if (touch != null && touch.primaryTouch.press.isPressed)
                    return touch.primaryTouch.position.ReadValue();

                Mouse mouse = Mouse.current;
                if (mouse != null)
                    return mouse.position.ReadValue();

                return Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.mousePosition;
#else
                return Vector2.zero;
#endif
            }
        }

        /// <summary>True on the frame the pointer went down.</summary>
        public static bool PressedDown
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Touchscreen touch = Touchscreen.current;
                if (touch != null && touch.primaryTouch.press.wasPressedThisFrame) return true;

                Mouse mouse = Mouse.current;
                return mouse != null && mouse.leftButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetMouseButtonDown(0);
#else
                return false;
#endif
            }
        }

        /// <summary>True while the pointer is held down.</summary>
        public static bool IsHeld
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Touchscreen touch = Touchscreen.current;
                if (touch != null && touch.primaryTouch.press.isPressed) return true;

                Mouse mouse = Mouse.current;
                return mouse != null && mouse.leftButton.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetMouseButton(0);
#else
                return false;
#endif
            }
        }

        /// <summary>True on the frame the pointer was released.</summary>
        public static bool Released
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Touchscreen touch = Touchscreen.current;
                if (touch != null && touch.primaryTouch.press.wasReleasedThisFrame) return true;

                Mouse mouse = Mouse.current;
                return mouse != null && mouse.leftButton.wasReleasedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetMouseButtonUp(0);
#else
                return false;
#endif
            }
        }

        /// <summary>R — restart the level.</summary>
        public static bool RestartPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Keyboard keyboard = Keyboard.current;
                return keyboard != null && keyboard.rKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetKeyDown(KeyCode.R);
#else
                return false;
#endif
            }
        }

        /// <summary>M — mute/unmute.</summary>
        public static bool MutePressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Keyboard keyboard = Keyboard.current;
                return keyboard != null && keyboard.mKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetKeyDown(KeyCode.M);
#else
                return false;
#endif
            }
        }
    }
}
