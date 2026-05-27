using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace IndoorNav.Navigation
{
    /// <summary>
    /// Sends a GET request to the routing endpoint with <c>from</c> and <c>to</c>
    /// query parameters, parses the JSON response, and hands it to a
    /// <see cref="NavigationController"/>.
    /// </summary>
    public class PathRequester : MonoBehaviour
    {
        [Tooltip("Base URL of the server's route endpoint (e.g. https://my-server.example/route).")]
        [SerializeField] private string serverUrl = "https://sweepingly-oxidative-dominga.ngrok-free.dev/route";

        [Tooltip("NavigationController that will consume the resolved path. Required.")]
        [SerializeField] private NavigationController navigationController;

        [Tooltip("Request timeout in seconds. 0 uses UnityWebRequest defaults.")]
        [SerializeField] private int timeoutSeconds = 10;

        [Tooltip("Accept self-signed / invalid TLS certificates (useful with ngrok tunnels). Do not enable in production.")]
        [SerializeField] private bool acceptAnyCertificate = false;

        /// <summary>Raised after a successful fetch and parse, before the controller is notified.</summary>
        public event Action<PathResponse> OnPathFetched;

        /// <summary>Raised with the error message when the request or parsing fails.</summary>
        public event Action<string> OnRequestFailed;
        

        /// <summary>
        /// Fetches the path from <paramref name="fromZoneId"/> to <paramref name="toZoneId"/>.
        /// Zone ids are URL-encoded before being appended to the request.
        /// </summary>
        public void RequestPath(string fromZoneId, string toZoneId)
        {
            if (string.IsNullOrWhiteSpace(serverUrl))
            {
                Debug.LogError("[PathRequester] Server URL is not set.");
                return;
            }
            if (string.IsNullOrWhiteSpace(fromZoneId) || string.IsNullOrWhiteSpace(toZoneId))
            {
                Debug.LogError("[PathRequester] 'from' and 'to' zone ids are required.");
                return;
            }
            if (navigationController == null)
            {
                Debug.LogError("[PathRequester] NavigationController reference is not assigned.");
                return;
            }

            StartCoroutine(SendRequest(fromZoneId, toZoneId));
        }

        private IEnumerator SendRequest(string fromZoneId, string toZoneId)
        {
            string url = $"{serverUrl}?from={UnityWebRequest.EscapeURL(fromZoneId)}&to={UnityWebRequest.EscapeURL(toZoneId)}";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            if (timeoutSeconds > 0)
                request.timeout = timeoutSeconds;

            if (acceptAnyCertificate)
                request.certificateHandler = new AcceptAllCertificatesHandler();

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string error = $"[PathRequester] Request to '{url}' failed: {request.error} (HTTP {request.responseCode})";
                Debug.LogError(error);
                OnRequestFailed?.Invoke(error);
                yield break;
            }

            string body = request.downloadHandler != null ? request.downloadHandler.text : null;
            if (string.IsNullOrEmpty(body))
            {
                string error = "[PathRequester] Server returned an empty response.";
                Debug.LogError(error);
                OnRequestFailed?.Invoke(error);
                yield break;
            }

            PathResponse response;
            try
            {
                response = JsonUtility.FromJson<PathResponse>(body);
            }
            catch (Exception e)
            {
                string error = $"[PathRequester] Failed to parse response: {e.Message}\nBody: {body}";
                Debug.LogError(error);
                OnRequestFailed?.Invoke(error);
                yield break;
            }

            if (response == null || response.path == null || response.path.Count == 0)
            {
                string error = $"[PathRequester] Response contained no path.\nBody: {body}";
                Debug.LogError(error);
                OnRequestFailed?.Invoke(error);
                yield break;
            }

            OnPathFetched?.Invoke(response);
            navigationController.StartNavigation(response);
        }

        private sealed class AcceptAllCertificatesHandler : CertificateHandler
        {
            protected override bool ValidateCertificate(byte[] certificateData) => true;
        }
    }
}
