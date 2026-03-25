# ARNavigation Server – Spring Boot

A Spring Boot 3 / Maven port of the original ARNavigation backend.

## Project Structure

```
arnavigation-server/
├── pom.xml
└── src/
    └── main/
        ├── java/com/arnav/
        │   ├── ArNavigationApplication.java      # Entry point
        │   ├── config/
        │   │   └── AppConfig.java                # @ConfigurationProperties
        │   ├── controller/
        │   │   ├── HomeController.java           # GET /  GET /health
        │   │   ├── LocateController.java         # POST /locate
        │   │   ├── RouteController.java          # GET /route
        │   │   ├── RoomsController.java          # GET /rooms
        │   └── service/
        │       ├── GraphService.java             # Loads building_graph.json
        │       ├── Pathfinder.java               # Dijkstra pathfinding
        │       ├── KNNService.java               # WiFi fingerprint KNN
        │       ├── ZoneStabilizer.java           # Sliding-window vote smoother
        │       ├── KalmanFilter.java             # 1-D Kalman smoother
        │       └── RoomsService.java             # Loads + filters rooms.json
        └── resources/
            └── application.properties
```

## Prerequisites

| Tool | Version |
|------|---------|
| Java | 17+     |
| Maven | 3.9+   |

## Data Files

Place your data files (same as the Flask version) under `data/` in the working directory:

```
data/
├── fingerprints.json
├── building_graph.json
└──rooms.json

```

Override paths via environment variables:

```
DATA_DIR=/custom/path
```

## Configuration

All settings map from `application.properties` and can be overridden with environment variables:

| Property | Env var | Default |
|----------|---------|---------|
| `server.port` | `FLASK_PORT` | `5000` |
| `app.x-app-secret` | `X_APP_SECRET` | `ar-nav-secret-2024` |
| `app.cors.origins` | `CORS_ORIGINS` | `*` |
| `app.knn.k` | `KNN_K` | `3` |
| `app.knn.rssi-missing` | `RSSI_MISSING` | `-100` |
| `app.zone.history-size` | `ZONE_HISTORY_SIZE` | `5` |
| `app.zone.confidence-threshold` | `ZONE_CONFIDENCE_THRESHOLD` | `0.6` |

## Build & Run

```bash
# Build fat JAR
mvn clean package -DskipTests

# Run
java -jar target/arnavigation-server-1.0.0.jar
```

Or using the Maven wrapper:

```bash
mvn spring-boot:run
```

## API Endpoints

All endpoints (except `GET /` and `GET /health`) require the header:

```
X-App-Secret: ar-nav-secret-2024
```

### Health
```
GET /health          → { "status": "ok" }
GET /               → { "service": "ARNavigation Server", "version": "1.0" }
```

### Locate
```
POST /locate
Content-Type: application/json
{ "scan": { "AA:BB:CC:11:22:33": -65, "AA:BB:CC:44:55:66": -72 } }

→ { "zone": "F1_LOBBY", "floor": 1, "confidence": 0.87 }
```

### Route
```
GET /route?from=F1_LOBBY&to=F2_ROOM201

→ {
    "steps": [
      { "zone": "F1_STAIRWELL", "direction": "Turn right", "distance": 12.5, "instruction": "..." },
      ...
    ],
    "total_distance": 42.0
  }
```

### Rooms
```
GET /rooms?q=lab&floor=2&type=classroom

→ { "rooms": [...], "count": 3 }
```


