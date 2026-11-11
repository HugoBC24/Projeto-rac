using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ProjectRac.Player
{
    public class PlayerInputHandler : MonoBehaviour
    {
        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool JumpTriggered { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool InteractTriggered { get; private set; }
        public bool PrimaryActionTriggered { get; private set; }

        private void Update()
        {
            ReadInputs();
        }

        private void LateUpdate()
        {
            // Clear one-shot triggers at the end of the frame
            JumpTriggered = false;
            InteractTriggered = false;
            PrimaryActionTriggered = false;
        }

        private void ReadInputs()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            if (keyboard != null)
            {
                float x = 0f;
                float y = 0f;

                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;

                MoveInput = new Vector2(x, y).normalized;
                IsSprinting = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;

                if (keyboard.spaceKey.wasPressedThisFrame)
                {
                    JumpTriggered = true;
                }

                if (keyboard.eKey.wasPressedThisFrame)
                {
                    InteractTriggered = true;
                }
            }

            if (mouse != null)
            {
                LookInput = mouse.delta.ReadValue();

                if (mouse.leftButton.wasPressedThisFrame)
                {
                    PrimaryActionTriggered = true;
                }
            }
#else
            float x = Input.GetAxisRaw("Horizontal");
            float y = Input.GetAxisRaw("Vertical");
            MoveInput = new Vector2(x, y).normalized;

            LookInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 10f;
            IsSprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (Input.GetKeyDown(KeyCode.Space))
            {
                JumpTriggered = true;
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                InteractTriggered = true;
            }

            if (Input.GetMouseButtonDown(0))
            {
                PrimaryActionTriggered = true;
            }
#endif
        }
    }
}
