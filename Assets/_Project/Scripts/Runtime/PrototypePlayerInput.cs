using UnityEngine;

namespace KimSurvival
{
    public enum PrototypeInputDevice
    {
        KeyboardMouse,
        Gamepad
    }

    public struct PrototypeRawInput
    {
        public float HorizontalAxis;
        public bool KeyboardLeft;
        public bool KeyboardRight;
        public bool MappedJump;
        public bool KeyboardJump;
        public bool GamepadJump;
        public bool KeyboardInteract;
        public bool GamepadInteract;
        public bool KeyboardReturn;
        public bool GamepadReturn;
        public bool KeyboardCancel;
        public bool GamepadCancel;
        public int BagSlotIndex;
    }

    public readonly struct PrototypePlayerActions
    {
        public PrototypePlayerActions(float horizontal, bool jumpPressed, bool interactPressed, bool returnPressed, bool cancelPressed, int bagSlotIndex)
        {
            Horizontal = horizontal;
            JumpPressed = jumpPressed;
            InteractPressed = interactPressed;
            ReturnPressed = returnPressed;
            CancelPressed = cancelPressed;
            BagSlotIndex = bagSlotIndex;
        }

        public float Horizontal { get; }
        public bool JumpPressed { get; }
        public bool InteractPressed { get; }
        public bool ReturnPressed { get; }
        public bool CancelPressed { get; }
        public int BagSlotIndex { get; }

        public static PrototypePlayerActions FromRaw(PrototypeRawInput raw)
        {
            float horizontal = raw.HorizontalAxis;
            if (Mathf.Abs(horizontal) < 0.01f)
            {
                if (raw.KeyboardLeft)
                {
                    horizontal = -1f;
                }

                if (raw.KeyboardRight)
                {
                    horizontal = 1f;
                }
            }

            return new PrototypePlayerActions(
                Mathf.Clamp(horizontal, -1f, 1f),
                raw.MappedJump || raw.KeyboardJump || raw.GamepadJump,
                raw.KeyboardInteract || raw.GamepadInteract,
                raw.KeyboardReturn || raw.GamepadReturn,
                raw.KeyboardCancel || raw.GamepadCancel,
                raw.BagSlotIndex);
        }
    }

    public sealed class LegacyPrototypePlayerInput
    {
        public PrototypeInputDevice ActiveDevice { get; private set; } = PrototypeInputDevice.KeyboardMouse;

        public string ActiveDeviceLabel
        {
            get { return ActiveDevice == PrototypeInputDevice.Gamepad ? "게임패드" : "키보드·마우스"; }
        }

        public void PollActiveDevice()
        {
            bool gamepad = false;
            for (int i = 0; i <= 15; i += 1)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.JoystickButton0 + i)))
                {
                    gamepad = true;
                    break;
                }
            }

            bool keyboard = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.W) ||
                            Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Space) ||
                            Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0);
            if (gamepad)
            {
                ActiveDevice = PrototypeInputDevice.Gamepad;
            }
            else if (keyboard)
            {
                ActiveDevice = PrototypeInputDevice.KeyboardMouse;
            }
        }

        public PrototypePlayerActions ReadActions(bool choosingLoot)
        {
            int bagSlotIndex = -1;
            if (choosingLoot)
            {
                for (int i = 0; i < GameSession.BagSlotCount; i += 1)
                {
                    if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)))
                    {
                        bagSlotIndex = i;
                        break;
                    }
                }
            }

            PrototypeRawInput raw = new PrototypeRawInput
            {
                // The legacy Horizontal mapping combines keyboard and joystick axes.
                HorizontalAxis = Input.GetAxisRaw("Horizontal"),
                KeyboardLeft = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow),
                KeyboardRight = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow),
                MappedJump = Input.GetButtonDown("Jump"),
                KeyboardJump = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow),
                GamepadJump = Input.GetKeyDown(KeyCode.JoystickButton0),
                KeyboardInteract = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F),
                GamepadInteract = Input.GetKeyDown(KeyCode.JoystickButton2),
                KeyboardReturn = Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Escape),
                GamepadReturn = Input.GetKeyDown(KeyCode.JoystickButton1),
                KeyboardCancel = Input.GetKeyDown(KeyCode.Escape),
                GamepadCancel = Input.GetKeyDown(KeyCode.JoystickButton1),
                BagSlotIndex = bagSlotIndex
            };
            return PrototypePlayerActions.FromRaw(raw);
        }
    }
}
