using System;
using System.Collections.Generic;
using UnityEngine;

namespace Navigation
{
    [Serializable]
    public class Waypoint
    {
        public float x;
        public float y;
        public float z;

        /// <summary>Converts this waypoint to a Unity Vector3.</summary>
        public Vector3 ToVector3() => new Vector3(x, y, z);
    }

    [Serializable]
    public class PathData
    {
        public List<Waypoint> waypoints;
    }
}
