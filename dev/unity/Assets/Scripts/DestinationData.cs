// Assets/Scripts/DestinationData.cs
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Destination
{
    public string name;
    public string metaInfo;       // e.g. "15 meters · 30 sec"
    public Color iconBackground;
    public Sprite icon;
    public Vector3 worldPosition; // position in AR world space
}

[CreateAssetMenu(fileName = "Destinations", menuName = "IndoorNav/DestinationData")]
public class DestinationData : ScriptableObject
{
    public List<Destination> destinations = new List<Destination>()
    {
        new Destination { name = "Computer Lab",  metaInfo = "15 meters · 30 sec" },
        new Destination { name = "Room 305",      metaInfo = "40 meters · 1 min"  },
        new Destination { name = "Cafeteria",     metaInfo = "80 meters · 2 min"  },
        new Destination { name = "Exit",          metaInfo = "120 meters · 3 min" },
    };
}
