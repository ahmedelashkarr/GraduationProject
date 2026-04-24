using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Navigation
{
    /// <summary>
    /// POSTs a path request to the navigation server and fires OnPathReceived
    /// with waypoints converted from map-local to world space.
    /// Attach to NetworkManager.
    /// </summary>
    public class PathReceiver : MonoBehaviour
    {
        [SerializeField] private string serverUrl;
        [SerializeField] private Transform mapSpace;

        /// <summary>Fires when a valid path response is received and ready for use.</summary>
        public static event Action<List<Vector3>> OnPathReceived;

        /// <summary>
        /// Sends a POST request to the server with the given start and destination positions.
        /// Fires OnPathReceived on success.
        /// </summary>
        public void RequestPath(Vector3 start, Vector3 destination)
        {
            if (string.IsNullOrEmpty(serverUrl))
            {
                Debug.LogWarning("[PathReceiver] Server URL is not set.");
                return;
            }
            StartCoroutine(SendPathRequest(start, destination));
        }

        private IEnumerator SendPathRequest(Vector3 start, Vector3 destination)
        {
            var body = new PathRequest
            {
                start = new Vec3(start),
                destination = new Vec3(destination)
            };

            byte[] rawBody = Encoding.UTF8.GetBytes(JsonUtility.ToJson(body));

            using var request = new UnityWebRequest(serverUrl, "POST");
            request.uploadHandler   = new UploadHandlerRaw(rawBody);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[PathReceiver] Request failed: {request.error}");
                yield break;
            }

            PathData pathData;
            try
            {
                pathData = JsonUtility.FromJson<PathData>(request.downloadHandler.text);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PathReceiver] Failed to parse response: {e.Message}");
                yield break;
            }

            if (pathData?.waypoints == null || pathData.waypoints.Count == 0)
            {
                Debug.LogWarning("[PathReceiver] Server returned an empty or null path.");
                yield break;
            }

            var worldWaypoints = new List<Vector3>(pathData.waypoints.Count);
            foreach (var wp in pathData.waypoints)
            {
                Vector3 localPos = wp.ToVector3();
                Vector3 worldPos = mapSpace != null ? mapSpace.TransformPoint(localPos) : localPos;
                worldWaypoints.Add(worldPos);
            }

            OnPathReceived?.Invoke(worldWaypoints);
        }

        // ── Serialization helpers ─────────────────────────────────

        [Serializable]
        private class Vec3
        {
            public float x, y, z;
            public Vec3(Vector3 v) { x = v.x; y = v.y; z = v.z; }
        }

        [Serializable]
        private class PathRequest
        {
            public Vec3 start;
            public Vec3 destination;
        }
    }
}
