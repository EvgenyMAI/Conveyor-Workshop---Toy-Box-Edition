using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public static class InputCompat
{
    public static bool GetKeyDown(KeyCode keyCode)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        return keyCode switch
        {
            KeyCode.Space => keyboard.spaceKey.wasPressedThisFrame,
            KeyCode.Alpha1 => keyboard.digit1Key.wasPressedThisFrame,
            KeyCode.Alpha2 => keyboard.digit2Key.wasPressedThisFrame,
            KeyCode.Alpha3 => keyboard.digit3Key.wasPressedThisFrame,
            KeyCode.Alpha4 => keyboard.digit4Key.wasPressedThisFrame,
            KeyCode.Keypad1 => keyboard.numpad1Key.wasPressedThisFrame,
            KeyCode.Keypad2 => keyboard.numpad2Key.wasPressedThisFrame,
            KeyCode.Keypad3 => keyboard.numpad3Key.wasPressedThisFrame,
            KeyCode.LeftArrow => keyboard.leftArrowKey.wasPressedThisFrame,
            KeyCode.RightArrow => keyboard.rightArrowKey.wasPressedThisFrame,
            KeyCode.R => keyboard.rKey.wasPressedThisFrame,
            _ => false
        };
#else
        return Input.GetKeyDown(keyCode);
#endif
    }

    public static bool IsLaneKeyDown(int laneIndex, KeyCode overrideKey)
    {
        if (GetKeyDown(overrideKey))
        {
            return true;
        }

        return laneIndex switch
        {
            0 => GetKeyDown(KeyCode.Alpha1) || GetKeyDown(KeyCode.Keypad1),
            1 => GetKeyDown(KeyCode.Alpha2) || GetKeyDown(KeyCode.Keypad2),
            2 => GetKeyDown(KeyCode.Alpha3) || GetKeyDown(KeyCode.Keypad3),
            _ => false
        };
    }

    public static bool IsArrowStepDown(bool previousDirection, KeyCode overrideKey)
    {
        if (GetKeyDown(overrideKey))
        {
            return true;
        }

        return previousDirection ? GetKeyDown(KeyCode.LeftArrow) : GetKeyDown(KeyCode.RightArrow);
    }
}
