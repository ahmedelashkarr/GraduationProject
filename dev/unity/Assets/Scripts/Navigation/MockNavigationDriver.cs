using System.Collections.Generic;
using UnityEngine;

namespace IndoorNav.Navigation
{
    /// <summary>
    /// Editor / playmode helper that bypasses <see cref="PathRequester"/> and
    /// feeds a hardcoded <see cref="PathResponse"/> straight into a
    /// <see cref="NavigationController"/>. Useful for testing the navigation
    /// pipeline without a running server.
    ///
    /// Workflow:
    /// 1. Place <see cref="Zone"/> components in your scene whose ids match the
    ///    strings in <c>mockPath</c>.
    /// 2. Drop this script onto any GameObject and assign the controller.
    /// 3. Press Play. With <c>startOnPlay</c> on, navigation starts immediately.
    /// 4. To simulate user motion, either drag the controller's userCamera
    ///    transform around in the Scene view, or use the context-menu action
    ///    "Teleport Simulated User To Current Zone".
    /// </summary>
    public class MockNavigationDriver : MonoBehaviour
    {
        [Tooltip("Controller that receives the mock PathResponse.")]
        [SerializeField] private NavigationController navigationController;

        [Tooltip("Optional transform moved by the 'Teleport...' context menu actions. Leave null to disable teleport helpers.")]
        [SerializeField] private Transform simulatedUser;

        [Tooltip("Optional CurrentZoneTracker. When assigned and 'logEvents' is on, the console also prints whenever the user's containing zone changes.")]
        [SerializeField] private CurrentZoneTracker currentZoneTracker;

        [Tooltip("Hardcoded zone ids to feed in. Ids that do not match any Zone in the scene are skipped with a warning by the controller.")]
        [SerializeField] private List<string> mockPath = new List<string>
        {
            "F1_ROOM13",
            "F1_ROOM13_DOOR",
            "F1_CORRIDOR1_DOOR",
            "F1_LOBBY",
            "F1_LOBBY2",
            "F1_LOBBY3",
            "F1_CORRIDOR2_DOOR",
            "F1_ROOM11_DOOR",
            "F1_ROOM11",
        };

        [Tooltip("Start navigation automatically in Start().")]
        [SerializeField] private bool startOnPlay = true;

        [Tooltip("Print a console line every time the controller fires an event.")]
        [SerializeField] private bool logEvents = true;

        private bool _hooked;

        private void Start()
        {
            HookEvents();
            if (startOnPlay) StartMockNavigation();
        }

        private void OnDestroy() => UnhookEvents();

        /// <summary>Builds a fake PathResponse and calls StartNavigation on the controller.</summary>
        [ContextMenu("Start Mock Navigation")]
        public void StartMockNavigation()
        {
            if (navigationController == null)
            {
                Debug.LogError("[MockNavigationDriver] NavigationController is not assigned.");
                return;
            }
            if (mockPath == null || mockPath.Count == 0)
            {
                Debug.LogError("[MockNavigationDriver] Mock path is empty.");
                return;
            }

            var response = new PathResponse { path = new List<string>(mockPath) };
            Debug.Log($"[MockNavigationDriver] Starting mock navigation with {response.path.Count} step(s): {string.Join(" → ", response.path)}");
            navigationController.StartNavigation(response);
        }

        /// <summary>Stops the active route on the controller.</summary>
        [ContextMenu("Stop Mock Navigation")]
        public void StopMockNavigation()
        {
            if (navigationController == null) return;
            navigationController.StopNavigation();
            Debug.Log("[MockNavigationDriver] Stopped.");
        }

        /// <summary>Snaps <see cref="simulatedUser"/> onto the current zone's center, which triggers a step advance.</summary>
        [ContextMenu("Teleport Simulated User To Current Zone")]
        public void TeleportToCurrentZone()
        {
            if (simulatedUser == null)
            {
                Debug.LogWarning("[MockNavigationDriver] Simulated User transform not assigned.");
                return;
            }
            if (navigationController == null) return;

            Zone target = navigationController.GetCurrentZone();
            if (target == null)
            {
                Debug.LogWarning("[MockNavigationDriver] No active target zone.");
                return;
            }

            Vector3 c = target.GetCenter();
            simulatedUser.position = new Vector3(c.x, simulatedUser.position.y, c.z);
            Debug.Log($"[MockNavigationDriver] Teleported to '{target.zoneId}' at {c}.");
        }

        /// <summary>Snaps <see cref="simulatedUser"/> onto the next zone's center, useful for skipping a step.</summary>
        [ContextMenu("Teleport Simulated User To Next Zone")]
        public void TeleportToNextZone()
        {
            if (simulatedUser == null || navigationController == null) return;
            Zone next = navigationController.GetNextZone();
            if (next == null)
            {
                Debug.LogWarning("[MockNavigationDriver] No next zone (already at destination).");
                return;
            }
            Vector3 c = next.GetCenter();
            simulatedUser.position = new Vector3(c.x, simulatedUser.position.y, c.z);
            Debug.Log($"[MockNavigationDriver] Teleported to next zone '{next.zoneId}' at {c}.");
        }

        private void HookEvents()
        {
            if (_hooked || !logEvents) return;
            if (navigationController == null && currentZoneTracker == null) return;

            if (navigationController != null)
            {
                navigationController.OnRouteStarted.AddListener(LogRouteStarted);
                navigationController.OnZoneReached.AddListener(LogZoneReached);
                navigationController.OnDestinationReached.AddListener(LogDestinationReached);
            }
            if (currentZoneTracker != null)
            {
                currentZoneTracker.OnCurrentZoneChanged.AddListener(LogCurrentZoneChanged);
            }
            _hooked = true;
        }

        private void UnhookEvents()
        {
            if (!_hooked) return;
            if (navigationController != null)
            {
                navigationController.OnRouteStarted.RemoveListener(LogRouteStarted);
                navigationController.OnZoneReached.RemoveListener(LogZoneReached);
                navigationController.OnDestinationReached.RemoveListener(LogDestinationReached);
            }
            if (currentZoneTracker != null)
            {
                currentZoneTracker.OnCurrentZoneChanged.RemoveListener(LogCurrentZoneChanged);
            }
            _hooked = false;
        }

        private void LogRouteStarted(Zone z) =>
            Debug.Log($"[MockNavigationDriver] OnRouteStarted → {z?.zoneId}");

        private void LogZoneReached(Zone z) =>
            Debug.Log($"[MockNavigationDriver] OnZoneReached → {z?.zoneId}");

        private void LogDestinationReached() =>
            Debug.Log("[MockNavigationDriver] OnDestinationReached");

        private void LogCurrentZoneChanged(Zone z) =>
            Debug.Log($"[MockNavigationDriver] CurrentZone → {(z != null ? z.zoneId : "(none)")}");
    }
}
