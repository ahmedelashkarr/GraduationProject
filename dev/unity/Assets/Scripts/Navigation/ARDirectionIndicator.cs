using UnityEngine;

namespace IndoorNav.Navigation
{
    /// <summary>
    /// Floats an arrow prefab in front of the AR camera and smoothly rotates it
    /// toward the center of the next zone in the active route. Hides itself
    /// automatically when the destination is reached or navigation is idle.
    /// </summary>
    public class ARDirectionIndicator : MonoBehaviour
    {
        [Tooltip("NavigationController that owns the active route.")]
        [SerializeField] private NavigationController navigationController;

        [Tooltip("AR camera transform. Falls back to Camera.main when unassigned.")]
        [SerializeField] private Transform userCamera;

        [Tooltip("Arrow prefab instantiated at Start. Leave null to use this GameObject as the arrow.")]
        [SerializeField] private GameObject arrowPrefab;

        [Tooltip("Distance in meters the arrow floats ahead of the camera.")]
        [SerializeField, Min(0.1f)] private float distanceFromCamera = 1.5f;

        [Tooltip("Vertical offset applied relative to the camera (negative = below eye level).")]
        [SerializeField] private float heightOffset = -0.25f;

        [Tooltip("Rotation smoothing speed (higher = snappier).")]
        [SerializeField, Min(0.1f)] private float rotationSpeed = 6f;

        [Tooltip("Additional rotation applied after the lookRotation, e.g. to lay a vertical arrow flat on the ground.")]
        [SerializeField] private Vector3 modelRotationOffsetEuler = Vector3.zero;

        [Tooltip("If true, the arrow points at the *next* zone (one step ahead). If false, it points at the current target.")]
        [SerializeField] private bool pointAtNextZone = false;

        private Transform _arrow;
        private bool _hookedEvents;

        private void Start()
        {
            if (userCamera == null && Camera.main != null)
                userCamera = Camera.main.transform;

            if (arrowPrefab != null)
            {
                _arrow = Instantiate(arrowPrefab, transform).transform;
            }
            else
            {
                _arrow = transform;
            }

            SetArrowActive(false);
            TryHookEvents();
        }

        private void OnEnable()  => TryHookEvents();
        private void OnDisable() => UnhookEvents();

        private void Update()
        {
            if (navigationController == null || userCamera == null || _arrow == null) return;

            Zone target = pointAtNextZone
                ? (navigationController.GetNextZone() ?? navigationController.GetCurrentZone())
                : navigationController.GetCurrentZone();

            if (target == null)
            {
                SetArrowActive(false);
                return;
            }

            SetArrowActive(true);

            Vector3 arrowPos = userCamera.position
                               + userCamera.forward * distanceFromCamera
                               + Vector3.up * heightOffset;
            _arrow.position = arrowPos;

            Vector3 direction = target.GetCenter() - _arrow.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.0001f)
            {
                Quaternion look = Quaternion.LookRotation(direction, Vector3.up);
                Quaternion desired = look * Quaternion.Euler(modelRotationOffsetEuler);
                _arrow.rotation = Quaternion.Slerp(_arrow.rotation, desired, rotationSpeed * Time.deltaTime);
            }
        }

        private void TryHookEvents()
        {
            if (_hookedEvents || navigationController == null) return;
            navigationController.OnDestinationReached.AddListener(HandleDestinationReached);
            _hookedEvents = true;
        }

        private void UnhookEvents()
        {
            if (!_hookedEvents || navigationController == null) return;
            navigationController.OnDestinationReached.RemoveListener(HandleDestinationReached);
            _hookedEvents = false;
        }

        private void HandleDestinationReached() => SetArrowActive(false);

        private void SetArrowActive(bool active)
        {
            if (_arrow == null) return;
            if (_arrow == transform)
            {
                foreach (Renderer r in GetComponentsInChildren<Renderer>())
                    r.enabled = active;
            }
            else if (_arrow.gameObject.activeSelf != active)
            {
                _arrow.gameObject.SetActive(active);
            }
        }
    }
}
