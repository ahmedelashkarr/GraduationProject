using System.Collections.Generic;
using UnityEngine;

namespace IndoorNav.Navigation
{
    /// <summary>
    /// Spawns a trail of small arrow prefabs along the active route, like the
    /// floor-arrow trails seen in airport / mall AR navigation apps. Arrows
    /// lie flat on the floor, point toward the next zone, and fade by
    /// distance to the camera. Rebuilds whenever the route advances.
    /// </summary>
    public class ArrowTrailRenderer : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────

        [Tooltip("Controller that owns the active route. Required.")]
        [SerializeField] private NavigationController navigationController;

        [Tooltip("Arrow prefab instantiated along the path. Should have a single Renderer with a colored material; fading writes to the material via a MaterialPropertyBlock so the prefab's asset isn't mutated.")]
        [SerializeField] private GameObject arrowPrefab;

        [Tooltip("Optional parent for instantiated arrows. Falls back to this GameObject's transform.")]
        [SerializeField] private Transform arrowParent;

        [Tooltip("Camera used for distance fade and behind-user culling. Falls back to Camera.main when unassigned.")]
        [SerializeField] private Transform userCamera;

        [Tooltip("Distance in meters between adjacent arrows along the path.")]
        [SerializeField, Min(0.05f)] private float spacing = 0.5f;

        [Tooltip("Y offset above the floor level of each segment to avoid z-fighting with floor meshes.")]
        [SerializeField] private float floorOffset = 0.02f;

        [Tooltip("Extra rotation applied after the look-rotation, e.g. (90,0,0) to lay a +Z-forward arrow flat on the floor.")]
        [SerializeField] private Vector3 arrowRotationOffset = new Vector3(90f, 0f, 0f);

        [Header("Fade")]
        [Tooltip("Modulate arrow alpha by distance to the camera.")]
        [SerializeField] private bool fadeByDistance = true;

        [Tooltip("Arrows closer than this distance to the camera are fully opaque.")]
        [SerializeField, Min(0f)] private float fadeStartDistance = 2f;

        [Tooltip("Arrows farther than this distance are fully transparent.")]
        [SerializeField, Min(0.01f)] private float fadeEndDistance = 15f;

        [Header("Behind-user culling")]
        [Tooltip("Hide arrows that are behind the camera so the trail doesn't visually wrap around the user.")]
        [SerializeField] private bool hideArrowsBehindUser = true;

        [Tooltip("Dot product cutoff between camera-forward and the direction-to-arrow. Arrows below this are hidden. -0.2 keeps a small buffer behind the user; lower values keep more of the trail visible during sharp turns.")]
        [SerializeField, Range(-1f, 1f)] private float behindUserDotThreshold = -0.2f;

        // ── State ────────────────────────────────────────────────────────────

        private static readonly int BaseColorId   = Shader.PropertyToID("_BaseColor"); // URP
        private static readonly int LegacyColorId = Shader.PropertyToID("_Color");     // built-in

        private readonly List<ArrowEntry> _arrows = new List<ArrowEntry>();
        private MaterialPropertyBlock _block;
        private Transform _parent;
        private bool _hooked;

        private struct ArrowEntry
        {
            public GameObject go;
            public Renderer   renderer;
            public Color      baseColor;
            public int        colorPropId; // -1 when the arrow has no compatible color property
        }

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            _parent = arrowParent != null ? arrowParent : transform;
            _block = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            HookEvents();

            // If we're enabled mid-route, build the trail immediately rather than
            // waiting for the next OnZoneReached event.
            if (navigationController != null && navigationController.IsNavigating)
                RebuildTrail();
        }

        private void OnDisable() => UnhookEvents();

        private void OnDestroy() => ClearArrows();

        private void Update()
        {
            if (_arrows.Count == 0) return;

            Transform cam = ResolveCamera();
            if (cam == null) return;

            Vector3 camPos = cam.position;
            Vector3 camFwd = cam.forward;

            for (int i = 0; i < _arrows.Count; i++)
            {
                ArrowEntry entry = _arrows[i];
                if (entry.go == null || entry.renderer == null) continue;

                bool shouldRender = true;

                if (hideArrowsBehindUser)
                {
                    Vector3 toArrow = entry.go.transform.position - camPos;
                    float sqr = toArrow.sqrMagnitude;
                    if (sqr > 0.0001f)
                    {
                        Vector3 toArrowDir = toArrow / Mathf.Sqrt(sqr);
                        float dot = Vector3.Dot(camFwd, toArrowDir);
                        if (dot < behindUserDotThreshold) shouldRender = false;
                    }
                }

                if (entry.renderer.enabled != shouldRender)
                    entry.renderer.enabled = shouldRender;

                if (!shouldRender) continue;

                if (fadeByDistance && entry.colorPropId != -1)
                {
                    float distance = Vector3.Distance(entry.go.transform.position, camPos);
                    float t = Mathf.InverseLerp(fadeStartDistance, fadeEndDistance, distance);
                    float alpha = Mathf.Lerp(1f, 0f, t);

                    Color c = entry.baseColor;
                    c.a *= alpha;

                    entry.renderer.GetPropertyBlock(_block);
                    _block.SetColor(entry.colorPropId, c);
                    entry.renderer.SetPropertyBlock(_block);
                }
            }
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Rebuild the trail from the current state of the navigation controller.</summary>
        public void ForceRebuild() => RebuildTrail();

        /// <summary>Destroys every spawned arrow and clears internal state.</summary>
        public void ClearArrows()
        {
            for (int i = 0; i < _arrows.Count; i++)
            {
                if (_arrows[i].go != null)
                {
                    if (Application.isPlaying) Destroy(_arrows[i].go);
                    else                       DestroyImmediate(_arrows[i].go);
                }
            }
            _arrows.Clear();
        }

        // ── Event subscriptions ──────────────────────────────────────────────

        private void HookEvents()
        {
            if (_hooked || navigationController == null) return;
            navigationController.OnRouteStarted.AddListener(HandleRouteStarted);
            navigationController.OnZoneReached.AddListener(HandleZoneReached);
            navigationController.OnDestinationReached.AddListener(HandleDestinationReached);
            _hooked = true;
        }

        private void UnhookEvents()
        {
            if (!_hooked || navigationController == null) return;
            navigationController.OnRouteStarted.RemoveListener(HandleRouteStarted);
            navigationController.OnZoneReached.RemoveListener(HandleZoneReached);
            navigationController.OnDestinationReached.RemoveListener(HandleDestinationReached);
            _hooked = false;
        }

        private void HandleRouteStarted(Zone _)         => RebuildTrail();
        private void HandleZoneReached(Zone _)          => RebuildTrail();
        private void HandleDestinationReached()         => ClearArrows();

        // ── Building the trail ───────────────────────────────────────────────

        private void RebuildTrail()
        {
            ClearArrows();

            if (navigationController == null)
            {
                Debug.LogWarning("[ArrowTrailRenderer] NavigationController is not assigned.");
                return;
            }
            if (arrowPrefab == null)
            {
                Debug.LogWarning("[ArrowTrailRenderer] Arrow prefab is not assigned.");
                return;
            }

            List<Zone> remaining = navigationController.GetRemainingZones();
            if (remaining == null || remaining.Count < 2) return;

            // Walk consecutive pairs, planting arrows every `spacing` meters along
            // each segment. The first arrow of each segment after the first is
            // placed `spacing` in so we don't stack on top of the previous
            // segment's last arrow.
            for (int i = 0; i < remaining.Count - 1; i++)
            {
                Zone from = remaining[i];
                Zone to   = remaining[i + 1];
                if (from == null || to == null) continue;

                SpawnArrowsBetween(from, to, isFirstSegment: i == 0);
            }
        }

        private void SpawnArrowsBetween(Zone from, Zone to, bool isFirstSegment)
        {
            Vector3 a = from.GetCenter();
            Vector3 b = to.GetCenter();

            // Use floor heights from each zone's collider bounds so multi-floor
            // segments still place arrows on the actual floor of each room.
            float floorYA = from.GetBounds().min.y;
            float floorYB = to.GetBounds().min.y;

            Vector3 horizontalDir = new Vector3(b.x - a.x, 0f, b.z - a.z);
            float segmentLen = horizontalDir.magnitude;
            if (segmentLen < 0.001f) return;
            Vector3 unit = horizontalDir / segmentLen;

            Quaternion rot = Quaternion.LookRotation(unit, Vector3.up)
                             * Quaternion.Euler(arrowRotationOffset);

            // Skip the very first position when we already laid down an arrow
            // at the end of a previous segment, to keep spacing visually even.
            float startDistance = isFirstSegment ? 0f : spacing;

            for (float d = startDistance; d <= segmentLen; d += spacing)
            {
                float t = d / segmentLen;
                float floorY = Mathf.Lerp(floorYA, floorYB, t) + floorOffset;
                Vector3 pos = new Vector3(
                    Mathf.Lerp(a.x, b.x, t),
                    floorY,
                    Mathf.Lerp(a.z, b.z, t));

                GameObject go = Instantiate(arrowPrefab, pos, rot, _parent);
                _arrows.Add(BuildEntry(go));
            }
        }

        private static ArrowEntry BuildEntry(GameObject go)
        {
            Renderer r = go.GetComponentInChildren<Renderer>();
            int    propId    = -1;
            Color  baseColor = Color.white;

            if (r != null && r.sharedMaterial != null)
            {
                if (r.sharedMaterial.HasProperty(BaseColorId))
                {
                    propId    = BaseColorId;
                    baseColor = r.sharedMaterial.GetColor(BaseColorId);
                }
                else if (r.sharedMaterial.HasProperty(LegacyColorId))
                {
                    propId    = LegacyColorId;
                    baseColor = r.sharedMaterial.GetColor(LegacyColorId);
                }
            }

            return new ArrowEntry
            {
                go          = go,
                renderer    = r,
                baseColor   = baseColor,
                colorPropId = propId,
            };
        }

        private Transform ResolveCamera()
        {
            if (userCamera != null) return userCamera;
            if (Camera.main != null) return Camera.main.transform;
            return null;
        }
    }
}
