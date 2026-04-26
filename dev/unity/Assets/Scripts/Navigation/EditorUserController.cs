using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace IndoorNav.Navigation
{
    /// <summary>
    /// WASD + mouse movement controller for testing navigation in the Editor.
    /// Compatible with both the legacy Input Manager and the new Input System package.
    /// Only intended for Editor / desktop testing.
    /// </summary>
    public class EditorUserController : MonoBehaviour
    {
        [Tooltip("Transform to move. Usually the XR Origin (so the AR camera follows).")]
        [SerializeField] private Transform target;

        [Tooltip("Movement speed in meters per second.")]
        [SerializeField] private float moveSpeed = 2.5f;

        [Tooltip("Mouse look sensitivity.")]
        [SerializeField] private float lookSensitivity = 0.15f;

        [Tooltip("If true, hold right mouse button to look around. If false, look is always active.")]
        [SerializeField] private bool requireRightMouseToLook = true;

        [Tooltip("Lock target Y to a fixed height (useful for indoor walking).")]
        [SerializeField] private bool lockHeight = true;

        [Tooltip("The fixed height when lockHeight is on (typical eye level ≈ 1.6m).")]
        [SerializeField] private float fixedHeight = 1.6f;

        private float _yaw;
        private float _pitch;

        private void Start()
        {
            if (target == null)
            {
                Debug.LogWarning("[EditorUserController] No target assigned.");
                enabled = false;
                return;
            }

            Vector3 e = target.eulerAngles;
            _yaw   = e.y;
            _pitch = e.x;
        }

        private void Update()
        {
            if (target == null) return;
            HandleLook();
            HandleMovement();
        }

        private void HandleLook()
        {
            bool looking = !requireRightMouseToLook || GetRightMouseHeld();
            if (!looking) return;

            Vector2 mouseDelta = GetMouseDelta();

            _yaw   += mouseDelta.x * lookSensitivity;
            _pitch -= mouseDelta.y * lookSensitivity;
            _pitch = Mathf.Clamp(_pitch, -89f, 89f);

            target.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private void HandleMovement()
        {
            Vector2 move = GetMoveAxis();
            if (move.sqrMagnitude < 0.0001f) return;

            Vector3 forward = target.forward; forward.y = 0f; forward.Normalize();
            Vector3 right   = target.right;   right.y   = 0f; right.Normalize();

            Vector3 delta = (forward * move.y + right * move.x) * (moveSpeed * Time.deltaTime);
            target.position += delta;

            if (lockHeight)
            {
                Vector3 p = target.position;
                p.y = fixedHeight;
                target.position = p;
            }
        }

        // ---------- Input abstraction (works with either system) ----------

        private Vector2 GetMoveAxis()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return Vector2.zero;
            float x = 0f, y = 0f;
            if (Keyboard.current.aKey.isPressed) x -= 1f;
            if (Keyboard.current.dKey.isPressed) x += 1f;
            if (Keyboard.current.sKey.isPressed) y -= 1f;
            if (Keyboard.current.wKey.isPressed) y += 1f;
            return new Vector2(x, y);
#else
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
        }

        private Vector2 GetMouseDelta()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
#else
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
        }

        private bool GetRightMouseHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.rightButton.isPressed;
#else
            return Input.GetMouseButton(1);
#endif
        }
    }
}