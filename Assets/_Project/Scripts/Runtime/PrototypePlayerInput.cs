using UnityEngine;

namespace KimSurvival
{
    public enum PrototypeInputDevice
    {
        KeyboardMouse,
        Gamepad
    }

    public readonly struct PrototypeInputActivity
    {
        public PrototypeInputActivity(bool keyboardMouse, bool gamepad)
        {
            KeyboardMouse = keyboardMouse;
            Gamepad = gamepad;
        }

        public bool KeyboardMouse { get; }
        public bool Gamepad { get; }
    }

    public sealed class PrototypeInputDeviceTracker
    {
        public PrototypeInputDevice ActiveDevice { get; private set; } = PrototypeInputDevice.KeyboardMouse;

        public void Update(PrototypeInputActivity activity)
        {
            if (activity.Gamepad)
            {
                ActiveDevice = PrototypeInputDevice.Gamepad;
            }
            else if (activity.KeyboardMouse)
            {
                ActiveDevice = PrototypeInputDevice.KeyboardMouse;
            }
        }
    }

    public static class PrototypeInputPromptKeys
    {
        public static string Camp(PrototypeInputDevice device)
        {
            return device == PrototypeInputDevice.Gamepad ? "controls.camp.gamepad" : "controls.camp.keyboard_mouse";
        }

        public static string Placement(PrototypeInputDevice device)
        {
            return device == PrototypeInputDevice.Gamepad ? "controls.placement.gamepad" : "controls.placement.keyboard_mouse";
        }

        public static string CampPopup(PrototypeInputDevice device)
        {
            return device == PrototypeInputDevice.Gamepad ? "controls.camp.popup.gamepad" : "controls.camp.popup.keyboard_mouse";
        }

        public static string CampProximity(PrototypeInputDevice device)
        {
            return device == PrototypeInputDevice.Gamepad ? "camp.interaction.prompt.gamepad" : "camp.interaction.prompt.keyboard_mouse";
        }

        public static string InteractGlyph(PrototypeInputDevice device)
        {
            return device == PrototypeInputDevice.Gamepad ? "input.glyph.interact.gamepad" : "input.glyph.interact.keyboard_mouse";
        }

        public static string CampModulePreview(PrototypeInputDevice device)
        {
            return device == PrototypeInputDevice.Gamepad ? "controls.module_preview.gamepad" : "controls.module_preview.keyboard_mouse";
        }

        public static string ExpeditionMap(PrototypeInputDevice device)
        {
            return device == PrototypeInputDevice.Gamepad ? "controls.expedition_map.gamepad" : "controls.expedition_map.keyboard_mouse";
        }

        public static string Explore(PrototypeInputDevice device)
        {
            return device == PrototypeInputDevice.Gamepad ? "controls.explore.gamepad" : "controls.explore.keyboard_mouse";
        }
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

    public struct PrototypeRawCampPlacementInput
    {
        public bool UsePointer;
        public float PointerWorldX;
        public float HorizontalAxis;
        public bool MouseConfirm;
        public bool KeyboardConfirm;
        public bool GamepadConfirm;
        public bool MouseCancel;
        public bool KeyboardCancel;
        public bool GamepadCancel;
    }

    public readonly struct PrototypeCampPlacementActions
    {
        public PrototypeCampPlacementActions(bool usePointer, float pointerWorldX, float horizontal, bool confirmPressed, bool cancelPressed)
        {
            UsePointer = usePointer;
            PointerWorldX = pointerWorldX;
            Horizontal = horizontal;
            ConfirmPressed = confirmPressed;
            CancelPressed = cancelPressed;
        }

        public bool UsePointer { get; }
        public float PointerWorldX { get; }
        public float Horizontal { get; }
        public bool ConfirmPressed { get; }
        public bool CancelPressed { get; }

        public static PrototypeCampPlacementActions FromRaw(PrototypeRawCampPlacementInput raw)
        {
            return new PrototypeCampPlacementActions(
                raw.UsePointer,
                raw.PointerWorldX,
                Mathf.Clamp(raw.HorizontalAxis, -1f, 1f),
                raw.MouseConfirm || raw.KeyboardConfirm || raw.GamepadConfirm,
                raw.MouseCancel || raw.KeyboardCancel || raw.GamepadCancel);
        }
    }

    public struct PrototypeRawCampModulePreviewInput
    {
        public float HorizontalAxis;
        public bool KeyboardPrevious;
        public bool KeyboardNext;
        public bool MouseConfirm;
        public bool KeyboardConfirm;
        public bool GamepadConfirm;
        public bool MouseCancel;
        public bool KeyboardCancel;
        public bool GamepadCancel;
    }

    public readonly struct PrototypeCampModulePreviewActions
    {
        public PrototypeCampModulePreviewActions(int cycleDirection, bool confirmPressed, bool cancelPressed)
        {
            CycleDirection = cycleDirection;
            ConfirmPressed = confirmPressed;
            CancelPressed = cancelPressed;
        }

        public int CycleDirection { get; }
        public bool ConfirmPressed { get; }
        public bool CancelPressed { get; }

        public static PrototypeCampModulePreviewActions FromRaw(PrototypeRawCampModulePreviewInput raw)
        {
            float horizontal = raw.HorizontalAxis;
            if (raw.KeyboardPrevious)
            {
                horizontal = -1f;
            }
            else if (raw.KeyboardNext)
            {
                horizontal = 1f;
            }

            return new PrototypeCampModulePreviewActions(
                horizontal < -0.5f ? -1 : horizontal > 0.5f ? 1 : 0,
                raw.MouseConfirm || raw.KeyboardConfirm || raw.GamepadConfirm,
                raw.MouseCancel || raw.KeyboardCancel || raw.GamepadCancel);
        }
    }

    public struct PrototypeRawExpeditionMapInput
    {
        public float HorizontalAxis;
        public float VerticalAxis;
        public bool KeyboardPrevious;
        public bool KeyboardNext;
        public bool KeyboardConfirm;
        public bool GamepadConfirm;
        public bool KeyboardCancel;
        public bool GamepadCancel;
    }

    public readonly struct PrototypeExpeditionMapActions
    {
        public PrototypeExpeditionMapActions(int cycleDirection, bool confirmPressed, bool cancelPressed)
        {
            CycleDirection = cycleDirection;
            ConfirmPressed = confirmPressed;
            CancelPressed = cancelPressed;
        }

        public int CycleDirection { get; }
        public bool ConfirmPressed { get; }
        public bool CancelPressed { get; }

        public static PrototypeExpeditionMapActions FromRaw(PrototypeRawExpeditionMapInput raw)
        {
            float navigation = Mathf.Abs(raw.VerticalAxis) > Mathf.Abs(raw.HorizontalAxis)
                ? -raw.VerticalAxis
                : raw.HorizontalAxis;
            if (raw.KeyboardPrevious)
            {
                navigation = -1f;
            }
            else if (raw.KeyboardNext)
            {
                navigation = 1f;
            }

            return new PrototypeExpeditionMapActions(
                navigation < -0.5f ? -1 : navigation > 0.5f ? 1 : 0,
                raw.KeyboardConfirm || raw.GamepadConfirm,
                raw.KeyboardCancel || raw.GamepadCancel);
        }
    }

    public struct PrototypeRawSystemInput
    {
        public bool KeyboardLanguage;
        public bool GamepadLanguage;
    }

    public readonly struct PrototypeSystemActions
    {
        public PrototypeSystemActions(bool languagePressed)
        {
            LanguagePressed = languagePressed;
        }

        public bool LanguagePressed { get; }

        public static PrototypeSystemActions FromRaw(PrototypeRawSystemInput raw)
        {
            return new PrototypeSystemActions(raw.KeyboardLanguage || raw.GamepadLanguage);
        }
    }

    public sealed class LegacyPrototypePlayerInput
    {
        private readonly PrototypeInputDeviceTracker deviceTracker = new PrototypeInputDeviceTracker();

        public PrototypeInputDevice ActiveDevice
        {
            get { return deviceTracker.ActiveDevice; }
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

            bool keyboardDirection = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) ||
                                     Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow);
            string[] joystickNames = Input.GetJoystickNames();
            bool gamepadAxis = joystickNames.Length > 0 && !keyboardDirection &&
                               (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.2f || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.2f);
            bool bagNumber = false;
            for (int i = 0; i < GameSession.MaximumBagSlotCount; i += 1)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)))
                {
                    bagNumber = true;
                    break;
                }
            }

            bool keyboard = keyboardDirection || bagNumber || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Space) ||
                             Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) ||
                            Mathf.Abs(Input.GetAxisRaw("Mouse X")) > 0.01f || Mathf.Abs(Input.GetAxisRaw("Mouse Y")) > 0.01f;
            deviceTracker.Update(new PrototypeInputActivity(keyboard, gamepad || gamepadAxis));
        }

        public PrototypePlayerActions ReadActions(bool choosingLoot, int activeBagSlotCount)
        {
            int bagSlotIndex = -1;
            if (choosingLoot)
            {
                int selectableSlotCount = Mathf.Clamp(activeBagSlotCount, 0, GameSession.MaximumBagSlotCount);
                for (int i = 0; i < selectableSlotCount; i += 1)
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

        public PrototypeCampPlacementActions ReadCampPlacementActions(Camera worldCamera)
        {
            Vector3 pointerWorld = worldCamera.ScreenToWorldPoint(Input.mousePosition);
            PrototypeRawCampPlacementInput raw = new PrototypeRawCampPlacementInput
            {
                UsePointer = ActiveDevice == PrototypeInputDevice.KeyboardMouse,
                PointerWorldX = pointerWorld.x,
                HorizontalAxis = Input.GetAxisRaw("Horizontal"),
                MouseConfirm = Input.GetMouseButtonDown(0),
                KeyboardConfirm = Input.GetKeyDown(KeyCode.Return),
                GamepadConfirm = Input.GetKeyDown(KeyCode.JoystickButton0),
                MouseCancel = Input.GetMouseButtonDown(1),
                KeyboardCancel = Input.GetKeyDown(KeyCode.Escape),
                GamepadCancel = Input.GetKeyDown(KeyCode.JoystickButton1)
            };
            return PrototypeCampPlacementActions.FromRaw(raw);
        }

        public PrototypeCampModulePreviewActions ReadCampModulePreviewActions()
        {
            return PrototypeCampModulePreviewActions.FromRaw(new PrototypeRawCampModulePreviewInput
            {
                HorizontalAxis = Input.GetAxisRaw("Horizontal"),
                KeyboardPrevious = Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A),
                KeyboardNext = Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D),
                MouseConfirm = Input.GetMouseButtonDown(0),
                KeyboardConfirm = Input.GetKeyDown(KeyCode.Return),
                GamepadConfirm = Input.GetKeyDown(KeyCode.JoystickButton0),
                MouseCancel = Input.GetMouseButtonDown(1),
                KeyboardCancel = Input.GetKeyDown(KeyCode.Escape),
                GamepadCancel = Input.GetKeyDown(KeyCode.JoystickButton1)
            });
        }

        public PrototypeExpeditionMapActions ReadExpeditionMapActions()
        {
            return PrototypeExpeditionMapActions.FromRaw(new PrototypeRawExpeditionMapInput
            {
                HorizontalAxis = Input.GetAxisRaw("Horizontal"),
                VerticalAxis = Input.GetAxisRaw("Vertical"),
                KeyboardPrevious = Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.W),
                KeyboardNext = Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.S),
                KeyboardConfirm = Input.GetKeyDown(KeyCode.Return),
                GamepadConfirm = Input.GetKeyDown(KeyCode.JoystickButton0),
                KeyboardCancel = Input.GetKeyDown(KeyCode.Escape),
                GamepadCancel = Input.GetKeyDown(KeyCode.JoystickButton1)
            });
        }

        public PrototypeSystemActions ReadSystemActions()
        {
            return PrototypeSystemActions.FromRaw(new PrototypeRawSystemInput
            {
                KeyboardLanguage = Input.GetKeyDown(KeyCode.F1),
                GamepadLanguage = Input.GetKeyDown(KeyCode.JoystickButton3)
            });
        }
    }
}
