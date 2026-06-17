using UnityEngine;

namespace IndoorNav.Navigation
{
    /// <summary>
    /// Draws a debug line in the Scene view from the user camera to the
    /// current target zone, plus the distance and zone id, so you can
    /// see at a glance whether navigation is tracking correctly.
    /// </summary>
    public class NavigationDebugger : MonoBehaviour
    {
        [SerializeField] private NavigationController navigationController;
        [SerializeField] private Transform userCamera;

        private void Update()
        {
            if (navigationController == null || userCamera == null) return;
            Zone target = navigationController.GetCurrentZone();
            if (target == null) return;

            Vector3 from = userCamera.position;
            Vector3 to = target.GetCenter();

            // Draw red line from camera to target (visible in Scene view only)
            Debug.DrawLine(from, to, Color.red);

            // Print distance every second
            if (Time.frameCount % 60 == 0)
            {
                float dist = Vector3.Distance(
                    new Vector3(from.x, 0, from.z),
                    new Vector3(to.x, 0, to.z)
                );
                Debug.Log($"[NavDebug] Target: {target.zoneId} | Distance: {dist:F2}m");
            }
        }
    }
}