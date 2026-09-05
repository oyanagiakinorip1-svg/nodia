using UnityEngine;
using UnityEngine.InputSystem;

namespace Nodia.Player
{
    // Free-flying FPS camera for browsing the note space: WASD to move, mouse to
    // look, Space/Ctrl to rise and sink. No gravity - this is a spectator-style
    // camera, not a platformer character.
    [RequireComponent(typeof(CharacterController))]
    public class FPSController : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float verticalSpeed = 4f;
        [SerializeField] private float mouseSensitivity = 0.25f;

        private const string PrefMoveSpeed = "nodia_move_speed";
        private const string PrefVerticalSpeed = "nodia_vertical_speed";
        private const string PrefMouseSensitivity = "nodia_mouse_sensitivity";

        private CharacterController controller;
        private float pitch;
        private bool cursorLocked = true;

        public float MoveSpeed
        {
            get => moveSpeed;
            set { moveSpeed = value; PlayerPrefs.SetFloat(PrefMoveSpeed, value); }
        }

        public float VerticalSpeed
        {
            get => verticalSpeed;
            set { verticalSpeed = value; PlayerPrefs.SetFloat(PrefVerticalSpeed, value); }
        }

        public float MouseSensitivity
        {
            get => mouseSensitivity;
            set { mouseSensitivity = value; PlayerPrefs.SetFloat(PrefMouseSensitivity, value); }
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            moveSpeed = PlayerPrefs.GetFloat(PrefMoveSpeed, moveSpeed);
            verticalSpeed = PlayerPrefs.GetFloat(PrefVerticalSpeed, verticalSpeed);
            mouseSensitivity = PlayerPrefs.GetFloat(PrefMouseSensitivity, mouseSensitivity);
            SetCursorLocked(true);
        }

        private void Update()
        {
            // The menu-open key (Tab) is handled centrally by
            // MainMenuController (which calls SetCursorLocked itself) -
            // handling it here too would double up and risk unlocking the
            // cursor without actually opening the menu.
            if (!cursorLocked) return;

            HandleLook();
            HandleMove();
        }

        // Called by the UI (note panel, connection anchors) when they need the
        // cursor free for point-and-click, and again when they close.
        public void SetCursorLocked(bool locked)
        {
            cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        // WebGL reports pointer-lock mouse delta in physical device pixels
        // rather than the OS-level points other platforms use, so on a
        // HiDPI/Retina display (devicePixelRatio 2) it arrives roughly 4x
        // stronger than the same physical mouse movement would elsewhere.
        private const float WebGLDeltaCorrection = 0.25f;

        private void HandleLook()
        {
            Vector2 rawDelta = Mouse.current.delta.ReadValue();
#if UNITY_WEBGL && !UNITY_EDITOR
            rawDelta *= WebGLDeltaCorrection;
#endif
            Vector2 delta = rawDelta * mouseSensitivity;
            transform.Rotate(Vector3.up, delta.x);
            pitch = Mathf.Clamp(pitch - delta.y, -85f, 85f);
            playerCamera.transform.localEulerAngles = new Vector3(pitch, 0f, 0f);
        }

        private void HandleMove()
        {
            var keyboard = Keyboard.current;
            Vector3 input = Vector3.zero;
            if (keyboard.wKey.isPressed) input += Vector3.forward;
            if (keyboard.sKey.isPressed) input += Vector3.back;
            if (keyboard.aKey.isPressed) input += Vector3.left;
            if (keyboard.dKey.isPressed) input += Vector3.right;

            Vector3 move = transform.TransformDirection(input.normalized) * moveSpeed;

            float vertical = 0f;
            if (keyboard.spaceKey.isPressed) vertical += verticalSpeed;
            if (keyboard.ctrlKey.isPressed) vertical -= verticalSpeed;
            move.y = vertical;

            controller.Move(move * Time.deltaTime);
        }
    }
}
