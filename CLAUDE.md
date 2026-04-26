# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository layout

This is a graduation-project monorepo containing three loosely-coupled components that together form an indoor AR navigation system. Each component lives under `dev/` and has its own build lifecycle:

- `dev/unity/` — Unity 6 (6000.4.0f1) mobile client, C# + AR Foundation 6.x + URP. Android-first; iOS supported.
- `dev/server/v1/arnavigation-springboot/arnavigation/` — Spring Boot 3.2 / Java 17 backend (Maven).
- `dev/ahmed/v0WifiFingerPrinting/` — Python scripts used offline to collect WiFi scans (`collect_point.py`), build the fingerprint `radio_map.json` (`build_radio_map.py`), and sanity-check KNN locally (`test_knn.py`).

There is **no top-level build** — commands below run from the component's own directory.

## Common commands

### Spring Boot server (`dev/server/v1/arnavigation-springboot/arnavigation/`)
```bash
mvn spring-boot:run                  # dev run, port 5000 by default
mvn clean package -DskipTests        # build fat jar in target/
java -jar target/arnavigation-server-1.0.0.jar
```
Port is configurable via `SERVER_PORT`; data dir via `DATA_DIR` (defaults to `<cwd>/arnavigation/data`). Other env overrides: `KNN_K`, `RSSI_MISSING`, `ZONE_HISTORY_SIZE`, `ZONE_CONFIDENCE_THRESHOLD` (see `application.properties`). Server expects `fingerprints.json`, `building_graph.json`, `rooms.json`, `ap_map.json` in the data dir.

### Unity client (`dev/unity/`)
Open the folder in Unity Hub with editor `6000.4.0f1`. Build Android APKs via **File → Build Settings** (IL2CPP, ARM64, min API 26). Scenes must be indexed 0/1/2 in Build Settings — `AppManager.cs` hardcodes these indices (`SCENE_SPLASH=0`, `SCENE_MAIN=1`, `SCENE_AR=2`). Scene wiring details live in `dev/unity/SCENE_SETUP.md` and `SCENE_SETUP_DETAILED.md`.

### Python fingerprinting tools (`dev/ahmed/v0WifiFingerPrinting/`)
Linux-only (uses `nmcli`). `build_radio_map.py` turns `raw_scans.csv` into `radio_map.json`; `test_knn.py` does a live KNN against that map. No test suite.

## High-level architecture

### End-to-end request flow
```
Android WifiManager (WifiScanner.cs)
    → ScanSender.cs  ──POST /locate──▶  LocateController
                                            → FusionService.locate()
                                                 ├─ KNNService.predict()  (weighted KNN over fingerprints.json)
                                                 └─ TrilaterationService.estimatePosition()  (uses ap_map.json)
                                            ← { zone, confidence, method, (x,y) }
    → NavigationData.startPoint

UI selects destination → AppNavigation.OpenARCamera()
    ──GET /route?from=&to=──▶  RouteController → Pathfinder (Dijkstra over building_graph.json)
                               ← { steps[], total_distance }
    → SceneManager.LoadScene(SCENE_AR)
```

### Server (`com.arnav` package)
- **Controllers**: `HomeController` (`/`, `/health`), `LocateController` (`POST /locate`), `RouteController` (`GET /route`), `RoomsController` (`GET /rooms`), `FloorplanController`.
- **Services**:
  - `KNNService` — lazy-loads `fingerprints.json`; weighted-KNN with `1/(dist+ε)` voting.
  - `TrilaterationService` + `TrilaterationConfig` — log-distance path-loss model over `ap_map.json`; needs ≥3 visible APs.
  - `FusionService` — orchestrates the two estimators. If trilateration returns nothing, falls back to KNN-only. If KNN confidence ≥ `KNN_TRUST_THRESHOLD` (0.65), trusts KNN zone and adds (x,y) from trilateration. Otherwise lets the graph node nearest to (x,y) override the zone and blends confidence `(knnConf + 0.5)/2`. **The commented-out code in `LocateController.locate()` is the pre-fusion Kalman/ZoneStabilizer path — the active return is the fusion result.**
  - `ZoneStabilizer` / `KalmanFilter` — legacy smoothers, currently bypassed.
  - `Pathfinder` — Dijkstra with turn-bias; `GraphService` loads `building_graph.json`.
- **Config**: `AppConfig` binds `app.*` properties via `@ConfigurationProperties`. Data paths resolve from `app.data.dir`.
- `README.md` documents an `X-App-Secret` header; no filter enforces it yet — treat endpoints as unauthenticated.

### Unity client (`dev/unity/Assets/Scripts/`)
**Two navigation stacks coexist.** The older coordinate-waypoint pipeline (`namespace Navigation` — `PathReceiver`, `WaypointManager`, `ArrowController`, in-namespace `NavigationManager`) was **removed** in favor of the zone-id pipeline. The original single-destination straight-line code at the root remains but is orthogonal. Pick exactly one per scene. See `ZONE_NAVIGATION_UPDATE.md` for the active design.

- **Legacy straight-line (root `Scripts/`, global namespace)**: `ARArrowController.cs`, `ARArrowAnchor.cs`, `ARArrowMeshBuilder.cs`, `NavigationManager.cs`. Single-destination arrow that points straight at a fixed target.
- **Zone-id pipeline (`Scripts/Navigation/`, `namespace IndoorNav.Navigation`)**: server returns an ordered list of zone ids; the client resolves each to a `Zone` MonoBehaviour placed in the scene.
  - `Zone` on every room/corridor/link GameObject; id matches the server string, center comes from the collider bounds.
  - `ZoneRegistry` — scene singleton indexing every `Zone` by id on `Awake`; warns on duplicates.
  - `PathRequester` — `GET /route?from=&to=`, parses `{ "path": [id, ...] }` into `PathResponse`, hands it to `NavigationController`.
  - `NavigationController` — resolves ids via the registry (skips unknowns), advances through the route using XZ-only distance (default 1.5 m), fires `UnityEvent<Zone>` lifecycle hooks, optionally drives a `LineRenderer`.
  - `ARDirectionIndicator` — world-space arrow in front of the camera, slerp-rotates toward the current/next zone, auto-hides on arrival.
- **WiFi / backend glue (`Scripts/scripts/`)**: `WifiScanner` wraps Android `WifiManager` via `AndroidJavaClass` (returns test data in the editor); `ScanSender` InvokeRepeats every 3s posting to `/locate` and stores `data.zone` in `NavigationData.startPoint`; `BypassCertificate` is used to accept the ngrok self-signed cert; `RealRSSIManager` and `appNavigation.cs` drive the route fetch on "Start AR" and switch scenes.
- **Shared state**: the static `NavigationData` class holds `startPoint`, `destination`, `lastScan` across scenes. `AppManager` is the DontDestroyOnLoad scene-transition singleton and also carries the selected `Destination`.
- **DTO models** in `Scripts/models/` mirror server JSON (`LocationResponse`, `ScanRequest`, `RoomResponse`, `ZoneResponse`, …) for `JsonUtility.FromJson`.

### Data contract between components
- `fingerprints.json`: `[ { "zone": "F1_LOBBY", "signals": { "BSSID": rssi, ... } }, ... ]`
- `building_graph.json`: nodes carry `x`/`y`; Dijkstra edges weighted by distance.
- `ap_map.json`: BSSID → AP position + path-loss params (for trilateration).
- Zone IDs are conventionally `F<floor>_<NAME>` — floor number is parsed out of the prefix in the legacy locate path.

## Git workflow (from repo README)
- Per-member branches (`morgan`, `rashad`, `shama`, `ahmed`) — do not commit directly to `main`.
- All changes land on `main` via Pull Request review.
- New work unrelated to an existing personal branch should go on a new descriptively-named branch.

## Gotchas
- Two classes now share the "navigation manager" role: the global `NavigationManager` in root `Scripts/` and the zone-id `IndoorNav.Navigation.NavigationController`. Fully-qualify when both are referenced in the same file. Arrow duplication is similar (`ARArrowController` in root vs `IndoorNav.Navigation.ARDirectionIndicator`).
- **`PathResponse` name clash**: a global `PathResponse { List<Waypoint> path }` lives inside `Scripts/NavigationManager.cs` and `IndoorNav.Navigation.PathResponse { List<string> path }` lives in `Scripts/Navigation/PathResponse.cs`. Namespace qualification keeps them separate but silently importing both will bite you.
- **Server contract mismatch** for the zone-id stack: `RouteController.java` returns `{ steps:[{zone,...}], total_distance }`, but `IndoorNav.Navigation.PathResponse` expects `{ path:[zoneId, ...] }`. Either adapt the server or extend the DTO before the new stack will work end-to-end.
- `application.properties` hardcodes the default port to 5000, but the property name is `server.port` (Spring-native) — Flask-style env var names (`FLASK_PORT`) no longer apply.
- Mobile APKs (`Morgan.apk`, `rashad.apk`) are committed at the Unity folder root — not build artifacts you should regenerate casually.
- The Unity client currently hardcodes the server URL to an ngrok hostname in `ScanSender.cs` and `appNavigation.cs`; update both when the backend moves.
