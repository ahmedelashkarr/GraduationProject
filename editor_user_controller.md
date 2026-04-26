# EditorUserController — Editor Walk-Through Helper

`dev/unity/Assets/Scripts/Navigation/EditorUserController.cs`,
namespace `IndoorNav.Navigation`.

A WASD + mouse-look first-person controller that drives the **XR Origin**
in the Unity Editor — same role AR Foundation plays on a real device, just
with keyboard and mouse instead of phone tracking. Companion to
`MockNavigationDriver` so you can walk the scene at a desk instead of
deploying to a phone every iteration.

Two modes:

- **Single-transform** (only `target` assigned) — yaw and pitch are both
  applied to the XR Origin. The whole rig tilts when you look up/down.
  Simple but unconventional.
- **Body-and-head split** (`target` + `cameraTransform` assigned) — yaw on
  the XR Origin, pitch on the camera locally. Body turns, head tilts.
  Canonical FPS feel, recommended.

---

## Why it exists

The zone-id navigation pipeline polls `userCamera.position` every frame to
decide when to advance through the route. On a real device, AR Foundation
drives that transform automatically. In the Editor it's just a regular
Camera with no AR tracking, so without an input driver it never moves and
the controller never advances past the first zone.

You now have two ways to move it:

| Tool | Use case |
|---|---|
| `MockNavigationDriver` (Teleport menu) | Snap-test: jump to each zone instantly, verify events fire. |
| `EditorUserController` (this file) | Realistic-test: walk continuously through corridors at human speed, watch the arrow rotate, watch the LineRenderer update, see how `reachThresholdMeters` actually feels. |

When you build to a real device, either remove the component or gate it so
it only runs in the Editor.

---

## Inspector fields

| Field | Default | Purpose |
|---|---|---|
| `Target` | — | Rig root that gets translated and yaw-rotated. **Assign the XR Origin (AR) transform.** |
| `Camera Transform` | — | *Optional.* AR Camera transform (child of XR Origin). When set, vertical look is applied here locally so the body yaws and the head pitches independently. Leave empty for single-transform mode. |
| `Move Speed` | `2.5` m/s | WASD walking speed (≈ casual walking pace). |
| `Look Sensitivity` | `0.15` | Mouse-look multiplier. |
| `Require Right Mouse To Look` | `true` | If on, view only rotates while holding right-click — so left-click on UI doesn't spin the camera. |
| `Lock Height` | `true` | Pins `target.y` to a fixed value so you stay at eye height instead of drifting up/down like a fly-cam. |
| `Fixed Height` | `1.6` m | Pinned Y when `Lock Height` is on. ≈ phone-at-eye-level. |

---

## Per-frame behavior

### Look (`HandleLook`)

1. If `Require Right Mouse To Look` is on, only run while RMB is held.
2. Read mouse delta (X/Y).
3. Add X to `_yaw`, subtract Y from `_pitch` (so up-mouse = look up).
4. Clamp pitch to ±89° to prevent flipping over.
5. Apply rotations:
   - **With `cameraTransform` assigned (recommended):**
     `target.rotation = Euler(0, yaw, 0)` and
     `cameraTransform.localRotation = Euler(pitch, 0, 0)`.
     Body yaws, head pitches.
   - **Without `cameraTransform`:** `target.rotation = Euler(pitch, yaw, 0)` —
     the entire XR Origin tilts.
   Roll stays zero in both modes.

### Move (`HandleMovement`)

1. Read WASD as a 2D axis (`x` = strafe, `y` = forward/back).
2. Take `target.forward` and `target.right`, **zero out their Y components**, then normalize. This way W moves you horizontally even when you're looking at the floor.
3. Multiply by `moveSpeed * Time.deltaTime` and add to `target.position`.
4. If `Lock Height` is on, snap Y back to `fixedHeight` after every move.

---

## Input abstraction (dual-system)

`#if ENABLE_INPUT_SYSTEM` blocks make this work with either input backend:

| Branch | Reads from |
|---|---|
| `ENABLE_INPUT_SYSTEM` defined | `Keyboard.current.wKey.isPressed`, `Mouse.current.delta`, `Mouse.current.rightButton.isPressed` |
| Legacy fallback | `Input.GetAxisRaw("Horizontal" / "Vertical")`, `Input.GetAxis("Mouse X" / "Mouse Y")`, `Input.GetMouseButton(1)` |

This project's `Packages/manifest.json` includes `com.unity.inputsystem
1.19.0`, so the new-system branch is what compiles for you.

---

## How to wire it up

On any GameObject in your test scene (often the same one that holds
`MockNavigationDriver`):

1. Add the `EditorUserController` component.
2. Drag the **`XR Origin (AR)`** transform into `Target`.
3. *Recommended:* drag the **AR `Main Camera`** transform (child of XR
   Origin under `Camera Offset`) into `Camera Transform` to get the
   body-yaws / head-pitches FPS feel.
4. Make sure `NavigationController.userCamera`, `ARDirectionIndicator.userCamera`,
   and `CurrentZoneTracker.userCamera` all point at the **AR Camera** — not
   the XR Origin — so they read the actual eye position regardless of
   whether you split rotations.
5. Press Play. Right-click + drag to look, WASD to walk.

The `ar camera screen.unity` scene already references this script, so it
may already be attached in your test scene.

---

## Stripping it from device builds

A few options, pick whichever you prefer:

- **Component-level guard** — at the top of `Start()`, add
  `if (!Application.isEditor) { Destroy(this); return; }`.
- **Conditional compilation** — wrap the whole class in
  `#if UNITY_EDITOR ... #endif`, then move the file to an `Editor/`
  folder. (Note: this also hides it from playmode builds, which is fine
  since AR Foundation drives the camera there.)
- **Manual** — disable the component in the device-build scene.

---

## Common gotchas

| Symptom | Likely cause |
|---|---|
| Camera doesn't move | `Target` not assigned, or another script (e.g. `ARSession`) is overwriting position each frame. Disable AR components in the test scene. |
| WASD moves you into the floor | `Lock Height` off and you're looking down. Turn `Lock Height` on. |
| Whole world tilts when you look up/down | Single-transform mode and the rig is rotating bodily. Assign `Camera Transform` to split the rotations. |
| Looking spins wildly | `Look Sensitivity` too high — try `0.1`. |
| Right-click does nothing | UI is eating the click. Move the cursor outside any UI panel. |
| Arrow / current-zone tracker never updates while you walk | They're polling a different transform than the one you're moving. They should read **the AR Camera** (which inherits movement from XR Origin); `EditorUserController` should drive the **XR Origin** root. |
