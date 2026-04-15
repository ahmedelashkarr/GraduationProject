using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ArrowManager : MonoBehaviour
{
    [Header("AR")]
    public ARPlaneManager planeManager;
    public Camera arCamera;

    [Header("Prefabs")]
    public GameObject arrow3DPrefab;   // arrows on floor
    public Transform uiArrow;         // arrow in screen (Right)

    [Header("Waypoints")]
    public Transform[] waypoints;

    [Header("Settings")]
    public float switchDistance = 1f;
    public float rotationSpeed = 5f;

    private List<GameObject> spawnedArrows = new();
    private ARPlane floor;
    private int currentIndex = 0;

    void OnEnable() => planeManager.planesChanged += OnPlanesChanged;
    void OnDisable() => planeManager.planesChanged -= OnPlanesChanged;

    // ───────── Plane Detection ─────────
    void OnPlanesChanged(ARPlanesChangedEventArgs args)
    {
        foreach (var p in args.added)
        {
            if (p.alignment == PlaneAlignment.HorizontalUp)
            {
                floor = p;
                PlaceArrows();
            }
        }
    }

    // ───────── Place 3D Arrows ─────────
    void PlaceArrows()
    {
        foreach (var a in spawnedArrows) Destroy(a);
        spawnedArrows.Clear();

        float y = floor.transform.position.y + 0.05f;

        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            Vector3 from = waypoints[i].position;
            Vector3 to = waypoints[i + 1].position;

            Vector3 mid = new Vector3(
                (from.x + to.x) / 2f,
                y,
                (from.z + to.z) / 2f
            );

            Vector3 dir = (to - from).normalized;

            GameObject arrow = Instantiate(
                arrow3DPrefab,
                mid,
                Quaternion.LookRotation(dir, Vector3.up)
            );

            spawnedArrows.Add(arrow);
        }
    }

    // ───────── Update ─────────
    void Update()
    {
        if (waypoints.Length == 0 || arCamera == null) return;

        UpdateWaypoint();
        RotateUIArrow();
    }

    // ───────── Waypoint Progress ─────────
    void UpdateWaypoint()
    {
        if (currentIndex >= waypoints.Length) return;

        float dist = Vector3.Distance(
            arCamera.transform.position,
            waypoints[currentIndex].position
        );

        if (dist < switchDistance)
        {
            currentIndex++;
            if (currentIndex >= waypoints.Length)
                currentIndex = waypoints.Length - 1;
        }
    }

    // ───────── UI Arrow Direction ─────────
    void RotateUIArrow()
    {
        Transform target = waypoints[currentIndex];

        Vector3 camForward = arCamera.transform.forward;
        Vector3 dirToTarget = (target.position - arCamera.transform.position).normalized;

        float cross = Vector3.Cross(camForward, dirToTarget).y;

        float angle = Mathf.Clamp(cross * 100f, -90f, 90f);

        Quaternion targetRot = Quaternion.Euler(0, 0, -angle);

        uiArrow.localRotation = Quaternion.Slerp(
            uiArrow.localRotation,
            targetRot,
            Time.deltaTime * rotationSpeed
        );
    }
    /// <summary>
/// Called by NetworkManager after fetching a path from the server.
/// Replaces the current waypoints and re-places arrows on the floor.
/// </summary>
public void SetWaypoints(Transform[] newWaypoints)
{
    waypoints = newWaypoints;
    currentIndex = 0;

    // Re-place arrows if a floor plane is already known
    if (floor != null)
        PlaceArrows();
    // If no floor yet, arrows will be placed when OnPlanesChanged fires
}
}