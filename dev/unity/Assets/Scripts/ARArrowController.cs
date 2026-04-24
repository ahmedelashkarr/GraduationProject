// Assets/Scripts/ARArrowController.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Spawns a trail of glowing AR arrows on detected floor planes,
/// pointing the user toward the selected destination.
///
/// Attach to an empty GameObject in ARScene alongside XR Origin.
/// </summary>
public class ARArrowController : MonoBehaviour
{
    [Header("AR References")]
    [SerializeField] ARRaycastManager  arRaycastManager;
    [SerializeField] ARPlaneManager    arPlaneManager;

    [Header("Arrow Prefab")]
    [SerializeField] GameObject arrowPrefab;      // ARArrow prefab
    [SerializeField] int        arrowCount  = 6;
    [SerializeField] float      arrowSpacing = 0.35f;
    [SerializeField] float      arrowScale   = 0.28f;

    [Header("Pulse Animation")]
    [SerializeField] float pulseSpeed      = 2.0f;
    [SerializeField] float pulseMinAlpha   = 0.50f;
    [SerializeField] float pulseMaxAlpha   = 1.00f;
    [SerializeField] Color arrowColor      = new Color(0.18f, 0.36f, 0.95f, 1f); // solid blue

    [Header("Navigation")]
    [SerializeField] Transform cameraTransform;   // AR camera

    // Internal state
    readonly List<GameObject> _arrows      = new();
    readonly List<ARRaycastHit> _hits = new();
    bool   _arrowsPlaced = false;
    float  _pulseT       = 0f;
    Vector3 _targetDirection = Vector3.forward;

    // ── Unity Lifecycle ───────────────────────────────────────

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main?.transform;

        // Wait for a plane to be detected, then place arrows
        if (arPlaneManager != null)
            arPlaneManager.trackablesChanged.AddListener(OnPlanesChanged);
    }

    void OnDestroy()
    {
        if (arPlaneManager != null)
            arPlaneManager.trackablesChanged.RemoveListener(OnPlanesChanged);
    }

    void Update()
    {
        if (!_arrowsPlaced) return;

        // Continuously update arrow direction toward destination
        UpdateArrowDirections();

        // Pulse opacity
        _pulseT += Time.deltaTime * pulseSpeed;
        float alpha = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha,
                                  (Mathf.Sin(_pulseT) + 1f) * 0.5f);

        for (int i = 0; i < _arrows.Count; i++)
        {
            if (_arrows[i] == null) continue;

            // Stagger the pulse per arrow
            float staggeredT = _pulseT - i * 0.28f;
            float a = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha,
                                  (Mathf.Sin(staggeredT) + 1f) * 0.5f);

            SetArrowAlpha(_arrows[i], a);
        }
    }

    // ── Plane Detection ───────────────────────────────────────

    void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
    {
        if (_arrowsPlaced) return;

        // Once at least one plane is added, try to place arrows
        if (args.added.Count > 0)
            StartCoroutine(PlaceArrowsOnFloor());
    }

    IEnumerator PlaceArrowsOnFloor()
    {
        // Wait one frame so AR plane mesh is ready
        yield return null;

        if (cameraTransform == null) yield break;

        // Raycast from screen centre downward to find floor
        Vector2 screenCentre = new Vector2(Screen.width * 0.5f, Screen.height * 0.4f);

        if (!arRaycastManager.Raycast(screenCentre, _hits, TrackableType.PlaneWithinPolygon))
            yield break;

        Vector3 basePosition  = _hits[0].pose.position;
        Quaternion baseRotation = _hits[0].pose.rotation;

        // Calculate direction toward selected destination (XZ plane only)
        var dest = AppManager.Instance?.selectedDestination;
        if (dest != null && dest.worldPosition != Vector3.zero)
        {
            Vector3 toTarget = dest.worldPosition - basePosition;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.01f)
                _targetDirection = toTarget.normalized;
        }

        SpawnArrows(basePosition);
    }

    // ── Arrow Spawning ────────────────────────────────────────

    void SpawnArrows(Vector3 origin)
    {
        ClearArrows();

        for (int i = 0; i < arrowCount; i++)
        {
            Vector3 pos = origin + _targetDirection * (arrowSpacing * i);
            pos.y = origin.y + 0.01f; // Slightly above floor

            Quaternion rot = Quaternion.LookRotation(_targetDirection, Vector3.up);

            var arrow = Instantiate(arrowPrefab, pos, rot);
            arrow.transform.localScale = Vector3.one * arrowScale;
            SetArrowColor(arrow, arrowColor);
            _arrows.Add(arrow);
        }

        _arrowsPlaced = true;
    }

    void UpdateArrowDirections()
    {
        var dest = AppManager.Instance?.selectedDestination;
        if (dest == null || _arrows.Count == 0) return;

        Vector3 camPos = cameraTransform.position;
        camPos.y = 0f;

        // Recalculate direction every frame
        if (dest.worldPosition != Vector3.zero)
        {
            Vector3 toTarget = dest.worldPosition - _arrows[0].transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.01f)
                _targetDirection = toTarget.normalized;
        }

        // Smooth rotation of arrows
        for (int i = 0; i < _arrows.Count; i++)
        {
            if (_arrows[i] == null) continue;
            Quaternion targetRot = Quaternion.LookRotation(_targetDirection, Vector3.up);
            _arrows[i].transform.rotation = Quaternion.Slerp(
                _arrows[i].transform.rotation, targetRot, Time.deltaTime * 5f);
        }
    }

    void ClearArrows()
    {
        foreach (var a in _arrows)
            if (a != null) Destroy(a);
        _arrows.Clear();
        _arrowsPlaced = false;
    }

    // ── Public Controls ───────────────────────────────────────

    /// <summary>Called by ARNavigationUI Stop button.</summary>
    public void StopNavigation()
    {
        ClearArrows();
    }

    /// <summary>Called by ARNavigationUI Recenter button.</summary>
    public void Recenter()
    {
        _arrowsPlaced = false;
        StartCoroutine(PlaceArrowsOnFloor());
    }

    // ── Helpers ───────────────────────────────────────────────

    void SetArrowAlpha(GameObject arrow, float alpha)
    {
        foreach (var rend in arrow.GetComponentsInChildren<Renderer>())
        {
            foreach (var mat in rend.materials)
            {
                Color c = mat.color;
                c.a = alpha;
                mat.color = c;
            }
        }
    }

    void SetArrowColor(GameObject arrow, Color color)
    {
        foreach (var rend in arrow.GetComponentsInChildren<Renderer>())
        {
            foreach (var mat in rend.materials)
            {
                mat.color = color;
                // Enable emission for glow (URP/Lit shader)
                mat.SetColor("_EmissionColor", color * 2.5f);
                mat.EnableKeyword("_EMISSION");
            }
        }
    }
}
