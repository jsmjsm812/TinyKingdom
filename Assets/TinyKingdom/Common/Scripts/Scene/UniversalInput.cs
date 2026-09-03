using UnityEngine;

#if ENABLE_INPUT_SYSTEM && USE_NEW_INPUT_SYSTEM

using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

#endif

namespace TinyKingdom.Common.Scripts.Scene
{
    /// <summary>
    /// This class is used to handle both Input Systems, Legacy and New.
    /// </summary>
    public static class UniversalInput
    {
        public static Vector3 mousePosition
        {
            get
            {
                #if ENABLE_INPUT_SYSTEM && USE_NEW_INPUT_SYSTEM

                var mouse = Mouse.current;

                if (mouse != null)
                {
                    return mouse.position.ReadValue();
                }

                return Vector3.zero;

                #elif ENABLE_LEGACY_INPUT_MANAGER

                return Input.mousePosition;

                #else

                return Vector3.zero;

                #endif
            }
        }

        public static bool GetMouseButtonDown(int button)
        {
            #if ENABLE_INPUT_SYSTEM && USE_NEW_INPUT_SYSTEM

            var buttonControl = GetMouseControl(button);

            return buttonControl is { wasPressedThisFrame: true };

            #elif ENABLE_LEGACY_INPUT_MANAGER

            return Input.GetMouseButtonDown(button);

            #else

            return false;

            #endif
        }

        public static bool GetMouseButton(int button)
        {
            #if ENABLE_INPUT_SYSTEM && USE_NEW_INPUT_SYSTEM

            var buttonControl = GetMouseControl(button);

            return buttonControl is { isPressed: true };

            #elif ENABLE_LEGACY_INPUT_MANAGER

            return Input.GetMouseButton(button);

            #else

            return false;

            #endif
        }

        #if ENABLE_INPUT_SYSTEM && USE_NEW_INPUT_SYSTEM
        
        private static ButtonControl GetMouseControl(int button)
        {
            var mouse = Mouse.current;

            if (mouse == null) return null;

            return button switch
            {
                0 => mouse.leftButton,
                1 => mouse.rightButton,
                2 => mouse.middleButton,
                _ => null
            };
        }

        #endif

        public static bool GetKeyDown(KeyCode keyCode)
        {
            #if ENABLE_INPUT_SYSTEM && USE_NEW_INPUT_SYSTEM

            var keyboard = Keyboard.current;

            if (keyboard == null) return false;

            var newKey = ConvertKeyCodeToInputKey(keyCode);

            return newKey != Key.None && keyboard[newKey].wasPressedThisFrame;

            #elif ENABLE_LEGACY_INPUT_MANAGER

            return Input.GetKeyDown(keyCode);

            #else

            return false;

            #endif
        }

        public static bool GetKey(KeyCode keyCode)
        {
            #if ENABLE_INPUT_SYSTEM && USE_NEW_INPUT_SYSTEM
            
            var keyboard = Keyboard.current;
            
            if (keyboard == null) return false;

            var newKey = ConvertKeyCodeToInputKey(keyCode);
            
            return newKey != Key.None && keyboard[newKey].isPressed;

            #elif ENABLE_LEGACY_INPUT_MANAGER

            return Input.GetKey(keyCode);

            #else

            return false;

            #endif
        }

        public static bool GetKeyUp(KeyCode keyCode)
        {
            #if ENABLE_INPUT_SYSTEM && USE_NEW_INPUT_SYSTEM
            
            var keyboard = Keyboard.current;
            
            if (keyboard == null) return false;

            var newKey = ConvertKeyCodeToInputKey(keyCode);
            
            return newKey != Key.None && keyboard[newKey].wasReleasedThisFrame;

            #elif ENABLE_LEGACY_INPUT_MANAGER

            return Input.GetKeyUp(keyCode);

            #else

            return false;

            #endif
        }                                             

        #if ENABLE_INPUT_SYSTEM && USE_NEW_INPUT_SYSTEM

        private static Key ConvertKeyCodeToInputKey(KeyCode keyCode)
        {
            if (keyCode is >= KeyCode.A and <= KeyCode.Z)
            {
                if (System.Enum.TryParse(keyCode.ToString(), out Key resultKey)) return resultKey;
            }

            return keyCode switch
            {
                KeyCode.Alpha0 => Key.Digit0,
                KeyCode.Alpha1 => Key.Digit1,
                KeyCode.Alpha2 => Key.Digit2,
                KeyCode.Alpha3 => Key.Digit3,
                KeyCode.Alpha4 => Key.Digit4,
                KeyCode.Alpha5 => Key.Digit5,
                KeyCode.Alpha6 => Key.Digit6,
                KeyCode.Alpha7 => Key.Digit7,
                KeyCode.Alpha8 => Key.Digit8,
                KeyCode.Alpha9 => Key.Digit9,
                KeyCode.LeftArrow => Key.LeftArrow,
                KeyCode.RightArrow => Key.RightArrow,
                KeyCode.UpArrow => Key.UpArrow,
                KeyCode.DownArrow => Key.DownArrow,
                KeyCode.Space => Key.Space,
                KeyCode.Escape => Key.Escape,
                KeyCode.Return => Key.Enter,
                KeyCode.LeftShift => Key.LeftShift,
                KeyCode.RightShift => Key.RightShift,
                KeyCode.LeftControl => Key.LeftCtrl,
                KeyCode.RightControl => Key.RightCtrl,
                KeyCode.LeftAlt => Key.LeftAlt,
                KeyCode.RightAlt => Key.RightAlt,
                _ => Key.None
            };
        }

        #endif
    }
}