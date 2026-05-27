package com.arnav.service;

import org.springframework.stereotype.Service;

import java.util.Map;
import java.util.Optional;

/**
 * Fuses KNN zone prediction with trilateration (x, y) to produce a
 * single, best-effort location estimate.
 * Direct port of services/fusion_service.py.
 *
 * Strategy:
 *   1. Always run KNN and trilateration independently.
 *   2. If trilateration cannot see ≥3 APs → return KNN result only.
 *   3. If KNN confidence ≥ KNN_TRUST_THRESHOLD → trust the KNN zone,
 *      but enrich the response with the (x, y) from trilateration.
 *   4. Otherwise → let the nearest graph node to the (x, y) override
 *      the zone, and blend the confidence score.
 */
@Service
public class FusionService {

    /** If KNN is this confident, we anchor to its zone (mirrors Python constant). */
    private static final double KNN_TRUST_THRESHOLD = 0.65;

    private final KNNService            knn;
    private final TrilaterationService  trilat;
    private final GraphService          graph;

    public FusionService(KNNService knn,
                         TrilaterationService trilat,
                         GraphService graph) {
        this.knn    = knn;
        this.trilat = trilat;
        this.graph  = graph;
    }

    // ── public ───────────────────────────────────────────────────────────────

    /**
     * Produces a fused location estimate from a live WiFi scan.
     *
     * @param scan  map of BSSID → RSSI
     * @return      {@link LocationResult} with zone, confidence, optional (x,y) and method label
     */
    public LocationResult locate(Map<String, Integer> scan) {

        // 1. Run both estimators independently
        KNNService.PredictionResult knnResult = knn.predict(scan);
        String  zone    = knnResult.zone();
        double  knnConf = knnResult.confidence();

        Optional<TrilaterationService.Position> posOpt = trilat.estimatePosition(scan);

        // 2. Trilateration failed (fewer than 3 APs visible)
        if (posOpt.isEmpty()) {
            return new LocationResult(zone, knnConf, null, null, Method.KNN_ONLY);
        }

        TrilaterationService.Position pos = posOpt.get();

        // 3. High KNN confidence → trust zone, add xy for precision
        if (knnConf >= KNN_TRUST_THRESHOLD) {
            return new LocationResult(zone, knnConf, pos.x(), pos.y(), Method.FUSED);
        }

        // 4. Low KNN confidence → let trilateration override the zone
        String nearestZone  = xyToZone(pos.x(), pos.y());
        double blendedConf  = (knnConf + 0.5) / 2.0;   // partial trust (mirrors Python)
        return new LocationResult(nearestZone, blendedConf, pos.x(), pos.y(), Method.TRILAT_OVERRIDE);
    }

    // ── private ──────────────────────────────────────────────────────────────

    /**
     * Finds the graph node whose stored (x, y) coordinates are
     * closest (Euclidean) to the given position.
     */
    private String xyToZone(double x, double y) {
        String bestZone = null;
        double bestDist = Double.MAX_VALUE;

        for (Map.Entry<String, Map<String, Object>> entry : graph.getAllNodes().entrySet()) {
            Map<String, Object> node = entry.getValue();
            Object nx = node.get("x");
            Object ny = node.get("y");
            if (nx == null || ny == null) continue;

            double dx = toDouble(nx) - x;
            double dy = toDouble(ny) - y;
            double d  = Math.sqrt(dx * dx + dy * dy);

            if (d < bestDist) {
                bestDist = d;
                bestZone = entry.getKey();
            }
        }
        return bestZone;
    }

    private static double toDouble(Object v) {
        if (v instanceof Number n) return n.doubleValue();
        return 0.0;
    }

    // ── DTOs ─────────────────────────────────────────────────────────────────

    public enum Method {
        KNN_ONLY,
        FUSED,
        TRILAT_OVERRIDE;

        /** Snake-case label matching the Python strings. */
        @Override
        public String toString() {
            return name().toLowerCase();
        }
    }

    /**
     * Unified location result returned by {@link #locate}.
     *
     * @param zone        best-guess zone ID
     * @param confidence  score in [0, 1]
     * @param x           floor-plan X coordinate (null when trilateration unavailable)
     * @param y           floor-plan Y coordinate (null when trilateration unavailable)
     * @param method      which estimation strategy was used
     */
    public record LocationResult(
            String zone,
            double confidence,
            Double x,
            Double y,
            Method method
    ) {}
}
