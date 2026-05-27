using UnityEngine;

    namespace IndoorNav.Navigation
{
    /// <summary>
    /// When a new route starts, snaps the XR Origin so the Main Camera lands
    /// on the first zone's XZ center. The camera's Y is preserved, since the
    /// rig height is set elsewhere. We move the rig (the XR Origin), not the
    /// camera, so AR Foundation tracking on a real device doesn't fight the
    /// teleport — same reasoning <see cref="EditorUserController"/> uses.
    ///
    /// Why <see cref="DefaultExecutionOrderAttribute"/> -100: this script must
    /// subscribe to <see cref="NavigationController.OnRouteStarted"/> *before*
    /// <see cref="ArrowTrailRenderer"/> does, so its handler runs first and
    /// the trail is built against the post-alignment world positions. With
    /// the early execution order, our OnEnable runs sooner, AddListener
    /// happens sooner, and UnityEvent invokes listeners in subscription
    /// order. As an extra safety measure, after applying the move we call
    /// <see cref="Physics.SyncTransforms"/> so any same-frame readers of
    /// world positions see the updated values.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class StartZoneAligner : MonoBehaviour
    {
        [Tooltip("NavigationController whose OnRouteStarted event triggers alignment.")]
        [SerializeField] private NavigationController navigationController;

        [Tooltip("XR Origin transform — this is what gets moved so the camera ends up at the start zone.")]
        [SerializeField] private Transform xrOrigin;

        [Tooltip("Main Camera transform under the XR Origin — used to read the camera's current world position.")]
        [SerializeField] private Transform mainCamera;

        [Tooltip("If true, only align the first time a route starts during this session. If false, every route start re-aligns.")]
        [SerializeField] private bool alignOncePerSession = false;

        [Tooltip("Snap rotation as well so the camera looks toward the second zone in the path. Off by default — many AR apps prefer to keep the device's tracked yaw.")]
        [SerializeField] private bool alignRotation = false;

        [Tooltip("If true, log alignment moves to the console.")]
        [SerializeField] private bool verboseLogging = true;

        private bool _alignedOnce;
        private bool _hooked;

        /// <summary>True after at least one alignment has been applied this session.</summary>
        public bool HasAligned => _alignedOnce;

        /// <summary>The most recent zone we aligned the rig to, or <c>null</c> before the first alignment.</summary>
        public Zone LastAlignedZone { get; private set; }

        private void Awake()
        {
            if (mainCamera == null && Camera.main != null)
                mainCamera = Camera.main.transform;
        }

        private void OnEnable()
        {
            if (_hooked || navigationController == null) return;
            navigationController.OnRouteStarted.AddListener(HandleRouteStarted);
            _hooked = true;
        }

        private void OnDisable()
        {
            if (!_hooked || navigationController == null) return;
            navigationController.OnRouteStarted.RemoveListener(HandleRouteStarted);
            _hooked = false;
        }

        private void HandleRouteStarted(Zone firstZone)
        {
            if (alignOncePerSession && _alignedOnce) return;

            if (firstZone == null)
            {
                Debug.LogWarning("[StartZoneAligner] OnRouteStarted fired with a null first zone; skipping alignment.");
                return;
            }
            if (xrOrigin == null)
            {
                Debug.LogWarning("[StartZoneAligner] XR Origin reference is not assigned; skipping alignment.");
                return;
            }
            if (mainCamera == null)
            {
                Debug.LogWarning("[StartZoneAligner] Main Camera reference is not assigned and Camera.main is null; skipping alignment.");
                return;
            }

            Vector3 zoneCenter   = firstZone.GetCenter();
            Vector3 camPos       = mainCamera.position;
            Vector3 oldOriginPos = xrOrigin.position;

            // Move only on XZ; preserve current camera Y (rig height is set elsewhere).
            Vector3 delta = new Vector3(zoneCenter.x - camPos.x, 0f, zoneCenter.z - camPos.z);
            xrOrigin.position += delta;

            // Optional yaw alignment toward the second zone in the route.
            if (alignRotation)
            {
                Zone secondZone = ResolveSecondZone();
                if (secondZone != null)
                {
                    Vector3 secondCenter = secondZone.GetCenter();
                    Vector3 dir = new Vector3(
                        secondCenter.x - zoneCenter.x,
                        0f,
                        secondCenter.z - zoneCenter.z);

                    if (dir.sqrMagnitude > 0.0001f)
                    {
                        Quaternion yaw = Quaternion.LookRotation(dir.normalized, Vector3.up);
                        xrOrigin.rotation = yaw;
                    }
                }
            }

            // Make sure same-frame readers (NavigationController distance check,
            // ArrowTrailRenderer rebuild) see the new world positions.
            Physics.SyncTransforms();

            _alignedOnce = true;
            LastAlignedZone = firstZone;

            if (verboseLogging)
            {
                Debug.Log(
                    $"[StartZoneAligner] Aligned to '{firstZone.zoneId}'. " +
                    $"XR Origin {oldOriginPos} → {xrOrigin.position}.");
            }
        }

        // Prefer GetNextZone() (next target after the current one). At route
        // start, the controller's currentIndex is 0 so GetNextZone() returns
        // route[1] — exactly the "second zone" we want. Falls back to
        // GetRemainingZones()[1] if for some reason the controller doesn't
        // have a next zone yet.
        private Zone ResolveSecondZone()
        {
            if (navigationController == null) return null;

            Zone next = navigationController.GetNextZone();
            if (next != null) return next;

            var remaining = navigationController.GetRemainingZones();
            if (remaining != null && remaining.Count > 1) return remaining[1];

            return null;
        }
    }
}

