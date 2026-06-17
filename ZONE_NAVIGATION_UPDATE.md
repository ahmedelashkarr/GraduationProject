# Zone-Based Navigation — Update Summary

## Overview
Added a zone-id-driven navigation stack under `dev/unity/Assets/Scripts/Navigation/`,
alongside the existing coordinate-waypoint pipeline. The new stack consumes the
server's `{ "path": ["F1_ROOM13", ...] }` contract, resolves each id to a
`Zone` MonoBehaviour placed in the scene, and advances the user through the route
using horizontal (XZ) distance checks.

Namespace: **`IndoorNav.Navigation`** — kept distinct from the existing
`Navigation` namespace so both stacks can compile and live side-by-side until one
is retired.

---

## New Files (`Assets/Scripts/Navigation/`)

### Zone.cs
Attach to every room, corridor, and link GameObject.
- `public string zoneId` — must match server ids exactly (case-sensitive).
- `public string displayName` — human-readable label for UI.
- `GetCenter()` — world-space center via collider bounds, falls back to `transform.position`.
- Optional `centerOverride` Transform for explicit pivots.
- `OnDrawGizmos()` draws a translucent cube + label in the Scene view.

### ZoneRegistry.cs
Scene singleton that indexes every `Zone` on `Awake`.
- `Dictionary<string, Zone>` lookup via `Get(id)` / `Contains(id)`.
- `Rebuild()` public method for rescans after runtime instantiation.
- Warns on duplicate ids and keeps the first one registered.
- `includeInactive` toggle for hidden zones.

### PathResponse.cs
`[Serializable]` class matching the server JSON:
```json
{ "path": ["F1_ROOM13", "F1_ROOM13_LINK_CORRIDOR1", "F1_HALL", "F1_ROOM11"] }
```
Parses with `JsonUtility.FromJson<PathResponse>(body)`.

### PathRequester.cs
Attach to a `Network` GameObject.
- `GET {serverUrl}?from=<id>&to=<id>` via `UnityWebRequest`.
- URL-encodes both ids; configurable timeout.
- Optional `acceptAnyCertificate` flag for ngrok / self-signed TLS.
- `OnPathFetched` and `OnRequestFailed` C# events for UI hooks.
- On success, calls `NavigationController.StartNavigation(response)`.

### NavigationController.cs
Attach to a `NavigationController` GameObject.
- `StartNavigation(PathResponse)` — resolves ids via the registry; unknown ids
  are skipped with a warning. Aborts if none resolve.
- Each frame: measures horizontal distance (XZ only — Y ignored for multi-floor)
  between `userCamera` and `GetCurrentZone().GetCenter()`; advances when within
  `reachThresholdMeters` (default 1.5 m).
- UnityEvents: `OnRouteStarted(Zone)`, `OnZoneReached(Zone)`, `OnDestinationReached()`.
- Public getters: `GetCurrentZone()`, `GetNextZone()`, `GetRemainingZones()`, `IsNavigating`.
- Optional `LineRenderer` automatically redrawn through the remaining zone centers.

### ARDirectionIndicator.cs
Attach to an `ARDirectionIndicator` GameObject.
- Floats the arrow prefab in front of the AR camera (`distanceFromCamera`, `heightOffset`).
- Smoothly slerps rotation toward the current (or next) zone's center — yaw only.
- `modelRotationOffsetEuler` lays vertical arrow meshes flat on the floor.
- Auto-hides on `OnDestinationReached` and when there is no active route.

---

## Inspector Wiring

| Component | Field | Assign |
|---|---|---|
| Zone (each room/corridor/link) | Zone Id | server id, e.g. `F1_ROOM13` |
| Zone | Display Name | UI label |
| Zone | Center Override | *(optional)* pivot transform |
| ZoneRegistry | Include Inactive | `true` to index disabled GameObjects |
| PathRequester | Server Url | `/route` endpoint |
| PathRequester | Navigation Controller | `NavigationController` GO |
| PathRequester | Accept Any Certificate | dev-only (ngrok) |
| NavigationController | Zone Registry | registry singleton *(optional)* |
| NavigationController | User Camera | AR camera transform |
| NavigationController | Reach Threshold Meters | default `1.5` |
| NavigationController | Path Line Renderer | *(optional)* route preview |
| ARDirectionIndicator | Navigation Controller | controller GO |
| ARDirectionIndicator | Arrow Prefab | arrow 3D model |
| ARDirectionIndicator | Model Rotation Offset Euler | `(90, 0, 0)` for flat-on-floor |

---

## Event Flow

```
UI button → PathRequester.RequestPath(fromId, toId)
    └─► GET {serverUrl}?from=&to=
            └─► PathResponse { path: [...] }
                    └─► NavigationController.StartNavigation(response)
                            ├─► resolves each id via ZoneRegistry.Get(id)
                            ├─► OnRouteStarted(firstZone) fires
                            └─► ARDirectionIndicator begins pointing at zone[0]
                                    └─► user walks → XZ distance ≤ threshold
                                            ├─► OnZoneReached(zone) fires per step
                                            └─► OnDestinationReached() on final zone
                                                    └─► arrow auto-hides
```

---

## Coexistence With the Existing Stack

The older coordinate-waypoint pipeline (namespace `Navigation`:
`PathReceiver`, `WaypointManager`, `Navigation.NavigationManager`,
`ArrowController`, `PathData`, plus the `MockPathTest` tester) was
**removed** once the zone-id stack replaced it. Only the original
root-level straight-line code (`ARArrowController` and the global
`NavigationManager`) still shares the project. Key notes:

- Wire only **one** stack into an AR scene at a time — two arrows would fight
  over the camera.
- `PathResponse` still exists in both the global namespace (inside legacy
  `Scripts/NavigationManager.cs`, with `List<Waypoint>`) and in
  `IndoorNav.Navigation` (with `List<string>`). Qualify the type explicitly if
  you import both.
- `NavigationController` (zone-id) and the root `NavigationManager` (legacy
  straight-line) are distinct classes. Pick one per scene.

---

## Server Contract Mismatch (action required)

The running Spring Boot `RouteController.java` currently returns
`{ "steps": [...], "total_distance": float }`, **not** the `{ "path": [...] }`
shape this client expects. Either:

1. Update the controller to emit `{ "path": ["F1_ROOM13", ...] }`, **or**
2. Extend `PathResponse` to parse `steps[].zone` out of the existing response.

No server change was made as part of this update.

---

## Quick Mock Test (no server needed)

```csharp
using System.Collections.Generic;
using IndoorNav.Navigation;
using UnityEngine;

public class MockZonePathTest : MonoBehaviour
{
    public NavigationController navigationController;

    void Start()
    {
        var mock = new PathResponse
        {
            path = new List<string>
            {
                "F1_ROOM13",
                "F1_ROOM13_LINK_CORRIDOR1",
                "F1_CORRIDOR1_lINK_HALL",
                "F1_HALL",
                "F1_ROOM11",
            }
        };
        navigationController.StartNavigation(mock);
    }
}
```

Drop onto any GameObject, assign `NavigationController`, place matching `Zone`
components in the scene, press Play.

---

## Follow-ups

- Floor transitions — detect `F<n>_` prefix changes and show a stairs/elevator
  card when crossing `_LINK_` zones.
- Animated path line — dashed scrolling shader on the `LineRenderer`.
- Off-path detection — fire an event + re-request when the user is far from
  every remaining zone.
- Re-localization — pipe `ScanSender`'s `/locate` response directly into
  `PathRequester.RequestPath` to rebuild the route after recentering.
- Editor tooling — inspector window listing all scene zones with
  missing/duplicate id flags.
