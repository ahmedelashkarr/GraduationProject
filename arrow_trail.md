# Arrow Trail Navigation Visual

`dev/unity/Assets/Scripts/Navigation/ArrowTrailRenderer.cs` and
`ArrowTrailDebug.cs`, namespace `IndoorNav.Navigation`.

A floor-arrow trail like the indoor-AR wayfinding visuals seen in
airport / mall apps: many small arrows lying flat on the floor, evenly
spaced along the route, each pointing toward the next zone, fading by
distance to the camera and culled behind the user.

Coexists with `ARDirectionIndicator` (the single hovering arrow). Pick
one per scene — disable the other in the inspector.

---

## Files added

| File | Purpose |
|---|---|
| `ArrowTrailRenderer.cs` | Spawns and updates the trail. Subscribes to `NavigationController` lifecycle events; rebuilds on route start / advance, clears on destination. Per-frame distance fade and behind-camera culling. |
| `ArrowTrailDebug.cs` | Editor-time gizmo helper. Walks a fixed list of zone ids and draws a path line plus a sphere at every predicted spawn point — lets you preview the trail layout without entering Play mode. |

---

## How it spawns the trail

When `NavigationController.OnRouteStarted` or `OnZoneReached` fires:

1. `ClearArrows()` destroys any previously-spawned arrows.
2. `GetRemainingZones()` is called.
3. For each consecutive pair of zones, walk in `spacing`-meter steps
   along the horizontal direction.
4. At each step, lerp the floor Y from each zone's collider
   `bounds.min.y` and add `floorOffset` to avoid z-fighting.
5. Instantiate `arrowPrefab` rotated by
   `Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(arrowRotationOffset)`.
6. Cache the renderer + base color + color property id (URP `_BaseColor`
   or built-in `_Color`) so per-frame fading uses a
   `MaterialPropertyBlock` (the prefab's source material is never
   mutated).

`OnDestinationReached` calls `ClearArrows()` again. `OnEnable` rebuilds
the trail proactively if the controller is already navigating, so
toggling the component mid-route doesn't leave you with a stale view.

---

## Inspector references — `ArrowTrailRenderer`

| Field | Default | Required? | Notes |
|---|---|---|---|
| `Navigation Controller` | — | **Yes** | Drag the `NavigationController` GameObject. |
| `Arrow Prefab` | — | **Yes** | A small floor-arrow prefab. One Renderer with a transparent material. |
| `Arrow Parent` | — | No | Optional parent for instantiated arrows. Falls back to `transform`. Useful for keeping the hierarchy tidy. |
| `User Camera` | — | No | Camera transform used for fade and behind-user culling. Falls back to `Camera.main`. |
| `Spacing` | `0.5` m | — | Distance between adjacent arrows. |
| `Floor Offset` | `0.02` m | — | Lift above the floor to avoid z-fighting. Added to each zone's `bounds.min.y`. |
| `Arrow Rotation Offset` | `(90, 0, 0)` | — | Lay-flat correction for a +Z-forward arrow mesh. |
| `Fade By Distance` | `true` | — | Modulate alpha by camera distance via `MaterialPropertyBlock`. |
| `Fade Start Distance` | `2` m | — | Fully opaque inside this radius. |
| `Fade End Distance` | `15` m | — | Fully transparent at this range. |
| `Hide Arrows Behind User` | `true` | — | Disable renderers for arrows whose direction-from-camera dot-product with `cam.forward` is below the threshold. |
| `Behind User Dot Threshold` | `-0.2` | — | `0` = strict behind hemisphere; lower = more buffer behind; `-1` would never cull. |

---

## Inspector references — `ArrowTrailDebug` (optional)

| Field | Default | Notes |
|---|---|---|
| `Preview Zone Ids` | `[]` | Order matters; mimics a server `PathResponse`. |
| `Spacing` | `0.5` m | Match `ArrowTrailRenderer.spacing` for accurate preview. |
| `Floor Offset` | `0.02` m | Match the renderer. |
| `Marker Radius` | `0.05` m | Sphere radius drawn at each spawn point. |
| `Gizmo Color` | cyan-ish | Path line + markers. |
| `Only When Selected` | `true` | Draw gizmos only when the GameObject is selected; turn off to always show. |

The debug component scans the scene for `Zone` components directly via
`FindObjectsByType<Zone>` (no `ZoneRegistry` needed) so it works in edit
mode.

---

## Arrow prefab requirements

- One mesh + one `Renderer` (or a single child with a Renderer).
- Material with `_BaseColor` (URP/Lit — recommended) or `_Color` (built-in).
- **Surface Type = Transparent** in the material — otherwise the alpha
  fade is silently ignored.
- Pivot at the base / tail of the arrow shape so the tip points along
  the floor when laid flat. With `arrowRotationOffset = (90, 0, 0)` and
  a +Z-forward mesh, the tip ends up pointing forward.
- ~0.3 m long once flat is a good starting size. Larger arrows look
  chunky in narrow corridors.
- Emission helps visibility on dark floors.

---

## Scene-setup checklist

```
Scene
├── XR Origin (AR)                ← already in scene
│   └── Camera Offset
│       └── Main Camera           ← AR camera transform
├── ZoneRegistry                  ← already wired
├── NavigationController          ← already wired
├── ARDirectionIndicator          ← DISABLE in inspector (or remove)
├── ArrowTrailRenderer            ← NEW empty GO + ArrowTrailRenderer
│       Navigation Controller   = NavigationController GO
│       Arrow Prefab            = your floor-arrow prefab
│       Arrow Parent            = (leave empty, defaults to self)
│       User Camera             = Main Camera transform
│       Spacing                 = 0.5
│       Floor Offset            = 0.02
│       Arrow Rotation Offset   = (90, 0, 0)
│       Fade By Distance        = ✓
│       Fade Start Distance     = 2
│       Fade End Distance       = 15
│       Hide Arrows Behind User = ✓
│       Behind User Dot Threshold = -0.2
└── ArrowTrailDebug               ← OPTIONAL helper for layout
        Preview Zone Ids        = your test path ids
        Spacing                 = 0.5   (match renderer)
        Floor Offset            = 0.02  (match renderer)
        Marker Radius           = 0.05
        Only When Selected      = ✓
```

---

## Tuning knobs

| Knob | What it does | Symptoms when wrong |
|---|---|---|
| `Spacing` | Distance between arrows. | Too small → arrows blur into a continuous line; too large → "where's the next one?" gaps. Try `0.4` in tight corridors, `0.7` in open halls. |
| `Floor Offset` | Lift above floor. | Too small → arrows z-fight with the floor (flickering stripes); too large → arrows visibly hover. `0.01–0.05` m is the safe range. |
| `Arrow Rotation Offset` | Lay-flat correction. | Arrows stand up like pillars → leave at `(90, 0, 0)`. Arrows flat but pointing wrong → try `(90, 180, 0)` or rebuild the prefab with mesh forward = +Z. |
| `Fade Start / End Distance` | Curve of distance fade. | Arrows pop at a fixed range → widen the gap. Arrows look ghosted close-up → raise `Fade Start`. |
| `Hide Arrows Behind User` | Cull behind-camera. | Trail wraps back when you turn around → keep ✓. Arrows disappear when looking sideways → lower `Behind User Dot Threshold` (try `-0.5`). |
| `Behind User Dot Threshold` | Cull aggressiveness. | `0` = strict behind hemisphere; `-0.2` keeps a small buffer; `-1` never culls. |

---

## How fading works (tech detail)

Per spawned arrow, on first instantiation:

- Find the `Renderer` (`GetComponentInChildren`).
- Read whichever color property exists on the source material —
  `_BaseColor` (URP) takes priority, falling back to `_Color` (built-in).
- Cache the property id and the original color in an `ArrowEntry` struct.

Every frame:

- Compute the camera distance for the arrow.
- `Mathf.InverseLerp(fadeStart, fadeEnd, distance)` → fade `t` in [0,1].
- Lerp alpha from 1 (close) to 0 (far).
- Multiply into the cached base color.
- Apply via a single shared `MaterialPropertyBlock` — no per-instance
  material instantiation, so the prefab's asset is never modified and
  there's no extra draw-call overhead.

If neither `_BaseColor` nor `_Color` exists on the prefab's material,
the cached `colorPropId` is `-1` and the per-frame branch is skipped
silently. The arrow still renders, just without fading.

---

## Coexistence with `ARDirectionIndicator`

Both can technically run in the same scene — they don't share state — but
they look like two competing indicators if both are visible. Disable
`ARDirectionIndicator` in the inspector when using the trail. The single
hovering arrow remains useful as a fallback for long open spaces where
trail spacing makes the next direction unclear.

---

## Semantic deviation worth knowing

The original spec said "Set Y to `floorOffset`". The renderer instead
adds `floorOffset` on top of the lerped floor Y from each zone's collider
bounds:

```csharp
float floorY = Mathf.Lerp(zoneA.bounds.min.y, zoneB.bounds.min.y, t)
             + floorOffset;
```

This handles multi-floor buildings (each segment rides the actual floor
of its room) and arbitrary AR origins (the AR session's y=0 isn't
necessarily the building floor).

If you want literal `pos.y = floorOffset` semantics — only works if your
world floor is exactly y=0 — change `SpawnArrowsBetween` to use
`floorOffset` directly instead of the lerp.

---

## Suggested next features

- **Animated marching arrows** — translate each arrow's UV or position
  by `Time.time * speed` modulo `spacing` so the trail visually flows
  toward the destination.
- **Destination pulse** — a separate larger arrow + emission pulse
  spawned once at the final zone's center, subscribed to
  `OnRouteStarted` rather than rebuilding on every `OnZoneReached`.
- **Arrival ring** — a flat ring on the floor centered on the final
  zone, animated to scale or rotate; show it within 3 m of arrival.
- **Off-path warning** — when `CurrentZoneTracker.OnCurrentZoneChanged`
  fires for a zone not in `GetRemainingZones()`, recolor the trail red
  until the user crosses back onto a route zone (or the route is
  re-fetched).
- **Object pooling** — instead of `Destroy`/`Instantiate` on each
  rebuild, pool arrows. Useful only if rebuilds become frequent (e.g.
  re-localizing every 5 s).
- **Depth occlusion** — drive the arrow shader so arrows behind walls
  fade or grey out, using AR Foundation's depth or raycasts against the
  building mesh. Important once you ship to a phone — otherwise arrows
  visible through walls look like x-ray vision.
