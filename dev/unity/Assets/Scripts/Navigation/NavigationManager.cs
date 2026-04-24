using UnityEngine;

namespace Navigation
{
    /// <summary>
    /// High-level controller that starts and stops navigation sessions.
    /// Coordinates PathReceiver and WaypointManager; subscribe to lifecycle events here.
    /// </summary>
    public class NavigationManager : MonoBehaviour
    {
        [SerializeField] private PathReceiver pathReceiver;
        [SerializeField] private WaypointManager waypointManager;
        [SerializeField] private Camera userCamera;

        private void Start()
        {
            if (userCamera == null)
                userCamera = Camera.main;
        }

        private void OnEnable()  => WaypointManager.OnDestinationReached += HandleDestinationReached;
        private void OnDisable() => WaypointManager.OnDestinationReached -= HandleDestinationReached;

        /// <summary>
        /// Requests a path from the server using the AR camera's current position as the start.
        /// The arrow becomes active automatically once OnPathReceived fires.
        /// </summary>
        public void StartNavigation(Vector3 destination)
        {
            if (pathReceiver == null)
            {
                Debug.LogWarning("[NavigationManager] PathReceiver is not assigned.");
                return;
            }
            if (userCamera == null)
            {
                Debug.LogWarning("[NavigationManager] User camera is not assigned.");
                return;
            }

            pathReceiver.RequestPath(userCamera.transform.position, destination);
        }

        /// <summary>Immediately clears the active path and hides the arrow.</summary>
        public void StopNavigation()
        {
            if (waypointManager == null) return;
            waypointManager.ClearPath();
        }

        private void HandleDestinationReached()
        {
            Debug.Log("[NavigationManager] Destination reached.");
            // Extend here: trigger a "You have arrived" UI panel or audio cue.
        }
    }
}
