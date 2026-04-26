using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace IndoorNav.Navigation
{
    /// <summary>
    /// Drives the user through a server-computed zone sequence. Resolves each
    /// zone id via <see cref="ZoneRegistry"/>, advances through the list as the
    /// AR camera moves within <see cref="reachThresholdMeters"/> (XZ only), and
    /// fires UnityEvents for lifecycle hooks.
    /// </summary>
    public class NavigationController : MonoBehaviour
    {
        [Tooltip("Zone registry used to resolve ids. Falls back to ZoneRegistry.Instance if unassigned.")]
        [SerializeField] private ZoneRegistry zoneRegistry;

        [Tooltip("Transform of the AR camera. Falls back to Camera.main when unassigned.")]
        [SerializeField] private Transform userCamera;

        [Tooltip("Horizontal (XZ-plane) distance at which the next zone is considered reached.")]
        [SerializeField, Min(0.01f)] private float reachThresholdMeters = 1.5f;

        [Tooltip("Optional LineRenderer drawn through the zone centers for the active route.")]
        [SerializeField] private LineRenderer pathLineRenderer;

        [Tooltip("Vertical offset applied to LineRenderer points so the line sits just above the floor.")]
        [SerializeField] private float pathLineYOffset = 0.05f;

        /// <summary>Raised once per zone as the user reaches it (final zone included).</summary>
        public ZoneEvent OnZoneReached = new ZoneEvent();

        /// <summary>Raised after the final zone is reached, immediately after the last <see cref="OnZoneReached"/>.</summary>
        public UnityEvent OnDestinationReached = new UnityEvent();

        /// <summary>Raised when a new route begins, with the first zone in the sequence.</summary>
        public ZoneEvent OnRouteStarted = new ZoneEvent();

        private readonly List<Zone> _route = new List<Zone>();
        private int _currentIndex = -1;
        private bool _isNavigating;

        /// <summary>True while a route is active (not yet completed or stopped).</summary>
        public bool IsNavigating => _isNavigating;

        private void Awake()
        {
            if (zoneRegistry == null)
                zoneRegistry = ZoneRegistry.Instance;
        }

        private void Start()
        {
            if (userCamera == null && Camera.main != null)
                userCamera = Camera.main.transform;
        }

        private void Update()
        {
            if (!_isNavigating || userCamera == null) return;
            if (_currentIndex < 0 || _currentIndex >= _route.Count) return;

            Zone target = _route[_currentIndex];
            if (target == null)
            {
                AdvanceIndex();
                return;
            }

            if (HorizontalDistance(userCamera.position, target.GetCenter()) <= reachThresholdMeters)
            {
                Zone reached = target;
                AdvanceIndex();
                OnZoneReached.Invoke(reached);

                if (_currentIndex >= _route.Count)
                {
                    _isNavigating = false;
                    OnDestinationReached.Invoke();
                }
            }
        }

        /// <summary>
        /// Begins a new navigation session. Unknown zone ids are skipped with a warning.
        /// </summary>
        public void StartNavigation(PathResponse response)
        {
            if (response == null || response.path == null || response.path.Count == 0)
            {
                Debug.LogWarning("[NavigationController] StartNavigation called with an empty response.");
                return;
            }

            ZoneRegistry registry = zoneRegistry != null ? zoneRegistry : ZoneRegistry.Instance;
            if (registry == null)
            {
                Debug.LogError("[NavigationController] No ZoneRegistry available; cannot resolve path.");
                return;
            }

            _route.Clear();
            foreach (string id in response.path)
            {
                Zone zone = registry.Get(id);
                if (zone == null)
                {
                    Debug.LogWarning($"[NavigationController] Zone '{id}' not found in registry; skipping.");
                    continue;
                }
                _route.Add(zone);
            }

            if (_route.Count == 0)
            {
                Debug.LogError("[NavigationController] No zones in the response could be resolved; aborting.");
                _isNavigating = false;
                return;
            }

            _currentIndex = 0;
            _isNavigating = true;

            UpdatePathLine();
            OnRouteStarted.Invoke(_route[0]);
        }

        /// <summary>Clears the current route without raising <see cref="OnDestinationReached"/>.</summary>
        public void StopNavigation()
        {
            _isNavigating = false;
            _currentIndex = -1;
            _route.Clear();
            ClearPathLine();
        }

        /// <summary>Returns the zone the user is currently walking toward, or <c>null</c> when inactive.</summary>
        public Zone GetCurrentZone()
        {
            if (!_isNavigating || _currentIndex < 0 || _currentIndex >= _route.Count) return null;
            return _route[_currentIndex];
        }

        /// <summary>Returns the zone immediately after the current one, or <c>null</c> when at the final step.</summary>
        public Zone GetNextZone()
        {
            int next = _currentIndex + 1;
            if (!_isNavigating || next < 0 || next >= _route.Count) return null;
            return _route[next];
        }

        /// <summary>
        /// Returns a new list containing every zone from the current target through the destination.
        /// Empty when inactive.
        /// </summary>
        public List<Zone> GetRemainingZones()
        {
            var remaining = new List<Zone>();
            if (!_isNavigating) return remaining;

            for (int i = Mathf.Max(0, _currentIndex); i < _route.Count; i++)
                remaining.Add(_route[i]);
            return remaining;
        }

        private void AdvanceIndex()
        {
            _currentIndex++;
            UpdatePathLine();
        }

        private void UpdatePathLine()
        {
            if (pathLineRenderer == null) return;

            int remaining = Mathf.Max(0, _route.Count - Mathf.Max(0, _currentIndex));
            if (remaining == 0)
            {
                ClearPathLine();
                return;
            }

            pathLineRenderer.positionCount = remaining;
            int writeIndex = 0;
            for (int i = _currentIndex; i < _route.Count; i++)
            {
                Vector3 c = _route[i].GetCenter();
                c.y += pathLineYOffset;
                pathLineRenderer.SetPosition(writeIndex++, c);
            }
        }

        private void ClearPathLine()
        {
            if (pathLineRenderer != null)
                pathLineRenderer.positionCount = 0;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>UnityEvent that carries a <see cref="Zone"/>, serializable in the inspector.</summary>
        [System.Serializable]
        public class ZoneEvent : UnityEvent<Zone> { }
    }
}
