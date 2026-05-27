// Assets/Scripts/FloorMapRenderer.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Renders a route line on the floor map camera view.
/// Uses a LineRenderer in 3D space that is captured by the MapCamera
/// and output to a RenderTexture shown on the UI.
///
/// Attach to the FloorPlanRoot GameObject in MainScene.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class FloorMapRenderer : MonoBehaviour
{
    [Header("Line Settings")]
    [SerializeField] float  lineWidth     = 0.15f;
    [SerializeField] Color  routeColor    = new Color(0.08f, 0.40f, 0.75f, 1f); // #1565C0
    [SerializeField] Material lineMaterial;

    [Header("You Marker")]
    [SerializeField] Transform youMarker;   // Assign a small sphere or icon GO here
    [SerializeField] Vector3   playerWorldPos = new Vector3(0f, 0f, 0f);

    LineRenderer _lr;

    // Static waypoints that model the corridor layout (XZ plane, Y=0)
    // Adjust these to match your actual floor plan scale
    static readonly Vector3[] _corridorWaypoints = new Vector3[]
    {
        new Vector3( 0.0f, 0f,  0.0f),   // Player start
        new Vector3( 0.0f, 0f,  2.0f),
        new Vector3( 2.5f, 0f,  2.0f),
        new Vector3( 2.5f, 0f,  5.0f),   // Branch point
    };

    // Per-destination end positions (world space)
    static readonly Dictionary<string, Vector3> _destPositions = new()
    {
        { "Computer Lab", new Vector3( 2.5f, 0f, 5.0f) },
        { "Room 305",     new Vector3(-1.5f, 0f, 4.0f) },
        { "Cafeteria",    new Vector3( 4.0f, 0f, 6.5f) },
        { "Exit",         new Vector3( 5.0f, 0f, 8.0f) },
    };

    // ── Unity Lifecycle ───────────────────────────────────────

    void Awake()
    {
        _lr = GetComponent<LineRenderer>();
        _lr.startWidth  = lineWidth;
        _lr.endWidth    = lineWidth;
        _lr.useWorldSpace = true;
        _lr.numCapVertices = 6;

        if (lineMaterial != null)
            _lr.material = lineMaterial;

        // Default: draw player position
        if (youMarker != null)
            youMarker.position = playerWorldPos;
    }

    // ── Public API ────────────────────────────────────────────

    public void DrawRouteTo(Destination dest)
    {
        if (dest == null) return;

        Vector3 endPos = _destPositions.ContainsKey(dest.name)
            ? _destPositions[dest.name]
            : dest.worldPosition;

        // Build waypoints: corridor path + straight line to destination
        var pts = new List<Vector3>(_corridorWaypoints) { endPos };

        _lr.positionCount = pts.Count;
        _lr.SetPositions(pts.ToArray());

        // Animate color with a gradient (start = white, end = blue)
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.white,    0.0f),
                new GradientColorKey(routeColor,     0.3f),
                new GradientColorKey(routeColor,     1.0f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        );
        _lr.colorGradient = grad;
    }

    public void ClearRoute()
    {
        _lr.positionCount = 0;
    }
}
