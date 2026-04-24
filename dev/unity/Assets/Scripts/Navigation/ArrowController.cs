using UnityEngine;

namespace Navigation
{
    /// <summary>
    /// Positions and orients a single arrow prefab in front of the user's camera,
    /// lying flat on the ground and pointing toward the current navigation waypoint
    /// with a subtle bobbing effect.
    /// Attach to ArrowManager.
    /// </summary>
    public class ArrowController : MonoBehaviour
    {
        [SerializeField] private GameObject arrowPrefab;
        [SerializeField] private Transform userCamera;
        [SerializeField] private WaypointManager waypointManager;
        [SerializeField] private float distanceFromUser = 1.5f;
        [SerializeField] private float heightOffset = -0.5f;
        [SerializeField] private float rotationSpeed = 5f;
        [SerializeField] private float offPathThreshold = 3f;

        // Tip-forward angle to lay the vertical arrow flat on the ground.
        // Adjust if the arrow appears upside-down (try -90f) or wrong-facing.
        [SerializeField] private float groundTiltAngle = 90f;

        private const float RotationSpeed = 5f;

        private Transform _arrowTransform;
        private bool _wasOffPath;

        /// <summary>Fires once when the user strays farther than offPathThreshold from the current waypoint.</summary>
        public static event System.Action OnOffPath;

        private void Start()
        {
            if (arrowPrefab == null)
            {
                Debug.LogWarning("[ArrowController] Arrow prefab is not assigned.");
                return;
            }

            if (userCamera == null)
                userCamera = Camera.main?.transform;

            _arrowTransform = Instantiate(arrowPrefab, Vector3.zero, Quaternion.identity).transform;
            _arrowTransform.gameObject.SetActive(false);

            WaypointManager.OnDestinationReached += HideArrow;
        }

        private void OnDestroy() => WaypointManager.OnDestinationReached -= HideArrow;

        private void Update()
        {
            if (_arrowTransform == null || waypointManager == null || userCamera == null) return;

            if (!waypointManager.HasPath())
            {
                _arrowTransform.gameObject.SetActive(false);
                return;
            }

            _arrowTransform.gameObject.SetActive(true);

            // Place arrow in front of the camera with a bobbing Y offset
            float bob     = Mathf.Sin(Time.time * 2f) * 0.05f;
            Vector3 pos   = userCamera.position + userCamera.forward * distanceFromUser;
            pos.y         = userCamera.position.y + heightOffset + bob;
            _arrowTransform.position = pos;

            // Smoothly rotate toward the current waypoint, lying FLAT on the ground
            Vector3 target    = waypointManager.GetCurrentTarget();
            Vector3 direction = target - _arrowTransform.position;
            direction.y       = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                // 1. Face the waypoint horizontally (yaw only)
                Quaternion lookRot = Quaternion.LookRotation(direction, Vector3.up);

                // 2. Tip the arrow forward so it lies flat on the ground
                Quaternion targetRot = lookRot * Quaternion.Euler(groundTiltAngle, 0f, 0f);

                // 3. Smoothly interpolate to the target rotation
                _arrowTransform.rotation = Quaternion.Slerp(
                    _arrowTransform.rotation, targetRot, RotationSpeed * Time.deltaTime);
            }

            // Off-path detection: fires event once when the threshold is first crossed
            float distToWaypoint = Vector3.Distance(userCamera.position, target);
            if (distToWaypoint > offPathThreshold && !_wasOffPath)
            {
                _wasOffPath = true;
                Debug.LogWarning("[ArrowController] User is off path.");
                OnOffPath?.Invoke();
            }
            else if (distToWaypoint <= offPathThreshold)
            {
                _wasOffPath = false;
            }

            waypointManager.CheckWaypointReached(userCamera.position);
        }

        private void HideArrow()
        {
            if (_arrowTransform != null)
                _arrowTransform.gameObject.SetActive(false);
        }
    }
}