using UnityEngine;
using UnityEngine.Events;
using TMPro;

namespace IndoorNav.Navigation
{
    /// <summary>
    /// Continuously detects which <see cref="Zone"/> the user is physically inside,
    /// independent of the active route. Fires <see cref="OnCurrentZoneChanged"/>
    /// only when the containing zone changes — including null transitions when
    /// the user steps outside every known zone.
    ///
    /// Cheap enough for typical scenes: a single bounds-contains test per zone
    /// per frame, with no allocations.
    /// </summary>
    public class CurrentZoneTracker : MonoBehaviour
    {
        [Tooltip("Transform whose position is tested for zone containment. Falls back to Camera.main when unassigned.")]
        [SerializeField] private Transform userCamera;

        [Tooltip("Zone registry to scan. Falls back to ZoneRegistry.Instance.")]
        [SerializeField] private ZoneRegistry zoneRegistry;

        [Tooltip("Drop the Y axis when testing containment. Turn on if your zone bounds are flat (floor markers) and the user's Y is above them.")]
        [SerializeField] private bool ignoreYAxis = false;

        [Tooltip("Print a console line each time the containing zone changes.")]
        [SerializeField] private bool logChanges = true;

        [Tooltip("Optional TMP label that will display the current zone's display name (or id when no display name is set).")]
        [SerializeField] private TMP_Text uiLabel;

        [Tooltip("Text shown when the user is not inside any known zone.")]
        [SerializeField] private string idleText = "—";

        /// <summary>The zone currently containing the user, or <c>null</c> if outside every zone.</summary>
        public Zone CurrentZone { get; private set; }

        /// <summary>Fires whenever <see cref="CurrentZone"/> changes. Argument may be <c>null</c>.</summary>
        public ZoneEvent OnCurrentZoneChanged = new ZoneEvent();

        private void Start()
        {
            if (zoneRegistry == null)
                zoneRegistry = ZoneRegistry.Instance;

            if (userCamera == null && Camera.main != null)
                userCamera = Camera.main.transform;

            UpdateLabel(null);
        }

        private void Update()
        {
            if (userCamera == null) return;

            ZoneRegistry registry = zoneRegistry != null ? zoneRegistry : ZoneRegistry.Instance;
            if (registry == null) return;

            Zone next = FindContainingZone(userCamera.position, registry);
            if (next == CurrentZone) return;

            Zone previous = CurrentZone;
            CurrentZone = next;

            if (logChanges)
            {
                string prevId = previous != null ? previous.zoneId : "(none)";
                string nextId = next     != null ? next.zoneId     : "(none)";
                Debug.Log($"[CurrentZoneTracker] {prevId} → {nextId}");
            }

            UpdateLabel(next);
            OnCurrentZoneChanged.Invoke(next);
        }

        private Zone FindContainingZone(Vector3 worldPos, ZoneRegistry registry)
        {
            foreach (var kvp in registry.Zones)
            {
                Zone z = kvp.Value;
                if (z == null) continue;

                Bounds b = z.GetBounds();
                Vector3 p = worldPos;
                if (ignoreYAxis) p.y = b.center.y;

                if (b.Contains(p)) return z;
            }
            return null;
        }

        private void UpdateLabel(Zone z)
        {
            if (uiLabel == null) return;
            if (z == null)
            {
                uiLabel.text = idleText;
                return;
            }
            uiLabel.text = string.IsNullOrEmpty(z.displayName) ? z.zoneId : z.displayName;
        }

        /// <summary>UnityEvent that carries a <see cref="Zone"/>, serializable in the inspector.</summary>
        [System.Serializable]
        public class ZoneEvent : UnityEvent<Zone> { }
    }
}
