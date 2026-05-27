using UnityEngine;

namespace IndoorNav.Navigation
{
    /// <summary>
    /// Represents a single navigable area (room, corridor, or link/transition zone)
    /// in the scene. Attach to every GameObject that the server may return as a
    /// path step.
    /// </summary>
    public class Zone : MonoBehaviour
    {
        /// <summary>
        /// Unique identifier matched against the server's path IDs.
        /// Case-sensitive; must be identical to the string used by the backend
        /// (e.g. "F1_ROOM13", "F1_ROOM13_LINK_CORRIDOR1").
        /// </summary>
        public string zoneId;

        /// <summary>Human-readable label shown in UI (e.g. "Computer Lab").</summary>
        public string displayName;

        [Tooltip("Optional explicit center override. If assigned, GetCenter() returns this transform's position instead of the collider bounds.")]
        [SerializeField] private Transform centerOverride;

        [Tooltip("Gizmo label color in the Scene view.")]
        [SerializeField] private Color gizmoColor = new Color(0f, 0.8f, 1f, 0.35f);

        private Collider _cachedCollider;

        private void Awake()
        {
            _cachedCollider = GetComponent<Collider>();
        }

        /// <summary>
        /// World-space center of this zone. Uses <see cref="centerOverride"/> when set,
        /// otherwise the attached <see cref="Collider"/>'s bounds center, otherwise
        /// <see cref="Transform.position"/>.
        /// </summary>
        public Vector3 GetCenter()
        {
            if (centerOverride != null)
                return centerOverride.position;

            if (_cachedCollider == null)
                _cachedCollider = GetComponent<Collider>();

            if (_cachedCollider != null)
                return _cachedCollider.bounds.center;

            return transform.position;
        }

        /// <summary>
        /// World-space bounds of this zone, or a unit cube centered on the transform
        /// when no collider is attached.
        /// </summary>
        public Bounds GetBounds()
        {
            if (_cachedCollider == null)
                _cachedCollider = GetComponent<Collider>();

            if (_cachedCollider != null)
                return _cachedCollider.bounds;

            return new Bounds(transform.position, Vector3.one);
        }

        private void OnDrawGizmos()
        {
            Bounds b = GetBounds();

            Gizmos.color = gizmoColor;
            Gizmos.DrawCube(b.center, b.size);

            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
            Gizmos.DrawWireCube(b.center, b.size);

#if UNITY_EDITOR
            string label = string.IsNullOrEmpty(zoneId) ? gameObject.name : zoneId;
            if (!string.IsNullOrEmpty(displayName))
                label += $"\n({displayName})";

            UnityEditor.Handles.Label(b.center + Vector3.up * (b.extents.y + 0.2f), label);
#endif
        }
    }
}
