using System.Collections;
using UnityEngine;

namespace IndoorNav.Navigation
{
    /// <summary>
    /// Prints the user's current zone to the Console on a fixed interval, like
    /// a heartbeat. Useful during on-device testing where
    /// <see cref="CurrentZoneTracker"/>'s change-only log is too quiet to
    /// confirm the system is still tracking when the user stands still.
    ///
    /// Reads from <see cref="CurrentZoneTracker.CurrentZone"/> (the zone the
    /// user is physically inside) and optionally from
    /// <see cref="NavigationController.GetCurrentZone"/> (the next route
    /// target). At least one of the two references must be assigned.
    /// </summary>
    public class CurrentZoneHeartbeat : MonoBehaviour
    {
        [Tooltip("Tracker that knows which zone the user is physically inside. Assign at least this or the NavigationController.")]
        [SerializeField] private CurrentZoneTracker currentZoneTracker;

        [Tooltip("Optional. When assigned, the heartbeat also prints the next route target so you can compare 'where I am' to 'where I'm headed'.")]
        [SerializeField] private NavigationController navigationController;

        [Tooltip("Seconds between heartbeat lines.")]
        [SerializeField, Min(0.1f)] private float intervalSeconds = 5f;

        [Tooltip("Begin logging automatically when this component is enabled.")]
        [SerializeField] private bool startOnEnable = true;

        [Tooltip("Skip the heartbeat line when nothing has changed since the previous one. Useful if you only want logs when you actually move.")]
        [SerializeField] private bool onlyOnChange = false;

        [Tooltip("Log prefix used to filter the Console.")]
        [SerializeField] private string logPrefix = "[CurrentZoneHeartbeat]";

        private Coroutine _loop;
        private string _lastLine;

        private void OnEnable()
        {
            if (startOnEnable) StartLogging();
        }

        private void OnDisable() => StopLogging();

        /// <summary>Starts the periodic heartbeat. Safe to call multiple times.</summary>
        [ContextMenu("Start Logging")]
        public void StartLogging()
        {
            if (_loop != null) return;
            _loop = StartCoroutine(LogLoop());
        }

        /// <summary>Stops the periodic heartbeat. Safe to call when not running.</summary>
        [ContextMenu("Stop Logging")]
        public void StopLogging()
        {
            if (_loop == null) return;
            StopCoroutine(_loop);
            _loop = null;
        }

        /// <summary>Print one heartbeat line right now without affecting the periodic loop.</summary>
        [ContextMenu("Log Now")]
        public void LogOnce()
        {
            if (currentZoneTracker == null && navigationController == null)
            {
                Debug.LogWarning($"{logPrefix} Neither CurrentZoneTracker nor NavigationController is assigned; nothing to log.");
                return;
            }

            string inside = currentZoneTracker != null && currentZoneTracker.CurrentZone != null
                ? currentZoneTracker.CurrentZone.zoneId
                : "(none)";

            string target = navigationController != null && navigationController.GetCurrentZone() != null
                ? navigationController.GetCurrentZone().zoneId
                : null;

            string line = target != null
                ? $"{logPrefix} inside={inside}  target={target}"
                : $"{logPrefix} inside={inside}";

            if (onlyOnChange && line == _lastLine) return;
            _lastLine = line;

            Debug.Log(line);
        }

        private IEnumerator LogLoop()
        {
            var wait = new WaitForSeconds(intervalSeconds);
            while (true)
            {
                LogOnce();
                yield return wait;
            }
        }
    }
}
