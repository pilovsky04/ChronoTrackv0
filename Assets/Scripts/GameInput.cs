using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Works with both the legacy Input Manager and the new Input System package.
public static class GameInput
{
    public static float Throttle
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            float v = 0f;
            Keyboard k = Keyboard.current;
            if (k != null)
            {
                if (k.wKey.isPressed || k.upArrowKey.isPressed) v += 1f;
                if (k.sKey.isPressed || k.downArrowKey.isPressed) v -= 1f;
            }
            Gamepad g = Gamepad.current;
            if (g != null) v += g.rightTrigger.ReadValue() - g.leftTrigger.ReadValue();
            return Mathf.Clamp(v, -1f, 1f);
#else
            return Mathf.Clamp(Input.GetAxisRaw("Vertical"), -1f, 1f);
#endif
        }
    }

    public static float Steer
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            float v = 0f;
            Keyboard k = Keyboard.current;
            if (k != null)
            {
                if (k.dKey.isPressed || k.rightArrowKey.isPressed) v += 1f;
                if (k.aKey.isPressed || k.leftArrowKey.isPressed) v -= 1f;
            }
            Gamepad g = Gamepad.current;
            if (g != null) v += g.leftStick.x.ReadValue();
            return Mathf.Clamp(v, -1f, 1f);
#else
            return Mathf.Clamp(Input.GetAxisRaw("Horizontal"), -1f, 1f);
#endif
        }
    }

    public static bool Brake
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            Gamepad g = Gamepad.current;
            return (k != null && k.spaceKey.isPressed) || (g != null && g.buttonSouth.isPressed);
#else
            return Input.GetKey(KeyCode.Space);
#endif
        }
    }

    public static bool ResetPressed
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            Gamepad g = Gamepad.current;
            return (k != null && k.rKey.wasPressedThisFrame) || (g != null && g.buttonNorth.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.R);
#endif
        }
    }

    public static bool PausePressed
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            Gamepad g = Gamepad.current;
            return (k != null && k.escapeKey.wasPressedThisFrame) || (g != null && g.startButton.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }
    }
}
