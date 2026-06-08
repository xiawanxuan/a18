using UnityEngine;

namespace SoftFluidPuzzle.PlayerControl
{
    public class PlayerInput : MonoBehaviour
    {
        [Header("Input Axes")]
        public string horizontalAxis = "Horizontal";
        public string verticalAxis = "Vertical";
        public string jumpButton = "Jump";
        public string interactButton = "Fire1";
        public string mouseXAxis = "Mouse X";
        public string mouseYAxis = "Mouse Y";

        [Header("Settings")]
        public bool enableMouseLook = true;
        public bool invertYAxis = false;

        public Vector2 MovementInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool InteractHeld { get; private set; }

        private void Update()
        {
            float horizontal = Input.GetAxis(horizontalAxis);
            float vertical = Input.GetAxis(verticalAxis);
            MovementInput = new Vector2(horizontal, vertical);

            if (enableMouseLook)
            {
                float mouseX = Input.GetAxis(mouseXAxis);
                float mouseY = Input.GetAxis(mouseYAxis);
                if (invertYAxis) mouseY = -mouseY;
                LookInput = new Vector2(mouseX, mouseY);
            }
            else
            {
                LookInput = Vector2.zero;
            }

            JumpPressed = Input.GetButtonDown(jumpButton);
            JumpHeld = Input.GetButton(jumpButton);

            InteractPressed = Input.GetButtonDown(interactButton);
            InteractHeld = Input.GetButton(interactButton);
        }
    }
}
