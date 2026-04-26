# Unity Linking Guide — Ground Arrow

How to wire up the zone-id navigation stack so the AR arrow appears on the
floor and rotates toward the next zone. Refers to scripts in
`dev/unity/Assets/Scripts/Navigation/` (namespace `IndoorNav.Navigation`).

The arrow is owned by `ARDirectionIndicator.cs`. Each frame it places the
arrow at:

```
userCamera.position
    + userCamera.forward * distanceFromCamera
    + Vector3.up      * heightOffset
```

…then yaw-rotates it toward the current zone's center and applies an extra
rotation (`modelRotationOffsetEuler`) so a vertical mesh can lie flat. So
"appears on the ground" is the combination of three knobs — a height offset
that drops it from eye level to floor level, a rotation offset that tips the
mesh flat, and (a precondition) an active route so the script doesn't keep
itself hidden.

---

## Inspector references on `ARDirectionIndicator`

| Field | What to wire | Required? |
|---|---|---|
| `Navigation Controller` | Drag the `NavigationController` GameObject from the scene. | **Yes** — without it, the arrow has no target and stays hidden. |
| `User Camera` | Drag the AR Camera's `Transform` (the one under `XR Origin → Camera Offset → Main Camera`). | Recommended. Falls back to `Camera.main` if left empty, so it works as long as the AR camera is tagged MainCamera. |
| `Arrow Prefab` | Drag your arrow prefab (mesh + material). | **Yes** — if null, the script uses *this GameObject* as the arrow and disables its renderers when hidden. |

---

## Settings that specifically control "on the ground"

| Field | Default | What it does | Typical on-ground value |
|---|---|---|---|
| `Distance From Camera` | `1.5` m | How far ahead of the user the arrow floats. | `1.5–2.5`. Too close = clips through the user; too far = drifts off the visible floor. |
| `Height Offset` | `−0.25` m | Vertical offset relative to the camera position. | **`−1.4` to `−1.6`.** The AR camera tracks roughly eye height; you want the arrow at floor level. Tune by holding the phone how the user will hold it and adjusting until the arrow sits on the floor. |
| `Model Rotation Offset Euler` | `(0,0,0)` | Extra rotation applied after the look-rotation, so a vertical mesh can lie flat. | Depends on the prefab's modelled orientation. **Most common: `(90, 0, 0)`** — tips a +Y-up arrow forward so the tip points along the floor. If the arrow points into the floor or skyward, try `(-90, 0, 0)` or `(90, 180, 0)`. |
| `Rotation Speed` | `6` | Slerp speed when the target changes. Doesn't affect "on the ground" but affects how snappy turns feel. | `5–10`. |
| `Point At Next Zone` | `false` | If on, points one step ahead. Useful for previewing the next turn before you arrive at the current waypoint. | Leave off for typical "follow the arrow" UX. |

---

## Prerequisites elsewhere in the scene

The arrow only shows when `NavigationController.GetCurrentZone()` returns
non-null. So you need:

1. **`ZoneRegistry`** — on a scene-root GameObject. Auto-indexes every
   `Zone` on `Awake`.
2. **`Zone` components** — on every room / corridor / link GameObject, with
   `Zone Id` matching the server (or mock) ids exactly. A `Collider` on the
   GameObject lets `GetCenter()` use the bounds; otherwise it falls back to
   `transform.position`.
3. **`NavigationController`** — with its `User Camera` field assigned. Use
   the **same transform** you give to `ARDirectionIndicator`.
4. **An active route** — either via `PathRequester.RequestPath(from, to)`
   from your UI, or via `MockNavigationDriver` for offline testing.

Without an active route, the arrow auto-hides — that's not a bug, that's
`ARDirectionIndicator.Update()` calling `SetArrowActive(false)` when the
controller has no current zone.

---

## Arrow-prefab hints

The on-the-ground look depends a lot on how the prefab is modeled:

- **Pivot at the base of the arrow.** If the pivot is at the centroid, the
  arrow appears half-buried when laid flat. Move the mesh inside the prefab
  so its pivot is at the base/tail, or add a parent empty and offset the child.
- **Default orientation: +Z forward, +Y up.** That's Unity's standard.
  `modelRotationOffsetEuler = (90, 0, 0)` then tips it to lie on the XZ
  plane with the tip along +Z.
- **Material that reads on the floor.** Emission helps a lot in a dim AR
  scene; URP/Lit with Emission color cyan or yellow is the typical choice.
  Without emission the arrow gets lost when the floor is dark.
- **Scale.** A 1×1×1 cube-sized arrow is huge in AR. Aim for 0.3–0.5 m long
  once flat.

---

## Quick visual debugging

| Symptom | Likely cause |
|---|---|
| Arrow doesn't appear at all | No active route → start one with `MockNavigationDriver`, OR `Arrow Prefab` is null and this GameObject has no renderer to show. |
| Arrow floats at face height instead of the floor | `Height Offset` not negative enough — try `-1.5`. |
| Arrow stands vertically like a pillar | `Model Rotation Offset Euler` not set — try `(90, 0, 0)`. |
| Arrow points the wrong way along the floor | The mesh's "forward" axis isn't +Z. Try `(90, 180, 0)` or rotate the mesh inside the prefab. |
| Arrow clips into the user / camera | `Distance From Camera` too small — try `2`. |
| Arrow lags behind turns | `Rotation Speed` too low — try `8`. |

---

## Full scene checklist

```
Scene root
├── XR Origin (AR)              ← already in scene
│   └── Camera Offset
│       └── Main Camera         ← AR camera; assign its Transform to (a) and (b)
├── AR Session                  ← already in scene
├── ZoneRegistry                ← empty GO + ZoneRegistry component
├── NavigationController        ← empty GO + NavigationController
│       User Camera           = Main Camera transform (a)
│       Reach Threshold       = 1.5
│       Path Line Renderer    = (optional)
├── ARDirectionIndicator        ← empty GO + ARDirectionIndicator
│       Navigation Controller = NavigationController GO above
│       User Camera           = Main Camera transform (b)
│       Arrow Prefab          = your arrow prefab
│       Distance From Camera  = 1.5
│       Height Offset         = −1.5
│       Rotation Speed        = 6
│       Model Rotation Offset = (90, 0, 0)
├── PathRequester               ← empty GO (or child of NavigationController)
│       Server Url             = your /route endpoint
│       Navigation Controller  = NavigationController GO
└── Map root (your 3D building)
    ├── F1_ROOM13               ← Zone component, Zone Id = "F1_ROOM13"
    ├── F1_HALL                 ← Zone component, Zone Id = "F1_HALL"
    ├── F1_ROOM11               ← ...
    └── ...
```

For offline testing replace `PathRequester` with `MockNavigationDriver` —
same wiring, but it feeds a hardcoded `PathResponse` straight into the
controller.

---

## Tuning workflow

1. Place a test `Zone` 3 m in front of where you'll stand.
2. Add `MockNavigationDriver` with a one-element mock path containing that
   zone's id.
3. Press Play in the Editor with the AR Camera transform draggable in the
   Scene view (set the camera to a known position above y=1.5).
4. Adjust `Height Offset` until the arrow rests on the floor.
5. Adjust `Model Rotation Offset Euler` until the arrow lies flat with the
   tip pointing toward the test zone.
6. Adjust `Distance From Camera` until the arrow is visible without
   crowding the camera.

Once those three values look right in the Editor, the same values usually
transfer to a real device build with no further tuning.
