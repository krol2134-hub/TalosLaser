using UnityEngine;

namespace TalosTest.Character
{
    public class PlayerInput
    {
        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public float Zoom { get; private set; }
        public bool Jump { get; private set; }
        public bool Interact { get; private set; }
        public bool ToggleZoom { get; private set; }

        public void Update()
        {
            ReadMovement();
            ReadLook();
            ReadActions();
        }

        private void ReadMovement()
        {
            var moveX = Input.GetAxisRaw(InputAxisKeys.Horizontal);
            var moveY = Input.GetAxisRaw(InputAxisKeys.Vertical);
            Move = new Vector2(moveX, moveY);
        }

        private void ReadLook()
        {
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                Look = Vector2.zero;
            }
            else
            {
                var lookX = Input.GetAxisRaw(InputAxisKeys.MouseX);
                var lookY = Input.GetAxisRaw(InputAxisKeys.MouseY);
                Look = new Vector2(lookX, lookY);
            }

#if UNITY_WEBGL
            Zoom = 0f;
#else
            Zoom = -Input.GetAxis(InputAxisKeys.MouseScroll);
#endif
        }

        private void ReadActions()
        {
            Jump = Input.GetKeyDown(KeyCode.Space);
            Interact = Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0);
            ToggleZoom = Input.GetMouseButtonDown(1);

            if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}