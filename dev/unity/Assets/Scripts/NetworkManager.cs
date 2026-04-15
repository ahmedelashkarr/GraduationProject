// NetworkManager.cs — attach to your NetworkManager GameObject
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class NetworkManager : MonoBehaviour
{
    [Header("Server Settings")]
    [SerializeField] string serverBaseUrl = "https://your-server.com/api";

    [Header("References")]
    [SerializeField] ArrowManager arrowManager;
    [SerializeField] ZoneRegistry zoneRegistry;

    [Header("UI — drag your buttons/text here")]
    [SerializeField] TextMeshProUGUI statusText;      // shows "Loading...", errors, etc.

    // Current user zone — updated by your RSSI/KNN localization
    private string currentZone = "F1_CORRIDOR";      // replace with live zone later

    // ── JSON Data Classes ────────────────────────────────────────────

    [System.Serializable]
    class NavStep
    {
        public string zone;
        public string direction;
        public float distance;
        public string instruction;
    }

    [System.Serializable]
    class NavPath
    {
        public List<NavStep> steps;
        public float total_distance;
    }

    // ── Public API — call from UI buttons ────────────────────────────

    /// <summary>
    /// Call this from your destination UI button.
    /// e.g. button.onClick.AddListener(() => networkManager.FetchPath("F1_ROOM"));
    /// </summary>
    public void FetchPath(string destinationZone)
    {
        StartCoroutine(FetchPathCoroutine(currentZone, destinationZone));
    }

    /// <summary>
    /// Call this when your RSSI/KNN returns a new zone to update user position.
    /// </summary>
    public void SetCurrentZone(string zoneId)
    {
        currentZone = zoneId;
        Debug.Log($"Current zone updated: {zoneId}");
    }

    // ── Network Request ──────────────────────────────────────────────

    IEnumerator FetchPathCoroutine(string fromZone, string toZone)
    {
        SetStatus("Loading path...");

        string url = $"{serverBaseUrl}/path?from={fromZone}&to={toZone}";
        Debug.Log($"Fetching path: {url}");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                SetStatus($"Error: {request.error}");
                Debug.LogError($"Path fetch failed: {request.error}");
                yield break;
            }

            string json = request.downloadHandler.text;
            Debug.Log($"Path received: {json}");
            ApplyPath(json);
        }
    }

    // ── Parse JSON → Zone Transforms ────────────────────────────────

    void ApplyPath(string json)
    {
        NavPath path = JsonUtility.FromJson<NavPath>(json);

        if (path == null || path.steps == null || path.steps.Count == 0)
        {
            SetStatus("No path found.");
            Debug.LogWarning("Path is empty or malformed.");
            return;
        }

        // Convert zone IDs → Unity Transforms via ZoneRegistry
        List<Transform> waypoints = new List<Transform>();

        foreach (NavStep step in path.steps)
        {
            Transform anchor = zoneRegistry.GetZoneAnchor(step.zone);

            if (anchor != null)
            {
                waypoints.Add(anchor);
                Debug.Log($"Zone resolved: {step.zone} → {anchor.position}");
            }
            else
            {
                Debug.LogWarning($"Zone not found in registry: {step.zone}");
            }
        }

        if (waypoints.Count < 2)
        {
            SetStatus("Not enough zones to build path.");
            return;
        }

        // Feed waypoints into ArrowManager
        arrowManager.SetWaypoints(waypoints.ToArray());
        SetStatus($"Route loaded — {path.steps.Count} steps, {path.total_distance}m total");
    }

    // ── UI Helper ────────────────────────────────────────────────────

    void SetStatus(string message)
    {
        Debug.Log($"[NetworkManager] {message}");
        if (statusText != null)
            statusText.text = message;
    }
}