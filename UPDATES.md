# Project Updates

Running log of structural changes to the repo. Dates in UTC.

---

## 2026-04-26 — `EditorUserController` drives the XR Origin

### Modified

- **`Navigation/EditorUserController.cs`** — the `target` field is now
  intended to be the **XR Origin (AR)** transform. Movement and yaw apply to
  the XR Origin so its child Camera follows along, matching how AR
  Foundation drives the rig on a real device. New optional
  `cameraTransform` field decouples vertical look so the body yaws on the
  XR Origin while the camera pitches locally — canonical FPS feel. When
  `cameraTransform` is empty, both rotations stay on the XR Origin (the old
  behavior, but on the rig instead of the camera). Pitch initialisation
  now normalises Euler angles into [-89, 89] so a non-zero starting pitch
  doesn't snap to zero on the first mouse delta.

### Why

Driving only the camera transform worked in the Editor but didn't match
the real AR runtime — AR Foundation moves the camera within the XR
Origin's space, not in world space. With the Editor controller now driving
the rig root, navigation polling (`NavigationController`,
`ARDirectionIndicator`, `CurrentZoneTracker`) keeps reading the AR Camera
as it always would, and the test setup mirrors device behaviour.

---

## 2026-04-26 — Current-zone tracking

### Added

- **`Navigation/CurrentZoneTracker.cs`** — continuously detects which `Zone`
  contains the user (independent of the active route). Per-frame iterates
  the registry, runs `Bounds.Contains()`, fires `OnCurrentZoneChanged(Zone)`
  only when the result changes. Optional `TMP_Text uiLabel` for an on-screen
  readout. `ignoreYAxis` toggle for flat / floor-marker zones.

### Modified

- **`Navigation/MockNavigationDriver.cs`** — gained an optional
  `currentZoneTracker` reference; when assigned together with `logEvents`,
  the console also prints `[MockNavigationDriver] CurrentZone → F1_HALL` on
  every containment change. Hook/unhook were generalized to support the
  controller, the tracker, or both.

### Why

`NavigationController.GetCurrentZone()` returns the *next route target*,
not the zone the user is currently standing in. The new tracker fills that
semantic gap so test logs and UI can show "you are here" without having to
infer it from `OnZoneReached` events on a route.

---

## 2026-04-25 — Zone-id navigation replaces waypoint pipeline

### Added

Six new files under `dev/unity/Assets/Scripts/Navigation/`, namespace
`IndoorNav.Navigation`, implementing a zone-id-driven AR navigation stack:

| File | Purpose |
|---|---|
| `Zone.cs` | MonoBehaviour on every room/corridor/link GameObject; exposes `zoneId`, `displayName`, `GetCenter()` (collider bounds), editor gizmos. |
| `ZoneRegistry.cs` | Scene singleton; indexes every `Zone` on `Awake`, `Get(id)` lookup, duplicate warning. |
| `PathResponse.cs` | `[Serializable]` DTO matching `{ "path": ["F1_ROOM13", ...] }`. |
| `PathRequester.cs` | `GET {serverUrl}?from=&to=` via `UnityWebRequest`, URL-encodes ids, optional TLS bypass for ngrok, hands response to `NavigationController`. |
| `NavigationController.cs` | Drives the user through the route; XZ-only distance checks (default 1.5 m); fires `UnityEvent<Zone>` hooks (`OnRouteStarted`, `OnZoneReached`, `OnDestinationReached`); optional `LineRenderer` preview. |
| `ARDirectionIndicator.cs` | Floats an arrow prefab in front of the AR camera, slerp-rotates toward the current/next zone, auto-hides on arrival. |

### Removed

The older coordinate-waypoint stack (namespace `Navigation`) was deleted —
it's superseded by the zone-id stack and was unused in any scene or prefab:

- `Navigation/PathData.cs` (+ `.meta`)
- `Navigation/PathReceiver.cs` (+ `.meta`)
- `Navigation/WaypointManager.cs` (+ `.meta`)
- `Navigation/NavigationManager.cs` (+ `.meta`) — the in-namespace one only;
  the root-level global `Scripts/NavigationManager.cs` is untouched.
- `Navigation/ArrowController.cs` (+ `.meta`)
- `Navigation/tester.cs` (+ `.meta`) — the `MockPathTest` helper.

Deletions are **staged for commit**, not yet committed. `git restore`
recovers any single file before commit.

### Documentation

- `CLAUDE.md` — created at repo root; now documents two coexisting nav stacks
  (legacy straight-line at root + zone-id in `Navigation/`) instead of three.
- `ZONE_NAVIGATION_UPDATE.md` — design notes for the zone-id stack; updated
  to reflect the waypoint-stack removal.
- `NAVIGATION_SYSTEM_SUMMARY.md` — pre-existing, describes the waypoint
  pipeline; now historical. Consider deleting or marking obsolete.

### Inspector wiring (zone-id stack)

| GameObject | Component | Required references |
|---|---|---|
| Every room/corridor/link | `Zone` | `Zone Id`, `Display Name` |
| `ZoneRegistry` (scene root) | `ZoneRegistry` | `Include Inactive` |
| `NavigationController` | `NavigationController` | `User Camera`, optional `Path Line Renderer` |
| same GO or `Network` child | `PathRequester` | `Server Url`, `Navigation Controller` |
| `ARDirectionIndicator` | `ARDirectionIndicator` | `Navigation Controller`, `Arrow Prefab`, `User Camera` |

### Event flow

```
UI button → PathRequester.RequestPath(fromId, toId)
    └─► GET {serverUrl}?from=&to=
            └─► PathResponse { path: [...] }
                    └─► NavigationController.StartNavigation(response)
                            ├─► OnRouteStarted(firstZone)
                            └─► ARDirectionIndicator points at zone[0]
                                    └─► user walks → XZ distance ≤ threshold
                                            ├─► OnZoneReached(zone) per step
                                            └─► OnDestinationReached() on final zone
                                                    └─► arrow auto-hides
```

### Anomalies flagged

- **`NavigationController.cs` went missing on disk mid-session** (never
  git-tracked) and was recreated from session memory. Byte-for-byte identical
  to the original draft. Worth diffing against anything you had locally.
- **Uncommitted inspector-default tweaks on `ArrowController.cs` were
  dropped** with the file: `distanceFromUser = 4-5f` (= −1) and
  `heightOffset = −0.2f`. If the `−0.2f` was tuned intentionally, port it to
  `ARDirectionIndicator.heightOffset`.

### Known issues / follow-ups

- **Server contract mismatch.** The Spring Boot `RouteController.java`
  returns `{ steps:[{zone,...}], total_distance }`, but the new
  `IndoorNav.Navigation.PathResponse` expects `{ path:[zoneId, ...] }`. Pick
  one: change the server to emit `path`, or extend the DTO to parse
  `steps[].zone`.
- **`PathResponse` name clash.** A global `PathResponse` still lives inside
  `Scripts/NavigationManager.cs` (with `List<Waypoint>`). Namespace
  qualification keeps them separate, but don't import both into the same file
  without an alias.
- **`NAVIGATION_SYSTEM_SUMMARY.md` is now stale** — it documents only the
  removed waypoint stack. Delete or replace it when convenient.

### Suggested next steps

- Floor transitions — detect `F<n>_` prefix changes across consecutive zones
  and show a stairs/elevator prompt when crossing `_LINK_` zones.
- Animated path line — scrolling dashed shader on the `LineRenderer`, or
  glowing footsteps between zone centers.
- Off-path detection — fire an event + re-request the route when the user is
  far from every remaining zone.
- Re-localization — pipe `ScanSender`'s `/locate` response into
  `PathRequester.RequestPath(response.zone, destination.zoneId)` so
  "Recenter" refixes the start and re-routes.
- Scene tooling — an editor window that lists every scene `Zone` with
  missing-id / duplicate-id flags and a ping-to-select button.

---

## 2026-04-25 — `CLAUDE.md` initialized

First version created at repo root. Covers: component layout (Unity /
Spring Boot / Python fingerprinting), build commands, end-to-end request
flow, server service topology, data contracts between components, git
workflow, and project-specific gotchas.
