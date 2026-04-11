using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;

[System.Serializable]
public class Waypoint { public float x, z; }

[System.Serializable]
public class PathResponse { public List<Waypoint> path; }

public class NavigationManager : MonoBehaviour
{
    public string serverUrl = "http://your-server.com/api/path";
    public List<Vector3> worldWaypoints = new List<Vector3>();

    public IEnumerator FetchPath(string startId, string destId)
    {
        string url = $"{serverUrl}?start={startId}&dest={destId}";
        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var data = JsonUtility.FromJson<PathResponse>(req.downloadHandler.text);
            worldWaypoints.Clear();
            foreach (var wp in data.path)
                // y=0 puts arrows on the floor; raise to eye level later
                worldWaypoints.Add(new Vector3(wp.x, 0f, wp.z));
        }
    }
}