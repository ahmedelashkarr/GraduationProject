# Navigation Arrow System — Update Summary

## Overview
Added a modular waypoint-based AR navigation system under `Assets/Scripts/Navigation/`.
This system replaces the single-destination arrow logic in the existing `ARArrowController.cs`
with a full path-following pipeline: server request → waypoint tracking → AR arrow display.

---

## New Files (`Assets/Scripts/Navigation/`)

### PathData.cs
Data classes for deserializing the server's path response.
- `Waypoint` — holds `x, y, z` floats and a `ToVector3()` helper
- `PathData` — wraps a `List<Waypoint>`

### PathReceiver.cs
Attach to **NetworkManager**.
- POSTs `{ start, destination }` JSON to the configured server URL
- Transforms each returned waypoint from map-local coordinates to world space using `mapSpace.TransformPoint()`
- Fires `static event OnPathReceived` with the world-space waypoint list

### WaypointManager.cs
Attach to **ArrowManager**.
- Subscribes to `PathReceiver.OnPathReceived` automatically
- Tracks the current waypoint index; `CheckWaypointReached()` advances it when the user is within `reachThreshold` (default 1 m)
- Fires `static event OnDestinationReached` when the last waypoint is cleared
- Draws cyan spheres + lines in the Scene view (gizmos) for debugging

### ArrowController.cs
Attach to **ArrowManager**.
- Instantiates a single arrow prefab on Start, hidden by default
- Each frame: positions the arrow in front of the AR camera, Slerp-rotates it toward the current waypoint (horizontal plane only), applies a subtle sin-based bobbing offset
- Detects when the user strays beyond `offPathThreshold` (default 3 m) and fires `static event OnOffPath` once per deviation
- Hides the arrow automatically when `OnDestinationReached` fires

### NavigationManager.cs
Attach to any persistent game manager object.
- `StartNavigation(Vector3 destination)` — reads the AR camera's current position and calls `PathReceiver.RequestPath()`
- `StopNavigation()` — calls `WaypointManager.ClearPath()` to cancel without triggering the arrived event
- Subscribes to `OnDestinationReached` for UI hook extension

---

## Inspector Wiring

| Component | Field | Assign |
|---|---|---|
| PathReceiver (NetworkManager) | Server Url | Route endpoint URL |
| PathReceiver (NetworkManager) | Map Space | `MapSpace` GameObject |
| ArrowController (ArrowManager) | Arrow Prefab | Arrow 3D model prefab |
| ArrowController (ArrowManager) | User Camera | `XR Origin > Camera` |
| ArrowController (ArrowManager) | Waypoint Manager | WaypointManager on same object |
| NavigationManager | Path Receiver | NetworkManager |
| NavigationManager | Waypoint Manager | ArrowManager |
| NavigationManager | User Camera | AR Camera (or leave blank for Camera.main) |

---

## Event Flow

```
UI calls NavigationManager.StartNavigation(destination)
    └─► PathReceiver.RequestPath(userPos, destination)
            └─► POST /route → server returns waypoints JSON
                    └─► OnPathReceived fires
                            └─► WaypointManager.SetPath()
                                    └─► ArrowController shows arrow, points to waypoint[0]
                                            └─► user walks → CheckWaypointReached advances index
                                                    └─► OnDestinationReached fires → arrow hides
```

---

## Quick Mock Test (no server needed)

```csharp
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
```

Attach to any GameObject, assign `WaypointManager`, and press Play.

---

## Namespace
All scripts use `namespace Navigation` — no conflicts with the existing global `NavigationManager` or `ARArrowController` classes.
