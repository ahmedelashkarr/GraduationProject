using System;
using System.Collections.Generic;
using UnityEngine;

namespace Navigation
{
    /// <summary>
    /// Maintains a list of world-space waypoints and tracks the user's progress
    /// through them. Subscribes to PathReceiver.OnPathReceived automatically.
    /// Attach to ArrowManager.
    /// </summary>
    public class WaypointManager : MonoBehaviour
    {
        [SerializeField] private float reachThreshold = 1.0f;

        private List<Vector3> _waypoints = new List<Vector3>();
        private int _currentIndex;

        /// <summary>Fires when the user reaches the last waypoint in the path.</summary>
        public static event Action OnDestinationReached;

        private void OnEnable()  => PathReceiver.OnPathReceived += SetPath;
        private void OnDisable() => PathReceiver.OnPathReceived -= SetPath;

        /// <summary>Loads a new path and resets progress to the first waypoint.</summary>
        public void SetPath(List<Vector3> path)
        {
            if (path == null || path.Count == 0)
            {
                Debug.LogWarning("[WaypointManager] Received null or empty path.");
                return;
            }
            _waypoints  = path;
            _currentIndex = 0;
        }

        /// <summary>Returns the current target waypoint, or Vector3.zero if no active path.</summary>
        public Vector3 GetCurrentTarget()
        {
            if (!HasPath()) return Vector3.zero;
            return _waypoints[_currentIndex];
        }

        /// <summary>
        /// Advances to the next waypoint if userPos is within reachThreshold.
        /// Returns true (and fires OnDestinationReached) when the final waypoint is reached.
        /// </summary>
        public bool CheckWaypointReached(Vector3 userPos)
        {
            if (!HasPath()) return false;

            if (Vector3.Distance(userPos, _waypoints[_currentIndex]) <= reachThreshold)
            {
                _currentIndex++;
                if (_currentIndex >= _waypoints.Count)
                {
                    _waypoints.Clear();
                    OnDestinationReached?.Invoke();
                    return true;
                }
            }
            return false;
        }

        /// <summary>Returns true when there are remaining waypoints to follow.</summary>
        public bool HasPath() =>
            _waypoints != null && _waypoints.Count > 0 && _currentIndex < _waypoints.Count;

        /// <summary>Clears the current path without firing OnDestinationReached.</summary>
        public void ClearPath()
        {
            _waypoints.Clear();
            _currentIndex = 0;
        }

        private void OnDrawGizmos()
        {
            if (_waypoints == null || _waypoints.Count == 0) return;

            Gizmos.color = Color.cyan;
            for (int i = 0; i < _waypoints.Count; i++)
            {
                Gizmos.DrawSphere(_waypoints[i], 0.15f);
                if (i < _waypoints.Count - 1)
                    Gizmos.DrawLine(_waypoints[i], _waypoints[i + 1]);
            }

            // Highlight the current target
            if (HasPath())
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(_waypoints[_currentIndex], 0.25f);
            }
        }
    }
}
