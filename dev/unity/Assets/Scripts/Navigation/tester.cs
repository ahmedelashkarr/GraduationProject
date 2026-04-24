using System.Collections.Generic;
using Navigation;
using UnityEngine;

public class MockPathTest : MonoBehaviour
{
    public WaypointManager waypointManager;

    void Start()
    {
        waypointManager.SetPath(new List<Vector3>
        {
            new Vector3(0, 0, 2),
            new Vector3(2, 0, 4),
            new Vector3(4, 0, 4),
        });
    }
}
