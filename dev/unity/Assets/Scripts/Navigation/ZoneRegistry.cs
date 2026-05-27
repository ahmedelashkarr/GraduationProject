using System.Collections.Generic;
using UnityEngine;

namespace IndoorNav.Navigation
{
    /// <summary>
    /// Scene-scoped singleton that indexes every <see cref="Zone"/> in the active
    /// scene by its <see cref="Zone.zoneId"/>. Rebuild the index manually via
    /// <see cref="Rebuild"/> after instantiating zones at runtime.
    /// </summary>
    public class ZoneRegistry : MonoBehaviour
    {
        /// <summary>Global access point. Null before Awake runs on the first instance.</summary>
        public static ZoneRegistry Instance { get; private set; }

        [Tooltip("If true, Zone GameObjects under inactive parents are also indexed (uses FindObjectsInactive.Include).")]
        [SerializeField] private bool includeInactive = true;

        private readonly Dictionary<string, Zone> _zonesById = new Dictionary<string, Zone>();

        /// <summary>Read-only view over the indexed zones.</summary>
        public IReadOnlyDictionary<string, Zone> Zones => _zonesById;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[ZoneRegistry] Duplicate ZoneRegistry on '{name}'; destroying the extra component.");
                Destroy(this);
                return;
            }

            Instance = this;
            Rebuild();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Rescans the scene for <see cref="Zone"/> components and rebuilds the lookup table.
        /// </summary>
        public void Rebuild()
        {
            _zonesById.Clear();

#if UNITY_2022_2_OR_NEWER
            Zone[] zones = FindObjectsByType<Zone>(
                includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
#else
            Zone[] zones = includeInactive
                ? Resources.FindObjectsOfTypeAll<Zone>()
                : FindObjectsOfType<Zone>();
#endif

            foreach (Zone zone in zones)
            {
                if (zone == null) continue;

                if (string.IsNullOrWhiteSpace(zone.zoneId))
                {
                    Debug.LogWarning($"[ZoneRegistry] Zone on '{zone.name}' has empty zoneId; skipped.", zone);
                    continue;
                }

                if (_zonesById.TryGetValue(zone.zoneId, out Zone existing))
                {
                    Debug.LogWarning(
                        $"[ZoneRegistry] Duplicate zoneId '{zone.zoneId}' on '{zone.name}' (already registered on '{existing.name}'). Keeping the first.",
                        zone);
                    continue;
                }

                _zonesById.Add(zone.zoneId, zone);
            }

            Debug.Log($"[ZoneRegistry] Indexed {_zonesById.Count} zone(s).");
        }

        /// <summary>
        /// Returns the zone with the given id, or <c>null</c> if no match is registered.
        /// </summary>
        public Zone Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _zonesById.TryGetValue(id, out Zone zone);
            return zone;
        }

        /// <summary>True when a zone with the given id is indexed.</summary>
        public bool Contains(string id) =>
            !string.IsNullOrEmpty(id) && _zonesById.ContainsKey(id);
    }
}
