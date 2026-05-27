using System.Collections.Generic;
using UnityEngine;

namespace IndoorNav.Navigation
{
    /// <summary>
    /// Editor-time gizmo that previews where <see cref="ArrowTrailRenderer"/>
    /// will spawn arrows for a given list of zone ids. Scans the scene for
    /// matching <see cref="Zone"/> components (no <see cref="ZoneRegistry"/>
    /// dependency, so it works without entering Play mode), then walks the
    /// path with the same spacing logic as the renderer and draws gizmos.
    ///
    /// Useful for laying out the building before testing — set the same
    /// <c>spacing</c> as the renderer to confirm the trail will look right.
    /// </summary>
    public class ArrowTrailDebug : MonoBehaviour
    {
        [Tooltip("Zone ids to walk. Order matters; mirrors a server PathResponse.")]
        [SerializeField] private List<string> previewZoneIds = new List<string>();

        [Tooltip("Distance between gizmo markers along each segment.")]
        [SerializeField, Min(0.05f)] private float spacing = 0.5f;

        [Tooltip("Y offset above each zone's floor, like ArrowTrailRenderer.floorOffset.")]
        [SerializeField] private float floorOffset = 0.02f;

        [Tooltip("Radius of the gizmo sphere drawn at each spawn point.")]
        [SerializeField, Min(0.01f)] private float markerRadius = 0.05f;

        [Tooltip("Color used for the path line and spawn-point markers.")]
        [SerializeField] private Color gizmoColor = new Color(0f, 0.85f, 1f, 0.9f);

        [Tooltip("Only draw gizmos when this GameObject is selected.")]
        [SerializeField] private bool onlyWhenSelected = true;

        private void OnDrawGizmos()
        {
            if (onlyWhenSelected) return;
            Draw();
        }

        private void OnDrawGizmosSelected()
        {
            if (!onlyWhenSelected) return;
            Draw();
        }

        private void Draw()
        {
            if (previewZoneIds == null || previewZoneIds.Count < 2) return;

            List<Zone> zones = ResolveZones();
            if (zones.Count < 2) return;

            Gizmos.color = gizmoColor;

            for (int i = 0; i < zones.Count - 1; i++)
            {
                Zone from = zones[i];
                Zone to   = zones[i + 1];
                if (from == null || to == null) continue;

                Vector3 a = from.GetCenter();
                Vector3 b = to.GetCenter();
                Gizmos.DrawLine(a, b);

                float floorYA = from.GetBounds().min.y;
                float floorYB = to.GetBounds().min.y;

                Vector3 horizontal = new Vector3(b.x - a.x, 0f, b.z - a.z);
                float segmentLen = horizontal.magnitude;
                if (segmentLen < 0.001f) continue;

                bool isFirstSegment = i == 0;
                float startDistance = isFirstSegment ? 0f : spacing;

                for (float d = startDistance; d <= segmentLen; d += spacing)
                {
                    float t = d / segmentLen;
                    float floorY = Mathf.Lerp(floorYA, floorYB, t) + floorOffset;
                    Vector3 p = new Vector3(
                        Mathf.Lerp(a.x, b.x, t),
                        floorY,
                        Mathf.Lerp(a.z, b.z, t));
                    Gizmos.DrawSphere(p, markerRadius);
                }
            }
        }

        private List<Zone> ResolveZones()
        {
            var result = new List<Zone>(previewZoneIds.Count);

#if UNITY_2022_2_OR_NEWER
            Zone[] all = FindObjectsByType<Zone>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            Zone[] all = Resources.FindObjectsOfTypeAll<Zone>();
#endif

            // Build a quick lookup so we don't re-scan per id.
            var byId = new Dictionary<string, Zone>(all.Length);
            for (int i = 0; i < all.Length; i++)
            {
                Zone z = all[i];
                if (z == null || string.IsNullOrEmpty(z.zoneId)) continue;
                if (!byId.ContainsKey(z.zoneId)) byId.Add(z.zoneId, z);
            }

            foreach (string id in previewZoneIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                if (byId.TryGetValue(id, out Zone z)) result.Add(z);
            }

            return result;
        }
    }
}
