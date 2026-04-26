using UnityEngine;

namespace IndoorNav.Navigation
{
    /// <summary>
    /// WASD + mouse movement controller for testing navigation in the Editor.
    /// Only active when running in the Editor (or a desktop build) — does
    /// nothing on real AR devices, so it won't interfere with AR Foundation
    /// camera tracking.
    /// </summary>
    public class EditorUserController : MonoBehaviour
    {
        [Tooltip("Transform to move. Usually the Main Camera under XR Origin.")]
        [SerializeField] private Transform target;

        [Tooltip("Movement speed in meters per second.")]
        [SerializeField] private float moveSpeed = 2.5f;

        [Tooltip("Mouse look sensitivity.")]
        [SerializeField] private float lookSensitivity = 2f;

        [Tooltip("If true, hold right mouse button to look around. If false, look is always active.")]
        [SerializeField] private bool requireRightMouseToLook = true;

        [Tooltip("Lock camera Y to this height (useful for indoor walking).")]
        [SerializeField] private bool lockHeight = true;

        [Tooltip("The fixed camera height when lockHeight is on (typical eye level ≈ 1.6m).")]
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

            // Initialize yaw/pitch from current rotation
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
            bool looking = !requireRightMouseToLook || Input.GetMouseButton(1);
            if (!looking) return;

            _yaw   += Input.GetAxis("Mouse X") * lookSensitivity;
            _pitch -= Input.GetAxis("Mouse Y") * lookSensitivity;
            _pitch = Mathf.Clamp(_pitch, -89f, 89f);

            target.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private void HandleMovement()
        {
            float h = Input.GetAxis("Horizontal"); // A/D
            float v = Input.GetAxis("Vertical");   // W/S
            if (Mathf.Approximately(h, 0f) && Mathf.Approximately(v, 0f)) return;

            // Move on the horizontal plane (ignore camera pitch when moving)
            Vector3 forward = target.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 right = target.right;
            right.y = 0f;
            right.Normalize();

            Vector3 delta = (forward * v + right * h) * (moveSpeed * Time.deltaTime);
            target.position += delta;

            if (lockHeight)
            {
                Vector3 p = target.position;
                p.y = fixedHeight;
                target.position = p;
            }
        }
    }
}