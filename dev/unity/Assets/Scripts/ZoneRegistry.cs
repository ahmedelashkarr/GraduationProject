// Assets/Scripts/ZoneRegistry.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maps zone ID strings (e.g. "F1_CORRIDOR", "F1_ROOM") to
/// Unity Transforms that act as anchor points in the scene.
///
/// Attach to an empty GameObject (e.g. "ZoneRegistry") in your scene.
/// Then drag each zone anchor Transform into the Zones list in the Inspector.
/// </summary>
public class ZoneRegistry : MonoBehaviour
{
    [System.Serializable]
    public class ZoneEntry
    {
        public string    zoneId;    // Must match exactly what the server returns
        public Transform anchor;    // The WP_X / empty GO placed at that zone
    }

    [SerializeField] List<ZoneEntry> zones = new List<ZoneEntry>();

    // Built at Start for O(1) lookups
    readonly Dictionary<string, Transform> _lookup = new();

    void Awake()
    {
        foreach (var entry in zones)
        {
            if (!string.IsNullOrEmpty(entry.zoneId) && entry.anchor != null)
                _lookup[entry.zoneId] = entry.anchor;
            else
                Debug.LogWarning($"[ZoneRegistry] Skipping invalid entry: '{entry.zoneId}'");
        }

        Debug.Log($"[ZoneRegistry] Registered {_lookup.Count} zones.");
    }

    /// <summary>
    /// Returns the Transform for the given zone ID, or null if not found.
    /// Called by NetworkManager when converting path steps to waypoints.
    /// </summary>
    public Transform GetZoneAnchor(string zoneId)
    {
        if (_lookup.TryGetValue(zoneId, out Transform anchor))
            return anchor;

        Debug.LogWarning($"[ZoneRegistry] Zone not found: '{zoneId}'");
        return null;
    }

    /// <summary>
    /// Optional: register a zone at runtime (e.g. from AR plane detection).
    /// </summary>
    public void RegisterZone(string zoneId, Transform anchor)
    {
        _lookup[zoneId] = anchor;
        Debug.Log($"[ZoneRegistry] Runtime registered: '{zoneId}'");
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (zones == null) return;
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.8f);
        foreach (var entry in zones)
        {
            if (entry.anchor == null) continue;
            Gizmos.DrawWireSphere(entry.anchor.position, 0.25f);
            UnityEditor.Handles.Label(
                entry.anchor.position + Vector3.up * 0.35f,
                entry.zoneId
            );
        }
    }
#endif
}