using System;
using System.Collections.Generic;

namespace IndoorNav.Navigation
{
    /// <summary>
    /// JSON shape returned by the server's route endpoint:
    /// <c>{ "path": ["F1_ROOM13", "F1_ROOM13_LINK_CORRIDOR1", ...] }</c>.
    /// Compatible with <see cref="UnityEngine.JsonUtility"/>.
    /// </summary>
    [Serializable]
    public class PathResponse
    {
        /// <summary>Ordered list of zone ids from start to destination.</summary>
        public List<string> path;
    }
}
